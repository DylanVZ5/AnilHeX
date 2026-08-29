using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

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

        foreach (var group in FormDatabase.ParadoxGroups) {
            if (group.Contains(currentSpc)) {
                currentGroup = group;
                break;
            }
        }

        // 1. Añadimos las Paradojas si las tiene
        if (currentGroup != null) 
        {
            foreach (string spc in currentGroup) 
            {
                string name = pbs.GetName(pbs.Species, spc, spc);
                // Solo marcamos las que son de otra especie como Paradoja
                string suffix = (spc != p.InternalSpecies) ? " (Variante/Paradoja)" : "";
                cbOptions.Items.Add(new FormOption { 
                    DisplayName = name + suffix, 
                    FormIndex = 0, 
                    SpeciesInternal = spc 
                });
            }
        } 
        else 
        {
            // Si no tiene paradojas, agregamos su forma base normal
            cbOptions.Items.Add(new FormOption { 
                DisplayName = "Forma Base", 
                FormIndex = 0, 
                SpeciesInternal = currentSpc 
            });
        }

        // 2. ESCANEAMOS MEGAS Y FORMAS EXTRAS
        string basePath = Path.Combine(MainForm.AppRoot, "Graphics", "Pokemon", "Icons");
        int maxFormsToScan = 30; 

        for (int i = 1; i <= maxFormsToScan; i++)
        {
            if (File.Exists(Path.Combine(basePath, $"{currentSpc}_{i}.png")) || 
                File.Exists(Path.Combine(basePath, $"{currentSpc}_{i}_s.png")) || 
                File.Exists(Path.Combine(basePath, $"{currentSpc}_{i}s.png")))
            {
                cbOptions.Items.Add(new FormOption { 
                    DisplayName = FormDatabase.GetFormName(currentSpc, i), 
                    FormIndex = i, 
                    SpeciesInternal = currentSpc 
                });
            }
        }

        // 3. Etiquetar EXACTAMENTE la forma actual y seleccionarla
        int selectedIdx = 0;
        for(int i = 0; i < cbOptions.Items.Count; i++) {
            var opt = (FormOption)cbOptions.Items[i];
            
            // Verificamos que coincida tanto la especie como el ID de la forma
            if (opt.SpeciesInternal == p.InternalSpecies && opt.FormIndex == p.Form) {
                opt.DisplayName += " (Actual)"; // Le pegamos la etiqueta a la forma exacta
                selectedIdx = i;
                break;
            }
        }
        if (cbOptions.Items.Count > 0) cbOptions.SelectedIndex = selectedIdx;

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