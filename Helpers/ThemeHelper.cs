using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Drawing;

namespace IKO_ZeugnisManager_Software.Helpers
{
    public static class ThemeHelper
    {
        private static string dokumanTuru = "Zeugnis";

        public static void SetDokumanTuru(string tip)
        {
            dokumanTuru = tip;
        }

        public static bool IsSE => dokumanTuru == "SE";

        public static Color HeaderColor1 => IsSE ? ColorTranslator.FromHtml("#ffe6f0") : ColorTranslator.FromHtml("#f4f9ff");
        public static Color HeaderColor2 => IsSE ? ColorTranslator.FromHtml("#ffccd9") : ColorTranslator.FromHtml("#d8e6f9");

        public static Color MainPanelColor => IsSE ? ColorTranslator.FromHtml("#fff5fa") : Color.White;
        public static Color FormBackground => IsSE ? ColorTranslator.FromHtml("#fff0f5") : Color.WhiteSmoke;

        public static Color ButtonPrimary => IsSE ? ColorTranslator.FromHtml("#f08080") : ColorTranslator.FromHtml("#007ACC");
        public static Color ButtonNeutral => ColorTranslator.FromHtml("#E0E0E0");

        public static Color TextPrimary => IsSE ? ColorTranslator.FromHtml("#880e4f") : Color.FromArgb(33, 33, 33);
        public static Color TextOnPrimary => Color.White;
        public static Color TextBoxBack => Color.White;
        public static Color ButtonDanger => IsSE ? ColorTranslator.FromHtml("#ff9999") : ColorTranslator.FromHtml("#D32F2F");
        public static Color ButtonWarning => IsSE ? ColorTranslator.FromHtml("#FFD699") : ColorTranslator.FromHtml("#FFA000");
        public static Color ButtonSuccess => IsSE ? ColorTranslator.FromHtml("#C8E6C9") : ColorTranslator.FromHtml("#4CAF50");
    
    }
}
