using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using wsl_docker_installer.Utils;

namespace wsl_docker_installer.Views.Steps
{
    public partial class TaskInstall : BaseStep
    {
        private readonly string distroName;
        public event Action<bool> IsDockerConfigured = delegate { };

        public TaskInstall(string distroName)
        {
            InitializeComponent();
            this.distroName = distroName;
        }

        public async void ConfigureDocker()
        {
            PortValue.IsEnabled = false;
            string port = PortValue.Text.Trim();

            if (!int.TryParse(port, out int portNum) || portNum < 1024 || portNum > 65535)
            {
                MessageBox.Show("Please enter a valid port between 1024 and 65535.", "Invalid port", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ConfigureDockerInstallerText.Visibility = Visibility.Visible;
            ConfigureDockerInstallerSpinner.Visibility = Visibility.Visible;

            ConfigureDockerInstallerText.Text = "Configure...";
            bool configured = await Configure(port);

            ConfigureDockerInstallerText.Text = "Install Docker CLI...";
            bool dockerCLIInstalled = await InstallDockerCLI();

            if (configured && dockerCLIInstalled)
            {
                ConfigureDockerInstallerSpinner.Visibility = Visibility.Collapsed;
                ConfigureDockerInstallerText.Text = "The configuration of Windows and docker was successful!";
                ConfigureDockerInstallerText.Foreground = new SolidColorBrush(Colors.Green);
                ConfigureDockerHostEnvironment(port);
                SetStepReady(true);
                IsDockerConfigured.Invoke(true);
            }
        }

        private static void ConfigureDockerHostEnvironment(string port)
        {
            string dockerHost = $"tcp://localhost:{port}";
            Environment.SetEnvironmentVariable("DOCKER_HOST", dockerHost, EnvironmentVariableTarget.User);
        }

        private static async Task<bool> InstallDockerCLI()
        {
            string DownloadUrl = "https://download.docker.com/win/static/stable/x86_64/docker-26.1.4.zip";
            string InstallPath = @"C:\sw\DockerCLI";

            try
            {
                Directory.CreateDirectory(InstallPath);
                string tempZipPath = Path.Combine(Path.GetTempPath(), "dockercli.zip");

                using (HttpClient client = new())
                {
                    using var response = await client.GetAsync(DownloadUrl);
                    response.EnsureSuccessStatusCode();
                    await using var fs = new FileStream(tempZipPath, FileMode.Create);
                    await response.Content.CopyToAsync(fs);
                }

                ZipFile.ExtractToDirectory(tempZipPath, InstallPath, overwriteFiles: true);
                File.Delete(tempZipPath);

                AddToPathIfMissing($"{InstallPath}\\docker");

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void AddToPathIfMissing(string folderPath)
        {
            string path = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? "";
            if (!path.Split(';', StringSplitOptions.RemoveEmptyEntries).Contains(folderPath, StringComparer.OrdinalIgnoreCase))
            {
                string newPath = path.TrimEnd(';') + ";" + folderPath;
                Environment.SetEnvironmentVariable("PATH", newPath, EnvironmentVariableTarget.User);
            }
        }

        private async Task<bool> Configure(string port)
        {
            string user = $"{Environment.UserDomainName}\\{Environment.UserName}";

            string innerCommand = $"wsl.exe -d {distroName} -- bash -c \\\"sudo dockerd -H unix:///var/run/docker.sock -H tcp://0.0.0.0:{port}\\\"";
            string scheduledTask = $"schtasks /Create /F /TN DockerStart " +
                                   $"/TR \"cmd.exe /c \\\"{innerCommand}\\\"\" " +
                                   $"/SC ONLOGON /RU \"{user}\"";

            string output = await ProcessStarter.RunCommandWithOutputAsync("wsl.exe", $"-d {distroName} hostname -I", Encoding.UTF8);
            MessageBox.Show(output);
            var match = RegexHelper.IpRegex().Match(output);
            string ip = match.Success ? match.Value : string.Empty;

            if (string.IsNullOrEmpty(ip)) return false;

            var commands = string.Join(" & ", new[]
            {
                $"netsh advfirewall firewall delete rule name=\"Docker TCP {port}\" >nul 2>&1",
                $"netsh interface portproxy delete v4tov4 listenport={port} listenaddress=0.0.0.0 >nul 2>&1",
                $"netsh advfirewall firewall add rule name=\"Docker TCP {port}\" dir=in action=allow protocol=TCP localport={port}",
                $"netsh interface portproxy add v4tov4 listenport={port} listenaddress=0.0.0.0 connectport={port} connectaddress={ip}",
                scheduledTask
            });

            return await ProcessStarter.RunCommandAsAdminAsync("cmd.exe", $"/c \"{commands}\"");
        }
    }

    public static partial class RegexHelper
    {
        [GeneratedRegex(@"\b(?:\d{1,3}\.){3}\d{1,3}\b")]
        public static partial Regex IpRegex();
    }
}
