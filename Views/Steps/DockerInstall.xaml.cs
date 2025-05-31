using System.Windows;
using System.Windows.Media;
using wsl_docker_installer.Utils;

namespace wsl_docker_installer.Views.Steps
{
    public partial class DockerInstall : BaseStep
    {
        public event Action<bool> DockerInstalled = delegate { };
        public bool InstallOldCompose = false;
        private readonly string distroName;

        public DockerInstall(string distroName)
        {
            InitializeComponent();
            this.distroName = distroName;
        }

        public async void InstallDocker()
        {

            SetStepReady(false);
            InstallOldCompose = OldDockerCheckBox.IsChecked ?? false;
            OldDockerCheckPanel.Visibility = Visibility.Collapsed;
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

            if (InstallOldCompose)
            {
                dockerInstallScript += """
                    && sudo curl -L https://github.com/docker/compose/releases/download/v2.24.0/docker-compose-$(uname -s)-$(uname -m) -o /usr/local/bin/docker-compose &&
                    sudo chmod +x /usr/local/bin/docker-compose
                """;
            }

            return await ProcessStarter.RunCommandInDistroAsync(distroName, dockerInstallScript, "Docker installation failed");
        }
    }
}
