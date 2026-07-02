using static CollectorFLN.UI.Theme;

namespace CollectorFLN.UI
{
    public static class InterfaceBuilder
    {
        // Helper to build styled label
        public static Label MakeLabel(string text, Point loc, float size = 9f, Color? color = null, FontStyle style = FontStyle.Regular)
        {
            return new Label
            {
                Text = text,
                Location = loc,
                AutoSize = true,
                Font = new Font("Segoe UI", size, style),
                ForeColor = color ?? textPrim,
                BackColor = Color.Transparent
            };
        }

        // Helper to build styled label
        public static Label MakeLabel(Point loc, float size = 9f, Color? color = null, FontStyle style = FontStyle.Regular)
        {
            return new Label
            {
                Text = string.Empty,
                Location = loc,
                AutoSize = true,
                Font = new Font("Segoe UI", size, style),
                ForeColor = color ?? textPrim,
                BackColor = Color.Transparent
            };
        }

        // Helper to build styled panel
        public static Panel MakeCard(Point loc, Size size, Color? fill = null)
        {
            var p = new Panel
            {
                Location = loc,
                Size = size,
                BackColor = fill ?? surface
            };
            p.Paint += (s, e) =>
            {
                using var pen = new Pen(border, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
            };
            return p;
        }

        // Helper to build styled text box
        public static TextBox MakeTextBox(string text, Point loc, Size sz)
        {
            return new TextBox
            {
                Text = text,
                Location = loc,
                Size = sz,
                ForeColor = textPrim,
                BackColor = Color.FromArgb(38, 38, 52),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10f),
                TextAlign = HorizontalAlignment.Center
            };
        }

        // Helper to build styled text box
        public static TextBox MakeTextBox(Point loc, Size sz)
        {
            return new TextBox
            {
                Text = string.Empty,
                Location = loc,
                Size = sz,
                ForeColor = textPrim,
                BackColor = Color.FromArgb(38, 38, 52),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10f),
                TextAlign = HorizontalAlignment.Center
            };
        }

        // Helper to build styled module toggle checkbox
        public static CheckBox MakeModuleToggle(string text, Point loc, bool isChecked)
        {
            return new CheckBox
            {
                Text = text,
                Location = loc,
                Checked = isChecked,
                AutoSize = true,
                ForeColor = isChecked ? accent : textMuted,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
        }
    }
}
