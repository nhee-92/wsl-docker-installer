using System.Net.Http;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using wsl_docker_installer.Views.Steps;

namespace wsl_docker_installer
{
    public partial class MainWindow : Window
    {
        private readonly String footerPrimaryButtonNameNext = "Next";
        private readonly String footerPrimaryButtonNameInstall = "Install";

        private int currentStep = 1;
        private bool isWslInstalled = false;
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
                    Header.Title = "Information";
                    Header.Subtitle = "Please read the following important information before continuing.";
                    currentStepControl = new StartStep();
                    break;

                case 2:
                    Header.Title = "WSL Installation";
                    Header.Subtitle = "Check for available installation";

                    var wslInstallationCheck = new WslInstallationCheck();
                    LockNextButtonWhileLoading(wslInstallationCheck);
                    currentStepControl = wslInstallationCheck;

                    wslInstallationCheck.WslCheckCompleted += (installed) =>
                    {
                        isWslInstalled = installed;
                        Footer.SetNextButtonText(installed ? footerPrimaryButtonNameNext : footerPrimaryButtonNameInstall);
                    };

                    wslInstallationCheck.StepReadyChanged += ready => Footer.IsNextEnabled = ready;
                    wslInstallationCheck.NextButtonTextChanged += text => Footer.SetNextButtonText(text);
                    break;

                case 3:
                    Header.Title = "Ubuntu Installation";
                    Header.Subtitle = "To ensure the right environment for docker, another VM is installed in the wsl that is only responsible for docker.";
                    Footer.SetNextButtonText(footerPrimaryButtonNameInstall);
                    var distroInstall = new DistroInstall();
                    LockNextButtonWhileLoading(distroInstall);
                    currentStepControl = distroInstall;

                    break;

                default:
                    Header.Title = "Error";
                    Header.Subtitle = "oops, that wasn't supposed to happen.";
                    currentStepControl = null!;
                    break;
            }

            StepContent.Content = currentStepControl;
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

        private async void FooterNextClicked(object sender, RoutedEventArgs e)
        {
            if (currentStepControl is WslInstallationCheck wslCheck && !isWslInstalled)
            {
                wslCheck.InstallWslAsync();
                return;
            }
            else if (currentStepControl is DistroInstall distroInstall)
            {
                Footer.NextButton.IsEnabled = false;
                Footer.SetNextButtonText("Installing...");
                await distroInstall.InstallCustomUbuntuDistroAsync();
            }
            else
            {
                currentStep++;
                ShowCurrentStep();
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