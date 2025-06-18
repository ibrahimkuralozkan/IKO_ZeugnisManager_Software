using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using IKO_ZeugnisManager_Software.Models;

namespace IKO_ZeugnisManager_Software
{
    internal static class Program
    {
        // ⏺ Global olarak kullanıcı adını sakla
        public static string GirisYapanKullanici { get; set; } = "Unbekannt";

        [STAThread]
        static void Main()
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            Application.ApplicationExit += OnApplicationExit;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            bool restart;
            do
            {
                restart = false;

                using (var startForm = new StartForm())
                {
                    if (startForm.ShowDialog() == DialogResult.OK)
                    {
                        // ✅ Kullanıcı adı alınır ve global değişkene yazılır
                        GirisYapanKullanici = StartForm.GirisYapanKullaniciAdi ?? "Unbekannt";

                        // ✅ Devam butonuna basıldığında yedek al
                        YedeklemeModulu.TumKlasoruZiple("AppData", GirisYapanKullanici);

                        // 🔒 Konular ve PDF’leri salt okunur yap
                        string appDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AppData");
                        if (Directory.Exists(appDataPath))
                        {
                            var konularDosyalari = Directory.GetFiles(appDataPath, "konular_*.json");
                            foreach (var dosya in konularDosyalari)
                                YedeklemeModulu.DosyayiSaltOkunurYap(dosya);
                        }

                        string templatesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates");
                        if (Directory.Exists(templatesPath))
                        {
                            var pdfDosyalari = Directory.GetFiles(templatesPath, "pdf_master_*.pdf");
                            foreach (var dosya in pdfDosyalari)
                                YedeklemeModulu.DosyayiSaltOkunurYap(dosya);
                        }

                        // Ana formu başlat
                        using (var mainForm = new FormZeugnisManager(
                            startForm.SecilenKlasse,
                            startForm.DokumanTipi,
                            startForm.SecilenOgretmen,
                            startForm.SecilenCinsiyet,
                            startForm.IsSeiteneinstieg
                        ))
                        {
                            var result = mainForm.ShowDialog();
                            if (result == DialogResult.Retry)
                                restart = true;
                        }
                    }
                }

            } while (restart);
        }

        // ⛔ Uygulama kapanırken de yedek al
        private static void OnApplicationExit(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(GirisYapanKullanici))
            {
                YedeklemeModulu.TumKlasoruZiple("AppData", GirisYapanKullanici);
            }
        }
    }
}
