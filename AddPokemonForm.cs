using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

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