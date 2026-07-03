using static CollectorFLN.UI.InterfaceBuilder;
using static CollectorFLN.UI.Theme;

namespace CollectorFLN.UI.Elements
{
    public class AppLogCard
    {
        private static readonly Point logPanelPosition = new(480, 42);
        private static readonly Size logPanelSize = new(440, 662);

        public Panel logCard { get; }
        private readonly TextBox logBox;

        public AppLogCard()
        {
            logCard = MakeCard(logPanelPosition, logPanelSize);

            Label logHeader = MakeLabel("LOG", new Point(16, 10), 7.5f, textMuted, FontStyle.Bold);
            Panel logDivider = new Panel { Location = new Point(14, 28), Size = new Size(412, 1), BackColor = border };

            logBox = new TextBox
            {
                Multiline = true,
                Location = new Point(14, 36),
                Size = new Size(412, 620),
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                BackColor = Color.FromArgb(22, 22, 32),
                ForeColor = textMuted,
                Font = new Font("Consolas", 8.5f),
                BorderStyle = BorderStyle.None
            };

            logCard.Controls.Add(logHeader);
            logCard.Controls.Add(logDivider);
            logCard.Controls.Add(logBox);
        }

        public void AppendLine(string message)
        {
            logBox.AppendText($"{DateTime.Now:HH:mm:ss}  {message}{Environment.NewLine}");
        }
    }
}
