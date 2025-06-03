using System.Diagnostics;
using System.Text;
using System.Windows;

namespace wsl_docker_installer.Utils
{
    /// <summary>
    /// Provides utility methods to start and manage external processes,
    /// including running with administrator privileges and capturing output.
    /// </summary>
    public class ProcessStarter
    {
        private static readonly string errorKey = "Error";

        /// <summary>
        /// Asynchronously runs a command and shows a message box if an error occurs.
        /// </summary>
        /// <param name="file">The path to the executable file.</param>
        /// <param name="arguments">The command-line arguments.</param>
        /// <param name="errorMessage">The message to display if the command fails.</param>
        /// <param name="encoding">The encoding used for standard output and error.</param>
        /// <returns><c>true</c> if the command completes successfully; otherwise, <c>false</c>.</returns>
        public static async Task<bool> RunCommandAsync(string file, string arguments, string errorMessage, Encoding encoding)
        {
            ProcessStartInfo psi = CreateProcessStart(file, arguments, true, true, false, true, false, encoding);
            try
            {
                return await ProcessStarterHandling(psi, errorMessage);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{ex.Message}", errorKey, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// Asynchronously runs a command and returns the standard output.
        /// </summary>
        /// <param name="file">The path to the executable file.</param>
        /// <param name="arguments">The command-line arguments.</param>
        /// <param name="encoding">The encoding used for standard output.</param>
        /// <returns>The standard output of the process as a <see cref="string"/>.</returns>
        public static async Task<string> RunCommandWithOutputAsync(string file, string arguments, Encoding encoding)
        {
            ProcessStartInfo psi = CreateProcessStart(file, arguments, true, true, false, true, false, encoding);

            try
            {
                using Process process = new() { StartInfo = psi };
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

        /// <summary>
        /// Asynchronously runs a command with administrator privileges.
        /// </summary>
        /// <param name="file">The path to the executable file.</param>
        /// <param name="arguments">The command-line arguments.</param>
        /// <returns><c>true</c> if the command completes successfully; otherwise, <c>false</c>.</returns>
        public static async Task<bool> RunCommandAsAdminAsync(string file, string arguments)
        {
            ProcessStartInfo psi = CreateProcessStart(file, arguments, false, false, true, true, true, Encoding.UTF8);
            try
            {
                return await ProcessStarterAdminHandling(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{ex.Message}", errorKey, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// Creates a <see cref="ProcessStartInfo"/> object with the given parameters.
        /// </summary>
        /// <param name="file">The path to the executable file.</param>
        /// <param name="arguments">The command-line arguments.</param>
        /// <param name="redirectStandardOutput">Whether to redirect the standard output.</param>
        /// <param name="redirectStandardError">Whether to redirect the standard error.</param>
        /// <param name="useShellExecute">Whether to use the shell to start the process.</param>
        /// <param name="createWindow">Whether to create a window for the process.</param>
        /// <param name="isAdmin">Whether to run the process with elevated privileges.</param>
        /// <param name="encoding">The encoding for standard output and error.</param>
        /// <returns>A configured <see cref="ProcessStartInfo"/> object.</returns>
        private static ProcessStartInfo CreateProcessStart(
            string file,
            string arguments,
            bool redirectStandardOutput,
            bool redirectStandardError,
            bool useShellExecute,
            bool createWindow,
            bool isAdmin,
            Encoding encoding)
        {
            ProcessStartInfo psi = new()
            {
                FileName = file,
                Arguments = arguments,
                RedirectStandardOutput = redirectStandardOutput,
                RedirectStandardError = redirectStandardError,
                UseShellExecute = useShellExecute,
                CreateNoWindow = createWindow,
                Verb = isAdmin ? "runas" : string.Empty
            };

            if (redirectStandardOutput && encoding != null)
                psi.StandardOutputEncoding = encoding;

            if (redirectStandardError && encoding != null)
                psi.StandardErrorEncoding = encoding;

            return psi;
        }

        /// <summary>
        /// Starts a process, waits for it to complete, and displays an error message if it fails.
        /// </summary>
        /// <param name="psi">The <see cref="ProcessStartInfo"/> configuration for the process.</param>
        /// <param name="errorMessage">The message to display if the process exits with an error code.</param>
        /// <returns><c>true</c> if the process exits successfully; otherwise, <c>false</c>.</returns>

        private static async Task<bool> ProcessStarterHandling(ProcessStartInfo psi, string errorMessage) 
        {
            using var process = new Process { StartInfo = psi };
            process.Start();

            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                MessageBox.Show($"{errorMessage}\nOutput: {output}\n{errorKey}: {error}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Starts a process with administrator privileges and waits for it to exit.
        /// </summary>
        /// <param name="psi">The <see cref="ProcessStartInfo"/> configuration for the process.</param>
        /// <returns><c>true</c> if the process exits successfully; otherwise, <c>false</c>.</returns>
        private static async Task<bool> ProcessStarterAdminHandling(ProcessStartInfo psi)
        {
            using var process = new Process { StartInfo = psi };
            process.Start();

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                return false;
            }

            return true;
        }
    }
}
