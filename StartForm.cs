using IKO_ZeugnisManager_Software.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using IKO_ZeugnisManager_Software.Helpers;
using System.Reflection; // gerektiği için
using System.Drawing;
using System.Drawing.Imaging;



namespace IKO_ZeugnisManager_Software
{
    public partial class StartForm : Form
    {
        public string SecilenKlasse { get; private set; }
        public Ogretmen SecilenOgretmen { get; private set; } // sadece log için
        public string SecilenCinsiyet { get; private set; } // sadece log için
        public string DokumanTipi { get; private set; }
        public static string GirisYapanKullaniciAdi { get; private set; }
        public bool IsSeiteneinstieg { get; private set; }






        private ComboBox cmbOgretmen;
        private ComboBox cmbKlasse;
        private RadioButton rbZeugnis;
        private RadioButton rbSE;
        private Button btnDevam;
        private Panel mainPanel;

        public StartForm()
        {
            InitializeComponent();
            InitLayout();
        }

        private void InitLayout()
        {
            this.WindowState = FormWindowState.Maximized;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "XYZ Zeugnis Manager";
            this.BackColor = Color.FromArgb(245, 245, 245);
            this.Font = new Font("Segoe UI", 11.5f, FontStyle.Regular);

            var assembly = Assembly.GetExecutingAssembly();
            using Stream stream = assembly.GetManifestResourceStream("IKO_ZeugnisManager.Assets.icon.ico");
            if (stream != null)
                this.Icon = new Icon(stream);

            string bgPath = Path.Combine(Application.StartupPath, "Assets", "bg_startform.png"); // kendi yolunu yaz

           
            
            
            
            if (File.Exists(bgPath))
            {
                using var original = new Bitmap(bgPath);
                this.BackgroundImage = ApplyOpacity(original, 0.2f); // 0.0 - 1.0 arasında opacity
                this.BackgroundImageLayout = ImageLayout.Stretch;
            }



            mainPanel = new Panel
            {
                Width = 700,
                Height = 500,
                BackColor = Color.Transparent,
                Padding = new Padding(20)

            };
         //  mainPanel.BackColor = Color.FromArgb(0, Color.Transparent);
            this.Controls.Add(mainPanel);
            CenterPanel();
            
             

            Label lblTitle = new Label
            {
                Text = "XYZ Zeugnis-Manager",
                Font = new Font("Segoe UI Semibold", 20, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 53, 85),
                AutoSize = true,
                Top = 40
            };
            lblTitle.Left = 165;
            mainPanel.Controls.Add(lblTitle);

            int targetRight = 300;

            // 1. Klasse (en üstte)
            Label lblKlasse = new Label
            {
                Text = "Klasse:",
                Width = 100,
                Left = targetRight - 100,
                Top = 130,
                TextAlign = ContentAlignment.MiddleRight
            };

            cmbKlasse = new ComboBox
            {
                Left = targetRight + 10,
                Top = 130,
                Width = 320,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 11.5f),
                Enabled = true
            };

            cmbKlasse.Items.AddRange(new string[] { "1a", "1b", "2a", "2b", "3a", "3b", "4a", "4b", "4c" });
            cmbKlasse.SelectedIndex = 0;

            // 2. Kullanıcı (öğretmen değil — loglama için)
            Label lblOgretmen = new Label
            {
                Text = "Benutzername:",
                Width = 220,
                Left = targetRight - 220,
                Top = 190,
                TextAlign = ContentAlignment.MiddleRight
            };

            cmbOgretmen = new ComboBox
            {
                Left = targetRight + 10,
                Top = 190,
                Width = 320,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 11.5f)
            };

            cmbOgretmen.Items.AddRange(OgretmenVeri.TumOgretmenler.Cast<object>().ToArray());

            // 3. Belge türü
            Label lblTip = new Label
            {
                Text = "Dokumenttyp:",
                Width = 120,
                Left = targetRight - 120,
                Top = 250,
                TextAlign = ContentAlignment.MiddleRight
            };

