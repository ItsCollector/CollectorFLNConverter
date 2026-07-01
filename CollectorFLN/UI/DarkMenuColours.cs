namespace CollectorFLN.UI
{
    public class DarkMenuColours : ProfessionalColorTable
    {
        private readonly Color surface;
        private readonly Color surfaceAlt;
        private readonly Color accent;

        public DarkMenuColours(Color surface, Color surfaceAlt, Color accent)
        {
            this.surface = surface;
            this.surfaceAlt = surfaceAlt;
            this.accent = accent;
        }

        // Hover highlight on dropdown items
        public override Color MenuItemSelected => surfaceAlt;
        public override Color MenuItemSelectedGradientBegin => surfaceAlt;
        public override Color MenuItemSelectedGradientEnd => surfaceAlt;
        public override Color MenuItemBorder => accent;
        public override Color MenuBorder => surface;
        public override Color ToolStripDropDownBackground => surface;
        public override Color ImageMarginGradientBegin => surface;
        public override Color ImageMarginGradientMiddle => surface;
        public override Color ImageMarginGradientEnd => surface;
        public override Color MenuStripGradientBegin => surface;
        public override Color MenuStripGradientEnd => surface;

        // Pressed / clicked top-level menu button 
        public override Color ButtonPressedGradientBegin => surfaceAlt;
        public override Color ButtonPressedGradientMiddle => surfaceAlt;
        public override Color ButtonPressedGradientEnd => surfaceAlt;
        public override Color ButtonSelectedGradientBegin => surfaceAlt;
        public override Color ButtonSelectedGradientMiddle => surfaceAlt;
        public override Color ButtonSelectedGradientEnd => surfaceAlt;
        public override Color ButtonPressedBorder => accent;
        public override Color ButtonSelectedBorder => accent;
        public override Color MenuItemPressedGradientBegin => surface;
        public override Color MenuItemPressedGradientMiddle => surface;
        public override Color MenuItemPressedGradientEnd => surface;
        public override Color RaftingContainerGradientBegin => surface;
        public override Color RaftingContainerGradientEnd => surface;
        public override Color ToolStripBorder => surface;
    }
}
