using System.Diagnostics;
using static CollectorFLN.UI.Theme;

namespace CollectorFLN.UI.Elements
{
    public class AppToolbar
    {
        public event EventHandler<bool>? LogVisibilityChanged;
        public event EventHandler<string>? LogMessage;  
        public event EventHandler? ResetDefaultsRequested;

        public MenuStrip menuStrip;
        public ToolStripMenuItem toolsMenuItem = null!;
        public ToolStripMenuItem openConfigMenuItem = null!;
        public ToolStripMenuItem toggleLogMenuItem = null!;
        public ToolStripMenuItem resetDefaultsMenuItem = null!;

        private bool showLog = true;

        public AppToolbar(Config config)
        {
            this.showLog = config.ShowLog;

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

            toolsMenuItem.DropDownItems.Add(openConfigMenuItem);
            toolsMenuItem.DropDownItems.Add(toggleLogMenuItem);
            toolsMenuItem.DropDownItems.Add(resetDefaultsMenuItem);

            menuStrip.Items.Add(toolsMenuItem);

            toggleLogMenuItem.Text = showLog ? "Hide Log" : "Show Log";
        }

        private void ToggleLogMenuItem_Click(object? sender, EventArgs e)
        {
            showLog = !showLog;
            toggleLogMenuItem.Text = showLog ? "Hide Log" : "Show Log";
            LogVisibilityChanged?.Invoke(this, showLog);
            LogMessage?.Invoke(this, $"Log visibility set to {showLog}");
        }

        private void OpenConfig_Click(object? sender, EventArgs e)
        {
            try
            {
                string configPath = Path.Combine(AppContext.BaseDirectory, "config.json");
                Process.Start(new ProcessStartInfo { FileName = configPath, UseShellExecute = true });
                LogMessage?.Invoke(this, "Opened config.json");
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke(this, $"Failed: {ex.Message}");
            }
        }

        private void ResetDefaultsMenuItem_Click(object? sender, EventArgs e)
        {
            ResetDefaultsRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}

