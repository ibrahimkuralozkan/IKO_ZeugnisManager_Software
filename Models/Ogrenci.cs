using System;
using System.Collections.Generic;

namespace IKO_ZeugnisManager_Software.Models
{
    public class Ogrenci
    {
        public string AdSoyad { get; set; }
        public DateTime Dogum { get; set; }
        public DateTime Seiteneinsteigergruppe { get; set; }

        public string Klasse { get; set; }
        public string Schuljahr { get; set; }
        public string Halbjahr { get; set; }
        public int Fehlstunden { get; set; }
        public int Unentschuldigt { get; set; }
        public int Verspaetungen { get; set; }
        public bool? ReligionTeilnahme { get; set; }
        public DateTime? Konferenzdatum { get; set; }
        public bool? SchuleingangsphaseJa { get; set; }

        public string? WiederbeginnWochentag { get; set; }
        public DateTime? WiederbeginnDatum { get; set; }

        private string _wiederbeginnUhrzeit;
        public string WiederbeginnUhrzeit
        {
            get => _wiederbeginnUhrzeit ?? "";
            set => _wiederbeginnUhrzeit = value;
        }

        private string _allgemeineBemerkungText;
        public string AllgemeineBemerkungText
        {
            get => _allgemeineBemerkungText ?? "";
            set => _allgemeineBemerkungText = value;
        }

        public bool? AllgemeineBemerkungJa { get; set; }

        public Dictionary<string, int[]> Notlar { get; set; } = new();
        public Dictionary<string, string> Aciklamalar { get; set; } = new();
        // test
        public override string ToString() => $"{AdSoyad} - {Klasse}";
    }
}