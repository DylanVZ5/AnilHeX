using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Linq;
using System.Collections.Generic;

public class MainForm : Form
{
    public static string GetAppRoot()
    {
        string[] possiblePaths = {
            Path.GetDirectoryName(Environment.ProcessPath), 
            Directory.GetCurrentDirectory(),                
            AppDomain.CurrentDomain.BaseDirectory,          
            Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\")) 
        };

        foreach (string path in possiblePaths)
        {
            if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(Path.Combine(path, "PBS")))
                return path;
        }
        return Path.GetDirectoryName(Environment.ProcessPath); 
    }

    public static readonly string AppRoot = GetAppRoot();

    public static readonly List<string[]> ParadoxGroups = new List<string[]> {
        new[] { "DONPHAN", "GREATTUSK", "IRONTREADS" },
        new[] { "VOLCARONA", "SLITHERWING", "IRONMOTH" },
        new[] { "JIGGLYPUFF", "SCREAMTAIL" },
        new[] { "AMOONGUSS", "BRUTEBONNET" },
        new[] { "MISDREAVUS", "FLUTTERMANE" },
        new[] { "MAGNETON", "SANDYSHOCKS" },
        new[] { "SALAMENCE", "ROARINGMOON" },
        new[] { "DELIBIRD", "IRONBUNDLE" },
        new[] { "HARIYAMA", "IRONHANDS" },
        new[] { "HYDREIGON", "IRONJUGULIS" },
        new[] { "TYRANITAR", "IRONTHORNS" },
        new[] { "GARDEVOIR", "GALLADE", "IRONVALIANT" },
        new[] { "SUICUNE", "WALKINGWAKE" },
        new[] { "VIRIZION", "IRONLEAVES" },
        new[] { "RAIKOU", "RAGINGBOLT" },
        new[] { "ENTEI", "GOUGINGFIRE" },
        new[] { "COBALION", "IRONCROWN" },
        new[] { "TERRAKION", "IRONBOULDER" },
        new[] { "CYCLIZAR", "KORAIDON", "MIRAIDON" }
    };

    private TabControl tabMain;
    private TabPage tabParty, tabPC, tabTrainer;
    private ListBox lstParty;
    private ContextMenuStrip partyMenu;
    private ComboBox cbBoxSelector;
    private Button btnPrevBox, btnNextBox, btnAddPokemon;
    private PictureBox[] pcSlots = new PictureBox[30];
    private ContextMenuStrip pcMenu;
    
    private TabControl tabBagPockets;
    private DataGridView[] dgvPockets = new DataGridView[9]; 
    private ComboBox cbBagItems;
    private NumericUpDown numBagQty;
    private Button btnBagAdd;
    private Button btnBagRemove; 
    private NumericUpDown numMoney; 

    private GroupBox grpEditor;
    private PictureBox picSprite;
    private CheckBox chkShiny;
    private CheckBox chkSuperShiny; 
    private Button btnInfo;
    private Label lblStatus;
    private Button btnLoad, btnSave;

    private TabControl tabEditor;
    private TextBox txtNickname;
    private NumericUpDown numLevel;
    private ComboBox cbNature, cbAbility, cbItem, cbGender;
    private Button btnDescAbility;
    private Label lblNatureInfo;

    private NumericUpDown[] numIVs = new NumericUpDown[6];
    private NumericUpDown[] numEVs = new NumericUpDown[6];
    private string[] statNames = { "PS", "Ataque", "Defensa", "At. Esp", "Def. Esp", "Velocidad" };

    private ComboBox[] cbMoves = new ComboBox[4];
    private Button[] btnDescMoves = new Button[4];
    private NumericUpDown[] numPPs = new NumericUpDown[4];
    private NumericUpDown[] numPPUps = new NumericUpDown[4];
    private Button btnMaxPP;

    private PBSReader pbs;
    private SaveDataModel currentSaveData;
    private string currentSavePath;
    
    private bool isEditingParty = true;
    private int currentBoxIndex = 0;
    private int currentSlotIndex = -1;
    private bool isUpdatingUI = false;

    private class DragData { public bool IsParty; public int BoxIndex; public int SlotIndex; }
    private DateTime lastBoxSwitch = DateTime.MinValue;
    private Point dragStartPos;
    private int dragSlotIndex = -1;
    private bool isDragging = false;

    private readonly string[] PocketNames = { 
        "", "Objetos", "Medicinas", "Poké Balls", "MTs y MOs", "Bayas", "Mega Piedras", "Batalla", "Clave" 
    };

    public MainForm()
    {
        this.Text = "AñilHeX - Editor Maestro";
        this.Size = new Size(820, 560);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

        InitializeUI();

        pbs = new PBSReader();
        string pbsPath = Path.Combine(AppRoot, "PBS");
        if (Directory.Exists(pbsPath)) pbs.LoadPBSDirectory(pbsPath);
    }

    private void InitializeUI()
    {
        btnLoad = new Button { Text = "Abrir Partida...", Location = new Point(12, 10), Size = new Size(120, 30) };
        btnLoad.Click += BtnLoad_Click;
        this.Controls.Add(btnLoad);

        btnSave = new Button { Text = "Guardar Cambios", Location = new Point(140, 10), Size = new Size(120, 30), Enabled = false, BackColor = Color.LightBlue };
        btnSave.Click += BtnSave_Click;
        this.Controls.Add(btnSave);

        tabMain = new TabControl { Location = new Point(12, 50), Size = new Size(380, 420) };
        
        tabParty = new TabPage("Equipo");
        lstParty = new ListBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 11F) };
        lstParty.SelectedIndexChanged += LstParty_SelectedIndexChanged;
        
        lstParty.MouseDown += (s, e) => {
            if (e.Button == MouseButtons.Right) {
                int idx = lstParty.IndexFromPoint(e.Location);
                if (idx != ListBox.NoMatches) lstParty.SelectedIndex = idx;
            }
        };

        partyMenu = new ContextMenuStrip();
        partyMenu.Items.Add("Enviar a la Caja del PC").Click += SendPartyToPC_Click;
        partyMenu.Items.Add("Cambiar Forma / Paradox").Click += ChangeForm_Click; 
        
        partyMenu.Opening += (s, e) => {
            if (lstParty.SelectedIndex < 0 || currentSaveData == null) { e.Cancel = true; return; }
            Pokemon p = currentSaveData.Party[lstParty.SelectedIndex];
            partyMenu.Items[1].Visible = HasFormsOrParadox(p.InternalSpecies);
        };
        
        lstParty.ContextMenuStrip = partyMenu;
        tabParty.Controls.Add(lstParty);
        
        tabPC = new TabPage("Cajas PC");
        
        btnPrevBox = new Button { Text = "<", Location = new Point(5, 10), Size = new Size(25, 25), AllowDrop = true };
        btnPrevBox.Click += (s, e) => { if (cbBoxSelector.SelectedIndex > 0) cbBoxSelector.SelectedIndex--; };
        btnPrevBox.DragOver += BtnBoxChange_DragOver;
        tabPC.Controls.Add(btnPrevBox);

        cbBoxSelector = new ComboBox { Location = new Point(35, 10), Size = new Size(215, 25), DropDownStyle = ComboBoxStyle.DropDownList };
        cbBoxSelector.SelectedIndexChanged += CbBoxSelector_SelectedIndexChanged;
        tabPC.Controls.Add(cbBoxSelector);

        btnNextBox = new Button { Text = ">", Location = new Point(255, 10), Size = new Size(25, 25), AllowDrop = true };
        btnNextBox.Click += (s, e) => { if (cbBoxSelector.SelectedIndex < cbBoxSelector.Items.Count - 1) cbBoxSelector.SelectedIndex++; };
        btnNextBox.DragOver += BtnBoxChange_DragOver;
        tabPC.Controls.Add(btnNextBox);

        btnAddPokemon = new Button { Text = "+ Añadir", Location = new Point(285, 9), Size = new Size(75, 27) };
        btnAddPokemon.Click += BtnAddPokemon_Click;
        tabPC.Controls.Add(btnAddPokemon);

        pcMenu = new ContextMenuStrip();
        pcMenu.Items.Add("Enviar al Equipo").Click += SendPCToParty_Click;
        pcMenu.Items.Add("Cambiar Forma / Paradox").Click += ChangeForm_Click; 
        pcMenu.Items.Add("Eliminar Pokémon").Click += DeletePokemon_Click;

        pcMenu.Opening += (s, e) => {
            var pb = (s as ContextMenuStrip).SourceControl as PictureBox;
            int slot = (int)pb.Tag;
            if (currentSaveData == null || currentSaveData.Boxes[currentBoxIndex].Slots[slot] == null) { e.Cancel = true; return; }
            Pokemon p = currentSaveData.Boxes[currentBoxIndex].Slots[slot];
            pcMenu.Items[1].Visible = HasFormsOrParadox(p.InternalSpecies);
        };

        for (int i = 0; i < 30; i++)
        {
            int col = i % 6, row = i / 6;
            pcSlots[i] = new PictureBox { Location = new Point(10 + (col * 58), 50 + (row * 58)), Size = new Size(54, 54), BorderStyle = BorderStyle.FixedSingle, SizeMode = PictureBoxSizeMode.CenterImage, Cursor = Cursors.Hand, Tag = i, AllowDrop = true, ContextMenuStrip = pcMenu };
            
            pcSlots[i].MouseDown += PcSlot_MouseDown;
            pcSlots[i].MouseMove += PcSlot_MouseMove;
            pcSlots[i].MouseUp += PcSlot_MouseUp;
            pcSlots[i].DragEnter += PcSlot_DragEnter;
            pcSlots[i].DragDrop += PcSlot_DragDrop;
            pcSlots[i].Click += PcSlot_ClickAction; 

            tabPC.Controls.Add(pcSlots[i]);
        }

        tabTrainer = new TabPage("Entrenador");
        
        tabBagPockets = new TabControl { Location = new Point(5, 5), Size = new Size(360, 315) };
        tabBagPockets.SelectedIndexChanged += (s, e) => UpdateBagItemDropdown();

        for (int pId = 1; pId <= 8; pId++)
        {
            TabPage pTab = new TabPage(PocketNames[pId]);
            // --- ARREGLO DE UX: BLOQUEAR REDIMENSIONAMIENTO DE TABLAS ---
            dgvPockets[pId] = new DataGridView { 
                Dock = DockStyle.Fill, AllowUserToAddRows = false, AllowUserToDeleteRows = true, 
                RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, 
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AllowUserToResizeRows = false,        // Bloquea encoger/agrandar filas
                AllowUserToResizeColumns = false      // Bloquea encoger/agrandar columnas
            };
            // -------------------------------------------------------------
            dgvPockets[pId].Columns.Add("InternalName", "ID");
            dgvPockets[pId].Columns["InternalName"].Visible = false;
            dgvPockets[pId].Columns.Add("Name", "Objeto");
            dgvPockets[pId].Columns["Name"].ReadOnly = true;
            dgvPockets[pId].Columns.Add("Quantity", "Cant.");
            dgvPockets[pId].Columns["Quantity"].Width = 60;

            dgvPockets[pId].RowsRemoved += (s, e) => UpdateBagItemDropdown();
            
            pTab.Controls.Add(dgvPockets[pId]);
            tabBagPockets.TabPages.Add(pTab);
        }
        tabTrainer.Controls.Add(tabBagPockets);

        cbBagItems = new ComboBox { Location = new Point(5, 325), Size = new Size(180, 25), AutoCompleteMode = AutoCompleteMode.SuggestAppend, AutoCompleteSource = AutoCompleteSource.ListItems };
        cbBagItems.TextUpdate += (s, e) => { if (cbBagItems.DroppedDown) cbBagItems.DroppedDown = false; };
        tabTrainer.Controls.Add(cbBagItems);

        numBagQty = new NumericUpDown { Location = new Point(190, 325), Size = new Size(65, 25), Minimum = 1, Maximum = 999, Value = 1 };
        tabTrainer.Controls.Add(numBagQty);

        btnBagAdd = new Button { Location = new Point(265, 324), Size = new Size(98, 27), Text = "Añadir Objeto", BackColor = Color.LightGreen };
        btnBagAdd.Click += BtnBagAdd_Click;
        tabTrainer.Controls.Add(btnBagAdd);

        Label lblMoney = new Label { Text = "Dinero:", Location = new Point(5, 360), Size = new Size(55, 20), Font = new Font(this.Font, FontStyle.Bold) };
        tabTrainer.Controls.Add(lblMoney);

        numMoney = new NumericUpDown { Location = new Point(65, 358), Size = new Size(100, 25), Minimum = 0, Maximum = 9999999, ThousandsSeparator = true };
        tabTrainer.Controls.Add(numMoney);

        btnBagRemove = new Button { Location = new Point(265, 357), Size = new Size(98, 27), Text = "Borrar Objeto", BackColor = Color.LightCoral };
        btnBagRemove.Click += BtnBagRemove_Click;
        tabTrainer.Controls.Add(btnBagRemove);

        tabMain.TabPages.Add(tabParty);
        tabMain.TabPages.Add(tabPC);
        tabMain.TabPages.Add(tabTrainer);
        this.Controls.Add(tabMain);

        grpEditor = new GroupBox { Text = "Editor de Pokémon", Location = new Point(410, 50), Size = new Size(380, 420), Enabled = false };
        picSprite = new PictureBox { Location = new Point(20, 20), Size = new Size(68, 56), SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle, Cursor = Cursors.Hand };
        picSprite.Click += (s, e) => { chkShiny.Checked = !chkShiny.Checked; };
        grpEditor.Controls.Add(picSprite);

        chkShiny = new CheckBox { Text = "⭐ Shiny", Location = new Point(15, 80), Size = new Size(70, 25), Appearance = Appearance.Button, TextAlign = ContentAlignment.MiddleCenter };
        chkShiny.CheckedChanged += (s, e) => { if(!isUpdatingUI) LoadSpriteOnly(); };
        grpEditor.Controls.Add(chkShiny);

        chkSuperShiny = new CheckBox { Text = "🌟 Radiante", Location = new Point(88, 80), Size = new Size(82, 25), Appearance = Appearance.Button, TextAlign = ContentAlignment.MiddleCenter };
        chkSuperShiny.CheckedChanged += (s, e) => { if(!isUpdatingUI) LoadSpriteOnly(); };
        grpEditor.Controls.Add(chkSuperShiny);

        btnInfo = new Button { Text = "Ver Info Extra", Location = new Point(175, 25), Size = new Size(185, 45), Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
        btnInfo.Click += BtnInfo_Click;
        grpEditor.Controls.Add(btnInfo);

        tabEditor = new TabControl { Location = new Point(10, 110), Size = new Size(360, 295) };
        
        TabPage pageGen = new TabPage("General");
        pageGen.Controls.Add(new Label { Text = "Mote:", Location = new Point(15, 20), Size = new Size(70, 20) });
        txtNickname = new TextBox { Location = new Point(90, 17), Size = new Size(130, 23) };
        pageGen.Controls.Add(txtNickname);

        pageGen.Controls.Add(new Label { Text = "Nivel:", Location = new Point(230, 20), Size = new Size(50, 20) });
        numLevel = new NumericUpDown { Location = new Point(280, 17), Size = new Size(50, 23), Minimum = 1, Maximum = 100 };
        pageGen.Controls.Add(numLevel);

        pageGen.Controls.Add(new Label { Text = "Objeto:", Location = new Point(15, 60), Size = new Size(70, 20) });
        cbItem = new ComboBox { Location = new Point(90, 57), Size = new Size(240, 23), AutoCompleteMode = AutoCompleteMode.SuggestAppend, AutoCompleteSource = AutoCompleteSource.ListItems };
        cbItem.TextUpdate += (s, e) => { if (cbItem.DroppedDown) cbItem.DroppedDown = false; };
        pageGen.Controls.Add(cbItem);

        pageGen.Controls.Add(new Label { Text = "Habilidad:", Location = new Point(15, 100), Size = new Size(70, 20) });
        
        // --- LIMPIEZA DE INTERFAZ: FUERA BOTÓN TODAS, NUEVO AUTO ▼ ---
        cbAbility = new ComboBox { Location = new Point(90, 97), Size = new Size(160, 23), AutoCompleteMode = AutoCompleteMode.SuggestAppend, AutoCompleteSource = AutoCompleteSource.ListItems };
        cbAbility.TextUpdate += (s, e) => { if (cbAbility.DroppedDown) cbAbility.DroppedDown = false; };
        pageGen.Controls.Add(cbAbility);
        
        btnDescAbility = new Button { Text = "?", Location = new Point(255, 96), Size = new Size(25, 25) };
        btnDescAbility.Click += (s, e) => ShowDescription(pbs.Abilities, pbs.AbilityDescriptions, cbAbility.Text, "Habilidad");
        pageGen.Controls.Add(btnDescAbility);

        Button btnAutoAbil = new Button { Text = "Auto ▼", Location = new Point(285, 96), Size = new Size(65, 25), BackColor = Color.LightYellow };
        ContextMenuStrip autoMenu = new ContextMenuStrip();
        autoMenu.Items.Add("Auto: Ranura 1 (Principal)").Click += (s,e) => { cbAbility.Text = "Auto: Ranura 1"; };
        autoMenu.Items.Add("Auto: Ranura 2 (Secundaria)").Click += (s,e) => { cbAbility.Text = "Auto: Ranura 2"; };
        autoMenu.Items.Add("Auto: Ranura 3 (Oculta)").Click += (s,e) => { cbAbility.Text = "Auto: Oculta"; };
        btnAutoAbil.Click += (s, e) => { autoMenu.Show(btnAutoAbil, new Point(0, btnAutoAbil.Height)); };
        pageGen.Controls.Add(btnAutoAbil);
        // --------------------------------------------------------------

        pageGen.Controls.Add(new Label { Text = "Naturaleza:", Location = new Point(15, 140), Size = new Size(70, 20) });
        cbNature = new ComboBox { Location = new Point(90, 137), Size = new Size(130, 23), DropDownStyle = ComboBoxStyle.DropDownList };
        cbNature.SelectedIndexChanged += (s, e) => {
            if (!isUpdatingUI) {
                string intNat = GetInternalId(pbs.Natures, cbNature.Text, "");
                lblNatureInfo.Text = GetNatureEffect(intNat);
            }
        };
        pageGen.Controls.Add(cbNature);

        lblNatureInfo = new Label { Location = new Point(225, 140), Size = new Size(125, 20), ForeColor = Color.DarkBlue, Font = new Font(this.Font, FontStyle.Italic) };
        pageGen.Controls.Add(lblNatureInfo);

        pageGen.Controls.Add(new Label { Text = "Sexo:", Location = new Point(15, 180), Size = new Size(70, 20) });
        cbGender = new ComboBox { Location = new Point(90, 177), Size = new Size(130, 23), DropDownStyle = ComboBoxStyle.DropDownList };
        cbGender.Items.AddRange(new string[] { "Macho ♂", "Hembra ♀", "Sin Género ⚲" });
        pageGen.Controls.Add(cbGender);

        Button btnMaxHappiness = new Button { Text = "Max Felicidad ♥", Location = new Point(230, 176), Size = new Size(110, 25), BackColor = Color.LightPink };
        btnMaxHappiness.Click += (s, e) => {
            Pokemon p = GetCurrentPokemon();
            if (p != null) { p.Happiness = 255; lblStatus.Text = $"¡Felicidad de {p.Nickname} al máximo (255)!"; lblStatus.ForeColor = Color.DeepPink; }
        };
        pageGen.Controls.Add(btnMaxHappiness);

        tabEditor.TabPages.Add(pageGen);

        TabPage pageStats = new TabPage("Stats");
        pageStats.Controls.Add(new Label { Text = "Stat", Location = new Point(20, 15), Size = new Size(60, 20), Font = new Font(this.Font, FontStyle.Bold) });
        pageStats.Controls.Add(new Label { Text = "IVs (0-31)", Location = new Point(120, 15), Size = new Size(80, 20), Font = new Font(this.Font, FontStyle.Bold) });
        pageStats.Controls.Add(new Label { Text = "EVs (0-252)", Location = new Point(220, 15), Size = new Size(80, 20), Font = new Font(this.Font, FontStyle.Bold) });
        for (int i = 0; i < 6; i++)
        {
            int yPos = 40 + (i * 35);
            pageStats.Controls.Add(new Label { Text = statNames[i], Location = new Point(20, yPos + 2), Size = new Size(80, 20) });
            numIVs[i] = new NumericUpDown { Location = new Point(120, yPos), Size = new Size(60, 23), Minimum = 0, Maximum = 31 };
            pageStats.Controls.Add(numIVs[i]);
            numEVs[i] = new NumericUpDown { Location = new Point(220, yPos), Size = new Size(60, 23), Minimum = 0, Maximum = 252 };
            pageStats.Controls.Add(numEVs[i]);
        }
        tabEditor.TabPages.Add(pageStats);

        TabPage pageMoves = new TabPage("Movimientos");
        pageMoves.Controls.Add(new Label { Text = "Ataque", Location = new Point(15, 10), Size = new Size(120, 20), Font = new Font(this.Font, FontStyle.Bold) });
        pageMoves.Controls.Add(new Label { Text = "PPs", Location = new Point(200, 10), Size = new Size(50, 20), Font = new Font(this.Font, FontStyle.Bold) });
        pageMoves.Controls.Add(new Label { Text = "+PP", Location = new Point(265, 10), Size = new Size(60, 20), Font = new Font(this.Font, FontStyle.Bold) });

        for (int i = 0; i < 4; i++)
        {
            int idx = i;
            int yPos = 35 + (i * 45);
            cbMoves[i] = new ComboBox { Location = new Point(15, yPos), Size = new Size(150, 23), AutoCompleteMode = AutoCompleteMode.SuggestAppend, AutoCompleteSource = AutoCompleteSource.ListItems };
            cbMoves[i].TextUpdate += (s, e) => { if (cbMoves[idx].DroppedDown) cbMoves[idx].DroppedDown = false; };
            cbMoves[i].SelectedIndexChanged += (s, e) => { if (!isUpdatingUI) UpdatePPLimits(idx); };
            pageMoves.Controls.Add(cbMoves[i]);
            
            btnDescMoves[i] = new Button { Text = "?", Location = new Point(168, yPos - 1), Size = new Size(25, 25) };
            btnDescMoves[i].Click += (s, e) => ShowDescription(pbs.Moves, pbs.MoveDescriptions, cbMoves[idx].Text, "Movimiento");
            pageMoves.Controls.Add(btnDescMoves[i]);

            numPPs[i] = new NumericUpDown { Location = new Point(200, yPos), Size = new Size(50, 23), Minimum = 0, Maximum = 99 };
            pageMoves.Controls.Add(numPPs[i]);

            numPPUps[i] = new NumericUpDown { Location = new Point(265, yPos), Size = new Size(45, 23), Minimum = 0, Maximum = 3 };
            numPPUps[i].ValueChanged += (s, e) => { if (!isUpdatingUI) UpdatePPLimits(idx); };
            pageMoves.Controls.Add(numPPUps[i]);
        }
        
        btnMaxPP = new Button { Text = "Max PPs Todos", Location = new Point(15, 225), Size = new Size(320, 30), BackColor = Color.LightGreen };
        btnMaxPP.Click += BtnMaxPP_Click;
        pageMoves.Controls.Add(btnMaxPP);

        tabEditor.TabPages.Add(pageMoves);
        grpEditor.Controls.Add(tabEditor);
        this.Controls.Add(grpEditor);

        lblStatus = new Label { Location = new Point(12, 485), Size = new Size(760, 20), Text = "Esperando...", ForeColor = Color.Gray };
        this.Controls.Add(lblStatus);
    }

    private void LogError(Exception ex, string context = "")
    {
        try
        {
            using (StreamWriter sw = new StreamWriter(Path.Combine(AppRoot, "errorlog.txt"), true))
                sw.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR: {context}\n{ex.ToString()}\n{new string('-', 50)}");
        } catch { }
    }

    private bool HasFormsOrParadox(string internalSpecies)
    {
        if (string.IsNullOrEmpty(internalSpecies)) return false;
        string spc = internalSpecies.ToUpper();
        
        if (ParadoxGroups.Any(g => g.Contains(spc))) return true;
        
        string basePath = Path.Combine(AppRoot, "Graphics", "Pokemon", "Icons");
        if (File.Exists(Path.Combine(basePath, $"{spc}_1.png")) || 
            File.Exists(Path.Combine(basePath, $"{spc}_1_s.png")) || 
            File.Exists(Path.Combine(basePath, $"{spc}_1s.png")))
        {
            return true;
        }

        return false;
    }

    private string GetNatureEffect(string internalNature)
    {
        switch (internalNature?.ToUpper())
        {
            case "LONELY": return "+Ataque, -Defensa";
            case "BRAVE": return "+Ataque, -Velocid.";
            case "ADAMANT": return "+Ataque, -At. Esp.";
            case "NAUGHTY": return "+Ataque, -Def. Esp.";
            case "BOLD": return "+Defensa, -Ataque";
            case "RELAXED": return "+Defensa, -Velocid.";
            case "IMPISH": return "+Defensa, -At. Esp.";
            case "LAX": return "+Defensa, -Def. Esp.";
            case "TIMID": return "+Velocid., -Ataque";
            case "HASTY": return "+Velocid., -Defensa";
            case "JOLLY": return "+Velocid., -At. Esp.";
            case "NAIVE": return "+Velocid., -Def. Esp.";
            case "MODEST": return "+At. Esp., -Ataque";
            case "MILD": return "+At. Esp., -Defensa";
            case "QUIET": return "+At. Esp., -Velocid.";
            case "RASH": return "+At. Esp., -Def. Esp.";
            case "CALM": return "+Def. Esp., -Ataque";
            case "GENTLE": return "+Def. Esp., -Defensa";
            case "SASSY": return "+Def. Esp., -Velocid.";
            case "CAREFUL": return "+Def. Esp., -At. Esp.";
            case "HARDY": case "DOCILE": case "SERIOUS": case "BASHFUL": case "QUIRKY": return "Neutra (Sin cambios)";
            default: return "";
        }
    }

    public static int CalculateExp(int level, string growthRate)
    {
        if (level <= 1) return 0;
        string gr = growthRate?.ToUpper() ?? "MEDIUMFAST";
        
        if (gr == "0") gr = "FAST";
        else if (gr == "1") gr = "MEDIUMFAST";
        else if (gr == "2") gr = "SLOW";
        else if (gr == "3" || gr == "PARABOLIC") gr = "MEDIUMSLOW";
        else if (gr == "4") gr = "ERRATIC";
        else if (gr == "5") gr = "FLUCTUATING";

        double L = level;
        double exp = 0;

        if (gr == "FAST") exp = 0.8 * Math.Pow(L, 3);
        else if (gr == "MEDIUMFAST" || gr == "MEDIUM") exp = Math.Pow(L, 3);
        else if (gr == "SLOW") exp = 1.25 * Math.Pow(L, 3);
        else if (gr == "MEDIUMSLOW") exp = 1.2 * Math.Pow(L, 3) - 15 * Math.Pow(L, 2) + 100 * L - 140;
        else if (gr == "ERRATIC") {
            if (L <= 50) exp = Math.Pow(L, 3) * (100 - L) / 50.0;
            else if (L <= 68) exp = Math.Pow(L, 3) * (150 - L) / 100.0;
            else if (L <= 98) exp = Math.Pow(L, 3) * Math.Floor((1911 - 10 * L) / 3.0) / 500.0;
            else exp = Math.Pow(L, 3) * (160 - L) / 100.0;
        }
        else if (gr == "FLUCTUATING") {
            if (L <= 15) exp = Math.Pow(L, 3) * (Math.Floor((L + 1) / 3.0) + 24) / 50.0;
            else if (L <= 36) exp = Math.Pow(L, 3) * (L + 14) / 50.0;
            else exp = Math.Pow(L, 3) * (Math.Floor(L / 2.0) + 32) / 50.0;
        }
        else exp = Math.Pow(L, 3); 

        return (int)Math.Max(0, exp);
    }

    private void SyncAllToRuby()
    {
        SaveWriter writer = new SaveWriter(currentSavePath, currentSaveData.RootData);
        for (int i = 0; i < currentSaveData.Party.Count; i++) writer.SyncPokemon(currentSaveData.Party[i], true, 0, i);
        foreach (var box in currentSaveData.Boxes)
            for (int i = 0; i < 30; i++)
                if (box.Slots[i] != null) writer.SyncPokemon(box.Slots[i], false, box.BoxIndex - 1, i);
    }

    private void ReloadFromMemory()
    {
        isUpdatingUI = true;
        
        SaveParser parser = new SaveParser(currentSavePath, pbs);
        currentSaveData = parser.ParseSave(currentSaveData.RootData); 

        lstParty.Items.Clear();
        foreach (var p in currentSaveData.Party) lstParty.Items.Add($"{p.Species} (Nv. {p.Level})");
        
        isUpdatingUI = false;
        RefreshPCGrid();
        LoadPokemonToEditor();
    }

    private void ChangeForm_Click(object sender, EventArgs e)
    {
        var item = sender as ToolStripItem;
        bool isParty = item.Owner == partyMenu;
        
        int slot = -1;
        if (isParty) slot = lstParty.SelectedIndex;
        else 
        {
            var pb = (item.Owner as ContextMenuStrip).SourceControl as PictureBox;
            slot = (int)pb.Tag;
        }

        if (slot < 0 || currentSaveData == null) return;
        Pokemon p = isParty ? currentSaveData.Party[slot] : currentSaveData.Boxes[currentBoxIndex].Slots[slot];
        if (p == null) return;

        using (ChangeFormDialog formDialog = new ChangeFormDialog(pbs, p))
        {
            if (formDialog.ShowDialog() == DialogResult.OK)
            {
                ApplyCurrentEdits();
                
                p.Form = formDialog.SelectedForm;
                if (!string.IsNullOrEmpty(formDialog.SelectedSpeciesInternal))
                {
                    p.InternalSpecies = formDialog.SelectedSpeciesInternal;
                    p.Species = pbs.GetName(pbs.Species, p.InternalSpecies, p.InternalSpecies);
                }

                p.InternalAbility = "AUTO_0"; 
                p.Ability = "Auto: Ranura 1"; 
                p.Nickname = p.Species; 

                SyncAllToRuby();
                ReloadFromMemory();
                
                lblStatus.Text = $"Forma de {p.Nickname} actualizada. La Habilidad será calculada por el juego.";
                lblStatus.ForeColor = Color.DarkViolet;
            }
        }
    }

    private void SendPartyToPC_Click(object sender, EventArgs e)
    {
        if (lstParty.SelectedIndex < 0 || currentSaveData == null) return;
        int partyIdx = lstParty.SelectedIndex;

        int targetBox = -1, targetSlot = -1;
        for (int b = 0; b < currentSaveData.Boxes.Count; b++)
        {
            for (int s = 0; s < 30; s++)
            {
                if (currentSaveData.Boxes[b].Slots[s] == null) { targetBox = b; targetSlot = s; break; }
            }
            if (targetBox != -1) break;
        }

        if (targetBox == -1) { MessageBox.Show("Todas las cajas del PC están llenas.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

        ApplyCurrentEdits();
        SyncAllToRuby();
        
        SaveWriter writer = new SaveWriter(currentSavePath, currentSaveData.RootData);
        writer.ClonePokemonInRuby(true, 0, partyIdx, false, targetBox, targetSlot);
        writer.DeletePokemonInRuby(true, 0, partyIdx);

        ReloadFromMemory();
        grpEditor.Enabled = false;
        lblStatus.Text = "Pokémon enviado a la caja con éxito.";
        lblStatus.ForeColor = Color.Green;
    }

    private void SendPCToParty_Click(object sender, EventArgs e)
    {
        var pb = ((sender as ToolStripItem).Owner as ContextMenuStrip).SourceControl as PictureBox;
        int slot = (int)pb.Tag;

        if (currentSaveData == null || currentSaveData.Boxes[currentBoxIndex].Slots[slot] == null) return;
        if (currentSaveData.Party.Count >= 6) { MessageBox.Show("El equipo ya está lleno (máximo 6 Pokémon).", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

        ApplyCurrentEdits();
        SyncAllToRuby();
        
        SaveWriter writer = new SaveWriter(currentSavePath, currentSaveData.RootData);
        writer.ClonePokemonInRuby(false, currentBoxIndex, slot, true, 0, currentSaveData.Party.Count);
        writer.DeletePokemonInRuby(false, currentBoxIndex, slot);

        ReloadFromMemory();
        grpEditor.Enabled = false;
        lblStatus.Text = "Pokémon enviado al equipo con éxito.";
        lblStatus.ForeColor = Color.Green;
    }

    private void UpdateBagItemDropdown()
    {
        if (!pbs.IsLoaded || tabBagPockets.SelectedIndex < 0) return;
        int currentPocket = tabBagPockets.SelectedIndex + 1; 
        
        if (dgvPockets[currentPocket] == null) return;
        
        string currentSelection = cbBagItems.Text;
        cbBagItems.Items.Clear();
        
        HashSet<string> existingItems = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (currentPocket == 4 || currentPocket == 8)
        {
            foreach (DataGridViewRow row in dgvPockets[currentPocket].Rows)
            {
                if (row.Cells[0].Value != null) existingItems.Add(row.Cells[0].Value.ToString());
            }
        }

        foreach (var kvp in pbs.Items)
        {
            int pkt = pbs.ItemPockets.ContainsKey(kvp.Key) ? pbs.ItemPockets[kvp.Key] : 1;
            if (pkt == currentPocket && !existingItems.Contains(kvp.Key))
            {
                cbBagItems.Items.Add(pbs.GetName(pbs.Items, kvp.Key, kvp.Value));
            }
        }
        if (cbBagItems.Items.Contains(currentSelection)) cbBagItems.SelectedItem = currentSelection;
        else if (cbBagItems.Items.Count > 0) cbBagItems.SelectedIndex = 0;
    }

    private void BtnBagAdd_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(cbBagItems.Text)) return;
        string intId = GetInternalId(pbs.Items, cbBagItems.Text, "");
        if (string.IsNullOrEmpty(intId)) return;
        
        int pocket = tabBagPockets.SelectedIndex + 1;
        int qtyToAdd = (int)numBagQty.Value;

        foreach (DataGridViewRow row in dgvPockets[pocket].Rows)
        {
            if (qtyToAdd <= 0) break; 
            if (row.Cells[0].Value != null && row.Cells[0].Value.ToString().Equals(intId, StringComparison.OrdinalIgnoreCase))
            {
                int currentQty = Convert.ToInt32(row.Cells[2].Value);
                if (currentQty < 999)
                {
                    int spaceAvailable = 999 - currentQty;
                    int amountToFill = Math.Min(qtyToAdd, spaceAvailable);
                    row.Cells[2].Value = currentQty + amountToFill;
                    qtyToAdd -= amountToFill; 
                }
            }
        }

        while (qtyToAdd > 0)
        {
            int amountForNewSlot = Math.Min(qtyToAdd, 999);
            dgvPockets[pocket].Rows.Add(intId, cbBagItems.Text, amountForNewSlot);
            qtyToAdd -= amountForNewSlot;
        }
        
        UpdateBagItemDropdown();
    }

    private void BtnBagRemove_Click(object sender, EventArgs e)
    {
        if (tabBagPockets.SelectedIndex < 0) return;
        int pocket = tabBagPockets.SelectedIndex + 1;
        
        if (dgvPockets[pocket].SelectedRows.Count > 0)
        {
            foreach (DataGridViewRow row in dgvPockets[pocket].SelectedRows)
            {
                if (!row.IsNewRow) dgvPockets[pocket].Rows.Remove(row);
            }
            UpdateBagItemDropdown();
        }
        else
        {
            MessageBox.Show("Por favor, selecciona primero una fila en la tabla de arriba para borrarla.", "Borrar Objeto", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void BtnAddPokemon_Click(object sender, EventArgs e)
    {
        if (currentSaveData == null) return;
        
        int targetSlot = -1;
        for (int i = 0; i < 30; i++) {
            if (currentSaveData.Boxes[currentBoxIndex].Slots[i] == null) {
                targetSlot = i;
                break;
            }
        }

        if (targetSlot == -1) {
            MessageBox.Show("La caja actual está llena.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using (AddPokemonForm form = new AddPokemonForm(pbs))
        {
            if (form.ShowDialog() == DialogResult.OK)
            {
                ApplyCurrentEdits(); 
                SyncAllToRuby();

                SaveWriter writer = new SaveWriter(currentSavePath, currentSaveData.RootData);
                string intSpc = form.SelectedSpeciesInternal;
                string gr = pbs.SpeciesGrowthRates.ContainsKey(intSpc) ? pbs.SpeciesGrowthRates[intSpc] : "MEDIUMFAST";
                int exactExp = CalculateExp(form.Level, gr);
                
                bool success = writer.AddPokemon(false, currentBoxIndex, intSpc, form.SelectedSpeciesName, form.Level, exactExp, "AUTO_0");
                
                if (success)
                {
                    ReloadFromMemory();
                    isEditingParty = false;
                    currentSlotIndex = targetSlot;
                    LoadPokemonToEditor();

                    lblStatus.Text = $"¡Pokémon añadido! Habilidad lista para el Modo Random.";
                    lblStatus.ForeColor = Color.Green;
                }
            }
        }
    }

    private void DeletePokemon_Click(object sender, EventArgs e)
    {
        var pb = ((sender as ToolStripItem).Owner as ContextMenuStrip).SourceControl as PictureBox;
        int slot = (int)pb.Tag;

        if (currentSaveData.Boxes[currentBoxIndex].Slots[slot] != null)
        {
            if (MessageBox.Show("¿Eliminar de forma permanente?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                ApplyCurrentEdits();
                SyncAllToRuby();

                SaveWriter writer = new SaveWriter(currentSavePath, currentSaveData.RootData);
                writer.DeletePokemonInRuby(false, currentBoxIndex, slot);
                
                ReloadFromMemory();
                grpEditor.Enabled = false;
                lblStatus.Text = "Pokémon eliminado.";
            }
        }
    }

    private void BtnBoxChange_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(typeof(DragData)) && (DateTime.Now - lastBoxSwitch).TotalMilliseconds > 700)
        {
            Button btn = sender as Button;
            if (btn == btnNextBox && cbBoxSelector.SelectedIndex < cbBoxSelector.Items.Count - 1) cbBoxSelector.SelectedIndex++;
            else if (btn == btnPrevBox && cbBoxSelector.SelectedIndex > 0) cbBoxSelector.SelectedIndex--;
            lastBoxSwitch = DateTime.Now;
        }
    }

    private void PcSlot_MouseDown(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            dragStartPos = e.Location;
            dragSlotIndex = (int)((PictureBox)sender).Tag;
            isDragging = false;
        }
    }

    private void PcSlot_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && dragSlotIndex != -1 && !isDragging)
        {
            if (Math.Abs(e.X - dragStartPos.X) > 4 || Math.Abs(e.Y - dragStartPos.Y) > 4)
            {
                if (currentSaveData != null && currentSaveData.Boxes[currentBoxIndex].Slots[dragSlotIndex] != null)
                {
                    isDragging = true;
                    int slot = dragSlotIndex;
                    dragSlotIndex = -1; 
                    ((PictureBox)sender).DoDragDrop(new DragData { IsParty = false, BoxIndex = currentBoxIndex, SlotIndex = slot }, DragDropEffects.Move);
                }
            }
        }
    }

    private void PcSlot_MouseUp(object sender, MouseEventArgs e) 
    { 
        if (!isDragging && dragSlotIndex != -1) PcSlot_ClickAction(sender, EventArgs.Empty);
        dragSlotIndex = -1; 
        isDragging = false;
    }

    private void PcSlot_ClickAction(object sender, EventArgs e)
    {
        if (isUpdatingUI) return;
        PictureBox clickedSlot = sender as PictureBox;
        if (clickedSlot == null) return;
        int slotIndex = (int)clickedSlot.Tag;
        
        if (currentSaveData.Boxes[currentBoxIndex].Slots[slotIndex] == null) { 
            ApplyCurrentEdits();
            grpEditor.Enabled = false; 
            currentSlotIndex = -1;
            return; 
        }
        
        ApplyCurrentEdits();
        isEditingParty = false;
        currentSlotIndex = slotIndex;
        LoadPokemonToEditor();
    }

    private void PcSlot_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(typeof(DragData))) e.Effect = DragDropEffects.Move;
    }

    private void PcSlot_DragDrop(object sender, DragEventArgs e)
    {
        DragData source = (DragData)e.Data.GetData(typeof(DragData));
        PictureBox pb = sender as PictureBox;
        int targetSlot = (int)pb.Tag;
        
        if (source.BoxIndex == currentBoxIndex && source.SlotIndex == targetSlot) return; 

        ApplyCurrentEdits();

        Pokemon p1 = currentSaveData.Boxes[source.BoxIndex].Slots[source.SlotIndex];
        Pokemon p2 = currentSaveData.Boxes[currentBoxIndex].Slots[targetSlot];
        currentSaveData.Boxes[source.BoxIndex].Slots[source.SlotIndex] = p2;
        currentSaveData.Boxes[currentBoxIndex].Slots[targetSlot] = p1;

        SaveWriter writer = new SaveWriter(currentSavePath, currentSaveData.RootData);
        writer.SwapPokemonInRuby(false, source.BoxIndex, source.SlotIndex, false, currentBoxIndex, targetSlot);

        if (!isEditingParty)
        {
            if (currentBoxIndex == source.BoxIndex && currentSlotIndex == source.SlotIndex) currentSlotIndex = targetSlot;
            else if (currentSlotIndex == targetSlot)
            {
                currentBoxIndex = source.BoxIndex;
                currentSlotIndex = source.SlotIndex;
            }
        }

        RefreshPCGrid();
        LoadPokemonToEditor();
    }

    private void PcSlot_Click(object sender, EventArgs e) { } 

    private void RefreshPCGrid()
    {
        if (currentSaveData == null || currentBoxIndex < 0 || currentBoxIndex >= currentSaveData.Boxes.Count) return;
        var box = currentSaveData.Boxes[currentBoxIndex];
        for (int i = 0; i < 30; i++)
        {
            Pokemon p = box.Slots[i];
            if (pcSlots[i].Image != null) pcSlots[i].Image.Dispose();
            pcSlots[i].Image = null;
            pcSlots[i].BackColor = (p != null) ? Color.LightCyan : Color.WhiteSmoke;
            if (p != null) pcSlots[i].Image = GetPokemonIcon(p.InternalSpecies, p.Form, p.IsShiny || p.IsSuperShiny);
        }
    }

    private void ShowDescription(Dictionary<string, string> nameDict, Dictionary<string, string> descDict, string localizedName, string title)
    {
        if (string.IsNullOrWhiteSpace(localizedName)) return;
        string internalId = GetInternalId(nameDict, localizedName, "");
        string desc = descDict.TryGetValue(internalId, out string d) ? d : "Sin descripción disponible.";
        MessageBox.Show(desc, $"{title}: {localizedName}", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void UpdatePPLimits(int moveIndex)
    {
        string moveName = cbMoves[moveIndex].Text;
        string internalId = GetInternalId(pbs.Moves, moveName, "");
        
        int basePP = 10; 
        if (pbs.MovePPs.TryGetValue(internalId, out int pp)) basePP = pp;
        
        int ppUps = (int)numPPUps[moveIndex].Value;
        int maxPP = basePP + (basePP * ppUps / 5); 
        
        numPPs[moveIndex].Maximum = Math.Max(1, maxPP);
        if (numPPs[moveIndex].Value > numPPs[moveIndex].Maximum) numPPs[moveIndex].Value = numPPs[moveIndex].Maximum;
    }

    private void BtnMaxPP_Click(object sender, EventArgs e)
    {
        for (int i = 0; i < 4; i++)
        {
            if (cbMoves[i].Enabled && !string.IsNullOrWhiteSpace(cbMoves[i].Text))
            {
                numPPUps[i].Value = 3;
                numPPs[i].Value = numPPs[i].Maximum;
            }
        }
    }

    private void ApplyCurrentEdits()
    {
        if (isUpdatingUI) return; 
        Pokemon p = GetCurrentPokemon();
        if (p == null) return;
        
        try
        {
            isUpdatingUI = true; 

            p.Nickname = txtNickname.Text;
            p.Level = (int)numLevel.Value;

            string gr = pbs.SpeciesGrowthRates.ContainsKey(p.InternalSpecies) ? pbs.SpeciesGrowthRates[p.InternalSpecies] : "MEDIUMFAST";
            p.Exp = CalculateExp(p.Level, gr); 

            p.IsShiny = chkShiny.Checked;
            p.IsSuperShiny = chkSuperShiny.Checked;
            p.Gender = cbGender.Text;
            
            p.HeldItem = cbItem.Text;
            p.InternalHeldItem = GetInternalId(pbs.Items, cbItem.Text, p.InternalHeldItem);
            if (string.IsNullOrWhiteSpace(p.InternalHeldItem)) p.InternalHeldItem = "Ninguno";
            
            p.Nature = cbNature.Text;
            p.InternalNature = GetInternalId(pbs.Natures, cbNature.Text, p.InternalNature);
            
            if (cbAbility.Text == "Auto: Ranura 1") {
                p.Ability = "Auto: Ranura 1"; p.InternalAbility = "AUTO_0";
            } else if (cbAbility.Text == "Auto: Ranura 2") {
                p.Ability = "Auto: Ranura 2"; p.InternalAbility = "AUTO_1";
            } else if (cbAbility.Text == "Auto: Oculta") {
                p.Ability = "Auto: Oculta"; p.InternalAbility = "AUTO_2";
            } else {
                p.Ability = cbAbility.Text;
                p.InternalAbility = GetInternalId(pbs.Abilities, cbAbility.Text, p.InternalAbility);
            }

            for (int i = 0; i < 6; i++) { p.IVs[i] = (int)numIVs[i].Value; p.EVs[i] = (int)numEVs[i].Value; }

            p.Moves.Clear();
            for (int i = 0; i < 4; i++) 
            {
                if (!string.IsNullOrWhiteSpace(cbMoves[i].Text)) 
                {
                    p.Moves.Add(new PokemonMove {
                        Name = cbMoves[i].Text,
                        InternalName = GetInternalId(pbs.Moves, cbMoves[i].Text, ""),
                        PP = (int)numPPs[i].Value,
                        PPUp = (int)numPPUps[i].Value
                    });
                }
            }
            
            if (isEditingParty && currentSlotIndex >= 0)
                lstParty.Items[currentSlotIndex] = $"{p.Species} (Nv. {p.Level})";
        }
        catch (Exception ex)
        {
            LogError(ex, "ApplyCurrentEdits");
        }
        finally
        {
            isUpdatingUI = false; 
        }
    }

    private void BtnLoad_Click(object sender, EventArgs e)
    {
        string appDataRoaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string[] possibleFolders = { Path.Combine(appDataRoaming, "Pokemon Anil"), Path.Combine(appDataRoaming, "Pokémon Añil"), Path.Combine(appDataRoaming, "PokemonAnil") };
        string initialDir = possibleFolders.FirstOrDefault(Directory.Exists) ?? appDataRoaming;

        using (OpenFileDialog ofd = new OpenFileDialog { Title = "Seleccionar Partida (.rxdata)", Filter = "Partidas (*.rxdata)|*.rxdata|Todos (*.*)|*.*", InitialDirectory = initialDir, RestoreDirectory = true })
        {
            if (ofd.ShowDialog() == DialogResult.OK) { currentSavePath = ofd.FileName; LoadSaveFile(); }
        }
    }

    private void LoadSaveFile()
    {
        try
        {
            SaveParser parser = new SaveParser(currentSavePath, pbs);
            currentSaveData = parser.ParseSave();

            isUpdatingUI = true;
            lstParty.Items.Clear();
            foreach (var p in currentSaveData.Party) lstParty.Items.Add($"{p.Species} (Nv. {p.Level})");

            cbBoxSelector.Items.Clear();
            foreach (var box in currentSaveData.Boxes) cbBoxSelector.Items.Add($"{box.Name} (Caja {box.BoxIndex})");

            foreach (var grid in dgvPockets) grid?.Rows.Clear();
            foreach (var item in currentSaveData.Bag)
            {
                int pId = item.Pocket >= 1 && item.Pocket <= 8 ? item.Pocket : 1;
                string realName = pbs != null ? pbs.GetName(pbs.Items, item.InternalName, item.Name) : item.Name;
                dgvPockets[pId]?.Rows.Add(item.InternalName, realName, item.Quantity);
            }

            SaveWriter tempWriter = new SaveWriter(currentSavePath, currentSaveData.RootData);
            numMoney.Value = Math.Min(numMoney.Maximum, Math.Max(0, tempWriter.GetMoney()));

            if (pbs.IsLoaded)
            {
                cbNature.Items.Clear(); cbNature.Items.AddRange(pbs.Natures.Values.Distinct().ToArray());
                
                // --- AHORA SE CARGAN TODAS LAS HABILIDADES POR DEFECTO ---
                cbAbility.Items.Clear();
                cbAbility.Items.AddRange(pbs.Abilities.Values.Distinct().ToArray());
                // -----------------------------------------------------------

                cbItem.Items.Clear(); 
                var allItemsList = pbs.Items.Select(x => pbs.GetName(pbs.Items, x.Key, x.Value)).Distinct().ToArray();
                cbItem.Items.AddRange(allItemsList);

                var allMoves = pbs.Moves.Values.Distinct().ToArray();
                foreach (var cb in cbMoves) { cb.Items.Clear(); cb.Items.AddRange(allMoves); }
            }

            isUpdatingUI = false; 
            
            if (cbBoxSelector.Items.Count > 0) cbBoxSelector.SelectedIndex = 0;
            RefreshPCGrid();

            tabBagPockets.SelectedIndex = 0;
            UpdateBagItemDropdown();

            btnSave.Enabled = true;
            lblStatus.Text = $"Partida cargada: {Path.GetFileName(currentSavePath)}";
            lblStatus.ForeColor = Color.Green;
        }
        catch (Exception ex)
        {
            lblStatus.Text = $"Error al cargar. Revisa errorlog.txt";
            lblStatus.ForeColor = Color.Red;
            btnSave.Enabled = false;
            LogError(ex, "LoadSaveFile");
        }
    }

    private void LstParty_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (isUpdatingUI || lstParty.SelectedIndex < 0) return;
        ApplyCurrentEdits();
        isEditingParty = true;
        currentSlotIndex = lstParty.SelectedIndex;
        LoadPokemonToEditor();
    }

    private void CbBoxSelector_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (isUpdatingUI || cbBoxSelector.SelectedIndex < 0 || currentSaveData == null) return;
        currentBoxIndex = cbBoxSelector.SelectedIndex;
        RefreshPCGrid();
    }

    private Pokemon GetCurrentPokemon()
    {
        if (currentSaveData == null || currentSlotIndex < 0) return null;
        if (isEditingParty && currentSlotIndex < currentSaveData.Party.Count) return currentSaveData.Party[currentSlotIndex];
        if (!isEditingParty && currentSlotIndex < 30) return currentSaveData.Boxes[currentBoxIndex].Slots[currentSlotIndex];
        return null;
    }

    private void LoadPokemonToEditor()
    {
        Pokemon p = GetCurrentPokemon();
        if (p == null) return;

        isUpdatingUI = true;

        txtNickname.Text = p.Nickname;
        numLevel.Value = Math.Min(100, Math.Max(1, p.Level));
        chkShiny.Checked = p.IsShiny;
        chkSuperShiny.Checked = p.IsSuperShiny;

        if (cbGender.Items.Contains(p.Gender)) cbGender.SelectedItem = p.Gender; else cbGender.SelectedIndex = 0;
        if (cbNature.Items.Contains(p.Nature)) cbNature.SelectedItem = p.Nature; else cbNature.Text = p.Nature;
        lblNatureInfo.Text = GetNatureEffect(p.InternalNature);
        
        string realItemName = pbs.GetName(pbs.Items, p.InternalHeldItem, p.HeldItem);
        if (cbItem.Items.Contains(realItemName)) cbItem.SelectedItem = realItemName; else cbItem.Text = realItemName;

        if (p.InternalAbility == "AUTO_0" || p.InternalAbility == "AUTO") {
            cbAbility.Text = "Auto: Ranura 1";
        } else if (p.InternalAbility == "AUTO_1") {
            cbAbility.Text = "Auto: Ranura 2";
        } else if (p.InternalAbility == "AUTO_2") {
            cbAbility.Text = "Auto: Oculta";
        } else {
            if (cbAbility.Items.Contains(p.Ability)) cbAbility.SelectedItem = p.Ability; 
            else cbAbility.Text = p.Ability;
        }
        
        for (int i = 0; i < 6; i++)
        {
            numIVs[i].Value = Math.Min(31, Math.Max(0, p.IVs[i]));
            numEVs[i].Value = Math.Min(252, Math.Max(0, p.EVs[i]));
        }

        for (int i = 0; i < 4; i++)
        {
            cbMoves[i].Enabled = numPPs[i].Enabled = numPPUps[i].Enabled = btnDescMoves[i].Enabled = true;

            if (i < p.Moves.Count)
            {
                if (cbMoves[i].Items.Contains(p.Moves[i].Name)) cbMoves[i].SelectedItem = p.Moves[i].Name; 
                else cbMoves[i].Text = p.Moves[i].Name;
                
                numPPUps[i].Value = p.Moves[i].PPUp;
                UpdatePPLimits(i);
                numPPs[i].Value = Math.Min(numPPs[i].Maximum, p.Moves[i].PP);
            }
            else
            {
                cbMoves[i].SelectedIndex = -1; cbMoves[i].Text = "";
                numPPs[i].Value = numPPUps[i].Value = 0;
            }
        }

        isUpdatingUI = false;
        LoadSpriteOnly();
        grpEditor.Enabled = true;
    }

    private void LoadSpriteOnly()
    {
        Pokemon p = GetCurrentPokemon();
        if (p == null) return;
        if (picSprite.Image != null) picSprite.Image.Dispose();
        picSprite.Image = GetPokemonIcon(p.InternalSpecies, p.Form, chkShiny.Checked || chkSuperShiny.Checked);
    }

    private Image GetPokemonIcon(string species, int form, bool isShiny)
    {
        string basePath = Path.Combine(AppRoot, "Graphics", "Pokemon", "Icons");
        string cleanSpecies = species?.Replace(" ", "")?.ToUpper() ?? ""; 
        
        string normalFile = Path.Combine(basePath, $"{cleanSpecies}.png");
        string normalFormFile = Path.Combine(basePath, $"{cleanSpecies}_{form}.png");
        string fileToLoad = File.Exists(normalFormFile) ? normalFormFile : (File.Exists(normalFile) ? normalFile : null);

        if (isShiny)
        {
            string shinyFile = Path.Combine(basePath, $"{cleanSpecies}s.png");
            string shinyFile2 = Path.Combine(basePath, $"{cleanSpecies}_s.png");
            string shinyFormFile = Path.Combine(basePath, $"{cleanSpecies}_{form}s.png");
            string shinyFormFile2 = Path.Combine(basePath, $"{cleanSpecies}_{form}_s.png");

            if (File.Exists(shinyFormFile)) fileToLoad = shinyFormFile;
            else if (File.Exists(shinyFormFile2)) fileToLoad = shinyFormFile2;
            else if (File.Exists(shinyFile)) fileToLoad = shinyFile;
            else if (File.Exists(shinyFile2)) fileToLoad = shinyFile2;
        }
        
        if (fileToLoad != null)
        {
            using (var fs = new FileStream(fileToLoad, FileMode.Open, FileAccess.Read))
            using (Image fullImg = Image.FromStream(fs))
            {
                int frameWidth = fullImg.Width / 2, frameHeight = fullImg.Height;
                Bitmap singleFrame = new Bitmap(frameWidth, frameHeight);
                using (Graphics g = Graphics.FromImage(singleFrame)) g.DrawImage(fullImg, new Rectangle(0, 0, frameWidth, frameHeight), new Rectangle(0, 0, frameWidth, frameHeight), GraphicsUnit.Pixel);
                return singleFrame;
            }
        }
        return null;
    }

    private string GetInternalId(Dictionary<string, string> dict, string localizedName, string originalInternal)
    {
        if (string.IsNullOrEmpty(localizedName)) return originalInternal;

        var pair = dict.FirstOrDefault(k => pbs.GetName(dict, k.Key, k.Value).Equals(localizedName, StringComparison.OrdinalIgnoreCase));
        if (pair.Key != null) return pair.Key;

        pair = dict.FirstOrDefault(k => k.Value.Equals(localizedName, StringComparison.OrdinalIgnoreCase));
        return pair.Key != null ? pair.Key : originalInternal;
    }

    private void BtnSave_Click(object sender, EventArgs e)
    {
        if (currentSaveData == null || string.IsNullOrEmpty(currentSavePath)) return;

        try
        {
            ApplyCurrentEdits(); 

            currentSaveData.Bag.Clear();
            for (int pId = 1; pId <= 8; pId++)
            {
                foreach (DataGridViewRow row in dgvPockets[pId].Rows)
                {
                    if (row.Cells[0].Value != null)
                    {
                        currentSaveData.Bag.Add(new ItemSlot {
                            InternalName = row.Cells[0].Value.ToString(),
                            Name = row.Cells[1].Value.ToString(),
                            Quantity = Convert.ToInt32(row.Cells[2].Value),
                            Pocket = pId
                        });
                    }
                }
            }

            SyncAllToRuby();
            
            SaveWriter writer = new SaveWriter(currentSavePath, currentSaveData.RootData);
            writer.SyncBag(currentSaveData.Bag);
            writer.SyncMoney((int)numMoney.Value);

            if (writer.Save())
            {
                lblStatus.Text = "¡Partida guardada correctamente! Se han inyectado todas las modificaciones.";
                lblStatus.ForeColor = Color.Blue;
            }
        }
        catch (Exception ex)
        {
            lblStatus.Text = $"Error crítico al guardar. Revisa errorlog.txt";
            lblStatus.ForeColor = Color.Red;
            LogError(ex, "BtnSave_Click");
        }
    }

    private void BtnInfo_Click(object sender, EventArgs e)
    {
        ApplyCurrentEdits();
        Pokemon p = GetCurrentPokemon();
        if (p == null) return;
        InfoForm infoWindow = new InfoForm(p, picSprite.Image);
        infoWindow.ShowDialog();
    }
}

