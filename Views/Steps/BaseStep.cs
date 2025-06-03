using System.Windows.Controls;

namespace wsl_docker_installer.Views.Steps
{
    public abstract class BaseStep : UserControl
    {
        public event Action<bool> StepReadyChanged = delegate { };
        public event Action<string> NextButtonTextChanged = delegate { };
        public event Action<int> OverrideNextStep = delegate { };
        public event Action ForceNextClicked = delegate { };

        protected void TriggerForceNextAction()
        {
            ForceNextClicked?.Invoke();
        }

        protected void SetStepReady(bool ready)
        {
            StepReadyChanged?.Invoke(ready);
        }

        protected void SetNextButtonText(string text)
        {
            NextButtonTextChanged?.Invoke(text);
        }

        protected void SetNextStep(int step)
        {
            OverrideNextStep?.Invoke(step);
        }
    }
}
