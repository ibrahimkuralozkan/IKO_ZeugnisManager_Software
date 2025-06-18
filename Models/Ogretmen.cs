using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IKO_ZeugnisManager_Software.Models
{
    public class Ogretmen
    {
        public string AdSoyad { get; set; }
        public string Geschlecht { get; set; } // männlich veya weiblich
        public string Sinif { get; set; } // BURAYA sınıfı da ekliyoruz

        public override string ToString()
        {
            return AdSoyad; // ComboBox'ta sadece ad soyad gözüksün
        }
    }

}
