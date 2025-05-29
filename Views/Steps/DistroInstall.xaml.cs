using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace wsl_docker_installer.Views.Steps
{
    public partial class DistroInstall : BaseStep
    {
        private string distroName = string.Empty;
        List<string> distros = [];

        public DistroInstall()
        {
            SetStepReady(false);
            InitializeComponent();
            CurrentInstallations();
        }

        public async Task<bool> InstallCustomUbuntuDistroAsync()
        {
            string rootFsUrl = "https://cloud-images.ubuntu.com/minimal/releases/jammy/release/ubuntu-22.04-minimal-cloudimg-amd64-root.tar.xz";
            string installDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WSLDistros", distroName);
            string tempRootFsPath = Path.Combine(Path.GetTempPath(), "ubuntu-custom.tar.xz");

            try
            {
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

                InstallText.Text = $"Install {distroName}:";
                InstallText.Visibility = Visibility.Visible;
                InstallProgressSpinner.Visibility = Visibility.Visible;

                var psi = new ProcessStartInfo
                {
                    FileName = "wsl.exe",
                    Arguments = $"--import {distroName} \"{installDirectory}\" \"{tempRootFsPath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.Unicode,
                    StandardErrorEncoding = Encoding.Unicode
                };

                using var process = new Process { StartInfo = psi };
                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    MessageBox.Show($"Fehler bei der WSL-Installation:\n{output}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                MessageBox.Show($"Die Distribution \"{distroName}\" wurde erfolgreich installiert.", "Erfolg", MessageBoxButton.OK, MessageBoxImage.Information);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
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
                var psi = new ProcessStartInfo
                {
                    FileName = "wsl.exe",
                    Arguments = "--list --quiet",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.Unicode,
                    StandardErrorEncoding = Encoding.Unicode
                };

                using var process = new Process { StartInfo = psi };
                process.Start();

                string output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    distros.Add(line.Trim());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Abrufen der Distributionen:\n{ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
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
