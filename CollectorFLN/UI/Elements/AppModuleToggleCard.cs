using static CollectorFLN.UI.InterfaceBuilder;
using static CollectorFLN.UI.Theme;

namespace CollectorFLN.UI.Elements
{
    public class AppModuleToggleCard
    {
        private static readonly Point cardPosition = new(20, 134);
        private static readonly Size cardSize = new(440, 120);

        public Panel moduleCard { get; }

        private readonly CheckBox chkEnableFLN;
        private readonly CheckBox chkEnableRemoveLN;
        private readonly CheckBox chkEnableRemoveSV;
        private readonly CheckBox chkEnableRateChange;

        public event EventHandler<bool>? ChkEnableFLNChanged;
        public event EventHandler<bool>? ChkEnableRemoveLNChanged;
        public event EventHandler<bool>? ChkEnableRemoveSVChanged;
        public event EventHandler<bool>? ChkEnableRateChangeChanged;

        public AppModuleToggleCard(ModuleToggleConfig config)
        {
            moduleCard = MakeCard(cardPosition, cardSize, surface);
            
            Label moduleHeader = MakeLabel("ACTIVE MODULES", new Point(16, 10), 7.5f, textMuted, FontStyle.Bold);
            Panel moduleDivider = new Panel { Location = new Point(14, 30), Size = new Size(412, 1), BackColor = border };

            chkEnableFLN = MakeModuleToggle("FLN Conversion", new Point(16, 44), config.EnableFLN);
            chkEnableRemoveLN = MakeModuleToggle("Remove LNs", new Point(195, 44), config.EnableRemoveLN);
            chkEnableRemoveSV = MakeModuleToggle("Remove SV", new Point(195, 80), config.EnableRemoveSV);
            chkEnableRateChange = MakeModuleToggle("Rate Change", new Point(16, 80), config.EnableRateChange);

            moduleCard.Controls.AddRange(new Control[] { moduleHeader, moduleDivider, chkEnableFLN, chkEnableRemoveLN, chkEnableRemoveSV, chkEnableRateChange });

            WireEvents();
        }

        private void WireEvents()
        {
            chkEnableFLN.CheckedChanged += (s, e) =>
            {
                ChkEnableFLNChanged?.Invoke(this, chkEnableFLN.Checked);
                chkEnableFLN.ForeColor = (chkEnableFLN.Checked) ? accent : textMuted;
            };
            chkEnableRemoveLN.CheckedChanged += (s, e) =>
            {
                ChkEnableRemoveLNChanged?.Invoke(this, chkEnableRemoveLN.Checked);
                chkEnableRemoveLN.ForeColor = (chkEnableRemoveLN.Checked) ? accent : textMuted;
            }; 
            chkEnableRemoveSV.CheckedChanged += (s, e) =>
            {
                ChkEnableRemoveSVChanged?.Invoke(this, chkEnableRemoveSV.Checked);
                chkEnableRemoveSV.ForeColor = (chkEnableRemoveSV.Checked) ? accent : textMuted;
            };
            chkEnableRateChange.CheckedChanged += (s, e) =>
            {
                ChkEnableRateChangeChanged?.Invoke(this, chkEnableRateChange.Checked);
                chkEnableRateChange.ForeColor = (chkEnableRateChange.Checked) ? accent : textMuted;
            };
        }

        private void chkEnableFLN_Click(object? sender, EventArgs e)
        {
            chkEnableFLN.Checked = !chkEnableFLN.Checked;
            ChkEnableFLNChanged?.Invoke(this, chkEnableFLN.Checked);
        }

        private void chkEnableRemoveLN_Click(object? sender, EventArgs e)
        {
            chkEnableRemoveLN.Checked = !chkEnableRemoveLN.Checked;
            ChkEnableRemoveLNChanged?.Invoke(this, chkEnableRemoveLN.Checked);
        }

        private void chkEnableRemoveSV_Click(object? sender, EventArgs e)
        {
            chkEnableRemoveSV.Checked = !chkEnableRemoveSV.Checked;
            ChkEnableRemoveSVChanged?.Invoke(this, chkEnableRemoveSV.Checked);
        }

        private void chkEnableRateChange_Click(object? sender, EventArgs e)
        {
            chkEnableRateChange.Checked = !chkEnableRateChange.Checked;
            ChkEnableRateChangeChanged?.Invoke(this, chkEnableRateChange.Checked);
        }
    }

    public record ModuleToggleConfig(bool EnableFLN, bool EnableRemoveLN, bool EnableRemoveSV, bool EnableRateChange);
}