public class AddPokemonForm : Form
{
    public string SelectedSpeciesInternal { get; private set; }
    public string SelectedSpeciesName { get; private set; }
    public int Level { get; private set; }

    public AddPokemonForm(PBSReader pbs)
    {
        this.Text = "Añadir Pokémon";
        this.Size = new Size(300, 200);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;

        Label lbl1 = new Label { Text = "Especie:", Location = new Point(20, 20), Size = new Size(60, 20) };
        this.Controls.Add(lbl1);

        ComboBox cbSpecies = new ComboBox { Location = new Point(90, 18), Size = new Size(170, 23), AutoCompleteMode = AutoCompleteMode.SuggestAppend, AutoCompleteSource = AutoCompleteSource.ListItems };
        cbSpecies.TextUpdate += (s, e) => { if (cbSpecies.DroppedDown) cbSpecies.DroppedDown = false; };
        cbSpecies.Items.AddRange(pbs.Species.Values.ToArray());
        if (cbSpecies.Items.Count > 0) cbSpecies.SelectedIndex = 0;
        this.Controls.Add(cbSpecies);

        Label lbl2 = new Label { Text = "Nivel:", Location = new Point(20, 60), Size = new Size(60, 20) };
        this.Controls.Add(lbl2);

        NumericUpDown numLvl = new NumericUpDown { Location = new Point(90, 58), Size = new Size(60, 23), Minimum = 1, Maximum = 100, Value = 50 };
        this.Controls.Add(numLvl);

        Button btnOk = new Button { Text = "Añadir", Location = new Point(90, 110), Size = new Size(80, 30), DialogResult = DialogResult.OK };
        btnOk.Click += (s, e) => {
            SelectedSpeciesName = cbSpecies.Text;
            SelectedSpeciesInternal = pbs.Species.FirstOrDefault(x => x.Value.Equals(SelectedSpeciesName, StringComparison.OrdinalIgnoreCase)).Key ?? SelectedSpeciesName.ToUpper();
            Level = (int)numLvl.Value;
        };
        this.Controls.Add(btnOk);
    }
}

