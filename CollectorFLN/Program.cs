using System.Globalization;
using System.Runtime.InteropServices;

namespace CollectorFLN
{
    static class Program
    {
        [DllImport("kernel32.dll")]
        static extern bool AllocConsole();

        [STAThread]
        public static void Main()
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

            // AllocConsole(); // Uncomment this line if you want to see console output for debugging

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}