using System.Text;
using System.Windows;
using wsl_docker_installer.Utils;
using wsl_docker_installer.Views.Steps;

namespace wsl_docker_installer
{
    public partial class MainWindow : Window
    {
        private readonly string footerPrimaryButtonNameNext = "Next";
        private readonly string footerPrimaryButtonNameInstall = "Install";
        private readonly string footerPrimaryButtonNameRetry = "Try Again";
        private readonly string footerPrimaryButtonNameFinish = "Finish";

        private int currentStep = 1;
        private string distroName = "";
        private bool isWslInstalled = false;
        private bool isCertificatesInstalled = false;
        private bool isDistroInstalled = false;
        private bool IsDockerInstalled = false;
        private bool IsDockerConfigured = false;
        private BaseStep currentStepControl = null!;

        public MainWindow()
        {
            InitializeComponent();
            ShowCurrentStep();
        }

        private void ShowCurrentStep()
        {
            switch (currentStep)
            {
                case 1:
                    RenderStartStep();

                    break;

                case 2:
                    RenderWslStep();

                    break;

                case 3:
                    RenderDistroStep();

                    break;

                case 4:
                    RenderCertificatesStep();

                    break;

                case 5:
                    RenderDockerStep();

                    break;

                case 6:
                    RenderTaskStep();

                    break;

                default:
                    DefaultStep();

                    break;
            }

            StepContent.Content = currentStepControl;
        }

        private void DefaultStep ()
        {
            Header.Title = "Error";
            Header.Subtitle = "oops, that wasn't supposed to happen.";
            currentStepControl = null!;
        }

        private void RenderStartStep()
        {
            Header.Title = "Information";
            Header.Subtitle = "Please read the following important information before continuing.";
            currentStepControl = new StartStep();
        }

        private void RenderWslStep()
        {
            Header.Title = "WSL Installation";
            Header.Subtitle = "Check for available installation";

            var wslInstallationCheck = new WslInstall();
            LockNextButtonWhileLoading(wslInstallationCheck);
            currentStepControl = wslInstallationCheck;

            wslInstallationCheck.WslCheckCompleted += (installed) =>
            {
                isWslInstalled = installed;
                Footer.SetNextButtonText(installed ? footerPrimaryButtonNameNext : footerPrimaryButtonNameInstall);
            };

            // TODO: Maybe unnecessary, but ensures the footer is updated correctly
            wslInstallationCheck.StepReadyChanged += ready => Footer.IsNextEnabled = ready;
            wslInstallationCheck.NextButtonTextChanged += text => Footer.SetNextButtonText(text);
        }

        private void RenderCertificatesStep()
        {
            Header.Title = "Certificate Installation (Optional)";
            Header.Subtitle = "Import your own certificates if needed";

            Footer.SetNextButtonText(footerPrimaryButtonNameInstall);
            var certificatesInstall = new CertificatesInstall(distroName);
            currentStepControl = certificatesInstall;

            certificatesInstall.CertificatesInstalled += (installed) =>
            {
                isCertificatesInstalled = installed;
                Footer.SetNextButtonText(installed ? footerPrimaryButtonNameNext : footerPrimaryButtonNameInstall);
            };


        }

        private void RenderDistroStep()
        {
            Header.Title = "Ubuntu Installation";
            Header.Subtitle = "To ensure the right environment for docker, another VM is installed in the wsl that is only responsible for docker.";
            Footer.SetNextButtonText(footerPrimaryButtonNameInstall);
            var distroInstall = new DistroInstall();
            LockNextButtonWhileLoading(distroInstall);
            currentStepControl = distroInstall;

            distroInstall.DistroNameConfirmed += name => {
                distroName = name;
            };

            distroInstall.DistroInstallCheck += (installed) =>
            {
                isDistroInstalled = installed;
                Footer.SetNextButtonText(installed ? footerPrimaryButtonNameNext : footerPrimaryButtonNameInstall);
            };
        }

        private void RenderDockerStep()
        {
            Header.Title = "Docker Installation";
            Header.Subtitle = "Docker is now being installed in the WSL VM.";
            Footer.SetNextButtonText(footerPrimaryButtonNameInstall);
            var dockerInstall = new DockerInstall(distroName);
            currentStepControl = dockerInstall;

            dockerInstall.DockerInstalled += (installed) =>
            {
                IsDockerInstalled = installed;
                Footer.SetNextButtonText(IsDockerInstalled ? footerPrimaryButtonNameNext : footerPrimaryButtonNameRetry);
            };
        }

        private void RenderTaskStep()
        {
            Header.Title = "Configure Docker and Windows";
            Header.Subtitle = "";
            Footer.SetNextButtonText(footerPrimaryButtonNameInstall);
            var taskInstall = new TaskInstall(distroName);
            currentStepControl = taskInstall;

            taskInstall.IsDockerConfigured += (configured) =>
            {
                IsDockerConfigured = configured;
                Footer.SetNextButtonText(IsDockerConfigured ? footerPrimaryButtonNameFinish : footerPrimaryButtonNameRetry);
            };
        }

        private void LockNextButtonWhileLoading(BaseStep step)
        {
            step.StepReadyChanged += (ready) =>
            {
                Footer.NextButton.IsEnabled = ready;
            };
            Footer.NextButton.IsEnabled = false;
            StepContent.Content = step;
        }

        private void FooterNextClicked(object sender, RoutedEventArgs e)
        {

            if (currentStepControl is WslInstall wslCheck && !isWslInstalled)
            {
                wslCheck.InstallWslAsync();
                return;
            }
            else if (currentStepControl is CertificatesInstall certificatesInstall && !isCertificatesInstalled)
            {
                certificatesInstall.InstallCertificate();
                return;
            }
            else if (currentStepControl is DistroInstall distroInstall && !isDistroInstalled)
            {
                distroInstall.HandleInstallDistro();
                return;
            }
            else if (currentStepControl is DockerInstall dockerInstall && !IsDockerInstalled)
            {
                LockNextButtonWhileLoading(dockerInstall);
                dockerInstall.InstallDocker();
                return;
            }
            else if (currentStepControl is TaskInstall taskInstall && !IsDockerConfigured)
            {
                LockNextButtonWhileLoading(taskInstall);
                taskInstall.ConfigureDocker();
                return;
            }
            else if (currentStepControl is TaskInstall && IsDockerConfigured)
            {
                AskAndRestart();
            }
            else
            {
                currentStep++;
                ShowCurrentStep();
            }
        }

        public static async void AskAndRestart()
        {
            var result = MessageBox.Show(
                "The system needs to restart to complete the installation. Do you want to restart now?",
                "Restart Required",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                await ProcessStarter.RunCommandAsync("shutdown.exe", "/r /t 0", "", Encoding.Unicode); 
            }
            if (result == MessageBoxResult.No)
            {
                Application.Current.Shutdown();
            }
        }

        private void FooterCancelClicked(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Do you really want to end the setup?", "Confirm cancel", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (result == MessageBoxResult.OK)
            {
                Application.Current.Shutdown();
            }
        }
    }
}