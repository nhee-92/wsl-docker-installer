using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace wsl_docker_installer.Views.Steps
{
    public partial class WslInstallationCheck : BaseStep
    {
        public bool IsWslInstalled { get; private set; } = false;
        public event Action<bool> WslCheckCompleted = delegate { };

        public WslInstallationCheck()
        {
            SetStepReady(false);
            InitializeComponent();
            Loaded += InstallationCheck_Loaded;
        }

        public async void InstallWslAsync()
        {
            SetStepReady(false);
            SetNextButtonText("Install...");

            StatusValue.Text = "Install WSL...";
            StatusValue.Foreground = new SolidColorBrush(Colors.Black);
            ProgressSpinner.Visibility = Visibility.Visible;

            try
            {
                var psi = new ProcessStartInfo("wsl.exe", "--install")
                {
                    UseShellExecute = true,
                    Verb = "runas"
                };

                using var process = new Process { StartInfo = psi };
                await process.WaitForExitAsync();

                if (process.ExitCode == 0)
                {
                    IsWslInstalled = true;
                    StatusValue.Text = "WSL successfully installed.";
                    StatusValue.Foreground = new SolidColorBrush(Colors.Green);
                    SetNextButtonText("Next");
                }
                else
                {
                    StatusValue.Text = "Installation failed.";
                    StatusValue.Foreground = new SolidColorBrush(Colors.Red);
                    SetNextButtonText("Try again");
                }
            }
            catch (Exception ex)
            {
                StatusValue.Text = $"Error: {ex.Message}";
                StatusValue.Foreground = new SolidColorBrush(Colors.Red);
                SetNextButtonText("Try again");
            }
            finally
            {
                ProgressSpinner.Visibility = Visibility.Collapsed;
                SetStepReady(true);
            }
        }

        private async void InstallationCheck_Loaded(object sender, RoutedEventArgs e)
        {
            await CheckWslInstalledAsync();
        }

        private async Task CheckWslInstalledAsync()
        {
            // Testing purpose
            await Task.Delay(1500);
            bool wslInstalled = await CheckWsl();

            SetStepReady(true);

            ProgressSpinner.Visibility = Visibility.Collapsed;
            StatusValue.Text = wslInstalled ? "WSL found." : "WSL must be installed.";
            StatusValue.Foreground = new SolidColorBrush(wslInstalled ? Colors.Green : Colors.Red);
            WslCheckCompleted.Invoke(wslInstalled);
        }

        private static async Task<bool> CheckWsl()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "wsl.exe",
                    Arguments = "--status",
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
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                    return false;

                return output.Contains("Standardversion: 2", StringComparison.OrdinalIgnoreCase) ||
                    output.Contains("Default Version: 2", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
