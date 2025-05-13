using System;
using System.Windows;

namespace Upgrader
{
    public partial class App : Application
    {
        // 在应用程序启动时获取命令行参数
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            if (e.Args.Length < 3)
            {
                Environment.Exit(0);
                return;
            }

            string secret = e.Args[0];
            string folderPath = e.Args[1];
            string zipUrl = e.Args[2];

            if (secret != "03jvR5Q6WIGUKPbkyiqT4hXhMNs")
            {
                Environment.Exit(0);
                return;
            }

            MainWindow mainWindow = new MainWindow(folderPath, zipUrl);
            mainWindow.ShowDialog();
        }
    }
}