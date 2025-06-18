using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IKO_ZeugnisManager_Software.Models
{
    public class YedekDosyaBilgisi
    {
        public string DosyaYolu { get; set; }
        public string Kullanici { get; set; }
        public DateTime Tarih { get; set; }

        public override string ToString()
        {
            return $"{Tarih:dd.MM.yyyy HH:mm:ss} - {Kullanici}";
        }
    }
}
