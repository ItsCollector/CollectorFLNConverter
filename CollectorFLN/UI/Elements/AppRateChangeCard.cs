using static CollectorFLN.UI.InterfaceBuilder;
using static CollectorFLN.UI.Theme;

namespace CollectorFLN.UI.Elements
{
    public class AppRateChangeCard
    {
        private static readonly Point cardPosition = new(20, 402);
        private static readonly Size cardSize = new(440, 148);

        public Panel rateChangeCard { get; }

        private readonly Label rateChangeHeader;
        private readonly Label txtOldBPM;
        private readonly TextBox txtNewBPM;
        private readonly TextBox txtRate;
        private readonly ToolTip tt = new() { InitialDelay = 300, ReshowDelay = 100 };
        private readonly CheckBox increasePitch;
        private readonly CheckBox decreasePitch;

        private readonly double originalBpm;
        private bool suppressEvents; // guards against BPM<->Rate update loops

        public event EventHandler<double>? RateChanged; // fires with the resolved rate multiplier

        public AppRateChangeCard(RateChangeConfig config)
        {
            originalBpm = config.OriginalBpm;

            rateChangeCard = MakeCard(cardPosition, cardSize);

            rateChangeHeader = MakeLabel("RATE CHANGE", new Point(16, 12), 7.5f, textMuted, FontStyle.Bold);
            Panel rateDivider = new Panel { Location = new Point(14, 30), Size = new Size(412, 1), BackColor = border };
            Label lblOldBPM = MakeLabel($"Original BPM", new Point(16, 46), 9f, textMuted);
            Label lblNewBPM = MakeLabel("Target BPM", new Point(16, 74), 9f, textMuted); // 16 74
            Label lblRate = MakeLabel("Rate (x)", new Point(16, 102), 9f, textMuted);

            txtOldBPM = MakeLabel($"{(originalBpm > 0 ? originalBpm.ToString("0.##") : "N/A")}", new Point(160, 46), 9f, textMuted);
            txtNewBPM = MakeTextBox(originalBpm > 0 ? originalBpm.ToString("0.##") : "", new Point(160, 70), new Size(70, 26));
            txtRate = MakeTextBox(config.Rate.ToString("0.00"), new Point(160, 98), new Size(70, 26));

            tt.SetToolTip(txtNewBPM, "Set a target BPM — rate is calculated automatically.");
            tt.SetToolTip(txtRate, "Set a playback rate — BPM is calculated automatically.");

            increasePitch = new CheckBox
            {
                Text = "Uprates increase pitch",
                Location = new Point(260, 72),
                AutoSize = true,
                ForeColor = config.increasePitch ? accent : textMuted,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand,
                Checked = config.increasePitch
            };

            decreasePitch = new CheckBox
            {
                Text = "Downrates decrease pitch",
                Location = new Point(260, 98),
                AutoSize = true,
                ForeColor = config.decreasePitch ? accent : textMuted,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand,
                Checked = config.decreasePitch
            };

            rateChangeCard.Controls.AddRange(new Control[]
            {
                rateChangeHeader, rateDivider, lblOldBPM, lblNewBPM, txtOldBPM, txtNewBPM, lblRate, txtRate, increasePitch, decreasePitch
            });

            WireEvents();
        }

        private void WireEvents()
        {
            txtNewBPM.TextChanged += (s, e) =>
            {
                if (suppressEvents) return;
                if (originalBpm <= 0) return; // can't compute a rate without a baseline
                if (!double.TryParse(txtNewBPM.Text, out double bpm) || bpm <= 0) return;

                double rate = bpm / originalBpm;

                suppressEvents = true;
                txtRate.Text = rate.ToString("0.00");
                suppressEvents = false;

                RateChanged?.Invoke(this, rate);
            };

            txtRate.TextChanged += (s, e) =>
            {
                if (suppressEvents) return;
                if (!double.TryParse(txtRate.Text, out double rate) || rate <= 0) return;

                if (originalBpm > 0)
                {
                    suppressEvents = true;
                    txtNewBPM.Text = (originalBpm * rate).ToString("0.##");
                    suppressEvents = false;
                }

                RateChanged?.Invoke(this, rate);
            };
        }

        public void ToggleEnabled(bool isEnabled)
        {
            txtNewBPM.Enabled = isEnabled;
            txtRate.Enabled = isEnabled;
            increasePitch.Enabled = isEnabled;
            decreasePitch.Enabled = isEnabled;
        }
    }

    public record RateChangeConfig(double OriginalBpm, double Rate, bool increasePitch, bool decreasePitch);
}