using static CollectorFLN.UI.InterfaceBuilder;
using static CollectorFLN.UI.Theme;

namespace CollectorFLN.UI.Elements
{
    public class AppMapInfoCard
    {
        private static readonly Point cardPosition = new(20, 36);
        private static readonly Size cardSize = new(440, 88);

        public Panel mapInfoCard { get; }

        private readonly Label lblSelectedMap;
        private readonly Label lblSelectedArtist;
        private readonly Label lblVersion;
        private readonly Label lblBpm;

        public AppMapInfoCard()
        {
            mapInfoCard = MakeCard(cardPosition, cardSize);

            Panel accentBar = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(4, cardSize.Height),
                BackColor = accent
            };

            lblSelectedMap = new Label
            {
                Text = "No map selected",
                Location = new Point(18, 12),
                Size = new Size(410, 28),
                Font = new Font("Segoe UI Semibold", 13f, FontStyle.Bold),
                ForeColor = textPrim,
                BackColor = Color.Transparent
            };

            lblSelectedArtist = new Label
            {
                Text = "",
                Location = new Point(18, 40),
                Size = new Size(410, 22),
                Font = new Font("Segoe UI", 10f),
                ForeColor = accent,
                BackColor = Color.Transparent
            };

            lblVersion = new Label
            {
                Text = "",
                Location = new Point(18, 62),
                Size = new Size(410, 18),
                Font = new Font("Segoe UI", 8f),
                ForeColor = textMuted,
                BackColor = Color.Transparent
            };

            lblBpm = new Label
            {
                Text = "",
                Location = new Point(18, 100),
                Size = new Size(410, 18),
                Font = new Font("Segoe UI", 8f),
                ForeColor = textMuted,
                BackColor = Color.Transparent
            };

            mapInfoCard.Controls.AddRange(new Control[] { accentBar, lblSelectedMap, lblSelectedArtist, lblVersion, lblBpm});
        }

        // Single entry point for updating displayed info — callers don't touch labels directly
        public void SetMapInfo(string mapName, string artist, string version, double bpm)
        {
            lblSelectedMap.Text = string.IsNullOrEmpty(mapName) ? "No map selected" : mapName;
            lblSelectedArtist.Text = artist;
            lblVersion.Text = version;
            lblBpm.Text = bpm > 0 ? $"BPM: {bpm}" : "";
        }
    }
}