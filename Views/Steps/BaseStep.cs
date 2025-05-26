using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace wsl_docker_installer.Views.Steps
{
    public abstract class BaseStep : UserControl
    {
        public event Action<bool> StepReadyChanged = delegate { };

        protected void SetStepReady(bool ready)
        {
            StepReadyChanged?.Invoke(ready);
        }
    }
}
