using System.IO;
using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using wsl_docker_installer.Utils;

namespace wsl_docker_installer.Views.Steps
{
    public partial class DistroInstall : BaseStep
    {
        public event Action<bool> DistroInstallCheck = delegate { };
        public event Action<string> DistroNameConfirmed = delegate { };
        private string distroName = string.Empty;
        List<string> distros = [];

        public DistroInstall()
        {
            SetStepReady(false);
            InitializeComponent();
            CurrentInstallations();
        }

        public async void HandleInstallDistro()
        {
            DistroNameInput.IsEnabled = false;
            SetStepReady(false);
            SetNextButtonText("Installing...");

            bool isCorrectInstalled = await InstallCustomUbuntuDistroAsync();
            
            if (isCorrectInstalled)
            {
                DistroInstallCheck.Invoke(true);
                DistroNameConfirmed.Invoke(distroName);
                SetNextButtonText("Next");
                SetStepReady(true);
            }
            else
            {
                MessageBox.Show("Installation failed. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private async Task<bool> InstallCustomUbuntuDistroAsync()
        {
            string installDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WSLDistros", distroName);
            string tempRootFsPath = Path.Combine(Path.GetTempPath(), "ubuntu-custom.tar.xz");

            try
            {
                await DownloadDistro(tempRootFsPath, installDirectory);

                InstallText.Text = $"Install {distroName}:";
                InstallText.Visibility = Visibility.Visible;
                InstallProgressSpinner.Visibility = Visibility.Visible;

                bool isDistroInstalled = await ProcessStarter.RunCommandAsync("wsl.exe", $"--import {distroName} \"{installDirectory}\" \"{tempRootFsPath}\"", $"Could not import {distroName}", Encoding.Unicode);

                if (isDistroInstalled)
                {
                    InstallProgressSpinner.Visibility = Visibility.Collapsed;
                    InstallText.Width = double.NaN;
                    InstallText.Text = $"Successfully installed {distroName}.";
                    InstallText.Foreground = new SolidColorBrush(Colors.Green);

                    return true;
                } else
                {
                    return false;
                }                
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                if (File.Exists(tempRootFsPath))
                {
                    File.Delete(tempRootFsPath);
                }
            }
        }

        private async Task<bool> DownloadDistro(string tempRootFsPath, string installDirectory)
        {
            string rootFsUrl = "https://cloud-images.ubuntu.com/minimal/releases/jammy/release/ubuntu-22.04-minimal-cloudimg-amd64-root.tar.xz";

            DownloadProgressBar.Visibility = Visibility.Visible;
            DownloadText.Visibility = Visibility.Visible;

            using var httpClient = new HttpClient();
            using var response = await httpClient.GetAsync(rootFsUrl, HttpCompletionOption.ResponseHeadersRead);

            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            var canReportProgress = totalBytes != -1;

            await using var contentStream = await response.Content.ReadAsStreamAsync();
            await using var fileStream = new FileStream(tempRootFsPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            var buffer = new byte[8192];
            long totalRead = 0;
            int read;
            while ((read = await contentStream.ReadAsync(buffer)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, read));
                totalRead += read;

                if (canReportProgress)
                {
                    var progress = (int)((totalRead * 100) / totalBytes);
                    DownloadProgressBar.Value = progress;
                }
            }

            fileStream.Close();
            Directory.CreateDirectory(installDirectory);

            return true;
        }

        private async void CurrentInstallations()
        {
            distros = await GetInstalledWslDistrosAsync();

            foreach (var distro in distros)
            {
                DistroListBox.Items.Add(distro);
            }
        }

        private static async Task<List<string>> GetInstalledWslDistrosAsync()
        {
            var distros = new List<string>();

            try
            {
                string output = await ProcessStarter.RunCommandWithOutputAsync("wsl.exe", "--list --quiet", Encoding.Unicode);

                foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    distros.Add(line.Trim());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error when retrieving the distributions:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return distros;
        }

        private void DistroNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            distroName = DistroNameInput.Text;

            if (string.IsNullOrWhiteSpace(distroName))
                SetStepReady(false);
            else if (distros.Contains(distroName, StringComparer.OrdinalIgnoreCase))
                SetStepReady(false);
            else
                SetStepReady(true);
        }
    }
}
