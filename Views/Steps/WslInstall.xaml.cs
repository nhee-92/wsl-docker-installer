using System.Text;
using System.Windows;
using System.Windows.Media;
using wsl_docker_installer.Utils;

namespace wsl_docker_installer.Views.Steps
{
    public partial class WslInstall : BaseStep
    {
        public event Action<bool> WslCheckCompleted = delegate { };

        public WslInstall()
        {
            SetStepReady(false);
            InitializeComponent();
            CheckWslInstalledAsync();
        }

        public async void InstallWslAsync()
        {
            SetStepReady(false);
            SetNextButtonText("Installing...");

            StatusValue.Text = "Install WSL2...";
            StatusValue.Foreground = new SolidColorBrush(Colors.Black);
            ProgressSpinner.Visibility = Visibility.Visible;

            bool isWslInstalled = await ProcessStarter.RunCommandAsAdminAsync("wsl.exe", "--install");

            if (isWslInstalled)
            {
                StatusValue.Text = "WSL2 successfully installed.";
                StatusValue.Foreground = new SolidColorBrush(Colors.Green);
                SetNextButtonText("Next");
            }
            else
            {
                StatusValue.Text = "Installation failed.";
                StatusValue.Foreground = new SolidColorBrush(Colors.Red);
                SetNextButtonText("Try again");
            }

            ProgressSpinner.Visibility = Visibility.Collapsed;
            SetStepReady(true);
        }

        private async void CheckWslInstalledAsync()
        {
            string output = await ProcessStarter.RunCommandForOutputAsync("wsl.exe", "--status", Encoding.Unicode);
            bool wslInstalled = output.Contains("Standardversion: 2", StringComparison.OrdinalIgnoreCase) ||
                                output.Contains("Default Version: 2", StringComparison.OrdinalIgnoreCase);

            SetStepReady(true);

            ProgressSpinner.Visibility = Visibility.Collapsed;
            StatusValue.Text = wslInstalled ? "WSL2 found." : "WSL2 must be installed.";
            StatusValue.Foreground = new SolidColorBrush(wslInstalled ? Colors.Green : Colors.Red);
            WslCheckCompleted.Invoke(wslInstalled);
        }
    }
}
