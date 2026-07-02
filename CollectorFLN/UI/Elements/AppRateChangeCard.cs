using static CollectorFLN.UI.InterfaceBuilder;
using static CollectorFLN.UI.Theme;

namespace CollectorFLN.UI.Elements
{
    public class AppRateChangeCard
    {
        private static readonly Point cardPosition = new(20, 394);
        private static readonly Size cardSize = new(440, 120);

        public Panel rateChangeCard { get; }

        private readonly Label rateChangeHeader;
        private readonly TextBox txtBPM;
        private readonly TextBox txtRate;
        private readonly ToolTip tt = new() { InitialDelay = 300, ReshowDelay = 100 };

        private readonly double originalBpm;
        private bool suppressEvents; // guards against BPM<->Rate update loops

        public event EventHandler<double>? RateChanged; // fires with the resolved rate multiplier

        public AppRateChangeCard(RateChangeConfig config)
        {
            originalBpm = config.OriginalBpm;

            rateChangeCard = MakeCard(cardPosition, cardSize);

            rateChangeHeader = MakeLabel("RATE CHANGE", new Point(16, 12), 7.5f, textMuted, FontStyle.Bold);
            Panel rateDivider = new Panel { Location = new Point(14, 30), Size = new Size(412, 1), BackColor = border };
            Label lblBPM = MakeLabel("Target BPM", new Point(16, 46), 9f, textMuted);
            Label lblRate = MakeLabel("Rate (x)", new Point(16, 74), 9f, textMuted);

            txtBPM = MakeTextBox(originalBpm > 0 ? originalBpm.ToString("0.##") : "", new Point(195, 42), new Size(70, 26));
            txtRate = MakeTextBox(config.Rate.ToString("0.00"), new Point(195, 70), new Size(70, 26));

            tt.SetToolTip(txtBPM, "Set a target BPM — rate is calculated automatically.");
            tt.SetToolTip(txtRate, "Set a playback rate — BPM is calculated automatically.");

            rateChangeCard.Controls.AddRange(new Control[]
            {
                rateChangeHeader, rateDivider, lblBPM, txtBPM, lblRate, txtRate
            });

            WireEvents();
        }

        private void WireEvents()
        {
            txtBPM.TextChanged += (s, e) =>
            {
                if (suppressEvents) return;
                if (originalBpm <= 0) return; // can't compute a rate without a baseline
                if (!double.TryParse(txtBPM.Text, out double bpm) || bpm <= 0) return;

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
                    txtBPM.Text = (originalBpm * rate).ToString("0.##");
                    suppressEvents = false;
                }

                RateChanged?.Invoke(this, rate);
            };
        }
    }

    public record RateChangeConfig(double OriginalBpm, double Rate);
}