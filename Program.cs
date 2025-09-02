using System;
using System.Windows.Forms;

namespace RobloxAccountManager
{
    internal static class Program
    {
        [STAThread] // This tells Windows Forms to run in single-threaded apartment
        static void Main()
        {
            ApplicationConfiguration.Initialize(); // Required for .NET 6+
            Application.Run(new Form1());
        }
    }
}
