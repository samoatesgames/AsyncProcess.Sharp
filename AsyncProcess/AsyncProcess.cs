using System;
using System.ComponentModel;
using System.Diagnostics;
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
        private bool m_isDisposed;

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
                if (!m_process.Start())
                {
                    return new AsyncProcessResult(AsyncProcessCompletionState.FailedToStart);
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
                while(IsRunning())
                {
                    // The user is capturing standard error or standard out, process the streams.
                    if (m_startInfo.IsCapturingOutput())
                    {
                        var stdOutput = await m_process.StandardOutput.ReadLineAsync();
                        if (!string.IsNullOrWhiteSpace(stdOutput))
                        {
                            m_startInfo.OnStandardOutputReceived?.Invoke(stdOutput);
                        }

                        var stdError = await m_process.StandardError.ReadLineAsync();
                        if (!string.IsNullOrWhiteSpace(stdError))
                        {
                            m_startInfo.OnStandardErrorReceived?.Invoke(stdError);
                        }
                    }

                    // Check to see if the process has been cancelled
                    if (m_taskCancellationToken.IsCancellationRequested)
                    {
                        return new AsyncProcessResult(AsyncProcessCompletionState.Cancelled);
                    }

                    // If the process is still running wait a little.
                    if (!m_process.HasExited)
                    {
                        await Task.Delay(10, m_taskCancellationToken);
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
            }

            // The process ran to completion, get the exit code and set it in the result.
            var result = new AsyncProcessResult(AsyncProcessCompletionState.Completed);
            result.SetExitCode(m_process.ExitCode);
            return result;
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

        /// <summary>
        /// Returns true if the process is still running or we are still
        /// processing the output/error streams of the process.
        /// </summary>
        /// <returns></returns>
        public bool IsRunning()
        {
            // Process is still running
            if (!m_process.HasExited)
            {
                return true;
            }

            // If we aren't capturing output, then there is nothing else to test.
            if (!m_startInfo.IsCapturingOutput())
            {
                return false;
            }

            // We are capturing output and still have some standard output to read.
            if (!m_process.StandardOutput.EndOfStream)
            {
                return true;
            }

            // We are capturing output and still have some standard error to read.
            if (!m_process.StandardError.EndOfStream)
            {
                return true;
            }

            // The process has ended and we have fully processed both the standard out
            // and error streams.
            return false;
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
