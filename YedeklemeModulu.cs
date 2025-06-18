using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.IO;
using System.IO.Compression;

namespace IKO_ZeugnisManager_Software
{
    public static class YedeklemeModulu
    {
        private static readonly string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        private static readonly string backupDir = Path.Combine(baseDir, "Backup");
        private static readonly string logPath = Path.Combine(baseDir, "log.txt");

        public static void DosyaYedekle(string jsonPath)
        {
            try
            {
                string backupKlasoru = Path.Combine(Application.StartupPath, "Backup");
                Directory.CreateDirectory(backupKlasoru);

                // ⬇️ Tam burada bu satırı görmelisin:
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

                string kullanici = StartForm.GirisYapanKullaniciAdi?.Replace(" ", "_") ?? "Unbekannt";
                string dosyaAdi = Path.GetFileNameWithoutExtension(jsonPath);
                string yedekDosyaAdi = $"backup_{dosyaAdi}_{kullanici}_{timestamp}.json";
                string hedefPath = Path.Combine(backupKlasoru, yedekDosyaAdi);

                File.Copy(jsonPath, hedefPath, true);
                LogKaydet($"Yedek oluşturuldu: {yedekDosyaAdi}");
            }
            catch (Exception ex)
            {
                LogKaydet($"HATA - Yedek oluşturulamadı: {ex.Message}");
            }
        }


        public static void YedekKlasorunuAc()
        {
            try
            {
                string backupPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backup");
                Directory.CreateDirectory(backupPath); // klasör yoksa oluştur

                System.Diagnostics.Process.Start("explorer.exe", backupPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Yedek klasörü açılamadı:\n\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogKaydet($"HATA - Yedek klasörü açılamadı: {ex.Message}");
            }
        }

        public static void JsonYedegiGeriYukle()
        {
            try
            {
                using OpenFileDialog ofd = new OpenFileDialog
                {
                    Title = "Geri Yüklenecek JSON Yedeğini Seç",
                    Filter = "JSON Dosyaları (*.json)|*.json",
                    InitialDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backup")
                };

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    string secilenYedek = ofd.FileName;
                    string hedefKlasor = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AppData");

                    string hedefDosya = Path.Combine(hedefKlasor, Path.GetFileName(secilenYedek));

                    // Eski dosyanın üzerine yaz
                    File.Copy(secilenYedek, hedefDosya, true);

                    MessageBox.Show($"Yedek başarıyla geri yüklendi:\n{Path.GetFileName(secilenYedek)}",
                                    "Geri Yükleme Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    LogKaydet($"JSON yedeği geri yüklendi: {Path.GetFileName(secilenYedek)}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Geri yükleme sırasında hata oluştu:\n\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogKaydet($"HATA - JSON yedeği geri yüklenemedi: {ex.Message}");
            }
        }


        public static void DosyayiSaltOkunurYap(string tekilDosyaYolu = null)
        {
            try
            {
                if (tekilDosyaYolu != null)
                {
                    // Tek bir dosya verilmişse onu koru
                    if (File.Exists(tekilDosyaYolu))
                    {
                        File.SetAttributes(tekilDosyaYolu, File.GetAttributes(tekilDosyaYolu) | FileAttributes.ReadOnly);
                        LogKaydet($"{Path.GetFileName(tekilDosyaYolu)} dosyası salt okunur yapıldı.");
                    }
                }
                else
                {
                    // Tüm hedef dosyaları tara ve sırayla uygula
                    string appDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AppData");

                    if (!Directory.Exists(appDataPath))
                        return;

                    // konular_*.json ve *_pdf_master*.pdf dosyalarını seç
                    string[] hedefDosyalar = Directory.GetFiles(appDataPath, "*", SearchOption.TopDirectoryOnly)
                        .Where(f => Path.GetFileName(f).StartsWith("konular_") ||
                                    Path.GetFileName(f).Contains("_pdf_master"))
                        .ToArray();

                    foreach (string dosya in hedefDosyalar)
                    {
                        try
                        {
                            File.SetAttributes(dosya, File.GetAttributes(dosya) | FileAttributes.ReadOnly);
                            LogKaydet($"{Path.GetFileName(dosya)} dosyası otomatik olarak salt okunur yapıldı.");
                        }
                        catch (Exception ex)
                        {
                            LogKaydet($"HATA - {dosya} için salt okunur yapılamadı: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Salt okunur işleminde hata oluştu:\n\n{ex.Message}",
                                "Dosya Koruma Hatası", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                LogKaydet($"HATA - Salt okunur genel hata: {ex.Message}");
            }
        }


        public static void GuvenliKaydet(string jsonPath, string json)
        {
            try
            {
                string tempPath = jsonPath + ".tmp";

                // Geçici dosyaya yaz
                File.WriteAllText(tempPath, json);

                // Başarılıysa orijinal dosya ile değiştir
                File.Copy(tempPath, jsonPath, true);
                File.Delete(tempPath);

                LogKaydet($"{Path.GetFileName(jsonPath)} dosyası güvenli şekilde kaydedildi.");
            }
            catch (Exception ex)
            {
                LogKaydet($"HATA - Güvenli kaydetme başarısız: {ex.Message}");
                MessageBox.Show($"Dosya kaydedilirken bir hata oluştu:\n\n{ex.Message}",
                                "Kaydetme Hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static void TumKlasoruZiple(string klasorAdi = "AppData", string kullanici = null)
        {
            try
            {
                string kaynakKlasor = Path.Combine(baseDir, klasorAdi);
                if (!Directory.Exists(kaynakKlasor))
                {
                    LogKaydet("KAYNAK klasör bulunamadı, zipleme iptal: " + kaynakKlasor);
                    return;
                }

                Directory.CreateDirectory(backupDir);

                // 🕓 Artık milisaniyeye kadar detaylı zaman etiketi
                string zamanDamgasi = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                string temizKullanici = string.IsNullOrWhiteSpace(kullanici) ? "Unbekannt" : kullanici.Replace(" ", "_");
                string zipDosyaAdi = $"Yedek_{klasorAdi}_{zamanDamgasi}_{temizKullanici}.zip";
                string zipYolu = Path.Combine(backupDir, zipDosyaAdi);

                ZipFile.CreateFromDirectory(kaynakKlasor, zipYolu);
                LogKaydet($"ZIP yedeği oluşturuldu: {zipDosyaAdi}");
            }
            catch (Exception ex)
            {
                LogKaydet($"HATA - Zipleme başarısız: {ex.Message}");
                MessageBox.Show($"'{klasorAdi}' klasörü ziplenirken hata oluştu:\n\n{ex.Message}",
                                "ZIP Hatası", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        public static void LogKaydet(string mesaj)
        {
            try
            {
                string satir = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {mesaj}";
                File.AppendAllText(logPath, satir + Environment.NewLine);
            }
            catch { /* Sessiz hata geç */ }
        }
    }
}
