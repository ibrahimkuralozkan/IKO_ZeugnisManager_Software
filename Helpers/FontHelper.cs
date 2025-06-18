using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Drawing;

namespace IKO_ZeugnisManager_Software.Helpers
{
    public static class FontHelper
    {
        public static Font Regular(float size = 10f) => new Font("Segoe UI", size, FontStyle.Regular);
        public static Font Bold(float size = 10f) => new Font("Segoe UI", size, FontStyle.Bold);
        public static Font Italic(float size = 10f) => new Font("Segoe UI", size, FontStyle.Italic);
        public static Font BoldItalic(float size = 10f) => new Font("Segoe UI", size, FontStyle.Bold | FontStyle.Italic);

        public static Font Title => new Font("Segoe UI Semibold", 20, FontStyle.Bold);
        public static Font SectionHeader => new Font("Segoe UI", 14, FontStyle.Bold);
    }
}
