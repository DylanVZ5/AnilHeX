using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Threading;

public class MainForm : Form
{
    public static readonly string AppRoot = GetAppRoot();
    public static string GetAppRoot() { string[] paths = { Path.GetDirectoryName(Environment.ProcessPath), Directory.GetCurrentDirectory(), AppDomain.CurrentDomain.BaseDirectory, Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\")) }; foreach (string p in paths) { if (!string.IsNullOrWhiteSpace(p) && Directory.Exists(Path.Combine(p, "PBS"))) return p; } return Path.GetDirectoryName(Environment.ProcessPath); }

    // --- VARIABLES ---
    private TabControl tabMain, tabBagPockets, tabEditor;
    private TabPage tabParty, tabPC, tabBag;
    private PictureBox[] partySlots = new PictureBox[6], pcSlots = new PictureBox[30];
    private Label[] partyLabels = new Label[6];
    private ComboBox cbBoxSelector, cbBagItems, cbNature, cbAbility, cbItem, cbGender;
    private Button btnPrevBox, btnNextBox, btnAddPokemon, btnBagAdd, btnBagRemove, btnInfo, btnLoad, btnSave, btnDescAbility, btnChangeFormInEditor, btnMaxPP;
    private ContextMenuStrip pokeMenu;
    private DataGridView[] dgvPockets = new DataGridView[9]; 
    private NumericUpDown numBagQty, numMoney, numLevel, numSaveSlot;
    private Label lblSaveSlot;
    
    private GroupBox grpEditor;
    private PictureBox picSprite;
    private CheckBox chkShiny, chkSuperShiny; 
    private Label lblStatus;
    private TextBox txtNickname;
    private NumericUpDown[] numIVs = new NumericUpDown[6], numEVs = new NumericUpDown[6], numPPs = new NumericUpDown[4], numPPUps = new NumericUpDown[4];
    private Label[] lblStatNames = new Label[6]; 
    private string[] statNames = { "PS", "Ataque", "Defensa", "At. Esp", "Def. Esp", "Velocidad" };
    private ComboBox[] cbMoves = new ComboBox[4];
    private Button[] btnDescMoves = new Button[4];

    // --- LÓGICA ---
    private PBSReader pbs;
    private SaveDataModel currentSaveData;
    private string currentSavePath;
    private bool isEditingParty = true, isUpdatingUI = false, isDragging = false;
    private int currentBoxIndex = 0, currentSlotIndex = -1, dragSlotIndex = -1;
    private Point dragStartPos;
    private DateTime lastBoxSwitch = DateTime.MinValue;
    private bool isRandomizedSave = false; 
    private class DragData { public bool IsParty; public int BoxIndex; public int SlotIndex; }
    private readonly string[] PocketNames = { "", "Objetos", "Medicinas", "Poké Balls", "MTs y MOs", "Bayas", "Mega Piedras", "Batalla", "Clave" };
    // --- LÓGICA ---
    private List<string> currentBagDropdownItems = new List<string>(); // MEMORIA DEL BUSCADOR
    public MainForm()
    {
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

        this.Text = "AñilHeX - Editor Maestro"; this.Size = new Size(840, 600); this.FormBorderStyle = FormBorderStyle.FixedSingle; this.MaximizeBox = false; this.StartPosition = FormStartPosition.CenterScreen; this.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        InitializeUI();
        pbs = new PBSReader(); string pbsPath = Path.Combine(AppRoot, "PBS"); if (Directory.Exists(pbsPath)) pbs.LoadPBSDirectory(pbsPath);
    }

    private void InitializeUI()
    {
        btnLoad = new Button { Text = "Abrir Partida...", Location = new Point(12, 10), Size = new Size(120, 30) }; btnLoad.Click += BtnLoad_Click; this.Controls.Add(btnLoad);
        btnSave = new Button { Text = "Guardar Cambios", Location = new Point(140, 10), Size = new Size(120, 30), Enabled = false, BackColor = Color.LightBlue }; btnSave.Click += BtnSave_Click; this.Controls.Add(btnSave);

        lblSaveSlot = new Label { Text = "Nº Partida:", Location = new Point(275, 15), AutoSize = true, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = Color.DarkSlateGray };
        this.Controls.Add(lblSaveSlot);
        numSaveSlot = new NumericUpDown { Location = new Point(355, 13), Size = new Size(50, 25), Minimum = 1, Maximum = 999, Value = 1, Enabled = false, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
        this.Controls.Add(numSaveSlot);

        tabMain = new TabControl { Location = new Point(12, 50), Size = new Size(390, 490), AllowDrop = true };
        tabMain.SelectedIndexChanged += (s, e) => { if (tabMain.SelectedTab == tabBag) UpdateBagItemDropdown(); };
        tabMain.DragOver += (s, e) => { Point pt = tabMain.PointToClient(new Point(e.X, e.Y)); for (int i = 0; i < tabMain.TabPages.Count; i++) { if (tabMain.GetTabRect(i).Contains(pt)) { if (tabMain.SelectedIndex != i) tabMain.SelectedIndex = i; return; } } };
        
        tabParty = new TabPage("Equipo"); tabParty.AllowDrop = true;
        pokeMenu = new ContextMenuStrip(); pokeMenu.Items.Add("Mover al Equipo / PC").Click += MovePokemonQuick_Click; pokeMenu.Items.Add("Cambiar Forma / Paradox").Click += ChangeForm_Click; pokeMenu.Items.Add("Eliminar Pokémon").Click += DeletePokemon_Click;
        pokeMenu.Opening += (s, e) => { var pb = (s as ContextMenuStrip).SourceControl as PictureBox; if (pb == null || currentSaveData == null) { e.Cancel = true; return; } bool isParty = pb.Parent == tabParty; int slot = (int)pb.Tag; Pokemon p = isParty ? (slot < currentSaveData.Party.Count ? currentSaveData.Party[slot] : null) : currentSaveData.Boxes[currentBoxIndex].Slots[slot]; if (p == null) { e.Cancel = true; return; } pokeMenu.Items[0].Text = isParty ? "Enviar a la Caja del PC" : "Enviar al Equipo"; pokeMenu.Items[1].Visible = FormDatabase.HasFormsOrParadox(p.InternalSpecies, AppRoot); };

        for (int i = 0; i < 6; i++) {
            int col = i % 2, row = i / 2, px = 20 + (col * 180), py = 20 + (row * 100);
            partySlots[i] = new PictureBox { Location = new Point(px, py), Size = new Size(64, 64), BorderStyle = BorderStyle.FixedSingle, SizeMode = PictureBoxSizeMode.CenterImage, Cursor = Cursors.Hand, Tag = i, AllowDrop = true, ContextMenuStrip = pokeMenu, BackColor = Color.WhiteSmoke }; 
            partySlots[i].MouseDown += UniversalSlot_MouseDown; partySlots[i].MouseMove += UniversalSlot_MouseMove; partySlots[i].MouseUp += UniversalSlot_MouseUp; partySlots[i].DragEnter += UniversalSlot_DragEnter; partySlots[i].DragDrop += PartySlot_DragDrop; partySlots[i].Click += PartySlot_ClickAction; 
            partyLabels[i] = new Label { Location = new Point(px + 70, py + 15), Size = new Size(100, 40), TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Text = "Vacío", ForeColor = Color.Gray };
            tabParty.Controls.Add(partySlots[i]); tabParty.Controls.Add(partyLabels[i]);
        }
        tabMain.TabPages.Add(tabParty);
        
        tabPC = new TabPage("Cajas PC"); tabPC.AllowDrop = true;
        btnPrevBox = new Button { Text = "<", Location = new Point(5, 10), Size = new Size(25, 25), AllowDrop = true }; btnPrevBox.Click += (s, e) => { if (cbBoxSelector.SelectedIndex > 0) cbBoxSelector.SelectedIndex--; }; btnPrevBox.DragOver += BtnBoxChange_DragOver; tabPC.Controls.Add(btnPrevBox);
        cbBoxSelector = new ComboBox { Location = new Point(35, 10), Size = new Size(215, 25), DropDownStyle = ComboBoxStyle.DropDownList }; cbBoxSelector.SelectedIndexChanged += CbBoxSelector_SelectedIndexChanged; tabPC.Controls.Add(cbBoxSelector);
        btnNextBox = new Button { Text = ">", Location = new Point(255, 10), Size = new Size(25, 25), AllowDrop = true }; btnNextBox.Click += (s, e) => { if (cbBoxSelector.SelectedIndex < cbBoxSelector.Items.Count - 1) cbBoxSelector.SelectedIndex++; }; btnNextBox.DragOver += BtnBoxChange_DragOver; tabPC.Controls.Add(btnNextBox);
        btnAddPokemon = new Button { Text = "+ Añadir", Location = new Point(285, 9), Size = new Size(75, 27) }; btnAddPokemon.Click += BtnAddPokemon_Click; tabPC.Controls.Add(btnAddPokemon);
        for (int i = 0; i < 30; i++) { 
            int col = i % 6, row = i / 6; 
            pcSlots[i] = new PictureBox { Location = new Point(10 + (col * 58), 50 + (row * 58)), Size = new Size(54, 54), BorderStyle = BorderStyle.FixedSingle, SizeMode = PictureBoxSizeMode.CenterImage, Cursor = Cursors.Hand, Tag = i, AllowDrop = true, ContextMenuStrip = pokeMenu }; 
            pcSlots[i].MouseDown += UniversalSlot_MouseDown; pcSlots[i].MouseMove += UniversalSlot_MouseMove; pcSlots[i].MouseUp += UniversalSlot_MouseUp; pcSlots[i].DragEnter += UniversalSlot_DragEnter; pcSlots[i].DragDrop += PcSlot_DragDrop; pcSlots[i].Click += PcSlot_ClickAction; 
            tabPC.Controls.Add(pcSlots[i]); 
        }
        tabMain.TabPages.Add(tabPC);

        tabBag = new TabPage("Mochila"); tabBagPockets = new TabControl { Location = new Point(5, 5), Size = new Size(370, 290) }; tabBagPockets.SelectedIndexChanged += (s, e) => UpdateBagItemDropdown();
        for (int pId = 1; pId <= 8; pId++) { TabPage pTab = new TabPage(PocketNames[pId]); dgvPockets[pId] = new DataGridView { Dock = DockStyle.Fill, AllowUserToAddRows = false, AllowUserToDeleteRows = true, RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AllowUserToResizeRows = false, AllowUserToResizeColumns = false }; dgvPockets[pId].Columns.Add("InternalName", "ID"); dgvPockets[pId].Columns["InternalName"].Visible = false; dgvPockets[pId].Columns.Add("Name", "Objeto"); dgvPockets[pId].Columns["Name"].ReadOnly = true; dgvPockets[pId].Columns.Add("Quantity", "Cant."); dgvPockets[pId].Columns["Quantity"].Width = 60; if (pId == 4 || pId == 8) { dgvPockets[pId].Columns["Quantity"].ReadOnly = true; dgvPockets[pId].Columns["Quantity"].DefaultCellStyle.BackColor = Color.WhiteSmoke; } dgvPockets[pId].RowsRemoved += (s, e) => UpdateBagItemDropdown(); pTab.Controls.Add(dgvPockets[pId]); tabBagPockets.TabPages.Add(pTab); }
        tabBag.Controls.Add(tabBagPockets);

        // --- 1. DOBLE CLIC PARA CAMBIAR MTs ---
        dgvPockets[4].CellDoubleClick += (s, ev) => {
            if (ev.RowIndex >= 0) {
                string internalName = dgvPockets[4].Rows[ev.RowIndex].Cells[0].Value.ToString();
                string currentDisplayName = dgvPockets[4].Rows[ev.RowIndex].Cells[1].Value.ToString();
                ChangeTMMove(internalName, currentDisplayName, ev.RowIndex);
            }
        };

        // --- 2. CLIC DERECHO PARA CAMBIAR MTs ---
        ContextMenuStrip tmMenu = new ContextMenuStrip();
        tmMenu.Items.Add("✏️ Cambiar Ataque de esta MT (Inyectar)").Click += (s, ev) => {
            if (dgvPockets[4].SelectedRows.Count > 0) {
                int rowIndex = dgvPockets[4].SelectedRows[0].Index;
                string internalName = dgvPockets[4].Rows[rowIndex].Cells[0].Value.ToString();
                string currentDisplayName = dgvPockets[4].Rows[rowIndex].Cells[1].Value.ToString();
                ChangeTMMove(internalName, currentDisplayName, rowIndex);
            }
        };
        dgvPockets[4].ContextMenuStrip = tmMenu;
        // -------------------------------------
        // Apagamos el AutoComplete nativo y le asignamos nuestro buscador dinámico
        // CÓDIGO CORREGIDO PARA INITIALIZEUI
        cbBagItems = new ComboBox { 
            Location = new Point(5, 301), 
            Size = new Size(160, 25),
            AutoCompleteMode = AutoCompleteMode.None // ¡VITAL para evitar crasheos!
        }; 
        cbBagItems.TextUpdate += CbBagItems_TextUpdate; 
        tabBag.Controls.Add(cbBagItems);     
        numBagQty = new NumericUpDown { Location = new Point(170, 301), Size = new Size(55, 25), Minimum = 1, Maximum = 999, Value = 1 }; tabBag.Controls.Add(numBagQty);
        btnBagAdd = new Button { Location = new Point(230, 300), Size = new Size(60, 27), Text = "Añadir", BackColor = Color.LightGreen }; btnBagAdd.Click += BtnBagAdd_Click; tabBag.Controls.Add(btnBagAdd);
        Button btnBagMaxSelected = new Button { Location = new Point(295, 300), Size = new Size(75, 27), Text = "+ Max Sel.", BackColor = Color.LightGreen }; btnBagMaxSelected.Click += BtnBagMaxSelected_Click; tabBag.Controls.Add(btnBagMaxSelected);
        Button btnBagMaxCurrent = new Button { Location = new Point(5, 332), Size = new Size(160, 27), Text = "Max 999 a mis Objetos", BackColor = Color.LightSkyBlue }; btnBagMaxCurrent.Click += BtnBagMaxCurrent_Click; tabBag.Controls.Add(btnBagMaxCurrent);
        Button btnBagAddAll = new Button { Location = new Point(170, 332), Size = new Size(200, 27), Text = "Inyectar Todos los del Juego", BackColor = Color.Plum }; btnBagAddAll.Click += BtnBagAddAll_Click; tabBag.Controls.Add(btnBagAddAll);
        btnBagRemove = new Button { Location = new Point(5, 364), Size = new Size(365, 27), Text = "Borrar Objeto Seleccionado", BackColor = Color.LightCoral }; btnBagRemove.Click += BtnBagRemove_Click; tabBag.Controls.Add(btnBagRemove);
        
        Label lblMoney = new Label { Text = "Dinero en Billetera:", Location = new Point(5, 415), AutoSize = true, Font = new Font("Segoe UI", 10.5F, FontStyle.Bold), ForeColor = Color.DarkSlateGray };
        numMoney = new NumericUpDown { Location = new Point(155, 413), Size = new Size(140, 25), Minimum = 0, Maximum = 9999999999m, ThousandsSeparator = true, Font = new Font("Segoe UI", 10.5F, FontStyle.Bold) };
        tabBag.Controls.Add(lblMoney);
        tabBag.Controls.Add(numMoney);

        tabMain.TabPages.Add(tabBag);
        this.Controls.Add(tabMain);

        // --- EDITOR POKEMON ---
        grpEditor = new GroupBox { Text = "Editor de Pokémon", Location = new Point(415, 50), Size = new Size(395, 480), Enabled = false };
        picSprite = new PictureBox { Location = new Point(20, 20), Size = new Size(68, 56), SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle, Cursor = Cursors.Hand }; picSprite.Click += (s, e) => { chkShiny.Checked = !chkShiny.Checked; }; grpEditor.Controls.Add(picSprite);
        chkShiny = new CheckBox { Text = "⭐ Shiny", Location = new Point(15, 80), Size = new Size(70, 25), Appearance = Appearance.Button, TextAlign = ContentAlignment.MiddleCenter, Cursor = Cursors.Hand }; chkShiny.CheckedChanged += ChkShiny_CheckedChanged; grpEditor.Controls.Add(chkShiny);
        chkSuperShiny = new CheckBox { Text = "🌟 Radiante", Location = new Point(88, 80), Size = new Size(82, 25), Appearance = Appearance.Button, TextAlign = ContentAlignment.MiddleCenter, Cursor = Cursors.Hand }; chkSuperShiny.CheckedChanged += ChkSuperShiny_CheckedChanged; grpEditor.Controls.Add(chkSuperShiny);
        btnInfo = new Button { Text = "Ver Info Extra", Location = new Point(175, 25), Size = new Size(200, 45), Font = new Font("Segoe UI", 10F, FontStyle.Bold) }; btnInfo.Click += BtnInfo_Click; grpEditor.Controls.Add(btnInfo);

        tabEditor = new TabControl { Location = new Point(10, 110), Size = new Size(375, 355) };
        TabPage pageGen = new TabPage("General");
        pageGen.Controls.Add(new Label { Text = "Mote:", Location = new Point(15, 20), Size = new Size(70, 20) }); txtNickname = new TextBox { Location = new Point(90, 17), Size = new Size(130, 23) }; pageGen.Controls.Add(txtNickname);
        pageGen.Controls.Add(new Label { Text = "Nivel:", Location = new Point(230, 20), Size = new Size(50, 20) }); numLevel = new NumericUpDown { Location = new Point(280, 17), Size = new Size(50, 23), Minimum = 1, Maximum = 100 }; pageGen.Controls.Add(numLevel);
        pageGen.Controls.Add(new Label { Text = "Objeto:", Location = new Point(15, 60), Size = new Size(70, 20) }); cbItem = new ComboBox { Location = new Point(90, 57), Size = new Size(240, 23), AutoCompleteMode = AutoCompleteMode.SuggestAppend, AutoCompleteSource = AutoCompleteSource.ListItems }; cbItem.TextUpdate += (s, e) => { if (cbItem.DroppedDown) cbItem.DroppedDown = false; }; pageGen.Controls.Add(cbItem);
        
        // --- FILA DE HABILIDAD CON BOTÓN DESPLEGABLE ---
        Button btnAutoAbilities = new Button { Text = "Habilidades ▼", Location = new Point(5, 96), Size = new Size(82, 25), BackColor = Color.LightYellow, Cursor = Cursors.Hand };
        btnAutoAbilities.Click += BtnAutoAbilities_Click;
        pageGen.Controls.Add(btnAutoAbilities);
        
        cbAbility = new ComboBox { Location = new Point(90, 97), Size = new Size(130, 23), AutoCompleteMode = AutoCompleteMode.SuggestAppend, AutoCompleteSource = AutoCompleteSource.ListItems }; 
        cbAbility.TextUpdate += (s, e) => { if (cbAbility.DroppedDown) cbAbility.DroppedDown = false; }; 
        pageGen.Controls.Add(cbAbility);
        
        btnDescAbility = new Button { Text = "?", Location = new Point(223, 96), Size = new Size(23, 25) }; 
        btnDescAbility.Click += BtnDescAbility_Click; 
        pageGen.Controls.Add(btnDescAbility);

        pageGen.Controls.Add(new Label { Text = "Naturaleza:", Location = new Point(15, 140), Size = new Size(70, 20) }); cbNature = new ComboBox { Location = new Point(90, 137), Size = new Size(240, 23), DropDownStyle = ComboBoxStyle.DropDownList }; cbNature.SelectedIndexChanged += (s, e) => { if (!isUpdatingUI) { UpdateStatColorsByNature(); } }; pageGen.Controls.Add(cbNature);
        pageGen.Controls.Add(new Label { Text = "Sexo:", Location = new Point(15, 180), Size = new Size(70, 20) }); cbGender = new ComboBox { Location = new Point(90, 177), Size = new Size(110, 23), DropDownStyle = ComboBoxStyle.DropDownList }; cbGender.Items.AddRange(new string[] { "Macho ♂", "Hembra ♀", "Sin Género ⚲" }); pageGen.Controls.Add(cbGender);
        Button btnMaxHappiness = new Button { Text = "Max Felicidad ♥", Location = new Point(210, 176), Size = new Size(120, 25), BackColor = Color.LightPink }; btnMaxHappiness.Click += BtnMaxHappiness_Click; pageGen.Controls.Add(btnMaxHappiness);
        btnChangeFormInEditor = new Button { Text = "🌀 Cambiar Forma / Paradox", Location = new Point(15, 220), Size = new Size(335, 30), BackColor = Color.Lavender, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) }; btnChangeFormInEditor.Click += ChangeForm_Click; pageGen.Controls.Add(btnChangeFormInEditor);
        tabEditor.TabPages.Add(pageGen);

        TabPage pageStats = new TabPage("Stats");
        pageStats.Controls.Add(new Label { Text = "Stat", Location = new Point(20, 15), Size = new Size(60, 20), Font = new Font(this.Font, FontStyle.Bold) }); pageStats.Controls.Add(new Label { Text = "IVs (0-31)", Location = new Point(120, 15), Size = new Size(80, 20), Font = new Font(this.Font, FontStyle.Bold) }); pageStats.Controls.Add(new Label { Text = "EVs (0-252)", Location = new Point(220, 15), Size = new Size(80, 20), Font = new Font(this.Font, FontStyle.Bold) });
        for (int i = 0; i < 6; i++) { int yPos = 40 + (i * 35); lblStatNames[i] = new Label { Text = statNames[i], Location = new Point(20, yPos + 2), Size = new Size(80, 20), Font = new Font(this.Font, FontStyle.Bold) }; pageStats.Controls.Add(lblStatNames[i]); numIVs[i] = new NumericUpDown { Location = new Point(120, yPos), Size = new Size(60, 23), Minimum = 0, Maximum = 31 }; pageStats.Controls.Add(numIVs[i]); numEVs[i] = new NumericUpDown { Location = new Point(220, yPos), Size = new Size(60, 23), Minimum = 0, Maximum = 252 }; pageStats.Controls.Add(numEVs[i]); }
        tabEditor.TabPages.Add(pageStats);

        TabPage pageMoves = new TabPage("Movimientos");
        pageMoves.Controls.Add(new Label { Text = "Ataque", Location = new Point(15, 10), Size = new Size(120, 20), Font = new Font(this.Font, FontStyle.Bold) }); pageMoves.Controls.Add(new Label { Text = "PPs", Location = new Point(200, 10), Size = new Size(50, 20), Font = new Font(this.Font, FontStyle.Bold) }); pageMoves.Controls.Add(new Label { Text = "+PP", Location = new Point(265, 10), Size = new Size(60, 20), Font = new Font(this.Font, FontStyle.Bold) });
        for (int i = 0; i < 4; i++) { int idx = i; int yPos = 35 + (i * 45); cbMoves[i] = new ComboBox { Location = new Point(15, yPos), Size = new Size(150, 23), AutoCompleteMode = AutoCompleteMode.SuggestAppend, AutoCompleteSource = AutoCompleteSource.ListItems }; cbMoves[i].TextUpdate += (s, e) => { if (cbMoves[idx].DroppedDown) cbMoves[idx].DroppedDown = false; }; cbMoves[i].SelectedIndexChanged += (s, e) => { if (!isUpdatingUI) UpdatePPLimits(idx); }; pageMoves.Controls.Add(cbMoves[i]); btnDescMoves[i] = new Button { Text = "?", Location = new Point(168, yPos - 1), Size = new Size(25, 25) }; btnDescMoves[i].Click += (s, e) => BtnDescMoves_Click(idx); pageMoves.Controls.Add(btnDescMoves[i]); numPPs[i] = new NumericUpDown { Location = new Point(200, yPos), Size = new Size(50, 23), Minimum = 0, Maximum = 99 }; pageMoves.Controls.Add(numPPs[i]); numPPUps[i] = new NumericUpDown { Location = new Point(265, yPos), Size = new Size(45, 23), Minimum = 0, Maximum = 3 }; numPPUps[i].ValueChanged += (s, e) => { if (!isUpdatingUI) UpdatePPLimits(idx); }; pageMoves.Controls.Add(numPPUps[i]); }
        btnMaxPP = new Button { Text = "Max PPs Todos", Location = new Point(15, 225), Size = new Size(335, 30), BackColor = Color.LightGreen }; btnMaxPP.Click += BtnMaxPP_Click; pageMoves.Controls.Add(btnMaxPP);
        tabEditor.TabPages.Add(pageMoves);
        
        grpEditor.Controls.Add(tabEditor);
        this.Controls.Add(grpEditor);

        lblStatus = new Label { Location = new Point(12, 540), Size = new Size(810, 20), Text = "Esperando...", ForeColor = Color.Gray };
        this.Controls.Add(lblStatus);
    }

    private void CbBagItems_TextUpdate(object sender, EventArgs e) {
        // 1. Guardamos el texto y el cursor ANTES de tocar la lista
        string searchText = cbBagItems.Text;
        int cursorPos = cbBagItems.SelectionStart;

        // 2. LA CURA: Cerramos el menú a la fuerza antes de limpiar la lista. 
        // Esto evita que Windows intente buscar el texto en una lista vacía.
        if (cbBagItems.DroppedDown) cbBagItems.DroppedDown = false;

        // 3. Ahora sí, limpiamos con seguridad
        cbBagItems.Items.Clear();

        // 4. Filtramos y rellenamos
        if (string.IsNullOrWhiteSpace(searchText)) {
            cbBagItems.Items.AddRange(currentBagDropdownItems.ToArray());
        } else {
            var filtered = currentBagDropdownItems
                .Where(x => x.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToArray();
            
            if (filtered.Length > 0) {
                cbBagItems.Items.AddRange(filtered);
            }
        }

        // 5. Devolvemos el texto a la caja (Windows ya no crasheará aquí)
        cbBagItems.Text = searchText;
        cbBagItems.SelectionStart = cursorPos;

        // 6. Volvemos a abrir el menú SOLO si hay resultados
        if (cbBagItems.Items.Count > 0 && !string.IsNullOrWhiteSpace(searchText)) {
            cbBagItems.DroppedDown = true;
            Cursor.Current = Cursors.Default;
        }
    }

    // --- MÉTODOS DE UI RESTAURADOS ---

    private Pokemon GetCurrentPokemon() { 
        if (currentSaveData == null || currentSlotIndex < 0) return null; 
        if (isEditingParty && currentSlotIndex < currentSaveData.Party.Count) return currentSaveData.Party[currentSlotIndex]; 
        if (!isEditingParty && currentSlotIndex < 30) return currentSaveData.Boxes[currentBoxIndex].Slots[currentSlotIndex]; 
        return null; 
    }

    private void RefreshPartyGrid() { 
        if (currentSaveData == null) return; 
        for (int i = 0; i < 6; i++) { 
            if (partySlots[i].Image != null) partySlots[i].Image.Dispose(); 
            partySlots[i].Image = null; 
            if (i < currentSaveData.Party.Count) { 
                Pokemon p = currentSaveData.Party[i]; 
                partySlots[i].BackColor = Color.LightCyan; 
                partySlots[i].Image = SpriteManager.GetPokemonIcon(AppRoot, p.InternalSpecies, p.Form, p.IsShiny || p.IsSuperShiny); 
                partyLabels[i].Text = $"{p.Nickname}\nNv. {p.Level}"; 
                partyLabels[i].ForeColor = Color.Black; 
            } else { 
                partySlots[i].BackColor = Color.WhiteSmoke; 
                partyLabels[i].Text = "Vacío"; 
                partyLabels[i].ForeColor = Color.Gray; 
            } 
        } 
    }

    private void RefreshPCGrid() { 
        if (currentSaveData == null || currentBoxIndex < 0 || currentBoxIndex >= currentSaveData.Boxes.Count) return; 
        var box = currentSaveData.Boxes[currentBoxIndex]; 
        for (int i = 0; i < 30; i++) { 
            Pokemon p = box.Slots[i]; 
            if (pcSlots[i].Image != null) pcSlots[i].Image.Dispose(); 
            pcSlots[i].Image = null; 
            pcSlots[i].BackColor = (p != null) ? Color.LightCyan : Color.WhiteSmoke; 
            if (p != null) pcSlots[i].Image = SpriteManager.GetPokemonIcon(AppRoot, p.InternalSpecies, p.Form, p.IsShiny || p.IsSuperShiny); 
        } 
    }

    private void UpdateStatColorsByNature() { 
        string selectedText = cbNature.Text; 
        string intNat = PokemonUtils.GetInternalIdFromNatureText(selectedText, pbs); 
        for (int i = 0; i < 6; i++) lblStatNames[i].ForeColor = Color.Black; 
        switch (intNat?.ToUpper()) { 
            case "LONELY": lblStatNames[1].ForeColor = Color.Red; lblStatNames[2].ForeColor = Color.Blue; break; 
            case "BRAVE":  lblStatNames[1].ForeColor = Color.Red; lblStatNames[5].ForeColor = Color.Blue; break; 
            case "ADAMANT":lblStatNames[1].ForeColor = Color.Red; lblStatNames[3].ForeColor = Color.Blue; break; 
            case "NAUGHTY":lblStatNames[1].ForeColor = Color.Red; lblStatNames[4].ForeColor = Color.Blue; break; 
            case "BOLD":   lblStatNames[2].ForeColor = Color.Red; lblStatNames[1].ForeColor = Color.Blue; break; 
            case "RELAXED":lblStatNames[2].ForeColor = Color.Red; lblStatNames[5].ForeColor = Color.Blue; break; 
            case "IMPISH": lblStatNames[2].ForeColor = Color.Red; lblStatNames[3].ForeColor = Color.Blue; break; 
            case "LAX":    lblStatNames[2].ForeColor = Color.Red; lblStatNames[4].ForeColor = Color.Blue; break; 
            case "MODEST": lblStatNames[3].ForeColor = Color.Red; lblStatNames[1].ForeColor = Color.Blue; break; 
            case "MILD":   lblStatNames[3].ForeColor = Color.Red; lblStatNames[2].ForeColor = Color.Blue; break; 
            case "QUIET":  lblStatNames[3].ForeColor = Color.Red; lblStatNames[5].ForeColor = Color.Blue; break; 
            case "RASH":   lblStatNames[3].ForeColor = Color.Red; lblStatNames[4].ForeColor = Color.Blue; break; 
            case "CALM":   lblStatNames[4].ForeColor = Color.Red; lblStatNames[1].ForeColor = Color.Blue; break; 
            case "GENTLE": lblStatNames[4].ForeColor = Color.Red; lblStatNames[2].ForeColor = Color.Blue; break; 
            case "SASSY":  lblStatNames[4].ForeColor = Color.Red; lblStatNames[5].ForeColor = Color.Blue; break; 
            case "CAREFUL":lblStatNames[4].ForeColor = Color.Red; lblStatNames[3].ForeColor = Color.Blue; break; 
            case "TIMID":  lblStatNames[5].ForeColor = Color.Red; lblStatNames[1].ForeColor = Color.Blue; break; 
            case "HASTY":  lblStatNames[5].ForeColor = Color.Red; lblStatNames[2].ForeColor = Color.Blue; break; 
            case "JOLLY":  lblStatNames[5].ForeColor = Color.Red; lblStatNames[3].ForeColor = Color.Blue; break; 
            case "NAIVE":  lblStatNames[5].ForeColor = Color.Red; lblStatNames[4].ForeColor = Color.Blue; break; 
        } 
    }

    private void UpdatePPLimits(int moveIndex) { 
        string moveName = cbMoves[moveIndex].Text; 
        string internalId = PokemonUtils.GetInternalId(pbs.Moves, moveName, "", pbs); 
        int basePP = 10; 
        if (pbs.MovePPs.TryGetValue(internalId, out int pp)) basePP = pp; 
        int ppUps = (int)numPPUps[moveIndex].Value; 
        int maxPP = basePP + (basePP * ppUps / 5); 
        numPPs[moveIndex].Maximum = Math.Max(1, maxPP); 
        if (numPPs[moveIndex].Value > numPPs[moveIndex].Maximum) numPPs[moveIndex].Value = numPPs[moveIndex].Maximum; 
    }

    // --- FORMATEO VISUAL ANTI-FUGAS ---
    private void ClearEditorUI() {
        isUpdatingUI = true;
        currentSlotIndex = -1;
        grpEditor.Enabled = false;
        if (picSprite.Image != null) { picSprite.Image.Dispose(); picSprite.Image = null; }
        txtNickname.Text = "";
        cbItem.SelectedIndex = -1; cbItem.Text = "";
        cbAbility.SelectedIndex = -1; cbAbility.Text = "";
        cbNature.SelectedIndex = -1; cbNature.Text = "";
        numLevel.Value = 1;
        if (cbGender.Items.Count > 0) cbGender.SelectedIndex = 0;
        for (int i = 0; i < 6; i++) { numIVs[i].Value = 0; numEVs[i].Value = 0; }
        for (int i = 0; i < 4; i++) { cbMoves[i].SelectedIndex = -1; cbMoves[i].Text = ""; numPPs[i].Value = 0; numPPUps[i].Value = 0; }
        isUpdatingUI = false;
    }

    // --- MÉTODOS DE EXTRACCIÓN Y SEGURIDAD ---
    private object Unwrap(object obj) { if (obj == null) return null; if (obj.GetType().Name == "RubyWrapper") { dynamic wrapper = obj; return wrapper.WrappedObject; } return obj; }

    private long SafeGetLong(object obj, long fallback = 0) {
        if (obj == null) return fallback;
        if (obj is long l) return l;
        if (obj is int i) return i;
        if (obj is System.Numerics.BigInteger bi) return (long)bi;
        try { return Convert.ToInt64(obj); } catch { return fallback; }
    }

    private int SafeGetInt(object obj, int fallback = 0) {
        if (obj == null) return fallback;
        if (obj is int i) return i;
        if (obj is long l) return (int)l;
        if (obj is System.Numerics.BigInteger bi) return (int)bi;
        try { return Convert.ToInt32(obj); } catch { return fallback; }
    }

    private dynamic FindObjectByClassName(object node, string targetClass, HashSet<object> visited = null) {
        visited ??= new HashSet<object>();
        if (node == null || visited.Contains(node)) return null;
        visited.Add(node);

        node = Unwrap(node);
        if (node == null) return null;

        var type = node.GetType();
        if (type.Name == "RubyObject") {
            dynamic ro = node;
            if (ro.ClassName == targetClass) return ro;
            if (ro.Attributes != null) {
                foreach (var val in ro.Attributes.Values) {
                    var found = FindObjectByClassName(val, targetClass, visited);
                    if (found != null) return found;
                }
            }
        }
        else if (node is IList list) {
            foreach (var item in list) { var found = FindObjectByClassName(item, targetClass, visited); if (found != null) return found; }
        }
        else if (node is IDictionary dict) {
            foreach (var item in dict.Values) { var found = FindObjectByClassName(item, targetClass, visited); if (found != null) return found; }
        }
        return null;
    }

    private List<dynamic> FindAllObjectsByClassName(object node, string targetClass, HashSet<object> visited = null, List<dynamic> results = null) {
        visited ??= new HashSet<object>();
        results ??= new List<dynamic>();
        if (node == null || visited.Contains(node)) return results;
        visited.Add(node);

        node = Unwrap(node);
        if (node == null) return results;

        var type = node.GetType();
        if (type.Name == "RubyObject") {
            dynamic ro = node;
            if (ro.ClassName == targetClass) results.Add(ro);
            if (ro.Attributes != null) {
                foreach (var val in ro.Attributes.Values) FindAllObjectsByClassName(val, targetClass, visited, results);
            }
        }
        else if (node is IList list) {
            foreach (var item in list) FindAllObjectsByClassName(item, targetClass, visited, results);
        }
        else if (node is IDictionary dict) {
            foreach (var item in dict.Values) FindAllObjectsByClassName(item, targetClass, visited, results);
        }
        return results;
    }

    private long GetMoneySafely() {
        try {
            dynamic trainer = FindObjectByClassName(currentSaveData.RootData, "PokeBattle_Trainer") ?? FindObjectByClassName(currentSaveData.RootData, "Player");
            if (trainer != null && trainer.Attributes != null && trainer.Attributes.ContainsKey("@money")) {
                object rawMoney = Unwrap(trainer.Attributes["@money"]);
                if (rawMoney != null) {
                    string strVal = rawMoney.ToString();
                    if (long.TryParse(strVal, out long val)) return val;
                    return 9999999999L; 
                }
            }
        } catch { }
        return 0;
    }

    private void SyncMoneySafely(long newMoney) {
        try {
            dynamic trainer = FindObjectByClassName(currentSaveData.RootData, "PokeBattle_Trainer") ?? FindObjectByClassName(currentSaveData.RootData, "Player");
            if (trainer != null && trainer.Attributes != null) {
                trainer.Attributes["@money"] = newMoney;
            }
        } catch { }
    }

    private void UpdateInternalSaveSlot(object node, int currentSlot, int newSlot, HashSet<object> visited = null) {
        visited ??= new HashSet<object>();
        if (node == null || visited.Contains(node)) return;
        visited.Add(node);

        node = Unwrap(node);
        if (node == null) return;

        string currentStr = currentSlot.ToString();
        string newStr = newSlot.ToString();

        var type = node.GetType();
        if (type.Name == "RubyObject") {
            dynamic ro = node;
            if (ro.Attributes != null) {
                string[] safeAttrs = { "@saveSlot", "@save_slot", "@saveFile", "@save_file", "@saveIndex", "@save_index", "@current_save_slot", "@title", "@name" };
                foreach (string attr in safeAttrs) {
                    if (ro.Attributes.ContainsKey(attr)) {
                        object orig = Unwrap(ro.Attributes[attr]);
                        
                        if (orig is string strVal) {
                            if (strVal.Contains("Partida " + currentStr)) ro.Attributes[attr] = strVal.Replace("Partida " + currentStr, "Partida " + newStr);
                            else if (strVal.Contains("Game " + currentStr)) ro.Attributes[attr] = strVal.Replace("Game " + currentStr, "Game " + newStr);
                            else if (strVal == currentStr) ro.Attributes[attr] = newStr;
                        }
                        else if (orig is int || orig is long || orig is System.Numerics.BigInteger) {
                            if (orig.ToString() == currentStr) {
                                ro.Attributes[attr] = newSlot;
                            }
                        }
                        else if (orig is byte[] byteVal) {
                            string strBytes = System.Text.Encoding.UTF8.GetString(byteVal);
                            if (strBytes.Contains("Partida " + currentStr)) ro.Attributes[attr] = System.Text.Encoding.UTF8.GetBytes(strBytes.Replace("Partida " + currentStr, "Partida " + newStr));
                            else if (strBytes.Contains("Game " + currentStr)) ro.Attributes[attr] = System.Text.Encoding.UTF8.GetBytes(strBytes.Replace("Game " + currentStr, "Game " + newStr));
                            else if (strBytes == currentStr) ro.Attributes[attr] = System.Text.Encoding.UTF8.GetBytes(newStr);
                        }
                    }
                }
                foreach (var val in ro.Attributes.Values) UpdateInternalSaveSlot(val, currentSlot, newSlot, visited);
            }
        }
        else if (node is IList list) {
            foreach (var item in list) UpdateInternalSaveSlot(item, currentSlot, newSlot, visited);
        }
        else if (node is IDictionary dict) {
            var keysToUpdate = new List<object>();
            foreach (DictionaryEntry entry in dict) {
                string k = Unwrap(entry.Key)?.ToString()?.Replace(":", "")?.Replace("@", "")?.Trim() ?? "";
                if (k == "saveSlot" || k == "save_slot" || k == "saveFile" || k == "save_file" || k == "saveIndex" || k == "save_index" || k == "current_save_slot" || k == "title" || k == "name") {
                    keysToUpdate.Add(entry.Key);
                }
                UpdateInternalSaveSlot(entry.Value, currentSlot, newSlot, visited);
            }
            foreach (var key in keysToUpdate) {
                object orig = Unwrap(dict[key]);
                if (orig is string strVal) {
                    if (strVal.Contains("Partida " + currentStr)) dict[key] = strVal.Replace("Partida " + currentStr, "Partida " + newStr);
                    else if (strVal.Contains("Game " + currentStr)) dict[key] = strVal.Replace("Game " + currentStr, "Game " + newStr);
                    else if (strVal == currentStr) dict[key] = newStr;
                }
                else if (orig is byte[] byteVal) {
                    string strBytes = System.Text.Encoding.UTF8.GetString(byteVal);
                    if (strBytes.Contains("Partida " + currentStr)) dict[key] = System.Text.Encoding.UTF8.GetBytes(strBytes.Replace("Partida " + currentStr, "Partida " + newStr));
                    else if (strBytes.Contains("Game " + currentStr)) dict[key] = System.Text.Encoding.UTF8.GetBytes(strBytes.Replace("Game " + currentStr, "Game " + newStr));
                    else if (strBytes == currentStr) dict[key] = System.Text.Encoding.UTF8.GetBytes(newStr);
                }
                else if (orig is int || orig is long || orig is System.Numerics.BigInteger) {
                    if (orig.ToString() == currentStr) {
                        dict[key] = newSlot;
                    }
                }
            }
        }
    }

    // --- ESCÁNER DE RANURAS Y LECTOR PBS DE HABILIDADES ---
    private int GetRealAbilityIndex(long personalId) {
        if (currentSaveData == null || currentSaveData.RootData == null) return 0;
        var pkmObjects = FindAllObjectsByClassName(currentSaveData.RootData, "PokeBattle_Pokemon");
        if (pkmObjects.Count == 0) pkmObjects = FindAllObjectsByClassName(currentSaveData.RootData, "Pokemon");
        foreach (dynamic ro in pkmObjects) {
            if (ro.Attributes != null) {
                object pidObj = ro.Attributes.ContainsKey("@personalID") ? ro.Attributes["@personalID"] : 
                               (ro.Attributes.ContainsKey("@personal_id") ? ro.Attributes["@personal_id"] : 
                               (ro.Attributes.ContainsKey("@pid") ? ro.Attributes["@pid"] : null));
                if (pidObj != null && SafeGetLong(Unwrap(pidObj)) == personalId) {
                    if (ro.Attributes.ContainsKey("@ability_index")) return SafeGetInt(Unwrap(ro.Attributes["@ability_index"]));
                }
            }
        }
        return 0;
    }

    private void SetRealAbilityIndex(long personalId, int newIndex) {
        if (currentSaveData == null || currentSaveData.RootData == null) return;
        var pkmObjects = FindAllObjectsByClassName(currentSaveData.RootData, "PokeBattle_Pokemon");
        if (pkmObjects.Count == 0) pkmObjects = FindAllObjectsByClassName(currentSaveData.RootData, "Pokemon");
        foreach (dynamic ro in pkmObjects) {
            if (ro.Attributes != null) {
                object pidObj = ro.Attributes.ContainsKey("@personalID") ? ro.Attributes["@personalID"] : 
                               (ro.Attributes.ContainsKey("@personal_id") ? ro.Attributes["@personal_id"] : 
                               (ro.Attributes.ContainsKey("@pid") ? ro.Attributes["@pid"] : null));
                if (pidObj != null && SafeGetLong(Unwrap(pidObj)) == personalId) {
                    ro.Attributes["@ability_index"] = newIndex;
                }
            }
        }
    }

    private string GetRealAbilityNameFromRuby(long personalId) {
        if (currentSaveData == null || currentSaveData.RootData == null) return null;
        var pkmObjects = FindAllObjectsByClassName(currentSaveData.RootData, "PokeBattle_Pokemon");
        if (pkmObjects.Count == 0) pkmObjects = FindAllObjectsByClassName(currentSaveData.RootData, "Pokemon");
        foreach (dynamic ro in pkmObjects) {
            if (ro.Attributes != null) {
                object pidObj = ro.Attributes.ContainsKey("@personalID") ? ro.Attributes["@personalID"] : 
                               (ro.Attributes.ContainsKey("@personal_id") ? ro.Attributes["@personal_id"] : 
                               (ro.Attributes.ContainsKey("@pid") ? ro.Attributes["@pid"] : null));
                if (pidObj != null && SafeGetLong(Unwrap(pidObj)) == personalId) {
                    if (ro.Attributes.ContainsKey("@ability")) {
                        return Unwrap(ro.Attributes["@ability"])?.ToString();
                    }
                }
            }
        }
        return null;
    }

    private void SetRealAbilityNameInRuby(long personalId, string newAbilityInternal) {
        if (currentSaveData == null || currentSaveData.RootData == null || string.IsNullOrEmpty(newAbilityInternal)) return;
        var pkmObjects = FindAllObjectsByClassName(currentSaveData.RootData, "PokeBattle_Pokemon");
        if (pkmObjects.Count == 0) pkmObjects = FindAllObjectsByClassName(currentSaveData.RootData, "Pokemon");
        foreach (dynamic ro in pkmObjects) {
            if (ro.Attributes != null) {
                object pidObj = ro.Attributes.ContainsKey("@personalID") ? ro.Attributes["@personalID"] : 
                               (ro.Attributes.ContainsKey("@personal_id") ? ro.Attributes["@personal_id"] : 
                               (ro.Attributes.ContainsKey("@pid") ? ro.Attributes["@pid"] : null));
                if (pidObj != null && SafeGetLong(Unwrap(pidObj)) == personalId) {
                    ro.Attributes["@ability"] = newAbilityInternal;
                }
            }
        }
    }

    private void ClearAbilityNameInRuby(long personalId) {
        if (currentSaveData == null || currentSaveData.RootData == null) return;
        var pkmObjects = FindAllObjectsByClassName(currentSaveData.RootData, "PokeBattle_Pokemon");
        if (pkmObjects.Count == 0) pkmObjects = FindAllObjectsByClassName(currentSaveData.RootData, "Pokemon");
        foreach (dynamic ro in pkmObjects) {
            if (ro.Attributes != null) {
                object pidObj = ro.Attributes.ContainsKey("@personalID") ? ro.Attributes["@personalID"] : 
                               (ro.Attributes.ContainsKey("@personal_id") ? ro.Attributes["@personal_id"] : 
                               (ro.Attributes.ContainsKey("@pid") ? ro.Attributes["@pid"] : null));
                if (pidObj != null && SafeGetLong(Unwrap(pidObj)) == personalId) {
                    if (ro.Attributes.ContainsKey("@ability")) ro.Attributes.Remove("@ability");
                }
            }
        }
    }

    private string GetPBSAbility(string internalSpecies, int form, int abilityIndex, string appRoot) {
        string abil = "";
        try {
            string pbsPath = Path.Combine(appRoot, "PBS", "pokemon.txt");
            string formsPath = Path.Combine(appRoot, "PBS", "pokemon_forms.txt");
            string abilitiesStr = "";
            string hiddenAbilitiesStr = "";

            if (form > 0 && File.Exists(formsPath)) {
                string[] lines = File.ReadAllLines(formsPath);
                bool found = false;
                for (int i=0; i<lines.Length; i++) {
                    string line = lines[i].Trim();
                    if (line.Equals($"[{internalSpecies},{form}]", StringComparison.OrdinalIgnoreCase) || line.Equals($"[{internalSpecies}_{form}]", StringComparison.OrdinalIgnoreCase)) found = true;
                    else if (found && line.StartsWith("[")) break;
                    else if (found && line.StartsWith("Abilities", StringComparison.OrdinalIgnoreCase)) abilitiesStr = line.Split('=')[1].Trim();
                    else if (found && (line.StartsWith("HiddenAbility", StringComparison.OrdinalIgnoreCase) || line.StartsWith("HiddenAbilities", StringComparison.OrdinalIgnoreCase))) hiddenAbilitiesStr = line.Split('=')[1].Trim();
                }
            }

            if (string.IsNullOrEmpty(abilitiesStr) && File.Exists(pbsPath)) {
                string[] lines = File.ReadAllLines(pbsPath);
                bool found = false;
                for (int i=0; i<lines.Length; i++) {
                    string line = lines[i].Trim();
                    if (line.Equals($"[{internalSpecies}]", StringComparison.OrdinalIgnoreCase)) found = true;
                    else if (found && line.StartsWith("[")) break;
                    else if (found && line.StartsWith("Abilities", StringComparison.OrdinalIgnoreCase)) abilitiesStr = line.Split('=')[1].Trim();
                    else if (found && (line.StartsWith("HiddenAbility", StringComparison.OrdinalIgnoreCase) || line.StartsWith("HiddenAbilities", StringComparison.OrdinalIgnoreCase))) hiddenAbilitiesStr = line.Split('=')[1].Trim();
                }
            }

            if (abilityIndex >= 2) {
                if (!string.IsNullOrEmpty(hiddenAbilitiesStr)) { string[] parts = hiddenAbilitiesStr.Split(','); abil = parts[0].Trim(); }
            } else {
                if (!string.IsNullOrEmpty(abilitiesStr)) { 
                    string[] parts = abilitiesStr.Split(','); 
                    if (abilityIndex == 1 && parts.Length > 1) abil = parts[1].Trim(); 
                    else abil = parts[0].Trim(); 
                }
            }
        } catch { }
        return abil;
    }

    private int DetectAbilitySlotFromText(string internalSpecies, int form, string abilityName) {
        string intId = PokemonUtils.GetInternalId(pbs.Abilities, abilityName, "", pbs);
        if (string.IsNullOrEmpty(intId)) return -1;
        if (GetPBSAbility(internalSpecies, form, 2, AppRoot).Equals(intId, StringComparison.OrdinalIgnoreCase)) return 2;
        if (GetPBSAbility(internalSpecies, form, 1, AppRoot).Equals(intId, StringComparison.OrdinalIgnoreCase)) return 1;
        if (GetPBSAbility(internalSpecies, form, 0, AppRoot).Equals(intId, StringComparison.OrdinalIgnoreCase)) return 0;
        return -1;
    }

    // --- RECALCULADORA DE STATS ---
    private int[] GetBaseStats(string internalSpecies, int form, string appRoot) {
        int[] stats = new int[] { 10, 10, 10, 10, 10, 10 };
        try {
            string pbsPath = Path.Combine(appRoot, "PBS", "pokemon.txt");
            string formsPath = Path.Combine(appRoot, "PBS", "pokemon_forms.txt");
            string baseStatsStr = "";

            if (form > 0 && File.Exists(formsPath)) {
                string[] lines = File.ReadAllLines(formsPath);
                bool found = false;
                for (int i=0; i<lines.Length; i++) {
                    string line = lines[i].Trim();
                    if (line.Equals($"[{internalSpecies},{form}]", StringComparison.OrdinalIgnoreCase) || 
                        line.Equals($"[{internalSpecies}_{form}]", StringComparison.OrdinalIgnoreCase)) {
                        found = true;
                    } else if (found && line.StartsWith("[")) break;
                    else if (found && line.StartsWith("BaseStats", StringComparison.OrdinalIgnoreCase)) {
                        baseStatsStr = line.Split('=')[1].Trim();
                    }
                }
            }

            if (string.IsNullOrEmpty(baseStatsStr) && File.Exists(pbsPath)) {
                string[] lines = File.ReadAllLines(pbsPath);
                bool found = false;
                for (int i=0; i<lines.Length; i++) {
                    string line = lines[i].Trim();
                    if (line.Equals($"[{internalSpecies}]", StringComparison.OrdinalIgnoreCase)) {
                        found = true;
                    } else if (found && line.StartsWith("[")) break;
                    else if (found && line.StartsWith("BaseStats", StringComparison.OrdinalIgnoreCase)) {
                        baseStatsStr = line.Split('=')[1].Trim();
                    }
                }
            }

            if (!string.IsNullOrEmpty(baseStatsStr)) {
                string[] parts = baseStatsStr.Split(new char[]{','}, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 6) {
                    stats[0] = int.Parse(parts[0].Trim()); 
                    stats[1] = int.Parse(parts[1].Trim()); 
                    stats[2] = int.Parse(parts[2].Trim()); 
                    stats[5] = int.Parse(parts[3].Trim()); 
                    stats[3] = int.Parse(parts[4].Trim()); 
                    stats[4] = int.Parse(parts[5].Trim()); 
                }
            }
        } catch { }
        return stats;
    }

    private double GetNatureModifier(string internalNature, int statIndex) {
        if (string.IsNullOrEmpty(internalNature)) return 1.0;
        string nat = internalNature.ToUpper();
        int up = -1, down = -1;
        
        switch (nat) {
            case "LONELY":  up = 1; down = 2; break; case "BRAVE":   up = 1; down = 5; break;
            case "ADAMANT": up = 1; down = 3; break; case "NAUGHTY": up = 1; down = 4; break;
            case "BOLD":    up = 2; down = 1; break; case "RELAXED": up = 2; down = 5; break;
            case "IMPISH":  up = 2; down = 3; break; case "LAX":     up = 2; down = 4; break;
            case "TIMID":   up = 5; down = 1; break; case "HASTY":   up = 5; down = 2; break;
            case "JOLLY":   up = 5; down = 3; break; case "NAIVE":   up = 5; down = 4; break;
            case "MODEST":  up = 3; down = 1; break; case "MILD":    up = 3; down = 2; break;
            case "QUIET":   up = 3; down = 5; break; case "RASH":    up = 3; down = 4; break;
            case "CALM":    up = 4; down = 1; break; case "GENTLE":  up = 4; down = 2; break;
            case "SASSY":   up = 4; down = 5; break; case "CAREFUL": up = 4; down = 3; break;
        }
        if (statIndex == up) return 1.1;
        if (statIndex == down) return 0.9;
        return 1.0;
    }

    private int[] CalculateStats(Pokemon p, string appRoot) {
        int[] bs = GetBaseStats(p.InternalSpecies, p.Form, appRoot);
        int[] calcStats = new int[6];
        if (p.InternalSpecies.ToUpper() == "SHEDINJA") calcStats[0] = 1;
        else calcStats[0] = (int)Math.Floor(0.01 * (2 * bs[0] + p.IVs[0] + Math.Floor(0.25 * p.EVs[0])) * p.Level) + p.Level + 10;
        
        for (int i = 1; i < 6; i++) {
            int rawStat = (int)Math.Floor(0.01 * (2 * bs[i] + p.IVs[i] + Math.Floor(0.25 * p.EVs[i])) * p.Level) + 5;
            calcStats[i] = (int)Math.Floor(rawStat * GetNatureModifier(p.InternalNature, i));
        }
        return calcStats;
    }

    // --- ESCRITURA PROFUNDA (ESTADÍSTICAS Y SUPER SHINY) ---
    // NOTA: Se eliminó el forzado de @nature para evitar que las naturalezas desaparezcan en el juego
    private void ApplyDirectRubyEdits(object rootData, List<Pokemon> allPokes) {
        var pkmObjects = FindAllObjectsByClassName(rootData, "PokeBattle_Pokemon");
        if (pkmObjects.Count == 0) pkmObjects = FindAllObjectsByClassName(rootData, "Pokemon");

        foreach (dynamic ro in pkmObjects) {
            if (ro.Attributes != null) {
                object pidObj = null;
                if (ro.Attributes.ContainsKey("@personalID")) pidObj = ro.Attributes["@personalID"];
                else if (ro.Attributes.ContainsKey("@personal_id")) pidObj = ro.Attributes["@personal_id"];
                else if (ro.Attributes.ContainsKey("@pid")) pidObj = ro.Attributes["@pid"];
                
                // FIX CRÍTICO: Ya no le exigimos que tenga @hp previamente para que lo recalcule
                if (pidObj != null) {
                    long pid = SafeGetLong(Unwrap(pidObj));
                    Pokemon pkm = allPokes.FirstOrDefault(p => p.PersonalID == pid);
                    if (pkm != null) {
                        int[] newStats = CalculateStats(pkm, AppRoot);
                        ro.Attributes["@attack"] = newStats[1];
                        ro.Attributes["@defense"] = newStats[2];
                        ro.Attributes["@spatk"] = newStats[3];
                        ro.Attributes["@spdef"] = newStats[4];
                        ro.Attributes["@speed"] = newStats[5];
                        ro.Attributes["@totalhp"] = newStats[0];
                        ro.Attributes["@hp"] = newStats[0]; // Fuerza a llenarle la barra de vida

                        if (pkm.IsSuperShiny) {
                            ro.Attributes["@shiny"] = true;
                            ro.Attributes["@radiant"] = true;
                            ro.Attributes["@radiante"] = true;
                            ro.Attributes["@super_shiny"] = true;
                            ro.Attributes["@superShiny"] = true;
                        } else if (pkm.IsShiny) {
                            ro.Attributes["@shiny"] = true;
                            if (ro.Attributes.ContainsKey("@radiant")) ro.Attributes.Remove("@radiant");
                            if (ro.Attributes.ContainsKey("@radiante")) ro.Attributes.Remove("@radiante");
                            if (ro.Attributes.ContainsKey("@super_shiny")) ro.Attributes.Remove("@super_shiny");
                            if (ro.Attributes.ContainsKey("@superShiny")) ro.Attributes.Remove("@superShiny");
                        } else {
                            if (ro.Attributes.ContainsKey("@shiny")) ro.Attributes.Remove("@shiny");
                            if (ro.Attributes.ContainsKey("@radiant")) ro.Attributes.Remove("@radiant");
                            if (ro.Attributes.ContainsKey("@radiante")) ro.Attributes.Remove("@radiante");
                            if (ro.Attributes.ContainsKey("@super_shiny")) ro.Attributes.Remove("@super_shiny");
                            if (ro.Attributes.ContainsKey("@superShiny")) ro.Attributes.Remove("@superShiny");
                        }

                        // EXORCISMO DE CORRUPCIÓN GLOBAL
                        ro.Attributes.Remove("@calc_stats");
                        ro.Attributes.Remove("@calc_level");
                    }
                }
            }
        }
    }

    // --- EVENTOS DE INTERFAZ ---
    private void UniversalSlot_MouseDown(object sender, MouseEventArgs e) { if (e.Button == MouseButtons.Left) { dragStartPos = e.Location; dragSlotIndex = (int)((PictureBox)sender).Tag; isDragging = false; } }
    private void UniversalSlot_MouseMove(object sender, MouseEventArgs e) { if (e.Button == MouseButtons.Left && dragSlotIndex != -1 && !isDragging) { if (Math.Abs(e.X - dragStartPos.X) > 4 || Math.Abs(e.Y - dragStartPos.Y) > 4) { PictureBox pb = (PictureBox)sender; bool isPartySlot = pb.Parent == tabParty; if (isPartySlot) { if (currentSaveData != null && dragSlotIndex < currentSaveData.Party.Count) { isDragging = true; int slot = dragSlotIndex; dragSlotIndex = -1; pb.DoDragDrop(new DragData { IsParty = true, BoxIndex = 0, SlotIndex = slot }, DragDropEffects.Move); } } else { if (currentSaveData != null && currentSaveData.Boxes[currentBoxIndex].Slots[dragSlotIndex] != null) { isDragging = true; int slot = dragSlotIndex; dragSlotIndex = -1; pb.DoDragDrop(new DragData { IsParty = false, BoxIndex = currentBoxIndex, SlotIndex = slot }, DragDropEffects.Move); } } } } }
    private void UniversalSlot_MouseUp(object sender, MouseEventArgs e) { if (!isDragging && dragSlotIndex != -1) { PictureBox pb = (PictureBox)sender; if (pb.Parent == tabParty) PartySlot_ClickAction(sender, EventArgs.Empty); else PcSlot_ClickAction(sender, EventArgs.Empty); } dragSlotIndex = -1; isDragging = false; }
    private void UniversalSlot_DragEnter(object sender, DragEventArgs e) { if (e.Data.GetDataPresent(typeof(DragData))) e.Effect = DragDropEffects.Move; }
    
    private void PartySlot_ClickAction(object sender, EventArgs e) { 
        if (isUpdatingUI) return; 
        PictureBox clickedSlot = sender as PictureBox; 
        if (clickedSlot == null) return; 
        int slotIndex = (int)clickedSlot.Tag; 
        if (currentSaveData == null || slotIndex >= currentSaveData.Party.Count) { ApplyCurrentEdits(); ClearEditorUI(); return; } 
        if (isEditingParty && currentSlotIndex == slotIndex) return; 
        ApplyCurrentEdits(); 
        isEditingParty = true; 
        currentSlotIndex = slotIndex; 
        LoadPokemonToEditor(); 
    }
    
    private void PcSlot_ClickAction(object sender, EventArgs e) { 
        if (isUpdatingUI) return; 
        PictureBox clickedSlot = sender as PictureBox; 
        if (clickedSlot == null) return; 
        int slotIndex = (int)clickedSlot.Tag; 
        if (currentSaveData.Boxes[currentBoxIndex].Slots[slotIndex] == null) { ApplyCurrentEdits(); ClearEditorUI(); return; } 
        if (!isEditingParty && currentSlotIndex == slotIndex) return; 
        ApplyCurrentEdits(); 
        isEditingParty = false; 
        currentSlotIndex = slotIndex; 
        LoadPokemonToEditor(); 
    }
    
    private void PartySlot_DragDrop(object sender, DragEventArgs e) { DragData source = (DragData)e.Data.GetData(typeof(DragData)); PictureBox pb = sender as PictureBox; int targetSlot = (int)pb.Tag; Unified_DragDrop(source, true, 0, targetSlot); }
    private void PcSlot_DragDrop(object sender, DragEventArgs e) { DragData source = (DragData)e.Data.GetData(typeof(DragData)); PictureBox pb = sender as PictureBox; int targetSlot = (int)pb.Tag; Unified_DragDrop(source, false, currentBoxIndex, targetSlot); }
    private void Unified_DragDrop(DragData source, bool targetIsParty, int targetBox, int targetSlot) {
        if (source.IsParty == targetIsParty && source.BoxIndex == targetBox && source.SlotIndex == targetSlot) return; 
        lblStatus.Text = "⏳ Moviendo Pokémon..."; lblStatus.ForeColor = Color.DarkOrange; this.Cursor = Cursors.WaitCursor; Application.DoEvents();
        try {
            ApplyCurrentEdits(); 
            SyncAllToRuby(); // <--- EL SEGURO DE VIDA: Sincroniza a la memoria de la partida ANTES de moverlo
            SaveWriter writer = new SaveWriter(currentSavePath, currentSaveData.RootData, pbs);
            
            if (source.IsParty && !targetIsParty) { 
                if (currentSaveData.Boxes[targetBox].Slots[targetSlot] != null) { writer.SwapPokemonInRuby(true, 0, source.SlotIndex, false, targetBox, targetSlot); } 
                else { writer.ClonePokemonInRuby(true, 0, source.SlotIndex, false, targetBox, targetSlot); writer.DeletePokemonInRuby(true, 0, source.SlotIndex); } 
            } else if (!source.IsParty && targetIsParty) { 
                if (targetSlot < currentSaveData.Party.Count) { writer.SwapPokemonInRuby(false, source.BoxIndex, source.SlotIndex, true, 0, targetSlot); } 
                else { 
                    if (currentSaveData.Party.Count >= 6) { MessageBox.Show("El equipo ya está lleno.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; } 
                    writer.ClonePokemonInRuby(false, source.BoxIndex, source.SlotIndex, true, 0, currentSaveData.Party.Count); writer.DeletePokemonInRuby(false, source.BoxIndex, source.SlotIndex); 
                } 
            } else if (!source.IsParty && !targetIsParty) { 
                writer.SwapPokemonInRuby(false, source.BoxIndex, source.SlotIndex, false, targetBox, targetSlot); 
            } else if (source.IsParty && targetIsParty) { 
                if (targetSlot < currentSaveData.Party.Count) { writer.SwapPokemonInRuby(true, 0, source.SlotIndex, true, 0, targetSlot); } 
                else { writer.ClonePokemonInRuby(true, 0, source.SlotIndex, true, 0, currentSaveData.Party.Count); writer.DeletePokemonInRuby(true, 0, source.SlotIndex); } 
            }
            ReloadFromMemory(); 
            if (isEditingParty == targetIsParty) { 
                if (currentBoxIndex == targetBox || targetIsParty) { currentSlotIndex = targetIsParty ? Math.Min(targetSlot, currentSaveData.Party.Count - 1) : targetSlot; } 
            } 
            LoadPokemonToEditor(); 
            lblStatus.Text = "Pokémon movido con éxito."; lblStatus.ForeColor = Color.Green;
        } finally { this.Cursor = Cursors.Default; }
    }
    private void MovePokemonQuick_Click(object sender, EventArgs e) { var pb = ((sender as ToolStripItem).Owner as ContextMenuStrip).SourceControl as PictureBox; if (pb == null) return; bool isParty = pb.Parent == tabParty; int slot = (int)pb.Tag; if (isParty) { int targetBox = -1, targetSlot = -1; for (int b = 0; b < currentSaveData.Boxes.Count; b++) { for (int s = 0; s < 30; s++) { if (currentSaveData.Boxes[b].Slots[s] == null) { targetBox = b; targetSlot = s; break; } } if (targetBox != -1) break; } if (targetBox == -1) { MessageBox.Show("Todas las cajas del PC están llenas.", "Aviso"); return; } Unified_DragDrop(new DragData { IsParty = true, BoxIndex = 0, SlotIndex = slot }, false, targetBox, targetSlot); } else { if (currentSaveData.Party.Count >= 6) { MessageBox.Show("El equipo ya está lleno.", "Aviso"); return; } Unified_DragDrop(new DragData { IsParty = false, BoxIndex = currentBoxIndex, SlotIndex = slot }, true, 0, currentSaveData.Party.Count); } }
    private void DeletePokemon_Click(object sender, EventArgs e) { var pb = ((sender as ToolStripItem).Owner as ContextMenuStrip).SourceControl as PictureBox; if (pb == null) return; bool isParty = pb.Parent == tabParty; int slot = (int)pb.Tag; if (MessageBox.Show("¿Eliminar de forma permanente?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes) { lblStatus.Text = "⏳ Eliminando Pokémon..."; lblStatus.ForeColor = Color.DarkOrange; this.Cursor = Cursors.WaitCursor; Application.DoEvents(); try { ApplyCurrentEdits(); SyncAllToRuby(); SaveWriter writer = new SaveWriter(currentSavePath, currentSaveData.RootData, pbs); if (isParty) writer.DeletePokemonInRuby(true, 0, slot); else writer.DeletePokemonInRuby(false, currentBoxIndex, slot); ReloadFromMemory(); ClearEditorUI(); lblStatus.Text = "Pokémon eliminado."; lblStatus.ForeColor = Color.Red; } finally { this.Cursor = Cursors.Default; } } }
    private void BtnAddPokemon_Click(object sender, EventArgs e) { if (currentSaveData == null) return; int targetSlot = -1; for (int i = 0; i < 30; i++) { if (currentSaveData.Boxes[currentBoxIndex].Slots[i] == null) { targetSlot = i; break; } } if (targetSlot == -1) { MessageBox.Show("La caja actual está llena.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; } using (AddPokemonForm form = new AddPokemonForm(pbs)) { if (form.ShowDialog() == DialogResult.OK) { lblStatus.Text = "⏳ Generando Pokémon e inyectando en la caja..."; lblStatus.ForeColor = Color.DarkOrange; this.Cursor = Cursors.WaitCursor; Application.DoEvents(); try { ApplyCurrentEdits(); SyncAllToRuby(); SaveWriter writer = new SaveWriter(currentSavePath, currentSaveData.RootData, pbs); string intSpc = form.SelectedSpeciesInternal; string gr = pbs.SpeciesGrowthRates.ContainsKey(intSpc) ? pbs.SpeciesGrowthRates[intSpc] : "MEDIUMFAST"; int exactExp = PokemonUtils.CalculateExp(form.Level, gr); bool success = writer.AddPokemon(false, currentBoxIndex, intSpc, form.SelectedSpeciesName, form.Level, exactExp, "AUTO_0"); if (success) { ReloadFromMemory(); isEditingParty = false; currentSlotIndex = targetSlot; LoadPokemonToEditor(); lblStatus.Text = $"¡Pokémon añadido! Habilidad lista para el Modo Random."; lblStatus.ForeColor = Color.Green; } } finally { this.Cursor = Cursors.Default; } } } }
    private void BtnBoxChange_DragOver(object sender, DragEventArgs e) { if (e.Data.GetDataPresent(typeof(DragData)) && (DateTime.Now - lastBoxSwitch).TotalMilliseconds > 700) { Button btn = sender as Button; if (btn == btnNextBox && cbBoxSelector.SelectedIndex < cbBoxSelector.Items.Count - 1) cbBoxSelector.SelectedIndex++; else if (btn == btnPrevBox && cbBoxSelector.SelectedIndex > 0) cbBoxSelector.SelectedIndex--; lastBoxSwitch = DateTime.Now; } }

    private void UpdateBagItemDropdown() { 
        if (!pbs.IsLoaded || tabBagPockets.SelectedIndex < 0) return; 
        int currentPocket = tabBagPockets.SelectedIndex + 1; 
        if (dgvPockets[currentPocket] == null) return; 
        
        string currentSelection = cbBagItems.Text; 
        cbBagItems.Sorted = false; 
        cbBagItems.Items.Clear(); 
        
        HashSet<string> existingItems = new HashSet<string>(StringComparer.OrdinalIgnoreCase); 
        if (currentPocket == 4 || currentPocket == 8) { 
            foreach (DataGridViewRow row in dgvPockets[currentPocket].Rows) { 
                if (row.Cells[0].Value != null) existingItems.Add(row.Cells[0].Value.ToString()); 
            } 
        } 
        
        List<string> sortedItems = new List<string>();
        // NUEVO: Este HashSet recordará los nombres mostrados para bloquear los duplicados
        HashSet<string> displayedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase); 

        foreach (var kvp in pbs.Items) { 
            int pkt = pbs.ItemPockets.ContainsKey(kvp.Key) ? pbs.ItemPockets[kvp.Key] : 1; 
            if (pkt == currentPocket && !existingItems.Contains(kvp.Key)) { 
                string name = pbs.GetName(pbs.Items, kvp.Key, kvp.Value);
                
                // Si este nombre NO está en nuestra lista de "ya mostrados", lo agregamos
                if (displayedNames.Add(name)) {
                    sortedItems.Add(name); 
                }
            } 
        } 
        
        sortedItems = sortedItems.OrderBy(x => System.Text.RegularExpressions.Regex.Replace(x, @"\d+", m => m.Value.PadLeft(10, '0'))).ToList();
        
        currentBagDropdownItems = new List<string>(sortedItems);
        
        cbBagItems.Items.AddRange(sortedItems.ToArray());
        
        if (cbBagItems.Items.Contains(currentSelection)) cbBagItems.SelectedItem = currentSelection; 
        else if (cbBagItems.Items.Count > 0) cbBagItems.SelectedIndex = 0; 
    }
    private string GetItemInternalIdFromDisplay(string display) {
        if (string.IsNullOrWhiteSpace(display)) return "";
        string cleanDisplay = display.Trim();
        
        // Compara el nombre contra nuestra traducción inteligente de MTs
        foreach (var kvp in pbs.Items) {
            if (pbs.GetName(pbs.Items, kvp.Key, kvp.Value).Equals(cleanDisplay, StringComparison.OrdinalIgnoreCase)) {
                return kvp.Key;
            }
        }
        // Si no lo encuentra, hace una búsqueda normal
        foreach (var kvp in pbs.Items) {
            if (kvp.Value.Equals(cleanDisplay, StringComparison.OrdinalIgnoreCase)) return kvp.Key;
        }
        return "";
    }
    private void BtnBagAdd_Click(object sender, EventArgs e) { 
        if (string.IsNullOrWhiteSpace(cbBagItems.Text)) return; 
        
        string intId = GetItemInternalIdFromDisplay(cbBagItems.Text); 
        if (string.IsNullOrEmpty(intId)) return; 
        
        int pocket = tabBagPockets.SelectedIndex + 1; 
        int qtyToAdd = (pocket == 4 || pocket == 8) ? 1 : (int)numBagQty.Value; 
        
        foreach (DataGridViewRow row in dgvPockets[pocket].Rows) { 
            if (qtyToAdd <= 0) break; 
            if (row.Cells[0].Value != null && row.Cells[0].Value.ToString().Equals(intId, StringComparison.OrdinalIgnoreCase)) { 
                int currentQty = Convert.ToInt32(row.Cells[2].Value); 
                int maxAllowed = (pocket == 4 || pocket == 8) ? 1 : 999; 
                if (currentQty < maxAllowed) { 
                    int spaceAvailable = maxAllowed - currentQty; 
                    int amountToFill = Math.Min(qtyToAdd, spaceAvailable); 
                    row.Cells[2].Value = currentQty + amountToFill; 
                    qtyToAdd -= amountToFill; 
                } else if (pocket == 4 || pocket == 8) { 
                    qtyToAdd = 0; 
                } 
            } 
        } 
        
        while (qtyToAdd > 0) { 
            int maxAllowed = (pocket == 4 || pocket == 8) ? 1 : 999; 
            int amountForNewSlot = Math.Min(qtyToAdd, maxAllowed); 
            dgvPockets[pocket].Rows.Add(intId, cbBagItems.Text, amountForNewSlot); 
            qtyToAdd -= amountForNewSlot; 
            if (pocket == 4 || pocket == 8) break; 
        } 
        
        // FIX: Ordenamos también la cuadrícula de la mochila al instante
        var sortedRows = dgvPockets[pocket].Rows.Cast<DataGridViewRow>()
            .OrderBy(r => System.Text.RegularExpressions.Regex.Replace(r.Cells[1].Value?.ToString() ?? "", @"\d+", m => m.Value.PadLeft(10, '0')))
            .ToArray();
        dgvPockets[pocket].Rows.Clear();
        dgvPockets[pocket].Rows.AddRange(sortedRows);
        
        UpdateBagItemDropdown(); 
    }
    private void BtnBagMaxSelected_Click(object sender, EventArgs e) { if (tabBagPockets.SelectedIndex < 0) return; int pocket = tabBagPockets.SelectedIndex + 1; if (pocket == 4 || pocket == 8) { MessageBox.Show("Las MTs y Objetos Clave no pueden tener más de 1 unidad.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information); return; } if (dgvPockets[pocket].SelectedRows.Count > 0) { foreach (DataGridViewRow row in dgvPockets[pocket].SelectedRows) { if (!row.IsNewRow) row.Cells[2].Value = 999; } UpdateBagItemDropdown(); } else { MessageBox.Show("Selecciona un objeto de la tabla haciendo clic en su fila.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning); } }
    private void BtnBagMaxCurrent_Click(object sender, EventArgs e) { if (currentSaveData == null) return; lblStatus.Text = "⏳ Maximizando los objetos que ya tienes..."; lblStatus.ForeColor = Color.DarkOrange; this.Cursor = Cursors.WaitCursor; Application.DoEvents(); try { for (int pId = 1; pId <= 8; pId++) { if (dgvPockets[pId] == null) continue; int targetQty = (pId == 4 || pId == 8) ? 1 : 999; foreach (DataGridViewRow row in dgvPockets[pId].Rows) { if (row.Cells[0].Value != null) row.Cells[2].Value = targetQty; } } UpdateBagItemDropdown(); lblStatus.Text = "¡Tus objetos actuales han sido maximizados a 999!"; lblStatus.ForeColor = Color.Green; } finally { this.Cursor = Cursors.Default; } }
    private void BtnBagAddAll_Click(object sender, EventArgs e) { if (!pbs.IsLoaded) return; lblStatus.Text = "⏳ Inyectando todos los objetos del juego... Esto puede tardar unos segundos."; lblStatus.ForeColor = Color.DarkOrange; this.Cursor = Cursors.WaitCursor; Application.DoEvents(); try { foreach (var kvp in pbs.Items) { string intId = kvp.Key; string realName = pbs.GetName(pbs.Items, intId, kvp.Value); int pkt = pbs.ItemPockets.ContainsKey(intId) ? pbs.ItemPockets[intId] : 1; int targetQty = (pkt == 4 || pkt == 8) ? 1 : 999; bool found = false; foreach (DataGridViewRow row in dgvPockets[pkt].Rows) { if (row.Cells[0].Value != null && row.Cells[0].Value.ToString().Equals(intId, StringComparison.OrdinalIgnoreCase)) { found = true; if (pkt != 4 && pkt != 8) row.Cells[2].Value = targetQty; break; } } if (!found) dgvPockets[pkt].Rows.Add(intId, realName, targetQty); } UpdateBagItemDropdown(); lblStatus.Text = "¡Todos los objetos del juego han sido añadidos a tu mochila!"; lblStatus.ForeColor = Color.Green; } finally { this.Cursor = Cursors.Default; } }
    private void BtnBagRemove_Click(object sender, EventArgs e) { if (tabBagPockets.SelectedIndex < 0) return; int pocket = tabBagPockets.SelectedIndex + 1; if (dgvPockets[pocket].SelectedRows.Count > 0) { foreach (DataGridViewRow row in dgvPockets[pocket].SelectedRows) { if (!row.IsNewRow) dgvPockets[pocket].Rows.Remove(row); } UpdateBagItemDropdown(); } else { MessageBox.Show("Por favor, selecciona primero una fila en la tabla de arriba para borrarla.", "Borrar Objeto", MessageBoxButtons.OK, MessageBoxIcon.Information); } }

    private void ChkShiny_CheckedChanged(object sender, EventArgs e) { if (isUpdatingUI) return; chkShiny.BackColor = chkShiny.Checked ? Color.Gold : SystemColors.Control; if (chkShiny.Checked && chkSuperShiny.Checked) { isUpdatingUI = true; chkSuperShiny.Checked = false; chkSuperShiny.BackColor = SystemColors.Control; isUpdatingUI = false; } ApplyCurrentEdits(); LoadSpriteOnly(); }
    private void ChkSuperShiny_CheckedChanged(object sender, EventArgs e) { if (isUpdatingUI) return; chkSuperShiny.BackColor = chkSuperShiny.Checked ? Color.Plum : SystemColors.Control; if (chkSuperShiny.Checked && chkShiny.Checked) { isUpdatingUI = true; chkShiny.Checked = false; chkShiny.BackColor = SystemColors.Control; isUpdatingUI = false; } ApplyCurrentEdits(); LoadSpriteOnly(); }
    private void BtnDescAbility_Click(object sender, EventArgs e) { string internalId = PokemonUtils.GetInternalId(pbs.Abilities, cbAbility.Text, "", pbs); string desc = pbs.AbilityDescriptions.TryGetValue(internalId, out string d) ? d : "Sin descripción disponible."; MessageBox.Show(desc, $"Habilidad: {cbAbility.Text}", MessageBoxButtons.OK, MessageBoxIcon.Information); }
    private void BtnMaxHappiness_Click(object sender, EventArgs e) { Pokemon p = GetCurrentPokemon(); if (p != null) { p.Happiness = 255; lblStatus.Text = $"¡Felicidad de {p.Nickname} al máximo (255)!"; lblStatus.ForeColor = Color.DeepPink; } }
    private void BtnDescMoves_Click(int idx) { string internalId = PokemonUtils.GetInternalId(pbs.Moves, cbMoves[idx].Text, "", pbs); string desc = pbs.MoveDescriptions.TryGetValue(internalId, out string d) ? d : "Sin descripción disponible."; MessageBox.Show(desc, $"Movimiento: {cbMoves[idx].Text}", MessageBoxButtons.OK, MessageBoxIcon.Information); }
    private void BtnMaxPP_Click(object sender, EventArgs e) { for (int i = 0; i < 4; i++) { if (cbMoves[i].Enabled && !string.IsNullOrWhiteSpace(cbMoves[i].Text)) { numPPUps[i].Value = 3; numPPs[i].Value = numPPs[i].Maximum; } } }
    private void BtnInfo_Click(object sender, EventArgs e) { ApplyCurrentEdits(); Pokemon p = GetCurrentPokemon(); if (p == null) return; InfoForm infoWindow = new InfoForm(p, picSprite.Image, pbs); infoWindow.ShowDialog(); }
    
    // --- ACTUALIZADOR DE VALORES (CON FIJACIÓN DE NATURALEZA Y SIN AUTO) ---
    private void ApplyCurrentEdits() {
        if (isUpdatingUI) return; Pokemon p = GetCurrentPokemon(); if (p == null) return;
        try {
            isUpdatingUI = true; 
            p.Nickname = txtNickname.Text; 
            p.Level = (int)numLevel.Value; 
            string gr = pbs.SpeciesGrowthRates.ContainsKey(p.InternalSpecies) ? pbs.SpeciesGrowthRates[p.InternalSpecies] : "MEDIUMFAST"; 
            p.Exp = PokemonUtils.CalculateExp(p.Level, gr); 
            p.IsShiny = chkShiny.Checked; 
            p.IsSuperShiny = chkSuperShiny.Checked; 
            if (p.IsSuperShiny) p.IsShiny = false; // Exclusividad visual interna

            p.Gender = cbGender.Text; 
            p.HeldItem = cbItem.Text; 
            // FIX: Usamos el traductor inteligente aquí también
            p.InternalHeldItem = GetItemInternalIdFromDisplay(cbItem.Text); 
            if (string.IsNullOrWhiteSpace(p.InternalHeldItem)) p.InternalHeldItem = "Ninguno"; 
            
            // --- FIX NATURALEZAS SEGURO ---
            string selNat = cbNature.Text.Trim();
            if (!string.IsNullOrEmpty(selNat)) {
                string infallibleNat = GetInfallibleNature(selNat);
                if (!string.IsNullOrEmpty(infallibleNat)) {
                    p.InternalNature = infallibleNat; 
                    string realName = pbs.GetName(pbs.Natures, infallibleNat, infallibleNat);
                    p.Nature = string.IsNullOrEmpty(realName) ? infallibleNat : realName;

                    // INYECCIÓN DIRECTA DE LA NATURALEZA A LA MEMORIA CRUDA
                    SetRealNatureInRuby(p.PersonalID, infallibleNat);
                }
            }
            
            string selAbil = cbAbility.Text.Trim();
            if (!string.IsNullOrEmpty(selAbil)) { 
                p.Ability = selAbil; 
                p.InternalAbility = PokemonUtils.GetInternalId(pbs.Abilities, selAbil, p.InternalAbility, pbs); 
                
                int newSlot = DetectAbilitySlotFromText(p.InternalSpecies, p.Form, selAbil);
                if (newSlot != -1) SetRealAbilityIndex(p.PersonalID, newSlot);
                
                SetRealAbilityNameInRuby(p.PersonalID, p.InternalAbility);
            } 

            for (int i = 0; i < 6; i++) { p.IVs[i] = (int)numIVs[i].Value; p.EVs[i] = (int)numEVs[i].Value; } 
            p.Moves.Clear(); 
            for (int i = 0; i < 4; i++) { 
                if (!string.IsNullOrWhiteSpace(cbMoves[i].Text)) { 
                    p.Moves.Add(new PokemonMove { Name = cbMoves[i].Text, InternalName = PokemonUtils.GetInternalId(pbs.Moves, cbMoves[i].Text, "", pbs), PP = (int)numPPs[i].Value, PPUp = (int)numPPUps[i].Value }); 
                } 
            }
            if (isEditingParty) RefreshPartyGrid(); else RefreshPCGrid();
        } catch (Exception ex) { LogError(ex, "ApplyCurrentEdits"); } finally { isUpdatingUI = false; }
    }

    private void CbBoxSelector_SelectedIndexChanged(object sender, EventArgs e) { 
        if (isUpdatingUI || cbBoxSelector.SelectedIndex < 0 || currentSaveData == null) return; 
        if (!isEditingParty && currentSlotIndex >= 0) ApplyCurrentEdits(); 
        currentBoxIndex = cbBoxSelector.SelectedIndex; 
        RefreshPCGrid(); 
        if (!isEditingParty) ClearEditorUI(); 
    }

    // --- LECTURA DEL POKEMON (CON BUSCADOR INTELIGENTE DE NATURALEZA Y REPARACIÓN) ---
    private void LoadPokemonToEditor() {
        Pokemon p = GetCurrentPokemon(); if (p == null) return; 
        
        isUpdatingUI = true;
        cbAbility.SelectedIndex = -1; cbAbility.Text = "";
        cbItem.SelectedIndex = -1; cbItem.Text = "";
        cbNature.SelectedIndex = -1; cbNature.Text = "";
        for(int i=0; i<4; i++) { cbMoves[i].SelectedIndex = -1; cbMoves[i].Text = ""; }

        btnChangeFormInEditor.Visible = FormDatabase.HasFormsOrParadox(p.InternalSpecies, AppRoot); 
        txtNickname.Text = p.Nickname; 
        numLevel.Value = Math.Min(100, Math.Max(1, p.Level)); 
        
        if (p.IsSuperShiny) {
            p.IsShiny = false; 
            chkSuperShiny.Checked = true;
            chkShiny.Checked = false;
        } else {
            chkSuperShiny.Checked = false;
            chkShiny.Checked = p.IsShiny;
        }
        chkShiny.BackColor = chkShiny.Checked ? Color.Gold : SystemColors.Control; 
        chkSuperShiny.BackColor = chkSuperShiny.Checked ? Color.Plum : SystemColors.Control; 
        
        if (cbGender.Items.Contains(p.Gender)) cbGender.SelectedItem = p.Gender; else cbGender.SelectedIndex = 0; 
        
        // --- LECTURA DIRECTA DE LA NATURALEZA (MENTAS INCLUIDAS) ---
        // --- LECTURA DIRECTA DE LA NATURALEZA (MENTAS INCLUIDAS) ---
        string rubyNature = GetRealNatureFromRuby(p.PersonalID);
        string internalNat = string.IsNullOrEmpty(rubyNature) ? (string.IsNullOrEmpty(p.InternalNature) ? p.Nature : p.InternalNature) : rubyNature;
        
        // 1. Traducimos el ID interno (ej. "JOLLY") a su nombre real en español (ej. "Alegre")
        string realNatureName = pbs.GetName(pbs.Natures, internalNat, internalNat);
        if (string.IsNullOrEmpty(realNatureName)) realNatureName = p.Nature;

        // 2. ¡CRUCIAL! Sincronizamos la memoria interna del Pokémon para que calcule bien los stats al guardar
        p.InternalNature = internalNat;
        p.Nature = realNatureName;

        // 3. Generamos el texto perfecto para el menú: "Alegre (+Velocid., -At. Esp.)"
        string natDisp = PokemonUtils.GetNatureDisplayName(realNatureName, internalNat); 
        if (string.IsNullOrEmpty(natDisp)) natDisp = internalNat;
        
        if (!string.IsNullOrEmpty(natDisp)) {
            int idx = cbNature.FindStringExact(natDisp);
            if (idx != -1) {
                cbNature.SelectedIndex = idx;
            } else {
                bool found = false;
                for (int i = 0; i < cbNature.Items.Count; i++) {
                    if (cbNature.Items[i].ToString().Equals(natDisp, StringComparison.OrdinalIgnoreCase)) {
                        cbNature.SelectedIndex = i; found = true; break;
                    }
                }
                if (!found) {
                    cbNature.Items.Add(natDisp);
                    cbNature.SelectedItem = natDisp;
                }
            }
        }
        UpdateStatColorsByNature(); 
        
        string realItemName = pbs.GetName(pbs.Items, p.InternalHeldItem, p.HeldItem); 
        if (cbItem.Items.Contains(realItemName)) cbItem.SelectedItem = realItemName; else cbItem.Text = realItemName; 
        
        string rubyAbility = GetRealAbilityNameFromRuby(p.PersonalID);
        int rSlot = GetRealAbilityIndex(p.PersonalID);
        
        if (string.IsNullOrWhiteSpace(rubyAbility) || rubyAbility.Contains("Desconocid") || rubyAbility.StartsWith("AUTO")) {
            // Usa el nuevo lector que toma en cuenta la forma
            string randomAbility = GetRandomizedAbility(p.InternalSpecies, p.Form, rSlot);
            
            if (!string.IsNullOrEmpty(randomAbility)) {
                rubyAbility = randomAbility;
            } else {
                string pbsInternal = GetPBSAbility(p.InternalSpecies, p.Form, rSlot, AppRoot);
                if (string.IsNullOrEmpty(pbsInternal)) pbsInternal = GetPBSAbility(p.InternalSpecies, p.Form, 0, AppRoot); 
                rubyAbility = pbsInternal;
            }
        }

        string displayAbil = pbs.GetName(pbs.Abilities, rubyAbility, rubyAbility);
        if (string.IsNullOrWhiteSpace(displayAbil)) displayAbil = p.Ability;
        
        if (cbAbility.Items.Contains(displayAbil)) cbAbility.SelectedItem = displayAbil;
        else cbAbility.Text = displayAbil;
        
        for (int i = 0; i < 6; i++) { numIVs[i].Value = Math.Min(31, Math.Max(0, p.IVs[i])); numEVs[i].Value = Math.Min(252, Math.Max(0, p.EVs[i])); } 
        for (int i = 0; i < 4; i++) { 
            cbMoves[i].Enabled = numPPs[i].Enabled = numPPUps[i].Enabled = btnDescMoves[i].Enabled = true; 
            if (i < p.Moves.Count) { 
                if (cbMoves[i].Items.Contains(p.Moves[i].Name)) cbMoves[i].SelectedItem = p.Moves[i].Name; else cbMoves[i].Text = p.Moves[i].Name; 
                numPPUps[i].Value = p.Moves[i].PPUp; int bp = 10; 
                if (pbs.MovePPs.TryGetValue(PokemonUtils.GetInternalId(pbs.Moves, cbMoves[i].Text, "", pbs), out int pp)) bp = pp; 
                int maxP = bp + (bp * (int)numPPUps[i].Value / 5); numPPs[i].Maximum = Math.Max(1, maxP); numPPs[i].Value = Math.Min(numPPs[i].Maximum, p.Moves[i].PP); 
            } else { 
                numPPs[i].Value = numPPUps[i].Value = 0; 
            } 
        } 
        isUpdatingUI = false; 
        LoadSpriteOnly(); 
        grpEditor.Enabled = true;
    }

    private string GetInfallibleNature(string text) {
        if (string.IsNullOrWhiteSpace(text)) return null;
        if (pbs == null || pbs.Natures == null) return null;
        
        // CORTAMOS LOS PARÉNTESIS PARA LEER SOLO EL NOMBRE
        string cleanText = text.Split('(')[0].Trim(); 

        foreach(var kvp in pbs.Natures) {
            string disp = pbs.GetName(pbs.Natures, kvp.Key, kvp.Key);
            if (!string.IsNullOrEmpty(disp) && disp.Equals(cleanText, StringComparison.OrdinalIgnoreCase)) return kvp.Key;
        }
        foreach(var kvp in pbs.Natures) {
            if (kvp.Key.Equals(cleanText, StringComparison.OrdinalIgnoreCase)) return kvp.Key;
        }
        return null;
    }

    private void LoadSpriteOnly() { 
        Pokemon p = GetCurrentPokemon(); if (p == null) return; 
        if (picSprite.Image != null) picSprite.Image.Dispose(); 
        picSprite.Image = SpriteManager.GetPokemonIcon(AppRoot, p.InternalSpecies, p.Form, chkShiny.Checked || chkSuperShiny.Checked); 
    }

    private void ChangeForm_Click(object sender, EventArgs e) { 
        PictureBox pb = null;
        if (sender is ToolStripItem tsi && tsi.Owner is ContextMenuStrip cms) pb = cms.SourceControl as PictureBox;
        else if (sender is Button) {
            if (currentSlotIndex < 0) return;
            pb = isEditingParty ? partySlots[currentSlotIndex] : pcSlots[currentSlotIndex];
        }
        if (pb == null) return; 
        
        bool isParty = pb.Parent == tabParty || (sender is Button && isEditingParty); 
        int slot = (sender is Button) ? currentSlotIndex : (int)pb.Tag; 
        if (currentSaveData == null) return; 
        
        Pokemon p = isParty ? (slot < currentSaveData.Party.Count ? currentSaveData.Party[slot] : null) : currentSaveData.Boxes[currentBoxIndex].Slots[slot]; 
        if (p == null) return; 

        ApplyCurrentEdits(); 
        
        int originalSlot = GetRealAbilityIndex(p.PersonalID);
        string oldSpecies = p.Species; // Guardamos la especie original para comprobar si tenía mote

        using (ChangeFormDialog formDialog = new ChangeFormDialog(pbs, p)) { 
            if (formDialog.ShowDialog() == DialogResult.OK) { 
                lblStatus.Text = "⏳ Aplicando transformación y guardando..."; lblStatus.ForeColor = Color.DarkOrange; this.Cursor = Cursors.WaitCursor; Application.DoEvents(); 
                try { 
                    p.Form = formDialog.SelectedForm; 
                    if (!string.IsNullOrEmpty(formDialog.SelectedSpeciesInternal)) { 
                        p.InternalSpecies = formDialog.SelectedSpeciesInternal; 
                        p.Species = pbs.GetName(pbs.Species, p.InternalSpecies, p.InternalSpecies); 
                    } 
                    
                    SetRealAbilityIndex(p.PersonalID, originalSlot);
                    ClearAbilityNameInRuby(p.PersonalID);
                    
                    p.InternalAbility = "AUTO_" + originalSlot;
                    p.Ability = ""; 
                    
                    // --- FIX DEL MOTE ---
                    // Si NO tenía mote (su nombre era igual a su especie original), lo actualizamos. 
                    // Si tenía un mote personalizado, lo dejamos intacto.
                    if (string.IsNullOrWhiteSpace(p.Nickname) || p.Nickname.Equals(oldSpecies, StringComparison.OrdinalIgnoreCase)) {
                        p.Nickname = p.Species; 
                    }
                    
                    // Aseguramos que la mega retenga los cambios guardando el objeto directo y recargando
                    SyncAllToRuby(); 
                    List<Pokemon> allMyPokes = new List<Pokemon>();
                    allMyPokes.AddRange(currentSaveData.Party);
                    foreach(var box in currentSaveData.Boxes) { foreach(var pSlot in box.Slots) if (pSlot != null) allMyPokes.Add(pSlot); }
                    ApplyDirectRubyEdits(currentSaveData.RootData, allMyPokes);

                    ReloadFromMemory(); 
                    lblStatus.Text = $"Forma de {p.Nickname} actualizada correctamente."; lblStatus.ForeColor = Color.DarkViolet; 
                } finally { this.Cursor = Cursors.Default; } 
            } 
        } 
    }

    private void BtnLoad_Click(object sender, EventArgs e) { 
        string appDataRoaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData); 
        string[] possibleFolders = { Path.Combine(appDataRoaming, "Pokemon Anil"), Path.Combine(appDataRoaming, "Pokémon Añil"), Path.Combine(appDataRoaming, "PokemonAnil") }; 
        string initialDir = possibleFolders.FirstOrDefault(Directory.Exists) ?? appDataRoaming; 
        using (OpenFileDialog ofd = new OpenFileDialog { Title = "Seleccionar Partida (.rxdata)", Filter = "Partidas (*.rxdata)|*.rxdata|Todos (*.*)|*.*", InitialDirectory = initialDir, RestoreDirectory = true }) { 
            if (ofd.ShowDialog() == DialogResult.OK) { 
                currentSavePath = ofd.FileName; 
                LoadSaveFile(); 
            } 
        } 
    }

    private void LoadSaveFile() {
        lblStatus.Text = "⏳ Cargando partida y leyendo base de datos... ¡Un momento!"; lblStatus.ForeColor = Color.DarkOrange; this.Cursor = Cursors.WaitCursor; Application.DoEvents();
        try {
            ClearEditorUI(); 
            
            SaveParser parser = new SaveParser(currentSavePath, pbs); currentSaveData = parser.ParseSave(); isUpdatingUI = true; 
            
            // --- NUEVO: Extraer MTs Randomizadas antes de cargar la Interfaz ---
            ExtractRandomizedTMs();
            
            string dir = Path.GetDirectoryName(currentSavePath);
            isRandomizedSave = Directory.GetFiles(dir, "*tm_compatibility*.dat").Any() || Directory.GetFiles(dir, "*random*.dat").Any();

            string fName = Path.GetFileNameWithoutExtension(currentSavePath);
            var match = Regex.Match(fName, @"\d+");
            if (match.Success && int.TryParse(match.Value, out int slotNumber)) {
                numSaveSlot.Value = Math.Min(numSaveSlot.Maximum, Math.Max(numSaveSlot.Minimum, slotNumber));
            } else {
                numSaveSlot.Value = 1;
            }
            numSaveSlot.Enabled = true;

            cbBoxSelector.Items.Clear(); foreach (var box in currentSaveData.Boxes) cbBoxSelector.Items.Add($"{box.Name} (Caja {box.BoxIndex})"); 
            foreach (var grid in dgvPockets) grid?.Rows.Clear(); 
            
            foreach (var item in currentSaveData.Bag) { 
                int pId = item.Pocket >= 1 && item.Pocket <= 8 ? item.Pocket : 1; 
                string realName = pbs != null ? pbs.GetName(pbs.Items, item.InternalName, item.Name) : item.Name; 
                dgvPockets[pId]?.Rows.Add(item.InternalName, realName, item.Quantity); 
            }
            
            long safeMoney = GetMoneySafely();
            numMoney.Value = Math.Min(numMoney.Maximum, Math.Max(0m, (decimal)safeMoney));

            if (pbs.IsLoaded) { 
                cbNature.Items.Clear(); foreach (var natKvp in pbs.Natures) { string disp = PokemonUtils.GetNatureDisplayName(natKvp.Value, natKvp.Key); if (!cbNature.Items.Contains(disp)) cbNature.Items.Add(disp); } 
                cbAbility.Items.Clear(); cbAbility.Items.AddRange(pbs.Abilities.Values.Distinct().ToArray()); 
                
                cbItem.Items.Clear(); 
                var allItemsList = pbs.Items.Select(x => pbs.GetName(pbs.Items, x.Key, x.Value)).Distinct().ToArray(); 
                cbItem.Items.AddRange(allItemsList); 
                
                var allMoves = pbs.Moves.Values.Distinct().ToArray(); 
                foreach (var cb in cbMoves) { cb.Items.Clear(); cb.Items.AddRange(allMoves); } 
            }
            
            isUpdatingUI = false; if (cbBoxSelector.Items.Count > 0) cbBoxSelector.SelectedIndex = 0; 
            RefreshPartyGrid(); RefreshPCGrid(); tabBagPockets.SelectedIndex = 0; UpdateBagItemDropdown();
            btnSave.Enabled = true; lblStatus.Text = $"Partida cargada: {Path.GetFileName(currentSavePath)}"; lblStatus.ForeColor = Color.Green;
        } catch (Exception ex) { lblStatus.Text = $"Error al cargar. Revisa errorlog.txt"; lblStatus.ForeColor = Color.Red; btnSave.Enabled = false; LogError(ex, "LoadSaveFile"); } finally { this.Cursor = Cursors.Default; }
    }

    private void BtnSave_Click(object sender, EventArgs e) {
        if (currentSaveData == null || string.IsNullOrEmpty(currentSavePath)) return;
        
        int targetSlot = (int)numSaveSlot.Value;
        string dir = Path.GetDirectoryName(currentSavePath);
        string originalFileName = Path.GetFileName(currentSavePath);
        
        int currentSlot = 1;
        var match = Regex.Match(originalFileName, @"\d+");
        if (match.Success) int.TryParse(match.Value, out currentSlot);

        string newFileName = originalFileName;
        if (match.Success) {
            newFileName = originalFileName.Substring(0, match.Index) + targetSlot.ToString() + originalFileName.Substring(match.Index + match.Length);
        } else {
            newFileName = $"Game_{targetSlot}.rxdata"; 
        }

        string targetPath = Path.Combine(dir, newFileName);

        if (targetSlot != currentSlot) { 
            if (File.Exists(targetPath)) { 
                var res = MessageBox.Show($"Ya existe una partida guardada en la ranura {targetSlot} ({newFileName}).\n\n¿Deseas reemplazarla?", "Atención", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (res == DialogResult.No) { 
                    lblStatus.Text = "Guardado cancelado por el usuario."; 
                    lblStatus.ForeColor = Color.DarkOrange; 
                    return; 
                }
            }
        }

        lblStatus.Text = "⏳ Inyectando cambios en la partida... No cierres la ventana."; lblStatus.ForeColor = Color.DarkOrange; this.Cursor = Cursors.WaitCursor; Application.DoEvents();
        try {
            ApplyCurrentEdits(); currentSaveData.Bag.Clear();
            for (int pId = 1; pId <= 8; pId++) { foreach (DataGridViewRow row in dgvPockets[pId].Rows) { if (row.Cells[0].Value != null) { currentSaveData.Bag.Add(new ItemSlot { InternalName = row.Cells[0].Value.ToString(), Name = row.Cells[1].Value.ToString(), Quantity = Convert.ToInt32(row.Cells[2].Value), Pocket = pId }); } } }
            
            UpdateInternalSaveSlot(currentSaveData.RootData, currentSlot, targetSlot);

            if (targetSlot != currentSlot) {
                try {
                    var datFiles = Directory.GetFiles(dir, $"*Partida_{currentSlot}.dat");
                    foreach (var file in datFiles) {
                        string newFile = file.Replace($"Partida_{currentSlot}.dat", $"Partida_{targetSlot}.dat");
                        File.Copy(file, newFile, true);
                    }
                    string tmNuevo = Path.Combine(dir, $"tm_compatibility_Partida_{targetSlot}.dat");
                    if (!File.Exists(tmNuevo)) {
                        string tmBase = Path.Combine(dir, "tm_compatibility.dat");
                        if (File.Exists(tmBase)) File.Copy(tmBase, tmNuevo, true);
                    }
                } catch (Exception ex) { LogError(ex, "Copiando archivos .dat"); }
            }

            SyncAllToRuby(); 
            
            // APLICAMOS LA MAGIA DIRECTAMENTE A LOS OBJETOS RUBY PARA QUE NO SE SOBREESCRIBA
            List<Pokemon> allMyPokes = new List<Pokemon>();
            allMyPokes.AddRange(currentSaveData.Party);
            foreach(var box in currentSaveData.Boxes) {
                foreach(var p in box.Slots) if (p != null) allMyPokes.Add(p);
            }
            ApplyDirectRubyEdits(currentSaveData.RootData, allMyPokes);

            currentSavePath = targetPath;
            SaveWriter writer = new SaveWriter(currentSavePath, currentSaveData.RootData, pbs); writer.SyncBag(currentSaveData.Bag); 
            SyncMoneySafely((long)numMoney.Value);

            // LIMPIADOR DE "DESCONOCIDAS" JUSTO ANTES DE GENERAR EL ARCHIVO
            var pkmObjs = FindAllObjectsByClassName(currentSaveData.RootData, "PokeBattle_Pokemon");
            if (pkmObjs.Count == 0) pkmObjs = FindAllObjectsByClassName(currentSaveData.RootData, "Pokemon");
            foreach (dynamic ro in pkmObjs) {
                if (ro.Attributes != null && ro.Attributes.ContainsKey("@ability")) {
                    string abStr = Unwrap(ro.Attributes["@ability"])?.ToString() ?? "";
                    if (abStr.Contains("Desconocid") || string.IsNullOrWhiteSpace(abStr) || abStr.StartsWith("AUTO") || abStr.StartsWith("Auto:")) {
                        ro.Attributes.Remove("@ability"); 
                    }
                }
            }
            
            if (writer.Save()) { lblStatus.Text = $"¡Partida guardada correctamente como {newFileName}!"; lblStatus.ForeColor = Color.Blue; }
        } catch (Exception ex) { lblStatus.Text = $"Error crítico al guardar. Revisa errorlog.txt"; lblStatus.ForeColor = Color.Red; LogError(ex, "BtnSave_Click"); } finally { this.Cursor = Cursors.Default; }
    }

    private void SyncAllToRuby() { SaveWriter writer = new SaveWriter(currentSavePath, currentSaveData.RootData, pbs); for (int i = 0; i < currentSaveData.Party.Count; i++) writer.SyncPokemon(currentSaveData.Party[i], true, 0, i); foreach (var box in currentSaveData.Boxes) for (int i = 0; i < 30; i++) if (box.Slots[i] != null) writer.SyncPokemon(box.Slots[i], false, box.BoxIndex - 1, i); }
    private void ReloadFromMemory() { isUpdatingUI = true; SaveParser parser = new SaveParser(currentSavePath, pbs); currentSaveData = parser.ParseSave(currentSaveData.RootData); isUpdatingUI = false; RefreshPartyGrid(); RefreshPCGrid(); LoadPokemonToEditor(); }
    private void LogError(Exception ex, string context = "") { try { using (StreamWriter sw = new StreamWriter(Path.Combine(AppRoot, "errorlog.txt"), true)) sw.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR: {context}\n{ex.ToString()}\n{new string('-', 50)}"); } catch { } }

    private string GetRandomizedAbility(string internalSpecies, int form, int abilityIndex) {
        if (currentSaveData == null || currentSaveData.RootData == null) return null;
        try {
            string[] keysToTry = form > 0 ? new[] { $"{internalSpecies}_{form}", internalSpecies } : new[] { internalSpecies };
            
            if (currentSaveData.RootData is IDictionary rootDict) {
                object randDataRaw = null;
                foreach (DictionaryEntry kvp in rootDict) {
                    if (Unwrap(kvp.Key)?.ToString() == "randomized_data") { randDataRaw = Unwrap(kvp.Value); break; }
                }
                
                if (randDataRaw is IDictionary randDict) {
                    object absRaw = null;
                    foreach (DictionaryEntry kvp in randDict) {
                        if (Unwrap(kvp.Key)?.ToString() == "abilities") { absRaw = Unwrap(kvp.Value); break; }
                    }
                    
                    if (absRaw is IDictionary absDict) {
                        object pokeAbsRaw = null;
                        foreach(string searchKey in keysToTry) {
                            foreach (DictionaryEntry kvp in absDict) {
                                if (Unwrap(kvp.Key)?.ToString().Equals(searchKey, StringComparison.OrdinalIgnoreCase) == true) {
                                    pokeAbsRaw = Unwrap(kvp.Value); break;
                                }
                            }
                            if (pokeAbsRaw != null) break;
                        }
                        
                        if (pokeAbsRaw is IDictionary pokeDict) {
                            string targetKey = (abilityIndex >= 2) ? "hidden" : "base";
                            int listIndex = (abilityIndex >= 2) ? (abilityIndex - 2) : abilityIndex; 
                            
                            object targetListRaw = null;
                            foreach (DictionaryEntry kvp in pokeDict) {
                                if (Unwrap(kvp.Key)?.ToString() == targetKey) { targetListRaw = Unwrap(kvp.Value); break; }
                            }
                            
                            if (targetListRaw is IList list) {
                                if (listIndex >= list.Count && list.Count > 0) listIndex = 0;
                                if (listIndex < list.Count) {
                                    return Unwrap(list[listIndex])?.ToString()?.Replace(":", "")?.Replace("@", "")?.Trim();
                                }
                            }
                        }
                    }
                }
            }
        } catch {}
        return null; 
    }

    private void BtnAutoAbilities_Click(object sender, EventArgs e) {
        Pokemon p = GetCurrentPokemon();
        if (p == null) return;
        
        ContextMenuStrip ctx = new ContextMenuStrip();
        
        string ab0 = GetAbilityForSlot(p, 0);
        string ab1 = GetAbilityForSlot(p, 1);
        string ab2 = GetAbilityForSlot(p, 2); 
        
        // Seguro de vida por si la ranura principal falla por algún motivo
        if (string.IsNullOrEmpty(ab0)) {
            ab0 = pbs.GetName(pbs.Abilities, GetPBSAbility(p.InternalSpecies, p.Form, 0, AppRoot), "Desconocida");
        }
        
        ctx.Items.Add($"Ranura 1: {ab0}").Click += (s, ev) => { cbAbility.Text = ab0; ApplyCurrentEdits(); };
        
        if (!string.IsNullOrEmpty(ab1) && ab1 != ab0) 
            ctx.Items.Add($"Ranura 2: {ab1}").Click += (s, ev) => { cbAbility.Text = ab1; ApplyCurrentEdits(); };
            
        if (!string.IsNullOrEmpty(ab2) && ab2 != ab0 && ab2 != ab1) 
            ctx.Items.Add($"Oculta: {ab2}").Click += (s, ev) => { cbAbility.Text = ab2; ApplyCurrentEdits(); };
        
        Button btn = sender as Button;
        ctx.Show(btn, new Point(0, btn.Height));
    }

    private string GetAbilityForSlot(Pokemon p, int slotIndex) {
        // 1. Buscamos si el randomizer le asignó una habilidad a esta ranura específica
        string ab = GetRandomizedAbility(p.InternalSpecies, p.Form, slotIndex);
        
        // 2. Si el randomizer no le dio nada, buscamos si el Pokémon tiene esta ranura de forma oficial (PBS)
        if (string.IsNullOrEmpty(ab)) {
            ab = GetPBSAbility(p.InternalSpecies, p.Form, slotIndex, AppRoot);
        }
        
        // FIX: Si después de eso sigue vacía, el Pokémon NO tiene esta ranura (ej. Slaking no tiene oculta). 
        // Devolvemos nulo para que el menú no intente rellenarlo con basura.
        if (string.IsNullOrEmpty(ab)) return null;
        
        return pbs.GetName(pbs.Abilities, ab, ab);
    }

    private string GetRubyString(object obj) {
        object val = Unwrap(obj);
        if (val == null) return "";
        
        // Si el juego guardó la estructura completa del Movimiento (RubyObject), lo abrimos y sacamos su @id
        if (val is RubyObject ro && ro.Attributes != null) {
            if (ro.Attributes.ContainsKey("@id")) {
                val = Unwrap(ro.Attributes["@id"]);
            }
        }
        
        return val?.ToString()?.Replace(":", "")?.Replace("@", "")?.Trim() ?? "";
    }

    private object FindAttributeInRubyDeep(object node, string targetAttr, HashSet<object> visited = null) {
        visited ??= new HashSet<object>();
        if (node == null || visited.Contains(node)) return null;
        visited.Add(node);

        node = Unwrap(node);
        if (node == null) return null;

        if (node is RubyObject ro && ro.Attributes != null) {
            if (ro.Attributes.ContainsKey(targetAttr)) return Unwrap(ro.Attributes[targetAttr]);
            foreach (var val in ro.Attributes.Values) {
                var found = FindAttributeInRubyDeep(val, targetAttr, visited);
                if (found != null) return found;
            }
        } else if (node is IList list) {
            foreach (var item in list) {
                var found = FindAttributeInRubyDeep(item, targetAttr, visited);
                if (found != null) return found;
            }
        } else if (node is IDictionary dict) {
            if (dict.Contains(targetAttr)) return Unwrap(dict[targetAttr]);
            foreach (DictionaryEntry kvp in dict) {
                string k = GetRubyString(kvp.Key);
                if (k == targetAttr || "@" + k == targetAttr) return Unwrap(kvp.Value);
                var found = FindAttributeInRubyDeep(kvp.Value, targetAttr, visited);
                if (found != null) return found;
            }
        }
        return null;
    }

    private void ExtractRandomizedTMs() {
        pbs.RandomizedItemMoves.Clear();
        if (currentSaveData == null || currentSaveData.RootData == null) return;
        
        try {
            // Radiografía directa al interior de la partida (.rxdata)
            object tmMapRaw = FindAttributeInRubyDeep(currentSaveData.RootData, "@tm_move_map");
            object randomMovesRaw = FindAttributeInRubyDeep(currentSaveData.RootData, "@random_moves");
            
            if (tmMapRaw is IDictionary tmMap) {
                foreach (DictionaryEntry kvp in tmMap) {
                    try {
                        string tmItem = GetRubyString(kvp.Key).ToUpper();
                        string tmMove = GetRubyString(kvp.Value).ToUpper();
                        if (!string.IsNullOrEmpty(tmItem) && !string.IsNullOrEmpty(tmMove) && tmMove != "RUBYOBJECT") {
                            pbs.RandomizedItemMoves[tmItem] = tmMove;
                            if (tmItem.StartsWith("TM")) pbs.RandomizedItemMoves["MT" + tmItem.Substring(2)] = tmMove;
                            if (tmItem.StartsWith("MT")) pbs.RandomizedItemMoves["TM" + tmItem.Substring(2)] = tmMove;
                        }
                    } catch { } 
                }
            } else if (tmMapRaw is IList tmArray) {
                for (int i = 1; i < tmArray.Count; i++) {
                    try {
                        string tmMove = GetRubyString(tmArray[i]).ToUpper();
                        if (!string.IsNullOrEmpty(tmMove) && tmMove != "RUBYOBJECT") {
                            string id2 = i.ToString("D2");
                            string id3 = i.ToString("D3");
                            pbs.RandomizedItemMoves["MT" + id2] = tmMove;
                            pbs.RandomizedItemMoves["TM" + id2] = tmMove;
                            pbs.RandomizedItemMoves["MT" + id3] = tmMove;
                            pbs.RandomizedItemMoves["TM" + id3] = tmMove;
                        }
                    } catch { }
                }
            }
            
            if (randomMovesRaw is IDictionary randomMovesDict) {
                Dictionary<string, string> globalRandomMoves = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (DictionaryEntry kvp in randomMovesDict) {
                    try {
                        string originalMove = GetRubyString(kvp.Key).ToUpper();
                        string newMove = GetRubyString(kvp.Value).ToUpper();
                        if (!string.IsNullOrEmpty(originalMove) && !string.IsNullOrEmpty(newMove) && newMove != "RUBYOBJECT") {
                            globalRandomMoves[originalMove] = newMove;
                        }
                    } catch { }
                }
                
                foreach (var kvp in pbs.ItemMoves) {
                    string tmItem = kvp.Key.ToUpper();
                    string originalTmMove = kvp.Value.ToUpper();
                    
                    if (!pbs.RandomizedItemMoves.ContainsKey(tmItem) && globalRandomMoves.ContainsKey(originalTmMove)) {
                        pbs.RandomizedItemMoves[tmItem] = globalRandomMoves[originalTmMove];
                        if (tmItem.StartsWith("TM")) pbs.RandomizedItemMoves["MT" + tmItem.Substring(2)] = globalRandomMoves[originalTmMove];
                        if (tmItem.StartsWith("MT")) pbs.RandomizedItemMoves["TM" + tmItem.Substring(2)] = globalRandomMoves[originalTmMove];
                    }
                }
            }
        } catch { }
        
        FixMissingRandomizedTMs(); 
    }

    private void FixMissingRandomizedTMs() {
        bool isRandomized = pbs.RandomizedItemMoves.Count > 0;
        if (!isRandomized) return;

        var allMoves = pbs.Moves.Keys.ToList();
        HashSet<string> usedMoves = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var move in pbs.RandomizedItemMoves.Values) { usedMoves.Add(move); }
        
        List<string> unusedMoves = allMoves.Where(m => !usedMoves.Contains(m)).ToList();
        
        int seed = 0;
        try {
            var trainer = FindObjectByClassName(currentSaveData.RootData, "PokeBattle_Trainer") ?? FindObjectByClassName(currentSaveData.RootData, "Player");
            if (trainer != null && trainer.Attributes != null && trainer.Attributes.ContainsKey("@id")) {
                seed = SafeGetInt(Unwrap(trainer.Attributes["@id"]));
            }
        } catch {}
        
        Random rnd = new Random(seed); 
        unusedMoves = unusedMoves.OrderBy(x => rnd.Next()).ToList();
        
        int missingIndex = 0;
        
        foreach (var kvp in pbs.ItemMoves.ToList()) {
            string exactKey = kvp.Key; 
            string exactKeyUpper = exactKey.ToUpper();

            // --- EL SEGURO DE VIDA ANTI-MOs ---
            // Si la MT ya tiene su ataque oficial extraído del juego, prohibimos que la sobrescriban
            var match = System.Text.RegularExpressions.Regex.Match(exactKeyUpper, @"\d+");
            if (match.Success) {
                string numPart = match.Value;
                if (pbs.RandomizedItemMoves.ContainsKey("MT" + numPart) || pbs.RandomizedItemMoves.ContainsKey("TM" + numPart)) {
                    continue; // ¡Salvada! Saltamos al siguiente objeto.
                }
            }

            if (!pbs.RandomizedItemMoves.ContainsKey(exactKey) && !pbs.RandomizedItemMoves.ContainsKey(exactKeyUpper)) {
                string newMove = (missingIndex < unusedMoves.Count) ? unusedMoves[missingIndex++] : allMoves[rnd.Next(allMoves.Count)];
                
                pbs.RandomizedItemMoves[exactKey] = newMove;
                pbs.RandomizedItemMoves[exactKeyUpper] = newMove;
                
                if (match.Success) {
                    string numPart = match.Value;
                    pbs.RandomizedItemMoves["MT" + numPart] = newMove;
                    pbs.RandomizedItemMoves["TM" + numPart] = newMove;
                    InjectTMIntoRuby("MT" + numPart, newMove);
                    InjectTMIntoRuby("TM" + numPart, newMove);
                }
            }
        }
    }

    private void InjectTMIntoRuby(string tmItem, string newMove) {
        try {
            if (currentSaveData == null || currentSaveData.RootData == null) return;
            if (currentSaveData.RootData is IDictionary dict) {
                object globalMetaRaw = null;
                foreach (DictionaryEntry kvp in dict) {
                    if (Unwrap(kvp.Key)?.ToString() == "global_metadata") { globalMetaRaw = Unwrap(kvp.Value); break; }
                }
                if (globalMetaRaw is RubyObject globalMeta && globalMeta.Attributes != null) {
                    if (!globalMeta.Attributes.ContainsKey("@tm_move_map")) {
                        globalMeta.Attributes["@tm_move_map"] = new Dictionary<object, object>();
                    }
                    if (Unwrap(globalMeta.Attributes["@tm_move_map"]) is IDictionary tmMap) {
                        tmMap[new RubySymbol(tmItem)] = new RubySymbol(newMove);
                    }
                }
            }
        } catch { }
    }

    private void ExtractFromTmMap(object tmMapRaw) {
        if (tmMapRaw is IDictionary tmMap) {
            foreach (DictionaryEntry kvp in tmMap) {
                try {
                    string tmItem = GetRubyString(kvp.Key).ToUpper();
                    string tmMove = GetRubyString(kvp.Value).ToUpper();
                    if (!string.IsNullOrEmpty(tmItem) && !string.IsNullOrEmpty(tmMove) && tmMove != "RUBYOBJECT") {
                        pbs.RandomizedItemMoves[tmItem] = tmMove;
                        if (tmItem.StartsWith("TM")) pbs.RandomizedItemMoves["MT" + tmItem.Substring(2)] = tmMove;
                        if (tmItem.StartsWith("MT")) pbs.RandomizedItemMoves["TM" + tmItem.Substring(2)] = tmMove;
                    }
                } catch { } 
            }
        }
        else if (tmMapRaw is IList tmArray) {
            for (int i = 1; i < tmArray.Count; i++) {
                try {
                    string tmMove = GetRubyString(tmArray[i]).ToUpper();
                    if (!string.IsNullOrEmpty(tmMove) && tmMove != "RUBYOBJECT") {
                        string id2 = i.ToString("D2");
                        string id3 = i.ToString("D3");
                        pbs.RandomizedItemMoves["MT" + id2] = tmMove;
                        pbs.RandomizedItemMoves["TM" + id2] = tmMove;
                        pbs.RandomizedItemMoves["MT" + id3] = tmMove;
                        pbs.RandomizedItemMoves["TM" + id3] = tmMove;
                    }
                } catch { }
            }
        }
    }

    private bool InjectIntoRootNode(object rootNode, string tmItem, string newMove) {
        bool changed = false;
        try {
            string numPart = System.Text.RegularExpressions.Regex.Match(tmItem, @"\d+").Value;
            if (string.IsNullOrEmpty(numPart)) numPart = tmItem.Replace("MT", "").Replace("TM", "");

            if (rootNode is IDictionary dict) {
                // A: global_metadata -> @tm_move_map
                object globalMetaRaw = null;
                foreach (DictionaryEntry kvp in dict) {
                    if (Unwrap(kvp.Key)?.ToString() == "global_metadata") { globalMetaRaw = Unwrap(kvp.Value); break; }
                }
                if (globalMetaRaw is RubyObject globalMeta && globalMeta.Attributes != null) {
                    if (!globalMeta.Attributes.ContainsKey("@tm_move_map")) {
                        globalMeta.Attributes["@tm_move_map"] = new Dictionary<object, object>();
                    }
                    if (Unwrap(globalMeta.Attributes["@tm_move_map"]) is IDictionary tmMap) {
                        tmMap[new RubySymbol("MT" + numPart)] = new RubySymbol(newMove);
                        tmMap[new RubySymbol("TM" + numPart)] = new RubySymbol(newMove);
                        changed = true;
                    }
                }
                
                // B: Es un hash directo (.dat file format)
                bool isTmDict = false;
                foreach (DictionaryEntry kvp in dict) {
                    string keyStr = GetRubyString(kvp.Key).ToUpper();
                    if (keyStr.StartsWith("TM") || keyStr.StartsWith("MT") || keyStr.StartsWith("ITEM_TM")) {
                        isTmDict = true; break;
                    }
                }
                if (isTmDict) {
                    // Metemos el ataque en todos los formatos posibles para que el juego no pueda escapar
                    dict[new RubySymbol("MT" + numPart)] = new RubySymbol(newMove);
                    dict[new RubySymbol("TM" + numPart)] = new RubySymbol(newMove);
                    dict[new RubySymbol("ITEM_MT" + numPart)] = new RubySymbol(newMove);
                    dict[new RubySymbol("ITEM_TM" + numPart)] = new RubySymbol(newMove);
                    
                    dict["MT" + numPart] = newMove;
                    dict["TM" + numPart] = newMove;
                    changed = true;
                }
            }
        } catch { }
        return changed;
    }

    private void ChangeTMMove(string tmInternalId, string currentDisplayName, int rowIndex) {
        if (!pbs.IsLoaded) return;

        using (Form form = new Form { Text = "Personalizar MT", Size = new Size(320, 290), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false }) {
            
            // 1. Obtenemos el ataque actual PRIMERO
            string currentMoveInternal = pbs.RandomizedItemMoves.ContainsKey(tmInternalId) ? pbs.RandomizedItemMoves[tmInternalId] : (pbs.ItemMoves.ContainsKey(tmInternalId) ? pbs.ItemMoves[tmInternalId] : "");
            string currentMoveName = pbs.Moves.ContainsKey(currentMoveInternal) ? pbs.Moves[currentMoveInternal] : "Ninguno";

            // 2. Colocamos el ataque actual en el título (Label) en vez de en la caja de texto
            Label lbl = new Label { Text = $"Elige un ataque para {tmInternalId}\n(Actual: {currentMoveName}):", Location = new Point(15, 5), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            
            // 3. La caja de texto ahora inicia 100% vacía
            TextBox txtSearch = new TextBox { Location = new Point(15, 45), Size = new Size(270, 25) };
            
            ListBox lbResults = new ListBox { Location = new Point(15, 75), Size = new Size(270, 110) };
            
            var allMovesRaw = pbs.Moves.Values.Distinct().OrderBy(m => m).ToList();
            lbResults.Items.AddRange(allMovesRaw.ToArray());
            
            txtSearch.TextChanged += (s, e) => {
                string search = txtSearch.Text;
                lbResults.Items.Clear();
                if (string.IsNullOrWhiteSpace(search)) {
                    lbResults.Items.AddRange(allMovesRaw.ToArray());
                } else {
                    var fil = allMovesRaw.Where(x => x.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
                    lbResults.Items.AddRange(fil);
                }
                if (lbResults.Items.Count > 0) lbResults.SelectedIndex = 0;
            };
            
            txtSearch.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Down && lbResults.SelectedIndex < lbResults.Items.Count - 1) {
                    lbResults.SelectedIndex++;
                    e.Handled = true;
                } else if (e.KeyCode == Keys.Up && lbResults.SelectedIndex > 0) {
                    lbResults.SelectedIndex--;
                    e.Handled = true;
                }
            };

            Button btnOk = new Button { Text = "💉 Inyectar", Location = new Point(90, 200), Size = new Size(130, 30), DialogResult = DialogResult.OK, BackColor = Color.LightGreen, Cursor = Cursors.Hand };
            
            lbResults.DoubleClick += (s, e) => { if (lbResults.SelectedItem != null) btnOk.PerformClick(); };

            form.Controls.Add(lbl); form.Controls.Add(txtSearch); form.Controls.Add(lbResults); form.Controls.Add(btnOk);
            form.AcceptButton = btnOk; 

            // Enfocar automáticamente la caja de texto vacía al abrir la ventana
            form.Shown += (s, e) => { txtSearch.Focus(); };

            if (form.ShowDialog() == DialogResult.OK && lbResults.SelectedItem != null) {
                string selectedMoveName = lbResults.SelectedItem.ToString();
                string selectedMoveInternal = PokemonUtils.GetInternalId(pbs.Moves, selectedMoveName, "", pbs);
                
                if (!string.IsNullOrEmpty(selectedMoveInternal)) {
                    pbs.RandomizedItemMoves[tmInternalId] = selectedMoveInternal;
                    
                    string numPart = System.Text.RegularExpressions.Regex.Match(tmInternalId, @"\d+").Value;
                    if (!string.IsNullOrEmpty(numPart)) {
                        pbs.RandomizedItemMoves["MT" + numPart] = selectedMoveInternal;
                        pbs.RandomizedItemMoves["TM" + numPart] = selectedMoveInternal;
                        InjectTMIntoRuby("MT" + numPart, selectedMoveInternal);
                        InjectTMIntoRuby("TM" + numPart, selectedMoveInternal);
                    } else {
                        InjectTMIntoRuby(tmInternalId, selectedMoveInternal);
                    }
                    
                    dgvPockets[4].Rows[rowIndex].Cells[1].Value = pbs.GetName(pbs.Items, tmInternalId, tmInternalId);
                    UpdateBagItemDropdown();
                    MessageBox.Show($"¡{tmInternalId} ahora enseñará '{selectedMoveName}'!", "Inyectado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
    }

    private string GetRealNatureFromRuby(long personalId) {
        if (currentSaveData == null || currentSaveData.RootData == null) return null;
        var pkmObjects = FindAllObjectsByClassName(currentSaveData.RootData, "PokeBattle_Pokemon");
        if (pkmObjects.Count == 0) pkmObjects = FindAllObjectsByClassName(currentSaveData.RootData, "Pokemon");
        foreach (dynamic ro in pkmObjects) {
            if (ro.Attributes != null) {
                object pidObj = ro.Attributes.ContainsKey("@personalID") ? ro.Attributes["@personalID"] : 
                               (ro.Attributes.ContainsKey("@personal_id") ? ro.Attributes["@personal_id"] : 
                               (ro.Attributes.ContainsKey("@pid") ? ro.Attributes["@pid"] : null));
                if (pidObj != null && SafeGetLong(Unwrap(pidObj)) == personalId) {
                    
                    // 1. Prioridad Absoluta: Naturaleza por Mentas
                    if (ro.Attributes.ContainsKey("@nature_for_stats") && ro.Attributes["@nature_for_stats"] != null) {
                        string mint = Unwrap(ro.Attributes["@nature_for_stats"]).ToString().Replace(":", "").Trim();
                        if (!string.IsNullOrEmpty(mint) && mint != "RUBYOBJECT") return mint;
                    }
                    if (ro.Attributes.ContainsKey("@calc_nature") && ro.Attributes["@calc_nature"] != null) {
                        string calc = Unwrap(ro.Attributes["@calc_nature"]).ToString().Replace(":", "").Trim();
                        if (!string.IsNullOrEmpty(calc) && calc != "RUBYOBJECT") return calc;
                    }
                    
                    // 2. Naturaleza de Nacimiento
                    if (ro.Attributes.ContainsKey("@nature") && ro.Attributes["@nature"] != null) {
                        string baseNat = Unwrap(ro.Attributes["@nature"]).ToString().Replace(":", "").Trim();
                        if (!string.IsNullOrEmpty(baseNat) && baseNat != "RUBYOBJECT") return baseNat;
                    }
                }
            }
        }
        return null;
    }

    private void SetRealNatureInRuby(long personalId, string newNatureInternal) {
        if (currentSaveData == null || currentSaveData.RootData == null || string.IsNullOrEmpty(newNatureInternal)) return;
        var pkmObjects = FindAllObjectsByClassName(currentSaveData.RootData, "PokeBattle_Pokemon");
        if (pkmObjects.Count == 0) pkmObjects = FindAllObjectsByClassName(currentSaveData.RootData, "Pokemon");
        foreach (dynamic ro in pkmObjects) {
            if (ro.Attributes != null) {
                object pidObj = ro.Attributes.ContainsKey("@personalID") ? ro.Attributes["@personalID"] : 
                               (ro.Attributes.ContainsKey("@personal_id") ? ro.Attributes["@personal_id"] : 
                               (ro.Attributes.ContainsKey("@pid") ? ro.Attributes["@pid"] : null));
                if (pidObj != null && SafeGetLong(Unwrap(pidObj)) == personalId) {
                    
                    object finalVal = new RubySymbol(newNatureInternal);
                    
                    // Sobrescribimos tanto el nacimiento como las mentas para que el juego obedezca al 100%
                    ro.Attributes["@nature"] = finalVal;
                    if (ro.Attributes.ContainsKey("@nature_for_stats")) ro.Attributes["@nature_for_stats"] = finalVal;
                    if (ro.Attributes.ContainsKey("@calc_nature")) ro.Attributes["@calc_nature"] = finalVal;
                }
            }
        }
    }

}