public class InfoForm : Form
{
    public InfoForm(Pokemon p, Image sprite)
    {
        this.Text = $"Datos de {p.Nickname}";
        this.Size = new Size(380, 500);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.BackColor = Color.White;

        PictureBox pic = new PictureBox { Location = new Point(20, 20), Size = new Size(64, 64), Image = sprite, SizeMode = PictureBoxSizeMode.Zoom };
        this.Controls.Add(pic);

        Label lblName = new Label { Text = p.Nickname, Location = new Point(95, 25), Font = new Font("Segoe UI", 16, FontStyle.Bold), AutoSize = true };
        this.Controls.Add(lblName);
        
        Label lblLvl = new Label { Text = $"Nivel {p.Level}  •  {p.Gender}", Location = new Point(98, 55), Font = new Font("Segoe UI", 10), ForeColor = Color.DimGray, AutoSize = true };
        this.Controls.Add(lblLvl);

        RichTextBox rtb = new RichTextBox 
        { 
            Location = new Point(20, 100), Size = new Size(320, 340), ReadOnly = true, BackColor = Color.WhiteSmoke, BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 10)
        };

        rtb.Text = $" Especie:\t\t{p.Species} (ID: {p.PersonalID})\n" +
                   $" Forma Activa:\t{p.Form}\n" +
                   $" Habilidad:\t\t{p.Ability}\n" +
                   $" Naturaleza:\t{p.Nature}\n" +
                   $" Objeto Equip.:\t{p.HeldItem}\n" +
                   $" Pokéball:\t\t{p.PokeBall}\n" +
                   $" Felicidad:\t\t{p.Happiness} / 255\n" +
                   $" Experiencia:\t{p.Exp} EXP\n" +
                   $" Forma Shiny:\t{(p.IsShiny ? "Sí" : "No")}\n" +
                   $" Super Shiny:\t{(p.IsSuperShiny ? "Sí (Radiante)" : "No")}\n\n" +
                   $" --- Datos de Captura ---\n" +
                   $" Nivel Obtención:\tNv. {p.ObtainLevel}\n" +
                   $" Mapa Obtención:\t{p.ObtainMap}\n" +
                   (string.IsNullOrEmpty(p.ObtainText) ? "" : $" Nota:\t\t{p.ObtainText}");

