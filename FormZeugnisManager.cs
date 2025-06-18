using IKO_ZeugnisManager_Software.Controls;
using IKO_ZeugnisManager_Software.Helpers;
using IKO_ZeugnisManager_Software.Models;
using iTextSharp.text.pdf;
using MaterialSkin;
using MaterialSkin.Controls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;




namespace IKO_ZeugnisManager_Software
{
    public partial class FormZeugnisManager : MaterialForm
    {
        private string secilenKlasse;
        private string dokumanTipi; // "Zeugnis" veya "SE"
        private string jsonKonularDosyasi;
        private string pdfTemplateDosyasi;
        private string formModu; // "birinci" veya "seiteneinstieg"
        private Panel mainPanel;
        private GroupBox groupBoxOgrenci;

        private TextBox txtAdSoyad;
        private bool IsSeiteneinstieg;
        private DateTimePicker dtSeiteneinsteigergruppe;

        private DateTimePicker dtDogum;
        private ComboBox cmbKlasse;
        private TextBox txtSchuljahr;
        private ComboBox cmbHalbjahr;
        private NumericUpDown numFehlstunden, numUnentschuldigt, numVerspaetungen;
        private bool isModified = false;
        private TextBox txtUhrzeit;
        private TextBox txtboxallgemain;
        private bool suppressChangeTracking = false;
        private RadioButton rbJa;
        private RadioButton rbNein;
        private Label lblTerfi;
        private Label lblogrencitipi;
        private DateTimePicker dtKonferenz;
        private DateTimePicker dtWiederbeginn;
        private ComboBox cmbWochentag;
        private RoundedButton btnGuncelle;
        private RoundedButton btnSil;
        private RoundedButton btnNotKaydet;
        private RoundedButton btnKlasoruAc;
        private RoundedButton btnAllesAuswaehlen;

        private ComboBox cmbOgrenciler;
        private Dictionary<string, TextBox> aciklamaKutulari = new Dictionary<string, TextBox>();
        private Dictionary<string, Label> aciklamaEtiketleri = new Dictionary<string, Label>(); // ← bunu ekle
        private Dictionary<string, List<Panel>> notPanelleri = new Dictionary<string, List<Panel>>();
        private Dictionary<string, RadioButton[]> religionRadioButtons = new Dictionary<string, RadioButton[]>();
        private Dictionary<string, ComboBox> gesamtnoteComboBoxes = new();
        private bool seiteneinstiegModu = false; // Form açılırken veya sınıf seçilirken ayarlanmalı
        private readonly Dictionary<string, string> feldMapping = new()
{
    { "Hören / Sprechen_Gesamtnote", "Hoeren_Sprechen_Gesamtnote" },
    { "Rechtschreiben / Schreiben_Gesamtnote", "Rechtschreiben_Schreiben_Gesamtnote" },
    { "Lesen – mit Texten und Medien umgehen_Gesamtnote", "Lesen_mit_Texten_und_Medien_umgehen_Gesamtnote" }
    // gerekiyorsa buraya daha fazla mapping ekleyebilirsin
};


        private string DokumanTipi; // örneğin "SE" veya "ZEUGNIS"



        private string pdfKayitKlasoru = null;
        private GroupBox groupBoxNotlar;
        ComboBox cmbOgrenciNotSecimi;
        private Ogretmen secilenOgretmen;
        private Label lblBilgiMesaji;
        private Label lblOgrenciSayisi;
        private RoundedButton btnKaydet;
        private RoundedButton btnVazgec;
        private RoundedButton btnEkle;
        private bool isUpdateMode = false;
        private Ogrenci secilenOgrenciGuncelleme = null;
        private List<Ogrenci> ogrenciListesi = new();
        private Ogrenci secilenOgrenci;
        private string secilenCinsiyet; // 👈 Eksik olan buydu
        private Dictionary<string, ComboBox> comboBoxGesamtnoten = new();
        private Dictionary<string, List<string>> notBasliklari = new();
        private const int StandartGroupBoxWidth = 1350;
        private Panel headerPanel;
        private CheckedListBox checkedListOgrenciler;
        private RoundedButton btnPdfOlustur;
        private GroupBox gbKonferenz;
        private RoundedButton btnGeriDon;
        private RoundedButton btnPdfKlasorAc;
        public static string SecilenDokumanTuru { get; set; }



        //Ekledim
        public FormZeugnisManager(string secilenKlasse, string dokumanTipi, Ogretmen secilenOgretmen, string secilenCinsiyet, bool isSeiteneinstieg)
        {
            this.secilenKlasse = secilenKlasse;
            this.dokumanTipi = dokumanTipi;
            this.secilenOgretmen = secilenOgretmen;
            this.secilenCinsiyet = secilenCinsiyet;
            this.seiteneinstiegModu = isSeiteneinstieg;
            this.IsSeiteneinstieg = isSeiteneinstieg;


            // 🌈 Tema bilgisi burada ThemeHelper’a veriliyor
            ThemeHelper.SetDokumanTuru(dokumanTipi); // Tema için SE/Zeugnis ayarı


            InitializeComponent();

            KonulariJsondanYukle(secilenKlasse);
            if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.PdfKayitKlasoru) &&
                Directory.Exists(Properties.Settings.Default.PdfKayitKlasoru))
            {
                pdfKayitKlasoru = Properties.Settings.Default.PdfKayitKlasoru;
            }

