using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace wsl_docker_installer.Views.Steps
{
    public partial class InstallationCheck : BaseStep
    {
        public InstallationCheck()
        {
            SetStepReady(false);
            InitializeComponent();
            Loaded += InstallationCheck_Loaded;
        }

        private async void InstallationCheck_Loaded(object sender, RoutedEventArgs e)
        {
            await CheckWslInstalledAsync();
        }

        private async Task CheckWslInstalledAsync()
        {
            // Simulate a delay to mimic a real check
            await Task.Delay(1500);

            bool wslInstalled = await Task.Run(() =>
            {
                try
                {
                    ProcessStartInfo psi = new()
                    {
                        FileName = "wsl.exe",
                        Arguments = "--status",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    
                    using Process? process = Process.Start(psi);
                    if (process == null)
                    {
                        return false;
                    }
                    process.WaitForExit(2000);
                    return process.ExitCode == 0;
                }
                catch
                {
                    return false;
                }
            });

            SetStepReady(true);
            if (wslInstalled)
            {
                ProgressSpinner.Visibility = Visibility.Collapsed;
                StatusText.Text = "✅ WSL gefunden.";
                StatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green);
            }
            else
            {
                ProgressSpinner.Visibility = Visibility.Collapsed;
                StatusText.Text = "❌ WSL muss installiert werden.";
                StatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
            }
        }
    }
}
