using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using wsl_docker_installer.Utils;

namespace wsl_docker_installer.Views.Steps
{
    public partial class CertificatesInstall : BaseStep
    {
        public event Action<bool> CertificatesInstalled = delegate { };
        private readonly string distroName;
        public CertificatesInstall(string DistroName)
        {
            InitializeComponent();
            this.distroName = DistroName;
        }

        public async void InstallCertificate()
        {
            PrepareUI();
            
            bool areCertificatesInstalled = await ImportCertificatesToVM(SelectCertTextBox.Text, SelectPemTextBox.Text);

            CertificatesInstalled.Invoke(areCertificatesInstalled);
            InstallCertsSpinner.Visibility = Visibility.Collapsed;
            InstallCertsTextBox.Text = areCertificatesInstalled ? "Certificates are successfully installed" : "Installation failed";
            InstallCertsTextBox.Foreground = new SolidColorBrush(areCertificatesInstalled ? Colors.Green : Colors.Red);
            SetStepReady(true);
        }

        public async Task<bool> ImportCertificatesToVM(string certsDir, string certsPemDir)
        {
            if (!Directory.Exists(certsDir))
                throw new ArgumentException("Das Verzeichnis für Zertifikate existiert nicht.", nameof(certsDir));

            if (!Directory.Exists(certsPemDir))
                throw new ArgumentException("Das Verzeichnis für PEM-Zertifikate existiert nicht.", nameof(certsPemDir));

            try
            {
                await ProcessStarter.RunCommandInDistroAsync(distroName, "mkdir -p /usr/local/share/ca-certificates", "Failed to create dir");

                foreach (var certFile in Directory.GetFiles(certsPemDir, "*.pem"))
                {
                    string wslCertFile = ConvertToWslPath(certFile);
                    string baseName = Path.GetFileNameWithoutExtension(certFile);
                    string destination = $"/usr/local/share/ca-certificates/{baseName}.crt";

                    await ProcessStarter.RunCommandInDistroAsync(distroName, $"cp \"{wslCertFile}\" \"{destination}\"", "Failed to copy certificates");
                }

                var commandWorked = await ProcessStarter.RunCommandInDistroAsync(distroName, "update-ca-certificates", "Failed to use update-ca-certificates");

                if (commandWorked) return true;
                else
                {
                    await ProcessStarter.RunCommandInDistroAsync(distroName, "cp /etc/ssl/certs/ca-certificates.crt /etc/ssl/certs/ca-certificates.crt.bak", "Failed to copy");
                    await ProcessStarter.RunCommandInDistroAsync(distroName, "cat /usr/local/share/ca-certificates/*.crt >> /etc/ssl/certs/ca-certificates.crt", "Failed to copy");

                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static string ConvertToWslPath(string windowsPath)
        {
            return "/mnt/" + windowsPath[0].ToString().ToLower() + windowsPath.Substring(2).Replace("\\", "/");
        }

        private void InstallCertsCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            SelectCertTextBox.Text = string.Empty;
            SelectCertButton.IsEnabled = false;
            SelectPemTextBox.Text = string.Empty;
            SelectPemButton.IsEnabled = false;

            SelectCertTextBox.IsEnabled = false;
            SelectPemTextBox.IsEnabled = false;

            CertificatesInstalled.Invoke(true);
            SetStepReady(true);

        }

        private void InstallCertsCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            SelectCertTextBox.IsEnabled = true;
            SelectPemTextBox.IsEnabled = true;
            SelectPemButton.IsEnabled = true;
            SelectCertButton.IsEnabled = true;

            CertificatesInstalled.Invoke(false);
            SetStepReady(false);
        }

        private void PrepareUI()
        {
            SetStepReady(false);
            SelectPemButton.IsEnabled = false;
            SelectPemTextBox.IsEnabled = false;
            SelectCertButton.IsEnabled = false;
            SelectCertTextBox.IsEnabled = false;
            InstallCertsCheckBox.IsEnabled = false;
            InstallCertsCheckBox.Visibility = Visibility.Collapsed;
            InstallCertsSpinner.Visibility = Visibility.Visible;
            InstallCertsTextBox.Text = "Install certificates...";
        }

        private void SelectFolderButton_Click(object sender, RoutedEventArgs e)
        {
            using FolderBrowserDialog dialog = new()
            {
                Description = "Please select a folder.",
                ShowNewFolderButton = true
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                if (sender is System.Windows.Controls.Button button && button.Tag is string textBoxName)
                {
                    if (this.FindName(textBoxName) is System.Windows.Controls.TextBox textBox)
                    {
                        textBox.Text = dialog.SelectedPath;
                    }
                }
            }
        }
    }
}
