using System;
using System.Windows.Forms;
using System.Diagnostics;

namespace BarkodStoksistemiApp
{
    internal static class Program
    {
        /// <summary>
        ///  Uygulama açılır açılmaz Python kütüphanelerini yükler
        /// </summary>


        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Uygulama açılır açılmaz Python kütüphanelerini yükle

            DatabaseHelper.InitializeDatabase();
            ApplicationConfiguration.Initialize();
            Application.Run(new LoginForm());
        }
    }
}