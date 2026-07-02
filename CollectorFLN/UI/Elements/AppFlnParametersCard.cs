using CollectorFLN;
using static CollectorFLN.UI.InterfaceBuilder;
using static CollectorFLN.UI.Theme;

namespace CollectorFLN.UI.Elements
{
    public class AppFlnParametersCard
    {
        private static readonly Point cardPosition = new(20, 264);
        private static readonly Size cardSize = new(440, 120);
        private static readonly string[] snapOptions = { "1/2", "1/3", "1/4", "1/6", "1/8", "1/12", "1/16" };

        public Panel flnParametersCard { get; }

        private readonly RadioButton rbMsMode;
        private readonly RadioButton rbSnapMode;
        private readonly Label lblGapLabel;
        private readonly TextBox txtGap;
        private readonly ComboBox cmbSnapDivisor;
        private readonly ToolTip tt = new() { InitialDelay = 300, ReshowDelay = 100 };

        public event EventHandler<bool>? GapModeChanged;      
        public event EventHandler<string>? GapMsChanged;   
        public event EventHandler<string>? GapSnapChanged; 

        public AppFlnParametersCard(Config config)
        {
            flnParametersCard = MakeCard(cardPosition, cardSize);

            Label flnHeader = MakeLabel("FLN PARAMETERS", new Point(16, 12), 7.5f, textMuted, FontStyle.Bold);
            Panel flnDivider = new Panel { Location = new Point(14, 30), Size = new Size(412, 1), BackColor = border };
            Label lblGapMode = MakeLabel("Gap Mode", new Point(16, 44), 9f, textMuted);

            rbMsMode = new RadioButton
            {
                Text = "Ms",
                Location = new Point(195, 42),
                AutoSize = true,
                ForeColor = config.UseSnapMode ? textMuted : accent,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Checked = !config.UseSnapMode
            };

            rbSnapMode = new RadioButton
            {
                Text = "Snap",
                Location = new Point(260, 42),
                AutoSize = true,
                ForeColor = config.UseSnapMode ? accent : textMuted,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Checked = config.UseSnapMode
            };

            lblGapLabel = MakeLabel("Gap", new Point(16, 80), 9f, textMuted);

            txtGap = MakeTextBox(new Point(195, 76), new Size(70, 26));
            txtGap.Text = config.Gap.ToString();
            txtGap.Visible = !config.UseSnapMode;

            cmbSnapDivisor = new ComboBox
            {
                Location = new Point(195, 76),
                Size = new Size(70, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                ForeColor = textPrim,
                BackColor = Color.FromArgb(38, 38, 52),
                Font = new Font("Segoe UI", 10f),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Visible = config.UseSnapMode
            };

            cmbSnapDivisor.Items.AddRange(snapOptions);
            cmbSnapDivisor.SelectedItem = config.SnapDivisor.ToString() ?? snapOptions[0];

            tt.SetToolTip(rbMsMode, "Use a fixed millisecond value for LN gaps.");
            tt.SetToolTip(rbSnapMode, "Use beat snap divisor for LN gaps (adapts to BPM).");

            flnParametersCard.Controls.AddRange(new Control[]
            {
                flnHeader, flnDivider, lblGapMode,
                rbMsMode, rbSnapMode, lblGapLabel, txtGap, cmbSnapDivisor
            });

            WireEvents();
        }

        private void WireEvents()
        {
            rbMsMode.CheckedChanged += (s, e) =>
            {
                if (!rbMsMode.Checked) return; 

                txtGap.Visible = true;
                cmbSnapDivisor.Visible = false;
                rbMsMode.ForeColor = accent;
                rbSnapMode.ForeColor = textMuted;
                GapModeChanged?.Invoke(this, false);
            };

            rbSnapMode.CheckedChanged += (s, e) =>
            {
                if (!rbSnapMode.Checked) return;

                txtGap.Visible = false;
                cmbSnapDivisor.Visible = true;
                rbSnapMode.ForeColor = accent;
                rbMsMode.ForeColor = textMuted;
                GapModeChanged?.Invoke(this, true);
            };

            txtGap.TextChanged += (s, e) =>
            {
                GapMsChanged?.Invoke(this, txtGap.Text);
            };

            cmbSnapDivisor.SelectedIndexChanged += (s, e) =>
            {
                GapSnapChanged?.Invoke(this, cmbSnapDivisor.SelectedItem?.ToString() ?? "");
            };
        }
    }
}