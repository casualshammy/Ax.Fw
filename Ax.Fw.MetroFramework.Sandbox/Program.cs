namespace Ax.Fw.MetroFramework.Sandbox
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            StyleManager.Current.SetColors(Color.Red, Color.Green, Color.Linen);
            Application.Run(new Form1());
        }
    }
}