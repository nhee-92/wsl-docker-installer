using System.Text;
using System.Windows;
using System.Windows.Media;
using wsl_docker_installer.Utils;

namespace wsl_docker_installer.Views.Steps
{
    public partial class DockerInstall : BaseStep
    {
        private readonly string distroName;

        public event Action<bool> DockerInstalled = delegate { };

        public DockerInstall(string distroName)
        {
            InitializeComponent();
            this.distroName = distroName;
        }

        public async void InstallDocker()
        {
            SetStepReady(false);
            InstallDockerProgressPanel.Visibility = Visibility.Visible;

            bool isInstalled = await InstallDockerInDistroAsync();

            InstallProgressSpinner.Visibility = Visibility.Collapsed;
            DockerInstallText.Text = isInstalled ? "Docker has been installed and works perfectly!" : "Docker installation failed!";
            DockerInstallText.Foreground = new SolidColorBrush(isInstalled ? Colors.Green : Colors.Red);
            DockerInstalled.Invoke(isInstalled);
            SetStepReady(true);
        }

        private async Task<bool> InstallDockerInDistroAsync()
        {
            string dockerInstallScript = """
                sudo apt update &&
                sudo apt install -y ca-certificates curl gnupg lsb-release &&
                sudo install -m 0755 -d /etc/apt/keyrings &&
                curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg &&
                echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu $(lsb_release -cs) stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null &&
                sudo apt update &&
                sudo apt install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
            """;

            string arguments = $"-d {distroName} -- bash -c \"{dockerInstallScript.Replace("\"", "\\\"")}\"";

            return await ProcessStarter.RunCommandAsync("wsl.exe", arguments, "Docker installation failed", Encoding.UTF8);
        }
    }
}
