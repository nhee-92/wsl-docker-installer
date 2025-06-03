using System.Diagnostics;
using System.Windows.Navigation;

namespace wsl_docker_installer.Views.Steps
{
    public partial class StartStep : BaseStep
    {
        public StartStep()
        {
            InitializeComponent();
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