        this.Controls.Add(rtb);
    }
}

public class ChangeFormDialog : Form
{
    public int SelectedForm { get; private set; }
    public string SelectedSpeciesInternal { get; private set; }

    private ComboBox cbOptions;

    private class FormOption
    {
        public string DisplayName { get; set; }
        public int FormIndex { get; set; }
        public string SpeciesInternal { get; set; }
        public override string ToString() => DisplayName; 
    }

    public ChangeFormDialog(PBSReader pbs, Pokemon p)
    {
        this.Text = $"Transformar a {p.Nickname}";
        this.Size = new Size(340, 200);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;

        Label lblInfo = new Label { 
            Text = "Selecciona la forma o variante a la que deseas convertir a este Pokémon:", 
            Location = new Point(20, 15), 
            Size = new Size(280, 40),
            Font = new Font("Segoe UI", 9F)
        };
        this.Controls.Add(lblInfo);

        cbOptions = new ComboBox { 
            Location = new Point(20, 60), 
            Size = new Size(285, 25), 
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 10F)
        };
        this.Controls.Add(cbOptions);

        string currentSpc = p.InternalSpecies?.ToUpper() ?? "";
        string[] currentGroup = null;

        foreach (var group in MainForm.ParadoxGroups) {
            if (group.Contains(currentSpc)) {
                currentGroup = group;
                break;
            }
        }

