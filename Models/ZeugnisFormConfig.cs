using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IKO_ZeugnisManager_Software.Models
{
    public class ZeugnisFormConfig
    {
        public string PdfTemplate { get; set; }
        public string KonularJson { get; set; }
        public string Kategori { get; set; } // Örn: "normal" veya "seiteneinstieg"
    }
}
