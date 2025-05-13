using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.IO.Compression;

namespace Upgrader
{
    public partial class MainWindow : Window
    {
        private Point mouseDownPosition;
        private string folderPath;
        private string zipUrl;

        public MainWindow(string folderPath, string zipUrl)
        {
            InitializeComponent();
            this.folderPath = folderPath;
            this.zipUrl = zipUrl;
            StartUpgradeAsync();
        }

        // MouseDown event handler to enable dragging
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                mouseDownPosition = e.GetPosition(this);
                CaptureMouse();
            }
        }

        // MouseMove event handler to drag the window
        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point currentPosition = e.GetPosition(this);
                Vector offset = currentPosition - mouseDownPosition;
                Left += offset.X;
                Top += offset.Y;
            }
        }

        // MouseUp event handler to release the mouse capture when the mouse button is released
        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ReleaseMouseCapture();
        }

        private async void StartUpgradeAsync()
        {
            try
            {
                string zipFilePath = Path.Combine(Path.GetTempPath(), $"app-update-{Guid.NewGuid()}.zip");

                // Download the zip file with progress
                await DownloadFileWithProgressAsync(zipUrl, zipFilePath);

                // Extract the zip file to the folder path
                ExtractZipFile(zipFilePath, folderPath);

                MessageBox.Show("Upgrade successful, launching the application.", "Upgrade Complete", MessageBoxButton.OK, MessageBoxImage.Information);

                // Launch the application
                LaunchApp();

                // Clean up the zip file after use
                TryDeleteFile(zipFilePath);

                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                // Handle and show any error that occurred during the upgrade process
                MessageBox.Show($"An error occurred during the upgrade process: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Exit();
            }
        }

        private async Task DownloadFileWithProgressAsync(string fileUrl, string destinationPath)
        {
            using (HttpClient client = new HttpClient())
            using (var response = await client.GetAsync(fileUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
                
                // Kill the old application if it's running
                KillAppProcess();

                long totalBytes = response.Content.Headers.ContentLength ?? -1;

                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    byte[] buffer = new byte[8192];
                    int bytesRead;
                    long totalBytesRead = 0;

                    while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                        totalBytesRead += bytesRead;

                        double progress = (totalBytes == -1) ? -1 : (double)totalBytesRead / totalBytes * 100;
                        Dispatcher.Invoke(() =>
                        {
                            // Update progress bar and percentage on the UI thread
                            DownloadProgressBar.Value = progress;
                            DownloadTextBlock.Text = $"{(int)progress}%";
                        });
                    }
                }
            }
        }

        private void KillAppProcess()
        {
            try
            {
                var processes = Process.GetProcessesByName("app");
                if (processes.Length > 0)
                {
                    foreach (var process in processes)
                    {
                        process.Kill();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error while terminating process: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Exit();
            }
        }

        private void ExtractZipFile(string zipFilePath, string extractPath)
        {
            try
            {
                using (ZipArchive archive = ZipFile.OpenRead(zipFilePath))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        string destinationPath = Path.Combine(extractPath, entry.FullName);

                        // Ensure the directory exists
                        string directoryPath = Path.GetDirectoryName(destinationPath);
                        if (!Directory.Exists(directoryPath))
                        {
                            Directory.CreateDirectory(directoryPath);
                        }

                        // If it's a file and already exists, delete it before extracting
                        if (File.Exists(destinationPath))
                        {
                            File.Delete(destinationPath);
                        }

                        // Extract the file
                        entry.ExtractToFile(destinationPath, overwrite: true);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error extracting zip file: {ex.Message}", "Extraction Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Exit();
            }
        }

        private void LaunchApp()
        {
            try
            {
                string appExePath = Path.Combine(folderPath, "app.exe");
                if (File.Exists(appExePath))
                {
                    Process.Start(appExePath);
                }
                else
                {
                    MessageBox.Show("Could not find app.exe, unable to launch the application.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Exit();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error launching app: {ex.Message}", "Launch Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Exit();
            }
        }

        private void TryDeleteFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting the temp zip file: {ex.Message}", "Cleanup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Exit();
            }
        }

        private void Exit()
        {
            Environment.Exit(0);
        }
    }
}
