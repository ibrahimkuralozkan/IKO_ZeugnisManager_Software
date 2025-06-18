using IKO_ZeugnisManager_Software.Helpers;
using System;
using System;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace IKO_ZeugnisManager_Software.Controls
{
    public class RoundedButton : Button
    {
        public int CornerRadius { get; set; } = 20;

        public RoundedButton()
        {
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            BackColor = ThemeHelper.ButtonPrimary;
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 10, FontStyle.Bold);
            Padding = new Padding(16, 6, 16, 6);
            Cursor = Cursors.Hand;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = GetRoundedPath(ClientRectangle, CornerRadius);
            using var brush = new SolidBrush(BackColor);
            using var textBrush = new SolidBrush(ForeColor);

            e.Graphics.FillPath(brush, path);

            var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter
            };

            e.Graphics.DrawString(Text, Font, textBrush, ClientRectangle, sf);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Region = new Region(GetRoundedPath(ClientRectangle, CornerRadius));
        }

        private GraphicsPath GetRoundedPath(Rectangle rect, int radius)
        {
            int diameter = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
