using System.Configuration;
using static CollectorFLN.UI.InterfaceBuilder;
using static CollectorFLN.UI.Theme;

namespace CollectorFLN.UI.Elements
{
    public class AppDifficultyOverrideCard
    {
        private static readonly Point cardPosition = new(20, 532);
        private static readonly Size cardSize = new(440, 120);

        public Panel difficultyOverrideCard { get; }

        private readonly TextBox txtOD;
        private readonly CheckBox chkOverrideOD;
        private readonly TextBox txtHP;
        private readonly CheckBox chkOverrideHP;
        private readonly ToolTip tt = new() { InitialDelay = 300, ReshowDelay = 100 };

        public event EventHandler<DifficultyOverrideEventArgs>? OverrideChanged;

        public AppDifficultyOverrideCard(DifficultyOverrideConfig config)
        {
            difficultyOverrideCard = MakeCard(cardPosition, cardSize);

            Label difficultyHeader = MakeLabel("DIFFICULTY OVERRIDES", new Point(16, 12), 7.5f, textMuted, FontStyle.Bold);
            Panel difficultyDivider = new Panel { Location = new Point(14, 30), Size = new Size(412, 1), BackColor = border };
            Label lblOD = MakeLabel("Overall Difficulty", new Point(16, 46), 9f, textMuted);
            Label lblHP = MakeLabel("HP Drain", new Point(16, 74), 9f, textMuted);

            txtOD = MakeTextBox(new Point(195, 42), new Size(70, 26));
            txtOD.Text = config.OD.ToString("0.#");
            txtOD.Enabled = config.OverrideOD;

            chkOverrideOD = new CheckBox
            {
                Text = "Override",
                Location = new Point(302, 44),
                AutoSize = true,
                ForeColor = config.OverrideOD ? accent : textMuted,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 8.5f),
                Cursor = Cursors.Hand,
                Checked = config.OverrideOD
            };

            txtHP = MakeTextBox(new Point(195, 70), new Size(70, 26));
            txtHP.Text = config.HP.ToString("0.#");
            txtHP.Enabled = config.OverrideHP;

            chkOverrideHP = new CheckBox
            {
                Text = "Override",
                Location = new Point(302, 72),
                AutoSize = true,
                ForeColor = config.OverrideHP ? accent : textMuted,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 8.5f),
                Cursor = Cursors.Hand,
                Checked = config.OverrideHP
            };

            tt.SetToolTip(chkOverrideOD, "Override the map's default Overall Difficulty value.");
            tt.SetToolTip(chkOverrideHP, "Override the map's default HP Drain value.");

            difficultyOverrideCard.Controls.AddRange(new Control[]
            {
                difficultyHeader, difficultyDivider,
                lblOD, txtOD, chkOverrideOD,
                lblHP, txtHP, chkOverrideHP
            });

            WireEvents();
        }

        private void WireEvents()
        {
            chkOverrideOD.CheckedChanged += (s, e) =>
            {
                txtOD.Enabled = chkOverrideOD.Checked;
                chkOverrideOD.ForeColor = chkOverrideOD.Checked ? accent : textMuted;
                RaiseChanged();
            };

            chkOverrideHP.CheckedChanged += (s, e) =>
            {
                txtHP.Enabled = chkOverrideHP.Checked;
                chkOverrideHP.ForeColor = chkOverrideHP.Checked ? accent : textMuted;
                RaiseChanged();
            };

            txtOD.TextChanged += (s, e) => RaiseChanged();
            txtHP.TextChanged += (s, e) => RaiseChanged();
        }

        private void RaiseChanged()
        {
            float.TryParse(txtOD.Text, out float od);
            float.TryParse(txtHP.Text, out float hp);

            OverrideChanged?.Invoke(this, new DifficultyOverrideEventArgs(
                chkOverrideOD.Checked, od,
                chkOverrideHP.Checked, hp));
        }

        public void SetDifficultyOverrideSettings(bool overrideOD, float od, bool overrideHP, float hp)
        {
            chkOverrideOD.Checked = overrideOD;
            chkOverrideOD.ForeColor = overrideOD ? accent : textMuted;
            txtOD.Text = od.ToString("0.#");
            txtOD.Enabled = overrideOD;

            chkOverrideHP.Checked = overrideHP;
            chkOverrideHP.ForeColor = overrideHP ? accent : textMuted;
            txtHP.Text = hp.ToString("0.#");
            txtHP.Enabled = overrideHP; txtOD.Enabled = overrideOD;
        }

        public void SetOD(float od)
        {
            txtOD.Text = od.ToString("0.#");
        }

        public void SetHP(float hp)
        {
            txtHP.Text = hp.ToString("0.#");
        }

        public bool GetOverrideOD() => chkOverrideOD.Checked;
        public bool GetOverrideHP() => chkOverrideHP.Checked;
    }

    public record DifficultyOverrideConfig(bool OverrideOD, float OD, bool OverrideHP, float HP);
    public record DifficultyOverrideEventArgs(bool OverrideOD, float OD, bool OverrideHP, float HP);
}