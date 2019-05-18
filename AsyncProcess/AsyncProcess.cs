using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SamOatesGames.System
{
    public class AsyncProcess : IDisposable
    {
        #region Private Members

        private readonly AsyncProcessStartInfo m_startInfo;
        private readonly Process m_process;
        private readonly CancellationToken m_taskCancellationToken;
        
        private readonly AutoResetEvent m_standardOutputClosed = new AutoResetEvent(false);
        private readonly AutoResetEvent m_standardErrorClosed = new AutoResetEvent(false);

        private readonly StringBuilder m_standardOutputBuilder = new StringBuilder();
        private readonly StringBuilder m_standardErrorBuilder = new StringBuilder();

        private bool m_isDisposed;

        #endregion

        #region Public Properties

        /// <summary>
        /// Returns true if the process is still running or we are still
        /// processing the output/error streams of the process.
        /// </summary>
        public bool IsRunning { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Class constructor
        /// </summary>
        /// <param name="startInfo">The information required for the running of the process.</param>
        /// <param name="cancellationToken">An optional cancellation token, which if set will about the running process.</param>
        public AsyncProcess(AsyncProcessStartInfo startInfo, CancellationToken? cancellationToken = null)
        {
            m_startInfo = startInfo;
            m_process = new Process
            {
                StartInfo = startInfo.ToProcessStartInfo()
            };

            m_taskCancellationToken = cancellationToken ?? CancellationToken.None;
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Dispose of the internal Process.
        /// </summary>
        public void Dispose()
        {
            if (m_isDisposed)
            {
                return;
            }

            m_isDisposed = true;
            m_process?.Dispose();
            m_standardOutputClosed?.Dispose();
            m_standardErrorClosed?.Dispose();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// The actual implementation of running a process and waiting for it
        /// to complete. It also handles the forwarding of standard out and standard
        /// error message if the user has setup subscriptions.
        /// </summary>
        /// <returns></returns>
        private async Task<AsyncProcessResult> InternalRun()
        {
            // First start the actual process and handle the possible fail cases
            try
            {
                if (m_startInfo.IsCapturingStandardOutput())
                {
                    m_process.OutputDataReceived += OnProcessStandardOutputReceived;
                }

                if (m_startInfo.IsCapturingStandardError())
                {
                    m_process.ErrorDataReceived += OnProcessStandardErrorReceived;
                }

                if (!m_process.Start())
                {
                    return new AsyncProcessResult(AsyncProcessCompletionState.FailedToStart);
                }

                if (m_startInfo.IsCapturingStandardOutput())
                {
                    m_process.BeginOutputReadLine();
                }

                if (m_startInfo.IsCapturingStandardError())
                {
                    m_process.BeginErrorReadLine();
                }
            }
            catch (Win32Exception e)
            {
                // Check to see if this is the known case of the user specifying a process file name
                // which doesn't exist.
                var state = AsyncProcessCompletionState.Unknown;
                if (e.Message == "The system cannot find the file specified")
                {
                    state = AsyncProcessCompletionState.ProcessToRunMissing;
                }

                return new AsyncProcessResult(state, e);
            }
            catch (Exception e)
            {
                // Something unknown happened! return passing the exception to the result
                return new AsyncProcessResult(AsyncProcessCompletionState.Unknown, e);
            }
            
            // We have started the process successfully, wait for the process to complete.
            try
            {
                IsRunning = true;

                while (!m_process.WaitForExit(10))
                {
                    // Check to see if the process has been cancelled
                    if (m_taskCancellationToken.IsCancellationRequested)
                    {
                        return new AsyncProcessResult(AsyncProcessCompletionState.Cancelled);
                    }
                }
            }
            catch (TaskCanceledException)
            {
                return new AsyncProcessResult(AsyncProcessCompletionState.Cancelled);
            }
            catch (Exception e)
            {
                // Something unknown happened! return passing the exception to the result
                return new AsyncProcessResult(AsyncProcessCompletionState.Unknown, e);
            }
            finally
            {
                // If we are stopping the run, but the process is still alive kill it.
                while (!m_process.HasExited)
                {
                    m_process.Kill();
                    await Task.Delay(1, CancellationToken.None);
                }

                if (m_startInfo.IsCapturingStandardOutput())
                {
                    if (!m_taskCancellationToken.IsCancellationRequested)
                    {
                        m_standardOutputClosed.WaitOne(m_startInfo.OutputRedirectingTimeout);
                    }

                    m_process.OutputDataReceived -= OnProcessStandardOutputReceived;
                }

                if (m_startInfo.IsCapturingStandardError())
                {
                    if (!m_taskCancellationToken.IsCancellationRequested)
                    {
                        m_standardErrorClosed.WaitOne(m_startInfo.OutputRedirectingTimeout);
                    }

                    m_process.ErrorDataReceived -= OnProcessStandardErrorReceived;
                }

                IsRunning = false;
            }

            // The process ran to completion, get the exit code and set it in the result.
            var result = new AsyncProcessResult(AsyncProcessCompletionState.Completed);
            result.SetExitCode(m_process.ExitCode);

            if (m_startInfo.CaptureOutputToProcessResult.HasFlag(ProcessOutputCaptureMode.Output))
            {
                result.SetStandardOutput(m_standardOutputBuilder.ToString());
            }

            if (m_startInfo.CaptureOutputToProcessResult.HasFlag(ProcessOutputCaptureMode.Error))
            {
                result.SetStandardError(m_standardErrorBuilder.ToString());
            }

            return result;
        }

        /// <summary>
        /// Callback from the internal process, used for capturing and or forwarding standard
        /// output messages, this will only be called if the start info is requesting some kind
        /// of output capturing.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnProcessStandardOutputReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null)
            {
                // Pipe closed
                m_standardOutputClosed.Set();
                return;
            }

            m_startInfo.OnStandardOutputReceived?.Invoke(e.Data);

            if (m_startInfo.CaptureOutputToProcessResult.HasFlag(ProcessOutputCaptureMode.Output))
            {
                m_standardOutputBuilder.AppendLine(e.Data);
            }
        }

        /// <summary>
        /// Callback from the internal process, used for capturing and or forwarding standard
        /// error messages, this will only be called if the start info is requesting some kind
        /// of error capturing.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnProcessStandardErrorReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null)
            {
                // Pipe closed
                m_standardErrorClosed.Set();
                return;
            }

            m_startInfo.OnStandardErrorReceived?.Invoke(e.Data);

            if (m_startInfo.CaptureOutputToProcessResult.HasFlag(ProcessOutputCaptureMode.Error))
            {
                m_standardOutputBuilder.AppendLine(e.Data);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Run the actual application described by this async process.
        /// </summary>
        /// <returns>An awaitable task containing the result of the process run.</returns>
        public async Task<AsyncProcessResult> Run()
        {
            if (m_process == null)
            {
                throw new NullReferenceException("The internal process is null.");
            }

            return await Task.Run(InternalRun, CancellationToken.None);
        }

        #endregion

        #region Public Static Methods

        /// <summary>
        /// Create a new process, executing the specified file.
        /// </summary>
        /// <param name="fileName">The file to use when launching the process.</param>
        /// <returns>An awaitable task containing the result of the process run.</returns>
        public static async Task<AsyncProcessResult> Run(string fileName)
        {
            return await Run(fileName, string.Empty);
        }

        /// <summary>
        /// Create a new process, executing the specified file with the provided launch arguments.
        /// </summary>
        /// <param name="fileName">The file to use when launching the process.</param>
        /// <param name="arguments">The arguments to use when launching the process.</param>
        /// <returns>An awaitable task containing the result of the process run.</returns>
        public static async Task<AsyncProcessResult> Run(string fileName, string arguments)
        {
            return await Run(new AsyncProcessStartInfo(fileName, arguments));
        }

        /// <summary>
        /// Create a new process, executing using the provided start info.
        /// </summary>
        /// <param name="startInfo">The information used when starting the new process.</param>
        /// <returns>An awaitable task containing the result of the process run.</returns>
        public static async Task<AsyncProcessResult> Run(AsyncProcessStartInfo startInfo)
        {
            using (var asyncProcess = new AsyncProcess(startInfo))
            {
                return await asyncProcess.Run();
            }
        }

        #endregion
    }
}
