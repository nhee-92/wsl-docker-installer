using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using wsl_docker_installer.Utils;

namespace wsl_docker_installer.Views.Steps
{
    public partial class TaskInstall : BaseStep
    {
        private readonly string distroName;
        public event Action<bool> isDockerConfigured = delegate { };

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

            bool taskCreated = await CreateScheduledTaskAsync(port);
            bool firewallConfigured = await ConfigureFirewallAndPortProxyAsync(port);
            bool dockerCLIInstalled = await InstallDockerCLI();
            

            if (taskCreated && firewallConfigured && dockerCLIInstalled)
            {
                ConfigureDockerHostEnvironment(port);
                isDockerConfigured.Invoke(true);
                MessageBox.Show($"Port forwarding and task for port {port} successfully set up.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private static void ConfigureDockerHostEnvironment(string port)
        {
            string dockerHost = $"tcp://localhost:{port}";
            Environment.SetEnvironmentVariable("DOCKER_HOST", dockerHost, EnvironmentVariableTarget.User);
            Console.WriteLine($"DOCKER_HOST wurde gesetzt auf: {dockerHost}");
        }

        private static async Task<bool> InstallDockerCLI()
        {
            string DownloadUrl = "https://download.docker.com/win/static/stable/x86_64/docker-26.1.4.zip";
            string InstallPath = @"C:\sw\DockerCLI\docker";

            try
            {
                Directory.CreateDirectory(InstallPath);
                string tempZipPath = Path.Combine(Path.GetTempPath(), "dockercli.zip");

                using (HttpClient client = new HttpClient())
                {
                    using var response = await client.GetAsync(DownloadUrl);
                    response.EnsureSuccessStatusCode();
                    await using var fs = new FileStream(tempZipPath, FileMode.Create);
                    await response.Content.CopyToAsync(fs);
                }

                ZipFile.ExtractToDirectory(tempZipPath, InstallPath, overwriteFiles: true);
                File.Delete(tempZipPath);

                AddToPathIfMissing(InstallPath);

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
                MessageBox.Show("Pfad zur PATH-Variablen hinzugefügt.", "Add to Path", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Pfad bereits in der PATH-Variable vorhanden.", "Add to Path", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async Task<bool> CreateScheduledTaskAsync(string port)
        {
            //TODO: This starts, everytime the user login in, a new cmd to start docker at the given port. Not nice but its works. I'll fix it later.
            string command = $"schtasks /Create /F /RL LIMITED /TN DockerStart /TR \"cmd.exe /k wsl.exe -d {distroName} -- bash -c \\\"sudo dockerd -H unix:///var/run/docker.sock -H tcp://0.0.0.0:{port}\\\"\" /SC ONLOGON";

            return await ProcessStarter.RunCommandAsync("cmd.exe", $"/c {command}", "Could not create the auto start task", Encoding.UTF8);
        }

        private async Task<bool> ConfigureFirewallAndPortProxyAsync(string port)
        {
            string output = await ProcessStarter.RunCommandAndGetOutputAsync("wsl.exe", $"-d {distroName} hostname -I", Encoding.UTF8);
            var match = Regex.Match(output, @"\b(?:\d{1,3}\.){3}\d{1,3}\b");
            string ip = match.Success ? match.Value : string.Empty;

            if (string.IsNullOrEmpty(ip)) return false;

            string addRule = $"netsh advfirewall firewall add rule name=\"Docker TCP {port}\" dir=in action=allow protocol=TCP localport={port}";
            string portProxy = $"netsh interface portproxy add v4tov4 listenport={port} listenaddress=0.0.0.0 connectport={port} connectaddress={ip}";

            await ProcessStarter.RunCommandAsync("netsh", $"advfirewall firewall delete rule name=\"Docker TCP {port}\"", "");
            await ProcessStarter.RunCommandAsync("netsh", $"interface portproxy delete v4tov4 listenport={port} listenaddress=0.0.0.0", "");

            bool firewallRule = await ProcessStarter.RunCommandAsync("cmd.exe", $"/c {addRule}", "Could not update firewall rule");
            bool proxyRule = await ProcessStarter.RunCommandAsync("cmd.exe", $"/c {portProxy}", "Could not update proxy rule");

            return firewallRule && proxyRule;
        }
    }
}
