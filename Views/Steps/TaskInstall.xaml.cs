﻿using System.IO;
using System.IO.Compression;
using System.Net.Http;
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
            WSLLocalhostFowarding();
            bool configured = await CreateSchtask(port);

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
            string dockerCLIUrl = "https://download.docker.com/win/static/stable/x86_64/docker-26.1.4.zip";
            string dockerComposeCLIUrl = "https://github.com/docker/compose/releases/download/1.29.2/docker-compose-Windows-x86_64.exe";
            string installPath = @"C:\sw\DockerCLI";
            string dockerComposeTargetDirectory = @"C:\sw\DockerCLI\docker";
            string tempFile = Path.Combine(Path.GetTempPath(), "docker-compose.exe");
            string targetFile = Path.Combine(dockerComposeTargetDirectory, "docker-compose.exe");

            try
            {
                Directory.CreateDirectory(installPath);
                string tempZipPath = Path.Combine(Path.GetTempPath(), "dockercli.zip");

                using (HttpClient client = new())
                {
                    using var response = await client.GetAsync(dockerCLIUrl);
                    response.EnsureSuccessStatusCode();
                    await using var fs = new FileStream(tempZipPath, FileMode.Create);
                    await response.Content.CopyToAsync(fs);
                }

                ZipFile.ExtractToDirectory(tempZipPath, installPath, overwriteFiles: true);
                File.Delete(tempZipPath);

                using (HttpClient client = new())
                {
                    using var response = await client.GetAsync(dockerComposeCLIUrl);
                    response.EnsureSuccessStatusCode();
                    using var fs = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None);
                    await response.Content.CopyToAsync(fs);
                }

                Directory.CreateDirectory(dockerComposeTargetDirectory);
                File.Copy(tempFile, targetFile, overwrite: true);
                File.Delete(tempFile);

                AddToPathIfMissing($"{installPath}\\docker");

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
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

        private static void WSLLocalhostFowarding() 
        {
            string wslConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".wslconfig");

            if (!File.Exists(wslConfigPath))
            {
                File.WriteAllText(wslConfigPath, "[wsl2]\nlocalhostForwarding=true\n");
            }
            else
            {
                var lines = File.ReadAllLines(wslConfigPath).ToList();
                bool sectionExists = lines.Any(line => line.Trim().Equals("[wsl2]", StringComparison.OrdinalIgnoreCase));
                bool forwardingSet = lines.Any(line => line.Trim().StartsWith("localhostForwarding", StringComparison.OrdinalIgnoreCase));

                if (!sectionExists)
                    lines.Add("[wsl2]");

                if (!forwardingSet)
                    lines.Add("localhostForwarding=true");

                File.WriteAllLines(wslConfigPath, lines);
            }
        }

        private async Task<bool> CreateSchtask(string port)
        {
            string user = $"{Environment.UserDomainName}\\{Environment.UserName}";
            string innerCommand = $"wsl.exe -d {distroName} -- bash -c \\\"sudo dockerd -H unix:///var/run/docker.sock -H tcp://127.0.0.1:{port}\\\"";
            string scheduledTask = $"schtasks /Create /F /TN DockerStart " +
                                   $"/TR \"cmd.exe /c \\\"{innerCommand}\\\"\" " +
                                   $"/SC ONLOGON /RU \"{user}\"";

            return await ProcessStarter.RunCommandAsAdminAsync("cmd.exe", $"/c \"{scheduledTask}\"");
        }
    }

    public static partial class RegexHelper
    {
        [GeneratedRegex(@"\b(?:\d{1,3}\.){3}\d{1,3}\b")]
        public static partial Regex IpRegex();
    }
}
