using IKO_ZeugnisManager_Software.Helpers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;

namespace IKO_ZeugnisManager_Software
{
    public partial class FormFormulierungen : Form
    {
        private readonly TextBox hedefTextBox;
        private readonly string alanAdi;
        private readonly string adSoyad;

        private ListView lstCumleler;
        private ListBox lstSecilenler;
        private ComboBox cmbRating;

        private Button btnGegenwart;
        private Button btnVergangenheit;

        private bool isPresent = true;

        public FormFormulierungen(string alanAdi, TextBox hedefTextBox, string adSoyad)
        {
            this.alanAdi = alanAdi;
            this.hedefTextBox = hedefTextBox;
            this.adSoyad = adSoyad;

            InitializeComponent();
            InitLayout();
            FormuDoldur();
        }

        private void InitLayout()
        {
            this.Text = $"Formulierungen für: {alanAdi}";
            this.Size = new Size(1100, 800);
            this.StartPosition = FormStartPosition.CenterParent;

            var txtAd = new TextBox
            {
                Left = 20,
                Top = 20,
                Width = 1000,
                Font = new Font("Segoe UI", 15),
                Text = adSoyad,
                ReadOnly = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            this.Controls.Add(txtAd);

            btnGegenwart = new Button
            {
                Text = "Gegenwart",
                Width = 450,
                Height = 50,
                Left = 20,
                Top = txtAd.Bottom + 10,
                BackColor = Color.HotPink,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            btnGegenwart.Click += (s, e) => { isPresent = true; UpdateTenseButtons(); FormuDoldur(); };

            btnVergangenheit = new Button
            {
                Text = "Vergangenheit",
                Width = 450,
                Height = 50,
                Left = btnGegenwart.Right + 20,
                Top = btnGegenwart.Top,
                BackColor = Color.LightGray,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            btnVergangenheit.Click += (s, e) => { isPresent = false; UpdateTenseButtons(); FormuDoldur(); };

            this.Controls.Add(btnGegenwart);
            this.Controls.Add(btnVergangenheit);

            cmbRating = new ComboBox
            {
                Left = 20,
                Top = btnVergangenheit.Bottom + 10,
                Width = 1000,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 15),
                BackColor = Color.AliceBlue,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            cmbRating.Items.AddRange(new object[]
            {
        "1 - sehr gut", "2 - gut", "3 - befriedigend", "4 - ausreichend", "5 - ungenügend", "Wahrheiten"
            });
            cmbRating.SelectedIndex = 0;
            cmbRating.SelectedIndexChanged += (s, e) => FormuDoldur();
            this.Controls.Add(cmbRating);

            lstCumleler = new ListView
            {
                Left = 20,
                Top = cmbRating.Bottom + 25,
                Width = 1000,
                Height = 200,
                Font = new Font("Segoe UI", 15),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Scrollable = true,
                MultiSelect = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            lstCumleler.Columns.Add("Formulierung", -2);
            this.Controls.Add(lstCumleler);

            var btnListeyeEkle = new Button
            {
                Text = "Zur Liste hinzufügen",
                Width = 300,
                Height = 40,
                Top = lstCumleler.Bottom + 10,
                Left = lstCumleler.Left,
                BackColor = Color.MediumPurple,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            this.Controls.Add(btnListeyeEkle);

            lstSecilenler = new ListBox
            {
                Name = "lstSecilenler",
                Left = 20,
                Top = btnListeyeEkle.Bottom + 10,
                Width = 1000,
                Height = 100,
                Font = new Font("Segoe UI", 12),
                HorizontalScrollbar = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            this.Controls.Add(lstSecilenler);

            var btnSil = new Button
            {
                Text = "Auswahl entfernen",
                Width = 150,
                Height = 40,
                Top = lstSecilenler.Bottom + 10,
                Left = lstSecilenler.Left,
                BackColor = Color.IndianRed,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            this.Controls.Add(btnSil);

            var btnKarneyeGonder = new Button
            {
                Text = "Zum Zeugnis hinzufügen",
                Width = 200,
                Height = 40,
                Top = lstSecilenler.Bottom + 10,
                Left = btnSil.Right + 20,
                BackColor = Color.SteelBlue,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };
            this.Controls.Add(btnKarneyeGonder);

            var lblHinweis = new Label
            {
                Left = 20,
                Top = btnKarneyeGonder.Bottom + 30,
                Width = 1000,
                Height = 30,
                ForeColor = Color.DarkRed,
                Font = new Font("Segoe UI", 15, FontStyle.Bold),
                Text = "❗ Bitte überprüfen Sie die eingefügte Formulierung auf Sinn und Grammatik.",
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            this.Controls.Add(lblHinweis);

            // Events
            btnListeyeEkle.Click += (s, e) =>
            {
                int maxWidth = 0;
                using (Graphics g = lstSecilenler.CreateGraphics())
                {
                    foreach (ListViewItem item in lstCumleler.SelectedItems)
                    {
                        if (!lstSecilenler.Items.Contains(item.Text))
                            lstSecilenler.Items.Add(item.Text);

                        var size = g.MeasureString(item.Text, lstSecilenler.Font);
                        if (size.Width > maxWidth)
                            maxWidth = (int)size.Width;
                    }
                }
                lstSecilenler.HorizontalExtent = maxWidth + 20;
            };

            btnSil.Click += (s, e) =>
            {
                if (lstSecilenler.SelectedItem != null)
                    lstSecilenler.Items.Remove(lstSecilenler.SelectedItem);
            };

            btnKarneyeGonder.Click += (s, e) =>
            {
                if (lstSecilenler.Items.Count > 0)
                {
                    if (!string.IsNullOrWhiteSpace(hedefTextBox.Text))
                        hedefTextBox.Text += " ";

                    var tumSecilenler = lstSecilenler.Items.Cast<string>();
                    hedefTextBox.Text += string.Join(" ", tumSecilenler);
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Bitte mindestens einen Satz zur Karne hinzufügen.");
                }
            };
        }

        private void UpdateTenseButtons()
        {
            btnGegenwart.BackColor = isPresent ? Color.HotPink : Color.LightGray;
            btnVergangenheit.BackColor = isPresent ? Color.LightGray : Color.DarkGray;
        }

        private void FormuDoldur()
        {
            lstCumleler.Items.Clear();

            if (cmbRating.SelectedItem == null)
                return;

            string rating = cmbRating.SelectedItem.ToString().Split(' ')[0];

            try
            {
                string dosyaAdi = isPresent
                    ? "formulierungen.json"
                    : "formulierungen_vergangenheit_klein.json";

                string jsonPath = Path.Combine(Application.StartupPath, "AppData", dosyaAdi);
                if (!File.Exists(jsonPath))
                {
                    MessageBox.Show($"{dosyaAdi} dosyası bulunamadı.");
                    return;
                }

                string json = File.ReadAllText(jsonPath);
                var veri = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, List<string>>>>(json);

                string Normalize(string s) => s.Replace("–", "-").Trim().ToLower();
                string? matchedKey = veri?.Keys.FirstOrDefault(k => Normalize(k) == Normalize(alanAdi));

                if (matchedKey != null &&
                    veri![matchedKey].TryGetValue(rating, out var cumleler) &&
                    cumleler != null && cumleler.Count > 0)
                {
                    string vorname = TextHelper.ExtractVorname(this.adSoyad);

                    foreach (var cumle in cumleler)
                    {
                        string sonuc = TextHelper.SafePrependName(vorname, cumle.Trim());
                        lstCumleler.Items.Add(sonuc);
                    }
                }
                else
                {
                    MessageBox.Show($"'{alanAdi}' için '{rating}' seviyesinde hazır cümle bulunamadı.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler beim Laden der Formulierungen:\n" + ex.Message);
            }
        }

        private void BtnEkle_Click(object sender, EventArgs e)
        {
            if (lstCumleler.SelectedItems.Count == 0)
            {
                MessageBox.Show("Bitte mindestens einen Satz auswählen.");
                return;
            }

            if (!string.IsNullOrWhiteSpace(hedefTextBox.Text))
                hedefTextBox.Text += " ";

            foreach (ListViewItem item in lstCumleler.SelectedItems)
            {
                hedefTextBox.Text += item.Text + " ";
            }

            this.Close();
        }
    }
}
