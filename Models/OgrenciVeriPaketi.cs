using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IKO_ZeugnisManager_Software.Models
{
    public class OgrenciVeriPaketi
    {
        public int Versiyon { get; set; }
        public List<Ogrenci> Ogrenciler { get; set; } = new();
    }
}