            Panel radioPanel = new Panel
            {
                Left = targetRight + 10,
                Top = 250,
                Width = 320,
                Height = 60
            };
            rbZeugnis = new RadioButton
            {
                Text = "Zeugnis",
                Top = 0,
                Checked = true,
                Font = new Font("Segoe UI", 11.5f)
            };

            rbSE = new RadioButton
            {
                Text = "SE",
                Top = 30,
                Font = new Font("Segoe UI", 11.5f),
                //Visible = false,   // Görünmez yap
                //Enabled = false    // Seçilemez yap
            };


            radioPanel.Controls.Add(rbZeugnis);
            radioPanel.Controls.Add(rbSE);

            // 4. Devam butonu
            btnDevam = new Button
            {
                Text = "Weiter",
                Left = (mainPanel.Width - 30) / 2,
                Top = 340,
                Width = 150,
                Height = 45,
                BackColor = Color.FromArgb(30, 144, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnDevam.FlatAppearance.BorderSize = 0;
            btnDevam.Click += BtnDevam_Click;

            mainPanel.Controls.AddRange(new Control[]
            {
                lblTitle,
                lblKlasse, cmbKlasse,
                lblOgretmen, cmbOgretmen,
                lblTip, radioPanel,
                btnDevam
            });

            this.Resize += (s, e) => CenterPanel();
        }

        private Image ApplyOpacity(Image image, float opacity)
        {
            Bitmap bmp = new Bitmap(image.Width, image.Height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                ColorMatrix matrix = new ColorMatrix
                {
                    Matrix00 = 1.0f,
                    Matrix11 = 1.0f,
                    Matrix22 = 1.0f,
                    Matrix33 = opacity, // alpha
                    Matrix44 = 1.0f
                };

                using ImageAttributes attributes = new ImageAttributes();
                attributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

                g.DrawImage(image, new Rectangle(0, 0, bmp.Width, bmp.Height),
                            0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attributes);
            }
            return bmp;
        }

        private void BtnDevam_Click(object sender, EventArgs e)
        {
            if (cmbOgretmen.SelectedItem is not Ogretmen ogretmen)
            {
                MessageBox.Show("Bitte wählen Sie eine Lehrkraft aus.");
                return;
            }

            if (cmbKlasse.SelectedItem is not string secilenSinif || string.IsNullOrWhiteSpace(secilenSinif))
            {
                MessageBox.Show("Bitte wählen Sie eine Klasse aus.");
                return;
            }

            SecilenOgretmen = ogretmen;
            SecilenCinsiyet = ogretmen.Geschlecht;
            SecilenKlasse = secilenSinif;
            DokumanTipi = rbZeugnis.Checked ? "Zeugnis" : "SE";
            GirisYapanKullaniciAdi = ogretmen.AdSoyad ?? "Unbekannt";
            IsSeiteneinstieg = rbSE.Checked;

            // ✅ Artık güvenli: Kullanıcı adı atanmış → Yedek al!
            YedeklemeModulu.TumKlasoruZiple("AppData", GirisYapanKullaniciAdi);

            // Formu göster
            var zeugnisForm = new FormZeugnisManager(SecilenKlasse, DokumanTipi, SecilenOgretmen, SecilenCinsiyet, IsSeiteneinstieg);
            this.Hide();

            var result = zeugnisForm.ShowDialog();

            if (result == DialogResult.Retry)
                this.Show();
            else
                this.Close();
        }

        public static void LogKaydet(string mesaj)
        {
            try
            {
                string logKlasoru = Path.Combine(Application.StartupPath, "Backup");
                Directory.CreateDirectory(logKlasoru);

                string logPath = Path.Combine(logKlasoru, "log.txt");
                string logSatiri = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {mesaj}";

                File.AppendAllText(logPath, logSatiri + Environment.NewLine, Encoding.UTF8);
            }
            catch
            {
                // Sessizce yut (uygulamayı bozmasın)
            }
        }

       

        private void CenterPanel()
        {
            if (mainPanel != null)
            {
                mainPanel.Left = (this.ClientSize.Width - mainPanel.Width) / 2;
                mainPanel.Top = (this.ClientSize.Height - mainPanel.Height) / 2;
            }
        }
    }
}
