using System.Diagnostics;
using static CollectorFLN.UI.Theme;

namespace CollectorFLN.UI
{
    public class Toolbar
    {
        private readonly Action<string> actionLog;
        private readonly Action<bool> setLogVisible;
        private readonly Action<bool> toggleChangePitchUprate;
        private readonly Action<bool> toggleChangePitchDownrate;

        public MenuStrip menuStrip;
        public ToolStripMenuItem toolsMenuItem = null!;
        public ToolStripMenuItem openConfigMenuItem = null!;
        public ToolStripMenuItem toggleLogMenuItem = null!;
        public ToolStripMenuItem resetDefaultsMenuItem = null!;
        public ToolStripMenuItem pitchMenuItem = null!;
        public ToolStripMenuItem uprateMenuItem = null!;
        public ToolStripMenuItem downrateMenuItem = null!;

        private bool changePitchUprate;
        private bool changePitchDownrate;
        private bool showLog = true;

        public Toolbar(Action<string> actionLog, Action<bool> setLogVisible, Action<bool> toggleChangePitchUprate, Action<bool> toggleChangePitchDownrate, bool showLog, bool changePitchUprate, bool changePitchDownrate) 
        {
            this.actionLog = actionLog;
            this.setLogVisible = setLogVisible;
            this.toggleChangePitchUprate = toggleChangePitchUprate;
            this.toggleChangePitchDownrate = toggleChangePitchDownrate;
            this.showLog = showLog;
            this.changePitchUprate = changePitchUprate;       
            this.changePitchDownrate = changePitchDownrate;

            menuStrip = new MenuStrip
            {
                BackColor = surface,
                ForeColor = textPrim,
                Font = font,
                Renderer = new ToolStripProfessionalRenderer(new DarkMenuColours(surface, surfaceAlt, accent))
            };

            toolsMenuItem = new ToolStripMenuItem("Tools") { ForeColor = textPrim, Font = font };

            openConfigMenuItem = new ToolStripMenuItem("Open config.json") { ForeColor = textPrim, Font = font };
            openConfigMenuItem.Click += OpenConfig_Click;

            toggleLogMenuItem = new ToolStripMenuItem("Show log") { ForeColor = textPrim, Font = font };
            toggleLogMenuItem.Click += ToggleLogMenuItem_Click;

            resetDefaultsMenuItem = new ToolStripMenuItem("Reset All Options to Default") { ForeColor = textPrim, Font = font };
            resetDefaultsMenuItem.Click += ResetDefaultsMenuItem_Click;

            pitchMenuItem = new ToolStripMenuItem("Pitch Settings") { ForeColor = textPrim, Font = font };

            uprateMenuItem = new ToolStripMenuItem("Preserve pitch on uprate") { ForeColor = textPrim, Font = font };
            downrateMenuItem = new ToolStripMenuItem("Preserve pitch on downrate") { ForeColor = textPrim, Font = font };

            uprateMenuItem.Click += ChangePitchUprate_Click;
            downrateMenuItem.Click += ChangePitchDownrate_Click;

            pitchMenuItem.DropDownItems.Add(uprateMenuItem);
            pitchMenuItem.DropDownItems.Add(downrateMenuItem);

            toolsMenuItem.DropDownItems.Add(openConfigMenuItem);
            toolsMenuItem.DropDownItems.Add(toggleLogMenuItem);
            toolsMenuItem.DropDownItems.Add(new ToolStripSeparator());
            toolsMenuItem.DropDownItems.Add(pitchMenuItem);
            toolsMenuItem.DropDownItems.Add(new ToolStripSeparator());
            toolsMenuItem.DropDownItems.Add(resetDefaultsMenuItem);

            menuStrip.Items.Add(toolsMenuItem);

            uprateMenuItem.Text = changePitchUprate ? "● Change Pitch on Uprate" : "○ Change Pitch on Uprate";
            downrateMenuItem.Text = changePitchDownrate ? "● Change Pitch on Downrate" : "○ Change Pitch on Downrate";
            toggleLogMenuItem.Text = showLog ? "Hide Log" : "Show Log";
        }

        public MenuStrip GetMenuStrip()
        {
            return this.menuStrip;
        }

        private void ChangePitchUprate_Click(object? sender, EventArgs e)
        {
            changePitchUprate = !changePitchUprate;
            toggleChangePitchUprate(changePitchUprate);

            uprateMenuItem.Text = changePitchUprate ? "● Change Pitch on Uprate" : "○ Change Pitch on Uprate";
        }

        private void ChangePitchDownrate_Click(object? sender, EventArgs e)
        {
            changePitchDownrate = !changePitchDownrate;
            toggleChangePitchDownrate(changePitchDownrate);

            downrateMenuItem.Text = (changePitchDownrate) ? "● Change Pitch on Downrate" : "○ Change Pitch on Downrate";
        }

        private void ToggleLogMenuItem_Click(object? sender, EventArgs e)
        {
            showLog = !showLog;
            setLogVisible(showLog);
            toggleLogMenuItem.Text = showLog ? "Hide Log" : "Show Log";
            actionLog($"Log visibility set to {showLog}");
        }

        private void OpenConfig_Click(object? sender, EventArgs e)
        {
            try
            {
                string configPath = Path.Combine(AppContext.BaseDirectory, "config.json");

                Process.Start(new ProcessStartInfo
                {
                    FileName = configPath,
                    UseShellExecute = true
                });

                actionLog("Opened config.json");
            }
            catch (Exception ex)
            {
                actionLog($"Failed: {ex.Message}");
            }
        }

        private void ResetDefaultsMenuItem_Click(object? sender, EventArgs e)
        {
            // TODO: implement actual reset-to-default logic against Config
            actionLog("Reset to defaults requested (not yet implemented)");
        }
    }
}
