using System;
using System.Drawing;
using System.Windows.Forms;

public class InfoForm : Form
{
    public InfoForm(Pokemon p, Image sprite, PBSReader pbs)
    {
        this.Text = $"Datos de {p.Nickname}";
        this.Size = new Size(400, 560);
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

        string realNature = pbs.GetName(pbs.Natures, p.InternalNature, p.Nature);
        string realBall = pbs.GetName(pbs.Items, p.PokeBall, p.PokeBall);
        if (string.IsNullOrWhiteSpace(realBall)) realBall = "Poké Ball";

        DataGridView dgv = new DataGridView 
        { 
            Location = new Point(20, 100), 
            Size = new Size(345, 400), 
            ReadOnly = true, 
            AllowUserToAddRows = false, 
            AllowUserToResizeRows = false,
            AllowUserToResizeColumns = false,
            RowHeadersVisible = false, 
            ColumnHeadersVisible = false,
            BackgroundColor = Color.White, 
            BorderStyle = BorderStyle.None, 
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            GridColor = Color.LightGray,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            Font = new Font("Segoe UI", 10F),
            ScrollBars = ScrollBars.Vertical
        };

        dgv.DefaultCellStyle.SelectionBackColor = Color.White;
        dgv.DefaultCellStyle.SelectionForeColor = Color.Black;

        dgv.Columns.Add("Prop", "Prop");
        dgv.Columns.Add("Val", "Val");
        
        dgv.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;

        dgv.Rows.Add("Especie", $"{p.Species} (ID: {p.PersonalID})");
        dgv.Rows.Add("Forma Activa", FormDatabase.GetFormName(p.InternalSpecies, p.Form));
        dgv.Rows.Add("Habilidad", p.Ability);
        dgv.Rows.Add("Naturaleza", realNature);
        dgv.Rows.Add("Objeto Equipo", string.IsNullOrEmpty(p.HeldItem) ? "Ninguno" : p.HeldItem);
        dgv.Rows.Add("Poké Ball", realBall);
        dgv.Rows.Add("Felicidad", $"{p.Happiness} / 255");
        dgv.Rows.Add("Experiencia", $"{p.Exp:N0} EXP");
        dgv.Rows.Add("Forma Shiny", p.IsShiny ? "⭐ Sí" : "No");
        dgv.Rows.Add("Super Shiny", p.IsSuperShiny ? "🌟 Sí (Radiante)" : "No");
        
        int sepIdx = dgv.Rows.Add("[ DATOS DE CAPTURA ]", "");
        dgv.Rows[sepIdx].DefaultCellStyle.BackColor = Color.WhiteSmoke;
        dgv.Rows[sepIdx].DefaultCellStyle.ForeColor = Color.DimGray;
        dgv.Rows[sepIdx].DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);

        dgv.Rows.Add("Nivel Obtención", $"Nv. {p.ObtainLevel}");
        dgv.Rows.Add("Mapa Obtención", p.ObtainMap);
        if (!string.IsNullOrEmpty(p.ObtainText)) dgv.Rows.Add("Nota", p.ObtainText);

        foreach (DataGridViewRow row in dgv.Rows) {
            row.Cells[0].Style.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            row.Cells[0].Style.ForeColor = Color.DarkSlateGray;
            row.Height = 28;
        }
        dgv.Rows[sepIdx].Height = 24; 

        dgv.ClearSelection();
        this.Controls.Add(dgv);
    }
}