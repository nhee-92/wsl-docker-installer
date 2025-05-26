using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using wsl_docker_installer.Views.Steps;

namespace wsl_docker_installer
{
    public partial class MainWindow : Window
    {
        private int currentStep = 1;
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
                    StepContent.Content = new StartStep();
                    break;

                case 2:
                    LockNextButtonWhileLoading(new InstallationCheck());
                    break;

                default:
                    StepContent.Content = null;
                    break;
            }
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

        private void Footer_NextClicked(object sender, RoutedEventArgs e)
        {
            currentStep++;
            if (currentStep > 2)
            {
                MessageBox.Show("Setup finished");
                Application.Current.Shutdown();
                return;
            }
            ShowCurrentStep();
        }

        private void Footer_CancelClicked(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Do you really want to end the setup?", "Confirm cancel", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (result == MessageBoxResult.OK)
            {
                Application.Current.Shutdown();
            }
        }
    }
}