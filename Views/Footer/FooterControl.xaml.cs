using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace wsl_docker_installer
{
    public partial class FooterControl : UserControl
    {
        public FooterControl()
        {
            InitializeComponent();
            NextClicked = delegate { };
            CancelClicked = delegate { };
        }

        public Button NextButton => NextBtn;
        public event RoutedEventHandler NextClicked;
        public event RoutedEventHandler CancelClicked;

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            NextClicked?.Invoke(this, e);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            CancelClicked?.Invoke(this, e);
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true
            });
            e.Handled = true;
        }
    }
}
