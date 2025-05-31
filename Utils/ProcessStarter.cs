using System.Diagnostics;
using System.Text;
using System.Windows;

namespace wsl_docker_installer.Utils
{
    public class ProcessStarter
    {
        private static readonly string errorKey = "Error";

        public static async Task<string> RunCommandAndGetOutputAsync(string file, string arguments, Encoding encoding)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = file,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = encoding,
                    StandardErrorEncoding = encoding
                };

                using var process = new Process { StartInfo = psi };
                process.Start();

                string output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                return output;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{ex.Message}", errorKey, MessageBoxButton.OK, MessageBoxImage.Error);
                return $"{errorKey}: no output";
            }
        }

        public static async Task<bool> RunCommandAsync(string file, string arguments, string errorMessage) 
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = file,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true, 
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.Unicode,
                    StandardErrorEncoding = Encoding.Unicode
                };
                return await ProcessStarterHandling(psi, errorMessage);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{ex.Message}", errorKey, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public static async Task<bool> RunCommandInDistroAsync(string distroName, string bashCommand, string errorMessage)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "wsl.exe",
                    Arguments = $"-d {distroName} -- bash -c \"{bashCommand.Replace("\"", "\\\"")}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                return await ProcessStarterHandling(psi, errorMessage, distroName);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{ex.Message}", errorKey, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private static async Task<bool> ProcessStarterHandling(ProcessStartInfo psi, string errorMessage)
        {
            return await ProcessStarterHandling(psi, errorMessage, "No-Distribution");
        }

        private static async Task<bool> ProcessStarterHandling(ProcessStartInfo psi, string errorMessage, string distroName) 
        {
            using var process = new Process { StartInfo = psi };
            process.Start();

            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                MessageBox.Show($"{errorMessage} Distribution: {distroName}\nOutput: {output}\n{errorKey}: {error}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }
    }
}