            LoadFromJson();
            InitLayout();
            RefreshOgrenciListeleri();
            this.Resize += (s, e) => CenterContent();
            btnEkle.Click += BtnEkle_Click;
            btnSil.Click += BtnSil_Click;
            cmbOgrenciNotSecimi.SelectedIndexChanged += CmbOgrenciNotSecimi_SelectedIndexChanged;
            btnNotKaydet.Click += BtnNotKaydet_Click;
            this.FormClosing += FormZeugnisManager_FormClosing;
        }

        private void FormZeugnisManager_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (this.DialogResult == DialogResult.Retry)
                return;

            // 1. Önce kullanıcıdan onay iste
            DialogResult result = MessageBox.Show(
                isModified
                ? "Änderungen wurden nicht gespeichert. Möchten Sie sie speichern und ein Backup erstellen?"
                : "Möchten Sie vor dem Schließen ein Backup erstellen?",
                "Programm beenden",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1);

            if (result == DialogResult.Cancel)
            {
                e.Cancel = true;
                return;
            }

            // 2. Kaydedilmemiş değişiklik varsa kaydet
            if (result == DialogResult.Yes && isModified)
            {
                BtnNotKaydet_Click(null, EventArgs.Empty, true); // silent mode
                isModified = false;
            }

            // 3. Her durumda yedek al
            string kullanici = StartForm.GirisYapanKullaniciAdi;
            if (!string.IsNullOrWhiteSpace(kullanici))
            {
                try
                {
                    YedeklemeModulu.TumKlasoruZiple("AppData", kullanici);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Yedekleme hatası:\n" + ex.Message, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        //Ekledim
        private bool SinifUc()
        {
            return secilenKlasse.StartsWith("3");
        }
        //Ekledim
        private bool SinifDort()
        {
            return secilenKlasse.StartsWith("4");
        }
        //Ekledim
        private void CenterContent()
        {
            if (groupBoxOgrenci != null)
                groupBoxOgrenci.Left = Math.Max((mainPanel.ClientSize.Width - groupBoxOgrenci.Width) / 2, 20);

            if (groupBoxNotlar != null)
                groupBoxNotlar.Left = Math.Max((mainPanel.ClientSize.Width - groupBoxNotlar.Width) / 2, 20);
        } 

        //Ekledim
        private void CmbOgrenciNotSecimi_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbOgrenciNotSecimi.SelectedItem is not Ogrenci secilen)
            {
                btnGuncelle.Text = "Aktualisieren";
                btnSil.Text = "Löschen";
                return;
            }

            suppressChangeTracking = true;

            txtAdSoyad.Text = secilen.AdSoyad;
            dtDogum.Value = secilen.Dogum;
            cmbKlasse.SelectedItem = secilen.Klasse;
            txtSchuljahr.Text = secilen.Schuljahr;
            cmbHalbjahr.SelectedItem = secilen.Halbjahr;
            numFehlstunden.Value = secilen.Fehlstunden;
            numUnentschuldigt.Value = secilen.Unentschuldigt;
            numVerspaetungen.Value = secilen.Verspaetungen;

            dtKonferenz.Value = secilen.Konferenzdatum ?? DateTime.Today;
            cmbWochentag.SelectedItem = secilen.WiederbeginnWochentag ?? cmbWochentag.Items[0];
            dtWiederbeginn.Value = secilen.WiederbeginnDatum ?? DateTime.Today;
            txtUhrzeit.Text = secilen.WiederbeginnUhrzeit ?? "";

            if (secilen.SchuleingangsphaseJa.HasValue)
            {
                rbJa.Checked = secilen.SchuleingangsphaseJa.Value;
                rbNein.Checked = !secilen.SchuleingangsphaseJa.Value;
            }
            else
            {
                rbJa.Checked = true;   // ✅ Varsayılan olarak "Ja"
                rbNein.Checked = false;
                secilen.SchuleingangsphaseJa = true; // opsiyonel: JSON'da da default olsun
            }


            txtboxallgemain.Text = secilen.AllgemeineBemerkungText ?? "";

            // SE'ye özel alan
            if (IsSeiteneinstieg && dtSeiteneinsteigergruppe != null)
            {
                dtSeiteneinsteigergruppe.Value = secilen.Seiteneinsteigergruppe;
            }

            if (lblTerfi != null)
            {
                string sinifNo = (int.TryParse(secilen.Klasse.FirstOrDefault().ToString(), out int sinif) ? sinif + 1 : 2).ToString();
                lblTerfi.Text = $"{secilen.AdSoyad} wird in Klasse {sinifNo} versetzt.";
            }

            foreach (var baslik in notPanelleri)
            {
                if (!secilen.Notlar.TryGetValue(baslik.Key, out int[] notlar) || notlar.Length != baslik.Value.Count)
                {
                    notlar = Enumerable.Repeat(1, baslik.Value.Count).ToArray();
                    secilen.Notlar[baslik.Key] = notlar;
                }

                for (int i = 0; i < baslik.Value.Count; i++)
                {
                    foreach (var rb in baslik.Value[i].Controls.OfType<RadioButton>())
                        rb.Checked = rb.Text == notlar[i].ToString();
                }
            }

            var gbReligion = groupBoxNotlar.Controls.OfType<GroupBox>().FirstOrDefault(g => g.Text == "Religion");
            if (gbReligion != null && !SinifDort())
            {
                var rbReligionEvet = gbReligion.Controls.Find("rbReligion_Evet", true).FirstOrDefault() as RadioButton;
                var rbReligionHayir = gbReligion.Controls.Find("rbReligion_Hayir", true).FirstOrDefault() as RadioButton;

                if (rbReligionEvet != null && rbReligionHayir != null)
                {
                    if (secilen.ReligionTeilnahme.HasValue)
                    {
                        rbReligionEvet.Checked = secilen.ReligionTeilnahme.Value;
                        rbReligionHayir.Checked = !secilen.ReligionTeilnahme.Value;
                    }
                    else
                    {
                        rbReligionEvet.Checked = true;   // ✅ varsayılan: Teilgenommen
                        rbReligionHayir.Checked = false;
                        secilen.ReligionTeilnahme = true; // (opsiyonel) JSON'da güncel olsun istersen
                    }
                }

            }

            foreach (var aciklama in aciklamaKutulari)
            {
                if (secilen.Aciklamalar != null && secilen.Aciklamalar.TryGetValue(aciklama.Key, out string text))
                    aciklama.Value.Text = text;
                else
                    aciklama.Value.Text = "";

                if (gbReligion != null && aciklama.Key == "Religion" && secilen.ReligionTeilnahme == false && string.IsNullOrWhiteSpace(aciklama.Value.Text))
                {
                    aciklama.Value.Text = $"{secilen.AdSoyad} hat nicht am Religionsunterricht teilgenommen.";
                    secilen.Aciklamalar["Religion"] = aciklama.Value.Text;
                }
            }

            GesamtnotenLade(secilen);

            btnGuncelle.Text = $"{secilen.AdSoyad} Stammdaten bearbeiten";
            btnSil.Text = $"{secilen.AdSoyad} löschen";
            btnNotKaydet.Text = GetNotKaydetButtonText();

            suppressChangeTracking = false;
            isModified = false;
            UpdateNotKaydetButtonStyle();
        }

        private void GeriYukleVeGuncelleUI(string jsonDosyaYolu)
        {
            try
            {
                if (!File.Exists(jsonDosyaYolu))
                {
                    MessageBox.Show("Seçilen dosya bulunamadı.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string json = File.ReadAllText(jsonDosyaYolu);
                var paket = System.Text.Json.JsonSerializer.Deserialize<OgrenciVeriPaketi>(json);

                if (paket?.Ogrenciler is { Count: > 0 })
                {
                    ogrenciListesi = paket.Ogrenciler;

                    cmbOgrenciNotSecimi.Items.Clear();
                    checkedListOgrenciler.Items.Clear();

                    foreach (var ogrenci in ogrenciListesi)
                    {
                        cmbOgrenciNotSecimi.Items.Add(ogrenci);
                        checkedListOgrenciler.Items.Add(ogrenci);
                    }

                    lblOgrenciSayisi.Text = $"Schülerzahl in Klasse {secilenKlasse} : {ogrenciListesi.Count}";



                    cmbOgrenciNotSecimi.SelectedIndex = cmbOgrenciNotSecimi.Items.Count > 0 ? 0 : -1;

                    MessageBox.Show("Veriler başarıyla yüklendi ve arayüz güncellendi.",
                                    "Geri Yükleme Tamamlandı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Yedekte geçerli öğrenci verisi bulunamadı.",
                                    "Veri Eksik", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Yedek dosyasını işlerken hata oluştu:\n\n{ex.Message}",
                                "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

      
        private string GetPdfTemplatePath(string klasse)
        {
            string numara = new string(klasse.TakeWhile(char.IsDigit).ToArray());
            string ad = seiteneinstiegModu ? $"{numara}_pdf_master_seiteneinstieg.pdf" : $"{numara}_pdf_master.pdf";
            return Path.Combine(AppContext.BaseDirectory, "AppData", ad);
        }

        private string GetOgrenciJsonPath()
        {
            string baseName = $"ogrenciler_{secilenKlasse}";
            if (seiteneinstiegModu)
                baseName += "_se";

            string appDataPath = Path.Combine(Application.StartupPath, "AppData");
            return Path.Combine(appDataPath, $"{baseName}.json");
        }

        //Ekledim
        private void BtnPdfOlustur_Click(object sender, EventArgs e)
        {
            BtnNotKaydet_Click(sender, e, true); // silent mode

            if (checkedListOgrenciler.CheckedItems.Count == 0)
            {
                MessageBox.Show("Bitte wählen Sie mindestens einen Schüler aus.");
                return;
            }

            if (string.IsNullOrWhiteSpace(Properties.Settings.Default.PdfKayitKlasoru) || !Directory.Exists(Properties.Settings.Default.PdfKayitKlasoru))
            {
                using (var folderDialog = new FolderBrowserDialog())
                {
                    folderDialog.Description = "Bitte wählen Sie den Ordner, in dem die Zeugnisse gespeichert werden sollen.";
                    folderDialog.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                    if (folderDialog.ShowDialog() == DialogResult.OK)
                    {
                        string zeugnisseKlasoru = Path.Combine(folderDialog.SelectedPath, "Zeugnisse");
                        Directory.CreateDirectory(zeugnisseKlasoru);
                        Properties.Settings.Default.PdfKayitKlasoru = zeugnisseKlasoru;
                        Properties.Settings.Default.Save();
                    }
                    else
                    {
                        MessageBox.Show("Kein Ordner ausgewählt. PDF konnte nicht erstellt werden.", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
            }

            pdfKayitKlasoru = Properties.Settings.Default.PdfKayitKlasoru;
            var olusturulanPdfYollari = new List<string>();

            foreach (Ogrenci secilen in checkedListOgrenciler.CheckedItems)
            {
                string templatePath = GetPdfTemplatePath(secilen.Klasse);

                if (!File.Exists(templatePath))
                {
                    MessageBox.Show($"PDF-Vorlagendatei wurde nicht gefunden für {secilen.AdSoyad}.", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    continue;
                }

                string outputPath = Path.Combine(pdfKayitKlasoru, $"ogrenci_{secilen.AdSoyad.Replace(" ", "_")}.pdf");

                try
                {
                    using var reader = new iTextSharp.text.pdf.PdfReader(templatePath);
                    using var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
                    using var stamper = new iTextSharp.text.pdf.PdfStamper(reader, fs);
                    var form = stamper.AcroFields;

                    // PDF alanlarını doldur
                    SetPdfFormFields(form, secilen);

                    // Öğretmen imzasını yerleştir (Image1 alanına)
                    AddOgretmenImzasi(stamper, form, secilen.Klasse);
                    AddMudurImzasi(stamper, form, "Image2");



                    // Diğer form alanları düzenlenebilir kalmaya devam eder
                    stamper.FormFlattening = false;

                    // Dosya yolunu listeye ekle
                    olusturulanPdfYollari.Add(outputPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fehler beim Erstellen der PDF für {secilen.AdSoyad}: {ex.Message}", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }


            }

            if (olusturulanPdfYollari.Count > 1)
            {
                string birlesikPdfYolu = Path.Combine(pdfKayitKlasoru, $"Zeugnisse_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
                MergePdfFiles(olusturulanPdfYollari, birlesikPdfYolu);

                MessageBox.Show($"Die Zeugnisse wurden zusammengeführt:\n{birlesikPdfYolu}", "Erfolg", MessageBoxButtons.OK, MessageBoxIcon.Information);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = birlesikPdfYolu,
                    UseShellExecute = true
                });

                foreach (var path in olusturulanPdfYollari)
                {
                    try { File.Delete(path); } catch { }
                }
            }
            else if (olusturulanPdfYollari.Count == 1)
            {
                string outputPath = olusturulanPdfYollari[0];
                MessageBox.Show($"PDF wurde erfolgreich erstellt:\n{outputPath}", "Erfolg", MessageBoxButtons.OK, MessageBoxIcon.Information);

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = outputPath,
                    UseShellExecute = true
                });
            }
        }
        private void AddOgretmenImzasi(PdfStamper stamper, AcroFields form, string sinifAdi)
        {
            var ogretmen = OgretmenVeri.TumOgretmenler.FirstOrDefault(o => o.Sinif == sinifAdi || o.Sinif == "ALL");
            if (ogretmen == null) return;

            string imzaDosyaAdi = ogretmen.AdSoyad.Replace(" ", "_") + ".png";
            string imzaYolu = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AppData", "Imzalar", imzaDosyaAdi);

            if (!File.Exists(imzaYolu))
            {
                MessageBox.Show($"İmza dosyası bulunamadı:\n{imzaYolu}", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var fieldPositions = form.GetFieldPositions("Image1");
            if (fieldPositions == null || fieldPositions.Count == 0)
            {
                MessageBox.Show("Image1 alanı PDF'de bulunamadı.", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Pozisyon bilgilerini al
            var position = fieldPositions[0].position;
            int page = fieldPositions[0].page;

            // İmzayı hazırla
            iTextSharp.text.Image imza = iTextSharp.text.Image.GetInstance(imzaYolu);
            imza.ScaleToFit(position.Width, position.Height);
            imza.SetAbsolutePosition(position.Left, position.Bottom);

            // Form alanını kaldırmadan, sadece görünümden çıkar (kapatılmış gibi davranır)
            form.RemoveField("Image1");

            // Görseli direkt içeriğe bastır (formdan bağımsız)
            stamper.GetOverContent(page).AddImage(imza);
        }
        private void AddMudurImzasi(PdfStamper stamper, AcroFields form, string alanAdi)
        {
            string imzaYolu = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AppData", "Imzalar", "Hasan_Hayati.png");

            if (!File.Exists(imzaYolu))
            {
                MessageBox.Show("Müdür imza dosyası bulunamadı:\n" + imzaYolu);
                return;
            }

            var positions = form.GetFieldPositions(alanAdi);
            if (positions == null || positions.Count == 0)
            {
                MessageBox.Show($"PDF içinde '{alanAdi}' alanı bulunamadı.");
                return;
            }

            var rect = positions[0].position;
            int page = positions[0].page;

            var image = iTextSharp.text.Image.GetInstance(imzaYolu);
            image.ScaleToFit(rect.Width, rect.Height);
            image.SetAbsolutePosition(rect.Left, rect.Bottom);

            // ❗ Form alanını görünümden kaldır (üzerine başka katman bindirmesin)
            form.RemoveField(alanAdi);

            // Görseli PDF'e bastır
            stamper.GetOverContent(page).AddImage(image);
        }


        private void SetPdfFormFields(AcroFields form, Ogrenci ogr)
        {
            string fontPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AppData", "DejaVuSans.ttf");

            if (!File.Exists(fontPath))
            {
                MessageBox.Show("Font dosyası bulunamadı: DejaVuSans.ttf\nLütfen AppData klasöründe olduğundan emin olun.", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            BaseFont unicodeFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);

            void SetFieldUnicode(string fieldName, string value)
            {
                form.SetFieldProperty(fieldName, "textfont", unicodeFont, null);
                form.SetField(fieldName, value);
            }

            string adSoyad = TextHelper.FormatAdSoyadOhneKlasse(ogr.AdSoyad);

            SetFieldUnicode("txt_adsoyad", adSoyad);
            SetFieldUnicode("txt_adsoyad_nokta", $"{adSoyad}...");
            SetFieldUnicode("txt_Page2", $"Seite 2 des Zeugnisses für {adSoyad}, Klasse {ogr.Klasse}, Schuljahr {ogr.Schuljahr}, {ogr.Halbjahr}");
            SetFieldUnicode("txt_Page3", $"Seite 3 des Zeugnisses für {adSoyad}, Klasse {ogr.Klasse}, Schuljahr {ogr.Schuljahr}, {ogr.Halbjahr}");
            SetFieldUnicode("txt_Page4", $"Seite 4 des Zeugnisses für {adSoyad}, Klasse {ogr.Klasse}, Schuljahr {ogr.Schuljahr}, {ogr.Halbjahr}");
            SetFieldUnicode("txt_Page5", $"Seite 5 des Zeugnisses für {adSoyad}, Klasse {ogr.Klasse}, Schuljahr {ogr.Schuljahr}, {ogr.Halbjahr}");
            SetFieldUnicode("txt_geboren", $"geboren am {ogr.Dogum:dd.MM.yyyy}");

            string klasseText = (IsSeiteneinstieg && !ogr.Klasse.StartsWith("4"))
                ? $"Seiteneinsteigergruppe seit {(ogr.Seiteneinsteigergruppe > DateTime.MinValue ? ogr.Seiteneinsteigergruppe.ToString("dd.MM.yyyy") : DateTime.Today.ToString("dd.MM.yyyy"))} / Regelklasse {ogr.Klasse}"
                : $"Klasse {ogr.Klasse}";
            SetFieldUnicode("txt_Klasse", klasseText);
            SetFieldUnicode("txt_Schuljahr", $"Schuljahr {ogr.Schuljahr}");
            SetFieldUnicode("txt_Halbjahr", ogr.Halbjahr);
            SetFieldUnicode("txt_fehlstunden", $"Fehlstunden insgesamt: {ogr.Fehlstunden},");
            SetFieldUnicode("txt_unentschuldigt", $"davon unentschuldigt: {ogr.Unentschuldigt}");
            SetFieldUnicode("txt_Verspätungen", $"Anzahl der Verspätungen: {ogr.Verspaetungen}");

            foreach (var konu in ogr.Notlar)
            {
                string key = GetPdfFieldName(konu.Key);
                if (key.Equals("Religion", StringComparison.OrdinalIgnoreCase)
                    && ogr.ReligionTeilnahme.HasValue && !ogr.ReligionTeilnahme.Value)
                    continue;

                for (int i = 0; i < konu.Value.Length; i++)
                {
                    string fieldName = $"rb_{key}_{i + 1}";
                    form.SetField(fieldName, konu.Value[i].ToString());
                }
            }

            if (ogr.Aciklamalar != null)
            {
                foreach (var aciklama in ogr.Aciklamalar)
                {
                    string key = GetPdfFieldName(aciklama.Key);
                    SetDynamicTextField(form, $"txt_{key}", aciklama.Value, unicodeFont);
                }
            }

            if (ogr.Aciklamalar?.TryGetValue("Religion", out string religionText) == true)
                SetDynamicTextField(form, "txt_Religion", religionText, unicodeFont);

            if (!string.IsNullOrWhiteSpace(ogr.AllgemeineBemerkungText))
                SetDynamicTextField(form, "txt_Allgemeine", ogr.AllgemeineBemerkungText, unicodeFont);

            foreach (var entry in ogr.Aciklamalar.Where(e => e.Key.EndsWith("_Gesamtnote")))
            {
                string key = GetPdfFieldName(entry.Key.Replace("_Gesamtnote", ""));
                form.SetField($"txt_Gesamtnote_{key}", entry.Value);
            }

            if (ogr.Konferenzdatum.HasValue)
                SetFieldUnicode("txt_Konferenz", $"Das Zeugnis wird aufgrund des Konferenzbeschlusses vom  {dtKonferenz.Value:dd.MM.yyyy} erteilt.");

            if (ogr.SchuleingangsphaseJa.HasValue)
                form.SetField("rb_versetzt", ogr.SchuleingangsphaseJa.Value ? "Ja" : "Nein");

            if (ogr.SchuleingangsphaseJa == true)
            {
                int mevcutSinif = ogr.Klasse?.FirstOrDefault(char.IsDigit) - '0' ?? 1;
                int sonrakiSinif = mevcutSinif + 1;
                SetFieldUnicode("txt_Schuleingangsphase", $"{adSoyad} wird in Klasse {sonrakiSinif} versetzt.");
            }

            if (!string.IsNullOrWhiteSpace(ogr.WiederbeginnWochentag))
                SetFieldUnicode("txt_wtag", ogr.WiederbeginnWochentag);

            if (ogr.WiederbeginnDatum.HasValue)
                SetFieldUnicode("txt_wdatum", ogr.WiederbeginnDatum.Value.ToString("dd.MM.yyyy"));

            if (!string.IsNullOrWhiteSpace(ogr.WiederbeginnUhrzeit))
                SetFieldUnicode("txt_wuhrzeit", ogr.WiederbeginnUhrzeit);

            SetFieldUnicode("txt_AktuelDatum", $"Essen, den {DateTime.Today:dd.MM.yyyy}");

            SetDynamicTextField(form, "txt_Allgemaine", txtboxallgemain.Text.Trim(), unicodeFont);

            string[] islenenGruplar = { "Leistungsbereitschaft", "Zuverlässigkeit/Sorgfalt" };
            string txtArbeitsverhalten = string.Join(" ", islenenGruplar
                .Where(k => ogr.Aciklamalar.ContainsKey(k))
                .Select(k => ogr.Aciklamalar[k]));
            SetDynamicTextField(form, "txt_Arbeitsverhalten_gesamt", txtArbeitsverhalten, unicodeFont);

            if (ogr.Klasse != null && ogr.Klasse.StartsWith("4"))
            {
                string sozialverhalten = ogr.Aciklamalar.ContainsKey("Sozialverhalten") ? ogr.Aciklamalar["Sozialverhalten"] : "";
                string txtSozialverhaltenFinal = (txtArbeitsverhalten + " " + sozialverhalten).Trim();
                SetDynamicTextField(form, "txt_Sozialverhalten", txtSozialverhaltenFinal, unicodeFont);
            }

            string[] matheGruplar = { "Arithmetik", "Geometrie", "Sachrechnen / Daten und Wahrscheinlichkeit" };
            string txtMathe = string.Join(" ", matheGruplar
                .Where(k => ogr.Aciklamalar.ContainsKey(k))
                .Select(k => ogr.Aciklamalar[k]));
            SetDynamicTextField(form, "txt_Mathematik", txtMathe, unicodeFont);

            string[] deutschGruplar = IsSeiteneinstieg
    ? new[] { "Hören / Sprechen", "Rechtschreiben / Schreiben", "Lesen – mit Texten und Medien umgehen" }
    : new[] { "Rechtschreiben", "Lesen – mit Texten und Medien umgehen", "Sprachgebrauch – Sprechen und Zuhören / Schreiben" };

            string txtDeutsch = string.Join(" ", deutschGruplar
                .Where(k => ogr.Aciklamalar.ContainsKey(k))
                .Select(k => ogr.Aciklamalar[k]));

            SetDynamicTextField(form, "txt_Deutsch", txtDeutsch, unicodeFont);



            var sinifOgretmeni = OgretmenVeri.TumOgretmenler.FirstOrDefault(o => o.Sinif == ogr.Klasse);
            if (sinifOgretmeni != null)
            {
                string geschlecht = sinifOgretmeni.Geschlecht?.ToLower().Trim();
                string unvan = geschlecht == "männlich" ? ", Klassenlehrer"
                              : geschlecht == "weiblich" ? ", Klassenlehrerin"
                              : ", Klassenleitung";
                form.SetField("txt_ogretmen", $"{sinifOgretmeni.AdSoyad}{unvan}");
            }
        }

        //Ekledim
        private void BtnNotKaydet_Click(object sender, EventArgs e)
        {
            BtnNotKaydet_Click(sender, e, false);
        }
        //Ekledim
        private void BtnNotKaydet_Click(object sender, EventArgs e, bool silent)
        {
            if (cmbOgrenciNotSecimi.SelectedItem is not Ogrenci secilen)
            {
                if (!silent)
                    MessageBox.Show("Bitte wählen Sie zuerst einen Schüler aus.");
                return;
            }

            // Notlar
            foreach (var baslik in notPanelleri)
            {
                var notlar = new List<int>();
                foreach (var panel in baslik.Value)
                {
                    var seciliRadio = panel.Controls.OfType<RadioButton>().FirstOrDefault(rb => rb.Checked);
                    notlar.Add(seciliRadio != null ? int.Parse(seciliRadio.Text) : 1);
                }
                secilen.Notlar[baslik.Key] = notlar.ToArray();
            }

            // Açıklamalar
            if (secilen.Aciklamalar == null)
                secilen.Aciklamalar = new();

            foreach (var aciklama in aciklamaKutulari)
            {
                string key = aciklama.Key;

                if (key == "Religion")
                {
                    var gb = groupBoxNotlar.Controls.OfType<GroupBox>().FirstOrDefault(g => g.Text == "Religion");

                    if (gb != null)
                    {
                        var rbReligionEvet = gb.Controls.Find("rbReligion_Evet", true).FirstOrDefault() as RadioButton;
                        var rbReligionHayir = gb.Controls.Find("rbReligion_Hayir", true).FirstOrDefault() as RadioButton;

                        if (rbReligionEvet != null && rbReligionEvet.Checked)
                            secilen.ReligionTeilnahme = true;
                        else if (rbReligionHayir != null && rbReligionHayir.Checked)
                            secilen.ReligionTeilnahme = false;
                        else
                            secilen.ReligionTeilnahme = null;

                        if (rbReligionHayir != null && rbReligionHayir.Checked)
                        {
                            secilen.Aciklamalar[key] = $"{secilen.AdSoyad} hat nicht am Religionsunterricht teilgenommen.";
                            continue;
                        }
                    }
                }

                secilen.Aciklamalar[key] = aciklama.Value.Text.Trim();
            }

            // Gesamtnoten
            SaveAllGesamtnoten(secilen);

            // Ortak alanlar
            secilen.SchuleingangsphaseJa = rbJa.Checked ? true : rbNein.Checked ? false : null;
            secilen.AllgemeineBemerkungJa = txtboxallgemain.Enabled;
            secilen.AllgemeineBemerkungText = txtboxallgemain.Text.Trim();
            secilen.Konferenzdatum = dtKonferenz.Value;
            secilen.WiederbeginnWochentag = cmbWochentag.SelectedItem?.ToString();
            secilen.WiederbeginnDatum = dtWiederbeginn.Value;
            secilen.WiederbeginnUhrzeit = txtUhrzeit.Text.Trim();

            if (IsSeiteneinstieg)
                secilen.Seiteneinsteigergruppe = dtSeiteneinsteigergruppe?.Value ?? DateTime.Today;

            SaveToJson();
            isModified = false;
            UpdateNotKaydetButtonStyle();

            if (!silent)
                MessageBox.Show("Noten und Bemerkungen wurden gespeichert.");
        }

        //Ekledim
        private void GesamtnotenLade(Ogrenci secilen)
        {
            foreach (var gb in groupBoxNotlar.Controls.OfType<GroupBox>())
            {
                foreach (Control inner in GetAllControlsRecursive(gb))
                {
                    if (inner is ComboBox cmb && cmb.Tag is string key && key.EndsWith("_Gesamtnote"))
                    {
                        string usedKey = key;

                        // Seiteneinstieg ise key mapping yap
                        if (IsSeiteneinstieg)
                            usedKey = GetPdfFieldName(key); // örn: "Hören / Sprechen_Gesamtnote" gibi orijinal karşılığı

                        if (secilen.Aciklamalar != null && secilen.Aciklamalar.TryGetValue(usedKey, out string value))
                        {
                            cmb.SelectedItem = value;
                        }
                    }
                }
            }
        }

        //Ekledim
        private void SaveToJson()
        {
            string jsonPath = GetOgrenciJsonPath();
            string backupPath = Path.Combine(
                Path.GetDirectoryName(jsonPath),
                $"{Path.GetFileNameWithoutExtension(jsonPath)}_backup.json");

            try
            {
                // Geçici dosyaya yaz
                string tempPath = jsonPath + ".tmp";
                var paket = new OgrenciVeriPaketi { Ogrenciler = ogrenciListesi };
                string json = System.Text.Json.JsonSerializer.Serialize(paket, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(tempPath, json);

                // Önce yedekle
                if (File.Exists(jsonPath))
                {
                    File.Copy(jsonPath, backupPath, overwrite: true);
                }

                // Asıl dosyayı güncelle
                File.Copy(tempPath, jsonPath, overwrite: true);
                File.Delete(tempPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Beim Speichern ist ein Fehler aufgetreten: {ex.Message}",
                    "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //Ekledim
        private void LoadFromJson()
        {
            string jsonPath = GetOgrenciJsonPath();
            string backupPath = Path.Combine(
                Path.GetDirectoryName(jsonPath),
                $"{Path.GetFileNameWithoutExtension(jsonPath)}_backup.json");

            if (!File.Exists(jsonPath))
            {
                ogrenciListesi = new List<Ogrenci>();
                return;
            }

            try
            {
                var json = File.ReadAllText(jsonPath);
                ogrenciListesi = DeserializeOgrenciler(json);

                foreach (var o in ogrenciListesi)
                    o.WiederbeginnUhrzeit ??= "";
            }
            catch (Exception ex)
            {
                if (File.Exists(backupPath))
                {
                    try
                    {
                        var backupJson = File.ReadAllText(backupPath);
                        ogrenciListesi = DeserializeOgrenciler(backupJson);

                        MessageBox.Show(
                            "Die Haupt-JSON-Datei war beschädigt. Es wurde aus der Sicherung geladen.",
                            "Warnung", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    catch
                    {
                        MessageBox.Show(
                            $"Auch die Sicherung ist beschädigt: {ex.Message}",
                            "Kritischer Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        ogrenciListesi = new List<Ogrenci>();
                    }
                }
                else
                {
                    MessageBox.Show(
                        $"JSON ist beschädigt und keine Sicherung vorhanden: {ex.Message}",
                        "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    ogrenciListesi = new List<Ogrenci>();
                }
            }
        }


        //Ekledim
        private List<Ogrenci> DeserializeOgrenciler(string json)
        {
            try
            {
                json = json.Trim();

                if (json.StartsWith("{"))
                {
                    var paket = System.Text.Json.JsonSerializer.Deserialize<OgrenciVeriPaketi>(json);
                    return paket?.Ogrenciler ?? new List<Ogrenci>();
                }
                else if (json.StartsWith("["))
                {
                    return System.Text.Json.JsonSerializer.Deserialize<List<Ogrenci>>(json) ?? new List<Ogrenci>();
                }
                else
                {
                    MessageBox.Show("Geçersiz JSON formatı.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return new List<Ogrenci>();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"JSON çözümleme hatası:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return new List<Ogrenci>();
            }
        }
        //Ekledim
        private void RefreshOgrenciListeleri()
        {
            var oncekiSecilenler = checkedListOgrenciler.CheckedItems
                .Cast<object>()
                .Select(item => item.ToString())
                .ToHashSet();

            checkedListOgrenciler.Items.Clear();
            cmbOgrenciNotSecimi.Items.Clear();

            var filtreli = ogrenciListesi
                .Where(o => o.Klasse == secilenKlasse)
                .OrderBy(o => o.AdSoyad)
                .ToList();

            foreach (var o in filtreli)
            {
                bool onceSecilmis = oncekiSecilenler.Contains(o.ToString());

                checkedListOgrenciler.Items.Add(o, onceSecilmis); // Ogrenci nesnesi
                cmbOgrenciNotSecimi.Items.Add(o);
            }

            if (lblOgrenciSayisi != null)
                lblOgrenciSayisi.Text = $"Schülerzahl in Klasse {secilenKlasse} : {filtreli.Count}";

            lblOgrenciSayisi.Left = checkedListOgrenciler.Left;
        }

        //Ekledim
        private void BtnEkle_Click(object sender, EventArgs e)
        {
            // Ortak alanları aktif yap
            txtAdSoyad.Enabled = true;
            dtDogum.Enabled = true;
            cmbHalbjahr.Enabled = true;
            numFehlstunden.Enabled = true;
            numUnentschuldigt.Enabled = true;
            numVerspaetungen.Enabled = true;

            // SE’ye özel tarih alanı aktif mi?
            if (IsSeiteneinstieg && dtSeiteneinsteigergruppe != null)
                dtSeiteneinsteigergruppe.Enabled = true;

            // Alanları temizle
            txtAdSoyad.Text = "";
            dtDogum.Value = DateTime.Today;
            cmbHalbjahr.SelectedIndex = 0;
            numFehlstunden.Value = 0;
            numUnentschuldigt.Value = 0;
            numVerspaetungen.Value = 0;

            if (IsSeiteneinstieg && dtSeiteneinsteigergruppe != null)
                dtSeiteneinsteigergruppe.Value = DateTime.Today;

            // Öğrenci seçim kutusunu temizle
            cmbOgrenciNotSecimi.SelectedIndex = -1;
            cmbOgrenciNotSecimi.Text = "";

            txtAdSoyad.Focus();

            // Kaydet/Vazgeç butonlarını göster
            if (!groupBoxOgrenci.Controls.Contains(btnKaydet))
                groupBoxOgrenci.Controls.Add(btnKaydet);

            if (!groupBoxOgrenci.Controls.Contains(btnVazgec))
                groupBoxOgrenci.Controls.Add(btnVazgec);

            btnKaydet.Visible = true;
            btnVazgec.Visible = true;

            // Ekle ve üst paneli devre dışı bırak
            btnEkle.Enabled = false;
            btnEkle.Visible = false;

            foreach (Control ctrl in headerPanel.Controls)
                ctrl.Enabled = false;

            ResetGuncelleButtonText();

            if (btnGuncelle != null)
                btnGuncelle.Visible = false;

            if (btnSil != null)
                btnSil.Visible = false;
        }

        //Ekledim
        private void BtnKaydet_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtAdSoyad.Text) || cmbKlasse.SelectedIndex == -1 || cmbHalbjahr.SelectedIndex == -1)
            {
                MessageBox.Show("Bitte füllen Sie alle Felder aus.");
                return;
            }

            if (isUpdateMode)
            {
                // GÜNCELLEME MODU
                secilenOgrenciGuncelleme.AdSoyad = txtAdSoyad.Text.Trim();
                secilenOgrenciGuncelleme.Dogum = dtDogum.Value;

                if (IsSeiteneinstieg)
                    secilenOgrenciGuncelleme.Seiteneinsteigergruppe = dtSeiteneinsteigergruppe.Value;

                secilenOgrenciGuncelleme.Klasse = cmbKlasse.SelectedItem.ToString();
                secilenOgrenciGuncelleme.Schuljahr = txtSchuljahr.Text;
                secilenOgrenciGuncelleme.Halbjahr = cmbHalbjahr.SelectedItem.ToString();
                secilenOgrenciGuncelleme.Fehlstunden = (int)numFehlstunden.Value;
                secilenOgrenciGuncelleme.Unentschuldigt = (int)numUnentschuldigt.Value;
                secilenOgrenciGuncelleme.Verspaetungen = (int)numVerspaetungen.Value;

                SaveAllGesamtnoten(secilenOgrenciGuncelleme);
                SaveToJson();
                RefreshOgrenciListeleri();
                MessageBox.Show("Die Stammdaten des Schülers wurden aktualisiert.");
            }
            else
            {
                // EKLEME MODU
                bool ayniOgrenciVar = ogrenciListesi.Any(o =>
                    string.Equals(o.AdSoyad.Trim(), txtAdSoyad.Text.Trim(), StringComparison.OrdinalIgnoreCase) &&
                    o.Klasse == cmbKlasse.SelectedItem.ToString() &&
                    o.Dogum.Date == dtDogum.Value.Date &&
                    (!IsSeiteneinstieg || o.Seiteneinsteigergruppe.Date == dtSeiteneinsteigergruppe.Value.Date)
                );

                if (ayniOgrenciVar)
                {
                    var sonuc = MessageBox.Show(
                        "Ein Schüler mit diesen Daten existiert bereits.\nTrotzdem hinzufügen?",
                        "Warnung",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (sonuc == DialogResult.No)
                        return;
                }

                var yeni = new Ogrenci
                {
                    AdSoyad = txtAdSoyad.Text.Trim(),
                    Dogum = dtDogum.Value,
                    Klasse = cmbKlasse.SelectedItem.ToString(),
                    Schuljahr = txtSchuljahr.Text,
                    Halbjahr = cmbHalbjahr.SelectedItem.ToString(),
                    Fehlstunden = (int)numFehlstunden.Value,
                    Unentschuldigt = (int)numUnentschuldigt.Value,
                    Verspaetungen = (int)numVerspaetungen.Value,
                    Aciklamalar = new Dictionary<string, string>()
                };

                if (IsSeiteneinstieg)
                    yeni.Seiteneinsteigergruppe = dtSeiteneinsteigergruppe.Value;

                SaveAllGesamtnoten(yeni);
                ogrenciListesi.Add(yeni);
                SaveToJson();
                RefreshOgrenciListeleri();
                MessageBox.Show("Schüler wurde hinzugefügt.");
            }

            // ORTAK TEMİZLİK ve MOD SIFIRLAMA
            isUpdateMode = false;
            secilenOgrenciGuncelleme = null;
            ResetGuncelleButtonText();

            txtAdSoyad.Text = "";
            dtDogum.Value = DateTime.Today;
            cmbHalbjahr.SelectedIndex = -1;
            numFehlstunden.Value = 0;
            numUnentschuldigt.Value = 0;
            numVerspaetungen.Value = 0;

            if (IsSeiteneinstieg)
                dtSeiteneinsteigergruppe.Value = DateTime.Today;

            txtAdSoyad.Enabled = false;
            dtDogum.Enabled = false;
            cmbHalbjahr.Enabled = false;
            numFehlstunden.Enabled = false;
            numUnentschuldigt.Enabled = false;
            numVerspaetungen.Enabled = false;

            if (IsSeiteneinstieg)
                dtSeiteneinsteigergruppe.Enabled = false;

            lblBilgiMesaji.Visible = false;
            btnKaydet.Visible = false;
            btnVazgec.Visible = false;
            btnEkle.Enabled = true;
            btnEkle.Visible = true;

            foreach (Control ctrl in headerPanel.Controls)
                ctrl.Enabled = true;

            if (btnGuncelle != null)
                btnGuncelle.Visible = true;

            if (btnSil != null)
                btnSil.Visible = true;

            ResetGuncelleButtonText();
        }

        //Ekledim 
        private void ResetGuncelleButtonText()
        {
            if (btnGuncelle != null)
                btnGuncelle.Text = "Aktualisieren";
            if (btnSil != null)
                btnSil.Text = "Löschen";
        }
        //Ekledim
        private void BtnSil_Click(object sender, EventArgs e)
        {
            if (cmbOgrenciNotSecimi.SelectedItem is not Ogrenci secilen)
            {
                MessageBox.Show("Bitte wählen Sie einen Schüler zum Löschen aus.");

                if (btnSil != null)
                    btnSil.Text = "Löschen";
                return;
            }

            if (MessageBox.Show($"Möchten Sie {secilen.AdSoyad} wirklich löschen?", "Bestätigung", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            ogrenciListesi.Remove(secilen);
            RefreshOgrenciListeleri();

            // Tüm alanları sıfırla
            cmbOgrenciNotSecimi.SelectedItem = null;
            txtAdSoyad.Text = "";
            dtDogum.Value = DateTime.Today;

            if (dtSeiteneinsteigergruppe != null)
                dtSeiteneinsteigergruppe.Value = DateTime.Today;

            cmbHalbjahr.SelectedIndex = -1;
            numFehlstunden.Value = 0;
            numUnentschuldigt.Value = 0;
            numVerspaetungen.Value = 0;

            if (lblTerfi != null)
                lblTerfi.Text = "";

            if (rbJa != null) rbJa.Checked = false;
            if (rbNein != null) rbNein.Checked = false;
            if (txtboxallgemain != null) txtboxallgemain.Text = "";

            // Not alanlarını da temizle
            if (groupBoxNotlar != null)
            {
                foreach (Control control in ControlsRecursive(groupBoxNotlar))
                {
                    switch (control)
                    {
                        case TextBox tb:
                            tb.Text = "";
                            break;
                        case ComboBox cb:
                            cb.SelectedIndex = -1;
                            break;
                        case RadioButton rb:
                            rb.Checked = false;
                            break;
                        case NumericUpDown nud:
                            nud.Value = 0;
                            break;
                    }
                }
            }

            isModified = false; // 🔧 Değişiklik olmadığını açıkça belirt
            btnNotKaydet.Text = "Noten speichern";
            UpdateNotKaydetButtonStyle();

            SaveToJson();

            MessageBox.Show($"{secilen.AdSoyad} wurde gelöscht.");

            if (btnSil != null)
                btnSil.Text = "Löschen";

            if (btnGuncelle != null)
                btnGuncelle.Text = "Aktualisieren";
        }

        //Ekledim
        private void BtnVazgec_Click(object sender, EventArgs e)
        {
            // Alanları temizle
            txtAdSoyad.Text = "";
            txtSchuljahr.Text = "";
            cmbKlasse.SelectedIndex = -1;
            cmbHalbjahr.SelectedIndex = -1;
            numFehlstunden.Value = 0;
            numUnentschuldigt.Value = 0;
            numVerspaetungen.Value = 0;

            // Tüm alanları pasifleştir
            txtAdSoyad.Enabled = false;
            dtDogum.Enabled = false;

            if (dtSeiteneinsteigergruppe != null)
                dtSeiteneinsteigergruppe.Enabled = false;

            cmbHalbjahr.Enabled = false;
            numFehlstunden.Enabled = false;
            numUnentschuldigt.Enabled = false;
            numVerspaetungen.Enabled = false;

            // Etiket ve buton görünürlüğü
            lblBilgiMesaji.Visible = false;
            btnKaydet.Visible = false;
            btnVazgec.Visible = false;
            btnEkle.Enabled = true;

            // Paneldeki diğer kontrolleri yeniden etkinleştir
            foreach (Control ctrl in headerPanel.Controls)
                ctrl.Enabled = true;

            // Güncelleme modunu sıfırla
            isUpdateMode = false;
            secilenOgrenciGuncelleme = null;
            btnEkle.Visible = true;

            if (btnGuncelle != null)
                btnGuncelle.Visible = true;

            if (btnSil != null)
                btnSil.Visible = true;

            ResetGuncelleButtonText();
        }

        //Ekledim
        private void InitLayout()
        {
            InitMainPanels();          // 1. önce main layout
            InitOgrenciBilgileri();    // 2. sonra öğrenci formu
            InitNotlar();              // 3. sonra notlar alanı
            RefreshOgrenciListeleri(); // 4. listeyi yenile

            if (cmbOgrenciNotSecimi.Items.Count > 0)
            {
                cmbOgrenciNotSecimi.SelectedIndex = 0;
            }

            // 5. merkezi temayı uygula
            ThemeHelper.SetDokumanTuru(dokumanTipi);
            ApplyThemeToControl(this); // bütün forma uygula

            InitChangeTracking(); // 6. değişiklik izleme başlat
        }


        //Ekledim.
        private void BtnGeriDon_Click(object sender, EventArgs e)
        {
            bool yedekAlinsinMi = true;

            if (isModified)
            {
                var result = MessageBox.Show(
                    "Änderungen wurden nicht gespeichert. Möchten Sie sie speichern?",
                    "Warnung",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button1);

                if (result == DialogResult.Cancel)
                    return;

                if (result == DialogResult.Yes)
                {
                    BtnNotKaydet_Click(null, EventArgs.Empty, true); // ✅ Sessiz kaydetme doğru şekilde
                    isModified = false;
                    yedekAlinsinMi = false; // ✅ Yeniden yedekleme gerekmez
                }

            }

            if (yedekAlinsinMi && !string.IsNullOrWhiteSpace(StartForm.GirisYapanKullaniciAdi))
            {
                try
                {
                    YedeklemeModulu.TumKlasoruZiple("AppData", StartForm.GirisYapanKullaniciAdi);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Yedekleme hatası:\n" + ex.Message, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            this.DialogResult = DialogResult.Retry;
            this.Close();
        }


        private void InitMainPanels()
        {
            this.WindowState = FormWindowState.Maximized;
            this.Text = "XYZ Zeugnis Manager";
            this.Font = new Font("Segoe UI", 9);

            // Header Panel
            headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = ThemeHelper.HeaderColor1
            };
            headerPanel.Paint += (s, e) =>
            {
                using var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                    headerPanel.ClientRectangle,
                    ThemeHelper.HeaderColor1,
                    ThemeHelper.HeaderColor2,
                    90f
                );
                e.Graphics.FillRectangle(brush, headerPanel.ClientRectangle);
            };
            this.Controls.Add(headerPanel);

            // Shadow Line
            Panel shadowLine = new Panel
            {
                Height = 10,
                Dock = DockStyle.Bottom,
                BackColor = ColorTranslator.FromHtml("#CCCCCC")
            };
            headerPanel.Controls.Add(shadowLine);

            // Selamlama
            string ogretmenAd = secilenOgretmen?.AdSoyad ?? "Lehrkraft";
            Label lblFuerZeugnis = new Label
            {
                Text = $"Hallo {ogretmenAd}, bitte wähle einen Schüler aus:",
                AutoSize = true,
                Font = new Font("Segoe UI", 14, FontStyle.Bold | FontStyle.Italic),
                ForeColor = ThemeHelper.TextPrimary,
                Top = 22,
                Left = 20
            };
            headerPanel.Controls.Add(lblFuerZeugnis);

            // ComboBox
            cmbOgrenciNotSecimi = new ComboBox
            {
                Top = lblFuerZeugnis.Top - 3,
                Left = lblFuerZeugnis.Right + 30,
                Width = 500,
                Height = 35,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 14, FontStyle.Italic),
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 28,
                BackColor = ThemeHelper.TextBoxBack,
                ForeColor = ThemeHelper.TextPrimary,
                FlatStyle = FlatStyle.Flat
            };

            cmbOgrenciNotSecimi.DrawItem += (s, e) =>
            {
                if (e.Index < 0) return;

                var isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

                // Her zaman aynı arka plan rengi
                Color backColor = ThemeHelper.TextBoxBack;
                using var backgroundBrush = new SolidBrush(backColor);
                e.Graphics.FillRectangle(backgroundBrush, e.Bounds);

                // Hafif çerçeve veya gölge efekti (isteğe bağlı, sadece seçiliyse)
                if (isSelected)
                {
                    using var borderPen = new Pen(Color.LightGray, 1);
                    e.Graphics.DrawRectangle(borderPen, e.Bounds.X, e.Bounds.Y, e.Bounds.Width - 1, e.Bounds.Height - 1);
                }

                // Metin rengi ve font
                string text = cmbOgrenciNotSecimi.Items[e.Index].ToString();
                using var textBrush = new SolidBrush(ThemeHelper.TextPrimary);

                // Y dikey ortalama için
                var textSize = e.Graphics.MeasureString(text, cmbOgrenciNotSecimi.Font);
                var textY = e.Bounds.Top + (e.Bounds.Height - textSize.Height) / 2;

                e.Graphics.DrawString(text, cmbOgrenciNotSecimi.Font, textBrush, e.Bounds.Left + 5, textY);

                // Odağı çerçeveyle göster (isteğe bağlı)
                e.DrawFocusRectangle();
            };


            headerPanel.Controls.Add(cmbOgrenciNotSecimi);

            // Not Kaydet Butonu
            btnNotKaydet = new RoundedButton
            {
                Text = GetNotKaydetButtonText(),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Height = 30,
                Top = 18,
                Left = cmbOgrenciNotSecimi.Right + 20,
                FlatStyle = FlatStyle.Flat,
                BackColor = ThemeHelper.ButtonPrimary,
                ForeColor = ThemeHelper.TextOnPrimary,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Image = SystemIcons.Information.ToBitmap(),
                ImageAlign = ContentAlignment.MiddleRight,
                TextAlign = ContentAlignment.MiddleLeft,
                TextImageRelation = TextImageRelation.TextBeforeImage,
                Padding = new Padding(8, 0, 8, 0)
            };
            headerPanel.Controls.Add(btnNotKaydet);

            // Geri Dön Butonu
            // Geri Dön Butonu
            btnGeriDon = new RoundedButton
            {
                Text = "← Zurück",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Height = 35,
                Top = 18,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(200, 200, 200), // Daha koyu gri
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(30, 30, 30),     // Çok koyu gri yazı
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand
            };

            // Hover efekti: daha da koyu gri
            btnGeriDon.MouseEnter += (s, e) =>
            {
                btnGeriDon.BackColor = Color.FromArgb(180, 180, 180); // Hover rengi
            };

            btnGeriDon.MouseLeave += (s, e) =>
            {
                btnGeriDon.BackColor = Color.FromArgb(200, 200, 200); // Normal rengi
            };

            // Tıklama işlevi
            btnGeriDon.Click += BtnGeriDon_Click;

            // Panele ekle
            headerPanel.Controls.Add(btnGeriDon);

            // Sağ hizalama
            headerPanel.SizeChanged += (s, e) =>
            {
                int paddingRight = 20;
                btnGeriDon.Left = headerPanel.Width - btnGeriDon.Width - paddingRight;
            };

            btnGeriDon.Click += BtnGeriDon_Click;
            headerPanel.Controls.Add(btnGeriDon);


            // Main Panel
            mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                AutoScrollMinSize = new Size(0, 6000),
                Padding = new Padding(20),
                Top = headerPanel.Bottom,
                BackColor = Color.White // Zeugnis rengi sabit
            };
            this.Controls.Add(mainPanel);

        }

        //Ekledim
        private string GetNotKaydetButtonText()
        {
            if (cmbOgrenciNotSecimi.SelectedItem is Ogrenci o)
                return $"Noten für {o.AdSoyad} speichern";
            else
                return "Noten speichern";
        }

        //Ekledim
        private void UpdateNotKaydetButtonStyle()
        {
            if (btnNotKaydet == null)
                return;

            if (isModified)
            {
                btnNotKaydet.BackColor = Color.OrangeRed;
                btnNotKaydet.ForeColor = Color.White;
                btnNotKaydet.Font = new Font("Segoe UI", 10, FontStyle.Bold | FontStyle.Underline);
            }
            else
            {
                bool isSeiteneinstieg = DokumanTipi == "SE";

                btnNotKaydet.BackColor = isSeiteneinstieg
                    ? ColorTranslator.FromHtml("#A7D8F5") // Form2 - pastel mavi
                    : ColorTranslator.FromHtml("#007ACC"); // Form1 - koyu mavi

                btnNotKaydet.ForeColor = isSeiteneinstieg
                    ? ColorTranslator.FromHtml("#2E2E2E") // Form2 - koyu gri
                    : Color.White;

                btnNotKaydet.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            }
        }
        //Ekledim
        private void InitOgrenciBilgileri()
        {
            groupBoxOgrenci = new GroupBox
            {
                Text = "  Schülerinformationen  ",
                Width = StandartGroupBoxWidth,
                Top = 120,
                Left = 20,
                Height = 800,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Padding = new Padding(15),
                BackColor = Color.White,
                ForeColor = ThemeHelper.TextPrimary
            };

            int leftLabel = 20;
            int leftControl = 180;
            int top = 100;
            int spacing = 40;

            Font labelFont = new Font("Segoe UI Semibold", 10);
            Font controlFont = new Font("Segoe UI", 10);

            void ApplyInputStyle(Control c)
            {
                c.BackColor = ThemeHelper.TextBoxBack;
                c.Font = controlFont;
                if (c is TextBox || c is ComboBox)
                    c.ForeColor = ThemeHelper.TextPrimary;
            }

            txtAdSoyad = new TextBox { Left = leftControl, Top = top, Width = 250, BorderStyle = BorderStyle.FixedSingle };
            ApplyInputStyle(txtAdSoyad);
            AddStyledLabel("Name:", txtAdSoyad, leftLabel, top, labelFont);

            dtDogum = new DateTimePicker
            {
                Left = leftControl,
                Top = top += spacing,
                Width = 250,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd.MM.yyyy"
            };
            ApplyInputStyle(dtDogum);
            AddStyledLabel("Geburtsdatum:", dtDogum, leftLabel, top, labelFont);

            Label lblSeiteneinsteiger = new Label
            {
                Text = "Seiteneinsteigergruppe:",
                Left = leftLabel,
                Top = top += spacing,
                Font = labelFont,
                AutoSize = true,
                ForeColor = ThemeHelper.TextPrimary,
                Visible = IsSeiteneinstieg
            };
            groupBoxOgrenci.Controls.Add(lblSeiteneinsteiger);

            dtSeiteneinsteigergruppe = new DateTimePicker
            {
                Left = leftControl,
                Top = lblSeiteneinsteiger.Top,
                Width = 250,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd.MM.yyyy",
                Visible = IsSeiteneinstieg,
                Enabled = IsSeiteneinstieg
            };
            ApplyInputStyle(dtSeiteneinsteigergruppe);
            groupBoxOgrenci.Controls.Add(dtSeiteneinsteigergruppe);

            if (IsSeiteneinstieg)
                top = dtSeiteneinsteigergruppe.Bottom;

            cmbKlasse = new ComboBox { Left = leftControl, Top = top += spacing, Width = 250, DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat };
            ApplyInputStyle(cmbKlasse);
            cmbKlasse.Items.Add(secilenKlasse);
            cmbKlasse.SelectedIndex = 0;
            cmbKlasse.Enabled = false;
            AddStyledLabel("Klasse:", cmbKlasse, leftLabel, top, labelFont);

            txtSchuljahr = new TextBox { Left = leftControl, Top = top += spacing, Width = 250, ReadOnly = true, Text = "2024/25", BorderStyle = BorderStyle.FixedSingle };
            ApplyInputStyle(txtSchuljahr);
            AddStyledLabel("Schuljahr:", txtSchuljahr, leftLabel, top, labelFont);

            cmbHalbjahr = new ComboBox
            {
                Left = leftControl,
                Top = top += spacing,
                Width = 250,
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat
            };
            ApplyInputStyle(cmbHalbjahr);
            cmbHalbjahr.Items.Add("2. Halbjahr");
            AddStyledLabel("Halbjahr:", cmbHalbjahr, leftLabel, top, labelFont);

           



            // Add the new label between controls
            Label lblAnwesenheit = new Label
            {
                Text = "Anwesenheit",
                Left = leftLabel,
                Top = top += spacing + 5,  // Extra 5px spacing
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 70, 120),
                AutoSize = true
            };
            groupBoxOgrenci.Controls.Add(lblAnwesenheit);

            // Fehlstunden (Absence hours)
            numFehlstunden = new NumericUpDown
            {
                Left = leftControl,
                Top = top += spacing,
                Width = 100,
                Maximum = 500,
                BorderStyle = BorderStyle.FixedSingle
            };
            ApplyInputStyle(numFehlstunden);
            AddStyledLabel("Fehlstunden:", numFehlstunden, leftLabel, top, labelFont);

            // Unentschuldigt
            numUnentschuldigt = new NumericUpDown { Left = leftControl, Top = top += spacing, Width = 100, Maximum = 500, BorderStyle = BorderStyle.FixedSingle };
            ApplyInputStyle(numUnentschuldigt);
            AddStyledLabel("Unentschuldigt:", numUnentschuldigt, leftLabel, top, labelFont);


            // Verspätungen
            numVerspaetungen = new NumericUpDown { Left = leftControl, Top = top += spacing, Width = 100, Maximum = 500, BorderStyle = BorderStyle.FixedSingle };
            ApplyInputStyle(numVerspaetungen);
            AddStyledLabel("Verspätungen:", numVerspaetungen, leftLabel, top, labelFont);

            btnEkle = new RoundedButton
            {
                Text = "Neue Schüler hinzufügen",
                Width = 200,
                Height = 35,
                Left = leftControl - 100,
                Top = numVerspaetungen.Bottom + 30,
                BackColor = Color.ForestGreen,
                ForeColor = ThemeHelper.TextOnPrimary
            };
            groupBoxOgrenci.Controls.Add(btnEkle);


            // Güncelle
            btnGuncelle = new RoundedButton
            {
                Text = "Aktualisieren",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Height = 35,
                Left = leftControl - 100,
                Top = btnEkle.Bottom + 20,
                BackColor = Color.Orange,
                ForeColor = ThemeHelper.TextOnPrimary
            };
            btnGuncelle.Click += BtnGuncelle_Click;
            groupBoxOgrenci.Controls.Add(btnGuncelle);

            // Sil
            btnSil = new RoundedButton
            {
                Text = "Löschen",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Height = 35,
                Left = leftControl - 100,
                Top = btnGuncelle.Bottom + 20
            };
            groupBoxOgrenci.Controls.Add(btnSil);
            btnSil.BackColor = Color.Red;
            btnSil.ForeColor = ThemeHelper.TextOnPrimary;


            // Bilgi mesajı
            lblBilgiMesaji = new Label
            {
                Text = "Bitte füllen Sie die Felder aus und klicken Sie auf Speichern oder Abbrechen.",
                AutoSize = true,
                ForeColor = Color.FromArgb(0, 102, 204),
                Font = new Font("Segoe UI", 9, FontStyle.Italic),
                Left = btnEkle.Left,
                Top = btnSil.Bottom + 15,
                Visible = false
            };
            groupBoxOgrenci.Controls.Add(lblBilgiMesaji);

            // Kaydet
            btnKaydet = new RoundedButton
            {
                Text = "Speichern",
                Width = 100,
                Height = 35,
                Left = btnSil.Left,
                Top = btnSil.Top,
                Visible = false,
                BackColor = Color.Green,
                ForeColor = Color.White

            };
            btnKaydet.Click += BtnKaydet_Click;
            groupBoxOgrenci.Controls.Add(btnKaydet);

            // Vazgeç
            btnVazgec = new RoundedButton
            {
                Text = "Abbrechen",
                Width = 100,
                Height = 35,
                Left = btnKaydet.Right + 20,
                Top = btnSil.Top,
                Visible = false,
                BackColor = Color.Red,
                ForeColor = Color.White
            };
            btnVazgec.Click += BtnVazgec_Click;
            groupBoxOgrenci.Controls.Add(btnVazgec);
            groupBoxOgrenci.Controls.SetChildIndex(btnVazgec, 0);



            // Öğrenciler Listesi
            checkedListOgrenciler = new CheckedListBox
            {
                Top = 100,
                Left = leftControl + 800,
                Width = 300,
                Height = 500,
                Font = controlFont,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White
            };
            groupBoxOgrenci.Controls.Add(checkedListOgrenciler);


            // Başlık etiketi (Schülerliste)
            Label schülerliste = new Label
            {
                Text = "Schülerliste",
                AutoSize = true,
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 70, 120),
                Top = checkedListOgrenciler.Top - 50,
            };
            groupBoxOgrenci.Controls.Add(schülerliste);

            // Ortala
            schülerliste.Left = checkedListOgrenciler.Left + (checkedListOgrenciler.Width - schülerliste.Width) / 2;

            // Öğrenci sayısı etiketi
            lblOgrenciSayisi = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 70, 120),
                Top = checkedListOgrenciler.Bottom + 5
            };
            groupBoxOgrenci.Controls.Add(lblOgrenciSayisi);

            // Ortala
            //  lblOgrenciSayisi.Left = checkedListOgrenciler.Left + (checkedListOgrenciler.Width - lblOgrenciSayisi.Width) / 2;


            lblogrencitipi = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 70, 120),
                Top = lblOgrenciSayisi.Bottom + 5,
                Left = checkedListOgrenciler.Left

            };

            // Metin, seçilen doküman tipine göre belirlenir
            lblogrencitipi.Text = dokumanTipi == "SE" ? "(Entwicklungszeugnis/Seiteneinstieg)" : "(Zeugnis)";

            groupBoxOgrenci.Controls.Add(lblogrencitipi);





            bool tumSeciliMi = false;

            // "Alles auswählen" yerine geçen buton
            var btnAllesAuswaehlen = new RoundedButton
            {
                Text = "Alles auswählen",
                Width = 200,
                Height = 35,
                Top = checkedListOgrenciler.Top,
                Left = checkedListOgrenciler.Left - 200,
                BackColor = ColorTranslator.FromHtml("#B39DDB"),  // Lila ton
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Visible = true

            };

            // Butona tıklandığında tüm seçimleri yap veya kaldır
            btnAllesAuswaehlen.Click += (s, e) =>
            {
                tumSeciliMi = !tumSeciliMi;
                for (int i = 0; i < checkedListOgrenciler.Items.Count; i++)
                    checkedListOgrenciler.SetItemChecked(i, tumSeciliMi);

                btnAllesAuswaehlen.Text = tumSeciliMi ? "Auswahl aufheben" : "Alles auswählen";
            };

            // GroupBox değilse doğrudan uygun panele ekle
            groupBoxOgrenci.Controls.Add(btnAllesAuswaehlen);




            // PDF ve Klasör Butonları
            btnPdfOlustur = new RoundedButton
            {
                Text = "PDF erstellen",
                Width = 140,
                Height = 35,
                Top = btnAllesAuswaehlen.Bottom + 20,
                Left = checkedListOgrenciler.Left - 200,
                Visible = true

            };
            btnPdfOlustur.BackColor = ColorTranslator.FromHtml("#007ACC");
            btnPdfOlustur.ForeColor = Color.White; // metnin görünür olması için

            btnPdfOlustur.Click += BtnPdfOlustur_Click;
            groupBoxOgrenci.Controls.Add(btnPdfOlustur);


            var btnKlasoruAc = new RoundedButton
            {
                Text = "Ordner öffnen",
                Width = 140,
                Height = 35,
                Top = btnPdfOlustur.Bottom + 20,
                Left = btnPdfOlustur.Left,
                Visible = true
            };
            btnKlasoruAc.BackColor = ColorTranslator.FromHtml("#3399FF");
            btnKlasoruAc.ForeColor = Color.White;



            btnKlasoruAc.Click += (s, e) =>
            {
                string klasorYolu = Properties.Settings.Default.PdfKayitKlasoru;
                if (!string.IsNullOrWhiteSpace(klasorYolu) && Directory.Exists(klasorYolu))
                    System.Diagnostics.Process.Start("explorer.exe", klasorYolu);
                else
                    MessageBox.Show("PDF-Ordner ist nicht festgelegt oder existiert nicht.", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            };
            groupBoxOgrenci.Controls.Add(btnKlasoruAc);

            // Yedekleme butonları
            var btnBackupAc = new RoundedButton
            {
                Text = "Backup-Ordner öffnen",
                Width = 170,
                Height = 35,
                Top = btnKlasoruAc.Bottom + 20,
                Left = btnPdfOlustur.Left,
                Visible = false

            };
            btnBackupAc.Click += (s, e) => YedeklemeModulu.YedekKlasorunuAc();
            groupBoxOgrenci.Controls.Add(btnBackupAc);

            var btnYedekGeriYukle = new RoundedButton
            {
                Text = "JSON Yedeğini Yükle",
                Width = 170,
                Height = 35,
                Top = btnBackupAc.Bottom + 20,
                Left = btnPdfOlustur.Left,
                Visible = false
            };


            btnYedekGeriYukle.Click += (s, e) =>
            {
                using var ofd = new OpenFileDialog
                {
                    Title = "Yedek JSON dosyasını seçin",
                    Filter = "JSON Dosyaları (*.json)|*.json",
                    InitialDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backup")
                };

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    if (MessageBox.Show("Geri yükleme yapmadan önce mevcut veriler kaydedilecek. Devam etmek istiyor musunuz?",
                        "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                        return;

                    SaveToJson();

                    try
                    {
                        File.Copy(ofd.FileName, Path.Combine(Application.StartupPath, "AppData", Path.GetFileName(ofd.FileName)), true);
                        GeriYukleVeGuncelleUI(ofd.FileName);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Dosya geri yüklenirken hata oluştu:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            };
            groupBoxOgrenci.Controls.Add(btnYedekGeriYukle);

            // ListBox yedekler
            var listBoxYedekler = new ListBox
            {
                Width = 300,
                Height = 150,
                Top = btnYedekGeriYukle.Bottom + 15,
                Left = btnPdfOlustur.Left,
                Font = new Font("Segoe UI", 9),
                Name = "listBoxYedekler"
            };
            listBoxYedekler.DoubleClick += listBoxYedekler_DoubleClick;
            groupBoxOgrenci.Controls.Add(listBoxYedekler);

            ListeleYedekDosyalari();

            // Disable inputs initially
            txtAdSoyad.Enabled = false;
            dtDogum.Enabled = false;
            if (IsSeiteneinstieg && dtSeiteneinsteigergruppe != null)
                dtSeiteneinsteigergruppe.Enabled = false;
            txtSchuljahr.Enabled = false;
            cmbHalbjahr.Enabled = false;
            numFehlstunden.Enabled = false;
            numUnentschuldigt.Enabled = false;
            numVerspaetungen.Enabled = false;

            groupBoxOgrenci.Height = Math.Max(groupBoxOgrenci.Height, listBoxYedekler.Bottom + 20);
            mainPanel.Controls.Add(groupBoxOgrenci);
        }

        //Ekledim
        private void listBoxYedekler_DoubleClick(object sender, EventArgs e)
        {
            if (sender is ListBox lb && lb.SelectedItem is YedekDosyaBilgisi secilenYedek)
            {
                var sonuc = MessageBox.Show(
                    $"'{secilenYedek.Tarih:dd.MM.yyyy HH:mm}' tarihli yedeği geri yüklemek istiyor musunuz?",
                    "Yedek Geri Yükleme",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (sonuc == DialogResult.Yes)
                {
                    try
                    {
                        string hedefPath = Path.Combine(Application.StartupPath, "AppData", Path.GetFileName(secilenYedek.DosyaYolu));
                        File.Copy(secilenYedek.DosyaYolu, hedefPath, true);

                        // Güncel yedeği yükle
                        GeriYukleVeGuncelleUI(hedefPath);

                        // Listeyi yenile
                        ListeleYedekDosyalari();

                        MessageBox.Show("Yedek başarıyla geri yüklendi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Yedek geri yüklenirken hata oluştu:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        //Ekledim
        private void ListeleYedekDosyalari()
        {
            string backupKlasoru = Path.Combine(Application.StartupPath, "Backup");
            if (!Directory.Exists(backupKlasoru)) return;

            var dosyalar = Directory.GetFiles(backupKlasoru, "backup_*.json");
            var yedekler = new List<YedekDosyaBilgisi>();

            foreach (var dosya in dosyalar)
            {
                string ad = Path.GetFileNameWithoutExtension(dosya);
                var parcalar = ad.Split('_');

                if (parcalar.Length >= 6)
                {
                    try
                    {
                        string zaman = parcalar[^2] + "_" + parcalar[^1];

                        if (DateTime.TryParseExact(zaman, "yyyyMMdd_HHmmss", null,
                            System.Globalization.DateTimeStyles.None, out DateTime tarih))
                        {
                            string kullanici = string.Join("_", parcalar.Skip(3).Take(parcalar.Length - 5));

                            yedekler.Add(new YedekDosyaBilgisi
                            {
                                DosyaYolu = dosya,
                                Kullanici = kullanici,
                                Tarih = tarih
                            });
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }
            }

            var listBox = groupBoxOgrenci.Controls.Find("listBoxYedekler", true).FirstOrDefault() as ListBox;
            if (listBox != null)
            {
                listBox.DataSource = yedekler.OrderByDescending(y => y.Tarih).ToList();
                listBox.DisplayMember = nameof(YedekDosyaBilgisi.ToString);
                listBox.Visible = false; // Yedekler, yalnızca butonla açıldığında görünür olacak şekilde
            }
        }

        //Ekledim
        private void AddStyledLabel(string text, Control control, int left, int top, Font font)
        {
            var label = new Label
            {
                Text = text,
                Left = left,
                Top = top + 3,
                Font = font,
                ForeColor = Color.FromArgb(64, 64, 64),
                AutoSize = true
            };
            groupBoxOgrenci.Controls.Add(label);
            groupBoxOgrenci.Controls.Add(control);
        }
        //Ekledim
        private void MergePdfFiles(List<string> pdfPaths, string outputPath)
        {
            using (FileStream stream = new FileStream(outputPath, FileMode.Create))
            using (iTextSharp.text.Document doc = new iTextSharp.text.Document())
            using (iTextSharp.text.pdf.PdfCopy pdf = new iTextSharp.text.pdf.PdfCopy(doc, stream))
            {
                doc.Open();
                foreach (string file in pdfPaths)
                {
                    using (PdfReader reader = new PdfReader(file))
                    {
                        for (int i = 1; i <= reader.NumberOfPages; i++)
                        {
                            pdf.AddPage(pdf.GetImportedPage(reader, i));
                        }
                    }
                }
            }
        }

        //Ekledim
        private void BtnGuncelle_Click(object sender, EventArgs e)
        {
            if (cmbOgrenciNotSecimi.SelectedItem is not Ogrenci secilen)
            {
                MessageBox.Show("Bitte wählen Sie einen Schüler zum Aktualisieren aus.");
                return;
            }

            // Alanları düzenlenebilir yap
            txtAdSoyad.Enabled = true;
            dtDogum.Enabled = true;
            cmbHalbjahr.Enabled = true;
            numFehlstunden.Enabled = true;
            numUnentschuldigt.Enabled = true;
            numVerspaetungen.Enabled = true;

            // Güncelleme modunu aktif et
            isUpdateMode = true;
            secilenOgrenciGuncelleme = secilen;

            // Kaydet ve Vazgeç butonlarını göster
            btnKaydet.Visible = true;
            btnVazgec.Visible = true;

            // Diğer butonları gizle ve headerPanel'i kilitle
            btnEkle.Visible = false;
            btnEkle.Enabled = false;

            if (btnGuncelle != null)
                btnGuncelle.Visible = false;

            if (btnSil != null)
                btnSil.Visible = false;

            // headerPanel'deki tüm kontrolleri devre dışı bırak
            foreach (Control ctrl in headerPanel.Controls)
                ctrl.Enabled = false;

            if (IsSeiteneinstieg && dtSeiteneinsteigergruppe != null)
            {
                dtSeiteneinsteigergruppe.Enabled = true;
            }

        }

        //Ekledim

        private void InitNotlar()
        {
            groupBoxNotlar = new GroupBox
            {
                Text = "Noteneingabe",
                Width = StandartGroupBoxWidth,
                Top = groupBoxOgrenci.Bottom + 20,
                Left = 20,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowOnly,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Padding = new Padding(15)
            };

            int groupTop = 20;

            foreach (var baslik in notBasliklari)
            {
                string originalKey = baslik.Key;
                string normalizedKey = NormalizeKey(originalKey);

                int grupHeight = baslik.Value.Count * 50 + 160;
                if (originalKey == "Religion")
                    grupHeight += 100;
                if (originalKey == "Deutsch")
                    grupHeight = 150;

                GroupBox gb = new GroupBox
                {
                    Text = normalizedKey,
                    Font = new Font("Segoe UI", 11, FontStyle.Bold),
                    Width = StandartGroupBoxWidth - 40,
                    Left = 20,
                    Top = groupTop,
                    Height = grupHeight
                };

                int yy = 20;
                var satirListesi = new List<Panel>();

                TextBox txtAciklama = new TextBox
                {
                    Multiline = true,
                    Width = StandartGroupBoxWidth - 80,
                    Left = 20,
                    Height = 90,
                    BorderStyle = BorderStyle.FixedSingle,
                    Font = new Font("Segoe UI", 11, FontStyle.Regular)
                };

                // ComboBox eklenmesi gereken dersler
                var comboBoxDersleri = new List<string>
        {
            "Deutsch", "Mathematik", "Englisch", "Sachunterricht",
            "Sport", "Kunst", "Musik", "Religion",
            "Rechtschreiben", "Lesen", "Sprachgebrauch", "Hören / Sprechen"
        };

                if (comboBoxDersleri.Contains(normalizedKey) && (SinifUc() || SinifDort()))
                {
                    Panel pnlNoteBox = CreateNoteComboBox(normalizedKey, yy);
                    gb.Controls.Add(pnlNoteBox);
                    yy += pnlNoteBox.Height + 10;
                }

                if (originalKey == "Religion" && !SinifDort())
                {
                    Label lbl = new Label
                    {
                        Text = "Teilnahme am Religionsunterricht:",
                        Font = new Font("Segoe UI", 11),
                        Top = yy,
                        Left = 10,
                        Width = 250
                    };
                    gb.Controls.Add(lbl);

                    RadioButton rbEvet = new RadioButton
                    {
                        Text = "Teilgenommen",
                        Font = new Font("Segoe UI", 11),
                        Left = 270,
                        Top = yy,
                        Name = "rbReligion_Evet",
                        Checked = true
                    };

                    RadioButton rbHayir = new RadioButton
                    {
                        Text = "Nicht teilgenommen",
                        Font = new Font("Segoe UI", 11),
                        Left = 380,
                        Top = yy,
                        Name = "rbReligion_Hayir"

                    };


                    gb.Controls.Add(rbEvet);
                    gb.Controls.Add(rbHayir);
                    rbEvet.Checked = true;
                    yy += 40;

                    rbEvet.CheckedChanged += (s, e) =>
                    {
                        if (rbEvet.Checked)
                        {
                            foreach (var p in satirListesi) p.Visible = true;
                            txtAciklama.Text = "";
                        }
                    };

                    rbHayir.CheckedChanged += (s, e) =>
                    {
                        if (rbHayir.Checked)
                        {
                            foreach (var p in satirListesi) p.Visible = false;
                            if (cmbOgrenciNotSecimi.SelectedItem is Ogrenci secilenOgrenci)
                            {
                                txtAciklama.Text = $"{secilenOgrenci.AdSoyad} hat nicht am Religionsunterricht teilgenommen.";
                            }
                        }
                    };
                }

                foreach (var alt in baslik.Value)
                {
                    if (originalKey == "Deutsch" && alt.Contains("Gesamtnote"))
                    {
                        Label lblHinweis = new Label
                        {
                            Text = alt,
                            Font = new Font("Segoe UI", 11, FontStyle.Regular),
                            Top = yy,
                            Left = 15,
                            Width = 1000
                        };
                        gb.Controls.Add(lblHinweis);
                        yy += 35;
                        continue;
                    }

                    Panel satirPanel = new Panel
                    {
                        Left = 10,
                        Top = yy,
                        Width = 1350,
                        Height = 40
                    };

                    Label lblAlt = new Label
                    {
                        Text = alt,
                        Font = new Font("Segoe UI", 11),
                        Top = 8,
                        Left = 5,
                        Width = 850
                    };
                    satirPanel.Controls.Add(lblAlt);

                    for (int i = 1; i <= 5; i++)
                    {
                        satirPanel.Controls.Add(new RadioButton
                        {
                            Text = i.ToString(),
                            Font = new Font("Segoe UI", 11),
                            Left = 880 + (i - 1) * 50,
                            Top = 8,
                            Width = 45
                        });
                    }

                    gb.Controls.Add(satirPanel);
                    satirListesi.Add(satirPanel);
                    yy += 45;
                }

                var dorduncuSiniftaTextBoxOlmayanAlanlar = new HashSet<string>
{
    "Sprachgebrauch", "Lesen", "Rechtschreiben", "Englisch"
};


                // 4. sınıf SE için boşluk içeren başlıklarda da TextBox göster
                var seTextBoxIstisnalari = new HashSet<string>
{
    "Hören / Sprechen ",
    "Rechtschreiben / Schreiben ",
    "Lesen – mit Texten und Medien umgehen "
};

                bool textboxGorunsun =
                    GerekiyorMuAciklama(normalizedKey) &&
                    (
                        !(SinifDort() && dorduncuSiniftaTextBoxOlmayanAlanlar.Contains(normalizedKey)) ||
                        (SinifDort() && IsSeiteneinstieg && seTextBoxIstisnalari.Contains(originalKey))
                    );



                if (textboxGorunsun)
                {
                    txtAciklama.Top = yy;
                    gb.Controls.Add(txtAciklama);
                    aciklamaKutulari[originalKey] = txtAciklama;

                    yy += txtAciklama.Height + 10; // ← BU SATIR ÇOK ÖNEMLİ!
                }



                var sinif4IcinGizliAlanlar = new HashSet<string>
{
    "Religion", "Deutsch", "Sprachgebrauch", "Lesen", "Rechtschreiben",
    "Englisch", "Sachunterricht", "Mathematik", "Sport", "Musik", "Kunst"
};

                var sinif4SeFormulierungenOlmamali = new HashSet<string>
{
    
};


                var seFormulierungenIstisnalari = new HashSet<string>
{
    "Hören / Sprechen ",
    "Rechtschreiben / Schreiben ",
    "Lesen – mit Texten und Medien umgehen "
};

                bool gosterilsinMi =
                    (
                        !(SinifUc() && (normalizedKey == "Deutsch" || normalizedKey == "Mathematik")) &&
                        !(SinifDort() && sinif4IcinGizliAlanlar.Contains(normalizedKey))
                    )
                    || (SinifDort() && IsSeiteneinstieg && seFormulierungenIstisnalari.Contains(originalKey));



                int btnTop = textboxGorunsun ? txtAciklama.Bottom + 5 : yy;
                if (gosterilsinMi)
                {
                    var btnFormulierungen = new RoundedButton
                    {
                        Text = "Formulierungen...",
                        Width = 150,
                        Height = 30,
                        Top = btnTop,
                        Left = txtAciklama.Left,
                        BackColor = Color.MediumSlateBlue,
                        ForeColor = Color.White,
                        Font = new Font("Segoe UI", 10, FontStyle.Bold)
                    };

                    btnFormulierungen.Click += (s, e) =>
                    {
                        string adSoyad = TextHelper.FormatAdSoyadOhneKlasse(cmbOgrenciNotSecimi.SelectedItem?.ToString());
                        var frm = new FormFormulierungen(originalKey, txtAciklama, adSoyad);
                        frm.ShowDialog();
                    };

                    gb.Controls.Add(btnFormulierungen);
                    yy = btnFormulierungen.Bottom + 10;
                }
                else
                {
                    yy += 40;
                }
               




                gb.Height = yy + 20; // 📌 En son dinamik yükseklik hesaplaması burada!
                notPanelleri[originalKey] = satirListesi;
                groupBoxNotlar.Controls.Add(gb);
                groupTop += gb.Height + 15;

            }

            InitKonferenzInsideNotlar(ref groupTop);
            InitGenelAciklamaInsideNotlar(ref groupTop);
            mainPanel.Controls.Add(groupBoxNotlar);
        }

        //Ekledim
        private void InitGenelAciklamaInsideNotlar(ref int groupTop)
        {
            GroupBox gbNewGroup = new GroupBox
            {
                Text = "Allgemeine Bemerkungen",
                Width = StandartGroupBoxWidth - 40,
                Left = 20,
                Top = groupTop,
                Height = 200,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
            };

            Label lblSample = new Label
            {
                Text = "Ich möchte am Ende des Zeugnisses eine allgemeine Bemerkung hinzufügen:",
                Left = 20,
                Top = 30,
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Regular)
            };

            txtboxallgemain = new TextBox
            {
                Multiline = true,
                Height = 100,
                Width = gbNewGroup.Width - 60,
                Left = 30,
                Top = lblSample.Bottom + 20,
                Font = new Font("Segoe UI", 10),
                BorderStyle = BorderStyle.FixedSingle,
                Enabled = true,
                ReadOnly = false,
                BackColor = Color.White
            };

            txtboxallgemain.BringToFront();

            gbNewGroup.Controls.Add(lblSample);
            gbNewGroup.Controls.Add(txtboxallgemain);

            groupBoxNotlar.Controls.Add(gbNewGroup);
            groupTop += gbNewGroup.Height + 15;
        }

        //Ekledim
        private void InitKonferenzInsideNotlar(ref int groupTop)
        {
            gbKonferenz = new GroupBox
            {
                Text = "Konferenzbeschluss",
                Width = StandartGroupBoxWidth - 40,
                Left = 20,
                Top = groupTop,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Padding = new Padding(15),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowOnly
            };

            gbKonferenz.Paint += (sender, e) =>
            {
                Rectangle borderRect = new Rectangle(0, 0, gbKonferenz.Width - 1, gbKonferenz.Height - 1);
                e.Graphics.DrawRectangle(new Pen(Color.FromArgb(200, 200, 200), 1), borderRect);
            };

            // --- Section 1: Conference Date ---
            Panel datePanel = new Panel
            {
                Left = 20,
                Top = 30,
                Width = 300,
                Height = 35
            };

            Label lblKonferenz = new Label
            {
                Text = "Konferenzdatum:",
                Left = 0,
                Top = 8,
                Width = 120,
                Font = new Font("Segoe UI Semibold", 10),
                ForeColor = Color.FromArgb(64, 64, 64)
            };

            dtKonferenz = new DateTimePicker
            {
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd.MM.yyyy",
                Left = lblKonferenz.Right + 10,
                Top = 5,
                Width = 130,
                Font = new Font("Segoe UI", 10),
                CalendarTitleBackColor = Color.FromArgb(0, 114, 198),
                CalendarTitleForeColor = Color.White,
                CalendarTrailingForeColor = Color.FromArgb(180, 180, 180)
            };

            datePanel.Controls.Add(lblKonferenz);
            datePanel.Controls.Add(dtKonferenz);

            // --- Section 2: Promotion Decision ---
            Panel promotionPanel = new Panel
            {
                Left = 20,
                Top = 75,
                Width = gbKonferenz.Width - 40,
                Height = 45
            };

            lblTerfi = new Label
            {
                Text = "___ wird in Klasse ___ versetzt:",
                Left = 0,
                Top = 12,
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(64, 64, 64)
            };

            Panel radioPanel = new Panel
            {
                Left = 400, // ✔️ Sabit bir başlangıç noktası (örnek)
                Top = 3,
                Width = 200,
                Height = 35,
                BackColor = Color.Transparent
            };

            rbJa = new RadioButton
            {
                Text = "Ja",
                Left = 80

                ,  // ✔️ Panel içi sabit konum
                Top = 8,
                Width = 50,
                Height = 20,
                Font = new Font("Segoe UI", 10),
                Appearance = Appearance.Normal,
                AutoSize = false
            };

            rbNein = new RadioButton
            {
                Text = "Nein",
                Left = 150,
                Top = 8,
                Width = 60,
                Height = 20,
                Font = new Font("Segoe UI", 10),
                Appearance = Appearance.Normal,
                AutoSize = false
            };

            radioPanel.Controls.Add(rbJa);
            radioPanel.Controls.Add(rbNein);

            promotionPanel.Controls.Add(lblTerfi);
            promotionPanel.Controls.Add(radioPanel);


            // --- Section 3: Restart Date ---
            Panel restartPanel = new Panel
            {
                Left = 20,
                Top = 130,
                Width = gbKonferenz.Width - 40,
                Height = 180,
                BackColor = Color.Transparent
            };

            Label lblWiederbeginn = new Label
            {
                Text = "Wiederbeginn des Unterrichts:",
                Left = 0,
                Top = 0,
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(64, 64, 64)
            };

            Panel dayDatePanel = new Panel
            {
                Left = 0,
                Top = lblWiederbeginn.Bottom + 15,
                Width = 500,
                Height = 35
            };

            Label lblAm = new Label
            {
                Text = "am",
                Left = 0,
                Top = 8,
                AutoSize = true,
                Font = new Font("Segoe UI", 10)
            };

            cmbWochentag = new ComboBox
            {
                Left = lblAm.Right + 10,
                Top = 5,
                Width = 120,
                Font = new Font("Segoe UI", 10),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.WhiteSmoke,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(64, 64, 64)
            };
            cmbWochentag.Items.AddRange(new[] { "Montag", "Dienstag", "Mittwoch", "Donnerstag", "Freitag" });
            cmbWochentag.SelectedIndex = 0;

            Label lblDen = new Label
            {
                Text = "den",
                Left = cmbWochentag.Right + 15,
                Top = 8,
                AutoSize = true,
                Font = new Font("Segoe UI", 10)
            };

            dtWiederbeginn = new DateTimePicker
            {
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd.MM.yyyy",
                Left = lblDen.Right + 10,
                Top = 5,
                Width = 130,
                Font = new Font("Segoe UI", 10),
                BackColor = Color.WhiteSmoke
            };

            dayDatePanel.Controls.Add(lblAm);
            dayDatePanel.Controls.Add(cmbWochentag);
            dayDatePanel.Controls.Add(lblDen);
            dayDatePanel.Controls.Add(dtWiederbeginn);

            Panel timePanel = new Panel
            {
                Left = 0,
                Top = dayDatePanel.Bottom + 10,
                Width = 500,
                Height = 35
            };

            Label lblUm = new Label
            {
                Text = "um",
                Left = 0,
                Top = 8,
                AutoSize = true,
                Font = new Font("Segoe UI", 10)
            };

            txtUhrzeit = new TextBox
            {
                Left = lblUm.Right + 10,
                Top = 5,
                Width = 60,
                Font = new Font("Segoe UI", 10),
                BackColor = Color.WhiteSmoke,
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = HorizontalAlignment.Center
            };

            Label lblUhr = new Label
            {
                Text = "Uhr",
                Left = txtUhrzeit.Right + 10,
                Top = 8,
                AutoSize = true,
                Font = new Font("Segoe UI", 10)
            };

            timePanel.Controls.Add(lblUm);
            timePanel.Controls.Add(txtUhrzeit);
            timePanel.Controls.Add(lblUhr);

            restartPanel.Controls.Add(lblWiederbeginn);
            restartPanel.Controls.Add(dayDatePanel);
            restartPanel.Controls.Add(timePanel);
            restartPanel.Visible = false;

            gbKonferenz.Controls.Add(datePanel);
            gbKonferenz.Controls.Add(promotionPanel);
            gbKonferenz.Controls.Add(restartPanel);

            groupBoxNotlar.Controls.Add(gbKonferenz);
            groupTop += gbKonferenz.Height + 15;
        }

        //Ekledim
        void SetDynamicTextField(AcroFields form, string fieldName, string text, BaseFont font)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            float fontSize = CalculateFontSize(text, form, fieldName);

            if (fieldName == "txt_Allgemaine" && fontSize > 10f)
                fontSize = 10f;
            else if (fontSize > 8f)
                fontSize = 8f;

            form.SetFieldProperty(fieldName, "textfont", font, null);
            form.SetFieldProperty(fieldName, "textsize", fontSize, null);
            form.SetField(fieldName, text);

            if (fontSize <= 6f && text.Length > 400)
            {
                MessageBox.Show(
                    $"Achtung: Der Text im Feld '{fieldName}' ist sehr lang. Die Schriftgröße wurde auf 6pt gesetzt.\n\nBitte prüfen, ob der Text noch lesbar ist.",
                    "Hinweis", MessageBoxButtons.OK, MessageBoxIcon.Warning
                );
            }
        }


        //Ekledim
        private float CalculateFontSize(string text, AcroFields form, string fieldName)
        {
            const float baseFontSize = 10f;
            const float minFontSize = 6f;
            const float maxFontSize = 10f;

            if (string.IsNullOrWhiteSpace(text))
                return maxFontSize;

            var fieldPositions = form.GetFieldPositions(fieldName);
            if (fieldPositions == null || fieldPositions.Count == 0)
                return maxFontSize;

            var rect = fieldPositions[0].position;
            float width = rect.Width;
            float height = rect.Height;

            // Ortalama karakter genişliği ve satır yüksekliği tahmini
            float avgCharWidth = 4.5f;
            float lineHeight = 1.2f * baseFontSize;

            // Alanın içine kaç satır sığar
            int maxLines = (int)(height / lineHeight);

            // Satır sayısı × satır başına karakter sayısı = toplam karakter kapasitesi
            float maxChars = (width / avgCharWidth) * maxLines;

            // Oranla ölçekleme
            float scale = maxChars / text.Length;
            float adjusted = baseFontSize * scale;

            if (adjusted > maxFontSize) return maxFontSize;
            if (adjusted < minFontSize) return minFontSize;

            return (float)Math.Round(adjusted, 1);
        }

        //Ekledim
        private void InitChangeTracking()
        {
            void MarkChanged(object sender, EventArgs e)
            {
                if (!suppressChangeTracking)
                {
                    isModified = true;
                    UpdateNotKaydetButtonStyle();
                }
            }

            if (groupBoxNotlar != null)
            {
                foreach (Control c in ControlsRecursive(groupBoxNotlar))
                {
                    switch (c)
                    {
                        case TextBox tb:
                            tb.TextChanged += MarkChanged;
                            break;
                        case ComboBox cb:
                            cb.SelectedIndexChanged += MarkChanged;
                            break;
                        case NumericUpDown nud:
                            nud.ValueChanged += MarkChanged;
                            break;
                        case RadioButton rb:
                            rb.CheckedChanged += MarkChanged;
                            break;
                    }
                }
            }

            // Ek kontroller:
            if (dtKonferenz != null)
                dtKonferenz.ValueChanged += MarkChanged;

            if (dtWiederbeginn != null)
                dtWiederbeginn.ValueChanged += MarkChanged;

            if (txtUhrzeit != null)
                txtUhrzeit.TextChanged += MarkChanged;
        }

        //Ekledim
        private IEnumerable<Control> ControlsRecursive(Control parent)
        {
            foreach (Control child in parent.Controls)
            {
                yield return child;

                foreach (Control grandChild in ControlsRecursive(child))
                    yield return grandChild;
            }
        }

        //Ekledim
        private void SaveAllGesamtnoten(Ogrenci secilen)
        {
            foreach (var gb in groupBoxNotlar.Controls.OfType<GroupBox>())
            {
                foreach (Control inner in GetAllControlsRecursive(gb))
                {
                    if (inner is ComboBox cmb && cmb.Tag is string key && key.EndsWith("_Gesamtnote"))
                    {
                        secilen.Aciklamalar[key] = cmb.SelectedItem?.ToString() ?? "---------";
                    }
                }
            }
        }

        //Ekledim
        private IEnumerable<Control> GetAllControlsRecursive(Control parent)
        {
            foreach (Control child in parent.Controls)
            {
                yield return child;

                foreach (Control grandChild in GetAllControlsRecursive(child))
                    yield return grandChild;
            }
        }
        // Kontrol Ettim
        private string NormalizeKey(string key)
        {
            key = key.Trim();
            return key switch
            {
                "Lesen – mit Texten und Medien umgehen" => "Lesen",
                "Sprachgebrauch – Sprechen und Zuhören / Schreiben" => "Sprachgebrauch",
                "Hören / Sprechen" => "Sprachgebrauch",
                "Rechtschreiben / Schreiben" => "Rechtschreiben",
                _ => key
            };
        }

        private Panel CreateNoteComboBox(string tag, int top)
        {
            Label lbl = new Label
            {
                Text = "Gesamtnote:",
                Left = 10,
                Top = 8,
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Italic),
                ForeColor = Color.DimGray
            };

            ComboBox cmb = new ComboBox
            {
                Width = 130,
                Left = 10,
                Top = lbl.Bottom + 4,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                BackColor = Color.White,
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Tag = tag + "_Gesamtnote"
            };
            cmb.Items.AddRange(new[] { "---------", "s. Anlage", "sehr gut", "gut", "befriedigend", "ausreichend", "mangelhaft", "ungenügend" });
            cmb.SelectedIndex = 0;

            Panel pnl = new Panel
            {
                Width = 170,
                Height = 60,
                Top = top,
                Padding = new Padding(5),
                BackColor = Color.White
            };
            pnl.Left = (StandartGroupBoxWidth - 40 - pnl.Width) / 2;
            pnl.Paint += PanelPaintHandler;
            pnl.Controls.Add(lbl);
            pnl.Controls.Add(cmb);
            return pnl;
        }

        private void PanelPaintHandler(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            Panel pnl = (Panel)sender;
            Rectangle shadowRect = new Rectangle(2, 2, pnl.Width - 4, pnl.Height - 4);
            Rectangle mainRect = new Rectangle(0, 0, pnl.Width - 6, pnl.Height - 6);

            using Brush shadow = new SolidBrush(Color.FromArgb(30, 0, 0, 0));
            using Brush background = new SolidBrush(Color.White);
            using Pen border = new Pen(Color.LightGray, 1.2f);

            g.FillRoundedRectangle(shadow, shadowRect, 10);
            g.FillRoundedRectangle(background, mainRect, 10);
            g.DrawRoundedRectangle(border, mainRect, 10);
        }

        private bool GerekiyorMuAciklama(string normalizedKey)
        {
            if (SinifDort() && new[]
                { "Leistungsbereitschaft", "Zuverlässigkeit/Sorgfalt", "Sozialverhalten", "Sprachgebrauch", "Rechtschreiben", "Lesen" }.Contains(normalizedKey))
                return true;
            if (!SinifDort() && !(SinifUc() && (normalizedKey == "Deutsch" || normalizedKey == "Mathematik")))
                return true;
            return false;
        }

        private void KonulariJsondanYukle(string sinifAdi)
        {
            try
            {
                // Seiteneinstieg mi kontrol et
                bool isSeiteneinstieg = seiteneinstiegModu; // Bu field bool olarak FormZeugnisManager içinde tanımlı olmalı

                // Dosya adını dinamik oluştur
                string dosyaKodu = isSeiteneinstieg ? $"seiteneinstieg_{sinifAdi[0]}" : $"{sinifAdi[0]}";
                string dosyaAdi = $"konular_{dosyaKodu}.json";
                string dosyaYolu = Path.Combine(Application.StartupPath, "AppData", dosyaAdi);

                if (!File.Exists(dosyaYolu))
                {
                    MessageBox.Show($"Die Themen-Datei wurde nicht gefunden: {dosyaAdi}", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string json = File.ReadAllText(dosyaYolu);
                notBasliklari = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json)
                                 ?? new Dictionary<string, List<string>>();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler beim Lesen der Themen-Datei:\n" + ex.Message, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private string GetPdfFieldName(string kategoriAdi)
        {
            if (string.IsNullOrWhiteSpace(kategoriAdi))
                return "";

            // SE için özel eşleme
            if (IsSeiteneinstieg && feldMapping.ContainsKey(kategoriAdi))
                kategoriAdi = feldMapping[kategoriAdi];

            // Normalizasyon
            string temiz = kategoriAdi
                .Trim()
                .Replace("–", "_")
                .Replace("/", "_")
                .Replace(" ", "_")
                .Replace("\t", "_")
                .Replace("\n", "_")
                .Replace("ä", "ae")
                .Replace("ö", "oe")
                .Replace("ü", "ue")
                .Replace("ß", "ss")
                .Replace("Ä", "Ae")
                .Replace("Ö", "Oe")
                .Replace("Ü", "Ue")
                .Replace("é", "e");

            temiz = System.Text.RegularExpressions.Regex.Replace(temiz, "_+", "_");
            temiz = temiz.Trim('_');

            return temiz;
        }

        private void ApplyThemeToControl(Control control)
        {
            switch (control)
            {
                case Label label:
                    label.ForeColor = ThemeHelper.TextPrimary;
                    break;

                case RoundedButton roundedButton:
                    if (roundedButton.BackColor == SystemColors.Control || roundedButton.BackColor == Color.Empty)
                        roundedButton.BackColor = ThemeHelper.ButtonPrimary;
                    roundedButton.ForeColor = ThemeHelper.TextOnPrimary;
                    break;

                case MaterialTextBox materialTextBox:
                    materialTextBox.ForeColor = ThemeHelper.TextPrimary;
                    materialTextBox.BackColor = ThemeHelper.TextBoxBack; // Beyaz gibi
                    materialTextBox.Font = new Font("Segoe UI", 10f);
                    materialTextBox.UseAccent = false;
                    materialTextBox.Hint = " "; // Varsayılan grileşmeyi bastırır
                    break;



                case TextBox textBox:
                    textBox.BackColor = ThemeHelper.TextBoxBack;
                    textBox.ForeColor = ThemeHelper.TextPrimary;
                    textBox.BorderStyle = BorderStyle.FixedSingle;
                    textBox.Font = new Font("Segoe UI", 10f);
                    break;

                case ComboBox comboBox:
                    comboBox.BackColor = ThemeHelper.TextBoxBack;
                    comboBox.ForeColor = ThemeHelper.TextPrimary;
                    comboBox.FlatStyle = FlatStyle.Flat;
                    comboBox.Font = new Font("Segoe UI", 10f);
                    break;

                case CheckBox checkBox:
                    checkBox.ForeColor = ThemeHelper.TextPrimary;
                    checkBox.BackColor = Color.Transparent;
                    break;

                case ListBox listBox:
                    listBox.BackColor = ThemeHelper.TextBoxBack;
                    listBox.ForeColor = ThemeHelper.TextPrimary;
                    listBox.Font = new Font("Segoe UI", 10f);
                    break;

                case GroupBox groupBox:
                    groupBox.ForeColor = ThemeHelper.TextPrimary;
                    groupBox.BackColor = Color.Transparent;
                    foreach (Control inner in groupBox.Controls)
                        ApplyThemeToControl(inner);
                    break;

                case Panel panel:
                    panel.BackColor = Color.Transparent;
                    foreach (Control inner in panel.Controls)
                        ApplyThemeToControl(inner);
                    break;
            }
        }







    }
}