        if (currentGroup != null) 
        {
            foreach (string spc in currentGroup) 
            {
                string name = pbs.GetName(pbs.Species, spc, spc);
                cbOptions.Items.Add(new FormOption { 
                    DisplayName = name + (spc == currentSpc ? " (Actual)" : " (Variante/Paradoja)"), 
                    FormIndex = 0, 
                    SpeciesInternal = spc 
                });
            }
            cbOptions.SelectedIndex = Array.IndexOf(currentGroup, currentSpc);
        } 
        else 
        {
            cbOptions.Items.Add(new FormOption { 
                DisplayName = "Forma Base (Normal)", 
                FormIndex = 0, 
                SpeciesInternal = currentSpc 
            });

            string basePath = Path.Combine(MainForm.AppRoot, "Graphics", "Pokemon", "Icons");
            int maxFormsToScan = 30; 
            int selectedIdx = 0;

            for (int i = 1; i <= maxFormsToScan; i++)
            {
                if (File.Exists(Path.Combine(basePath, $"{currentSpc}_{i}.png")) || 
                    File.Exists(Path.Combine(basePath, $"{currentSpc}_{i}_s.png")) || 
                    File.Exists(Path.Combine(basePath, $"{currentSpc}_{i}s.png")))
                {
                    cbOptions.Items.Add(new FormOption { 
                        DisplayName = $"Forma Alternativa {i} (Regional / Mega)", 
                        FormIndex = i, 
                        SpeciesInternal = currentSpc 
                    });
                    
                    if (p.Form == i) selectedIdx = cbOptions.Items.Count - 1;
                }
            }
            cbOptions.SelectedIndex = selectedIdx;
        }

        Button btnOk = new Button { 
            Text = "Aplicar Transformación", 
            Location = new Point(20, 110), 
            Size = new Size(285, 35), 
            DialogResult = DialogResult.OK, 
            BackColor = Color.LightGreen,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        
        btnOk.Click += (s, e) => {
            var selected = cbOptions.SelectedItem as FormOption;
            if (selected != null) {
                SelectedForm = selected.FormIndex;
                SelectedSpeciesInternal = selected.SpeciesInternal;
            } else {
                SelectedForm = p.Form;
                SelectedSpeciesInternal = p.InternalSpecies;
            }
        };
        this.Controls.Add(btnOk);
    }
}