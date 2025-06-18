using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections.Generic;

namespace IKO_ZeugnisManager_Software.Models
{
    public static class OgretmenVeri
    {
        public static List<Ogretmen> TumOgretmenler { get; } = new()
        {
            new Ogretmen { AdSoyad = "Ahmet Kemal", Geschlecht = "männlich", Sinif = "1a" },
            new Ogretmen { AdSoyad = "Ayse Yilmaz", Geschlecht = "weiblich", Sinif = "1b" },
            new Ogretmen { AdSoyad = "Kamile Kacar", Geschlecht = "weiblich", Sinif = "2a" },
            new Ogretmen { AdSoyad = "Covanni Elber", Geschlecht = "männlich", Sinif = "2b" },
            new Ogretmen { AdSoyad = "Hatice Arici", Geschlecht = "weiblich", Sinif = "3a" },
            new Ogretmen { AdSoyad = "Samual Jackson", Geschlecht = "männlich", Sinif = "3b" },
            new Ogretmen { AdSoyad = "Zeynep Yasar", Geschlecht = "weiblich", Sinif = "4a" },
            new Ogretmen { AdSoyad = "Derya Tütüncü", Geschlecht = "weiblich", Sinif = "4b" },
            new Ogretmen { AdSoyad = "Merve Erol", Geschlecht = "weiblich", Sinif = "4c" },
            new Ogretmen { AdSoyad = "Irmak Gezer", Geschlecht = "weiblich", Sinif = "ALL" },
           
        };
    }
}
