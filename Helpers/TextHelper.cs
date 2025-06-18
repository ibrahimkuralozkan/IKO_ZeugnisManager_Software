using System;

namespace IKO_ZeugnisManager_Software.Helpers
{
    internal class TextHelper
    {
        /// <summary>
        /// "Soyad, Ad" formatındaki metni "Ad Soyad" olarak döndürür.
        /// Eğer virgül yoksa metni aynen döndürür.
        /// </summary>
        public static string FormatAdSoyad(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return "";

            if (raw.Contains(","))
            {
                var parts = raw.Split(',');
                if (parts.Length == 2)
                    return $"{parts[1].Trim()} {parts[0].Trim()}";
            }

            return raw.Trim();
        }

        /// <summary>
        /// "Soyad, Ad - 1a" gibi bir girişten sadece Ad ve ikinci isimleri döndürür.
        /// Örnek: "Abalo, Steven Komlan - 1a" → "Steven Komlan"
        /// </summary>
        public static string ExtractVorname(string adSoyad)
        {
            if (string.IsNullOrWhiteSpace(adSoyad))
                return "";

            // Eğer adSoyad “Soyad, Ad” biçimindeyse önce düzelt
            if (adSoyad.Contains(","))
                adSoyad = FormatAdSoyad(adSoyad);

            // Eğer sınıf bilgisi varsa (örn. “Steven Komlan Abalo - 1a”), onu temizle
            if (adSoyad.Contains("-"))
                adSoyad = adSoyad.Split('-')[0].Trim();

            // Sadece ilk kelimeyi al (ilk ad)
            var parcalar = adSoyad.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parcalar.Length > 0 ? parcalar[0] : "";
        }


        /// <summary>
        /// "Soyad, Ad - 1a" gibi girdilerden sadece "Ad Soyad" döner. Sınıf bilgisini temizler.
        /// </summary>
        public static string FormatAdSoyadOhneKlasse(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return "";

            // "Soyad, Ad - 1a" → "Soyad, Ad"
            if (raw.Contains("-"))
                raw = raw.Split('-')[0].Trim();

            return FormatAdSoyad(raw);
        }

        /// <summary>
        /// Cümle içerisinde isim tekrarını engellemek için bir yardımcı metot.
        /// </summary>
        public static string SafePrependName(string vorname, string cumle)
        {
            if (string.IsNullOrWhiteSpace(vorname) || string.IsNullOrWhiteSpace(cumle))
                return cumle?.Trim() ?? "";

            if (cumle.StartsWith(vorname))
                return cumle.Trim();

            return $"{vorname} {cumle.Trim()}";
        }
    }
}
