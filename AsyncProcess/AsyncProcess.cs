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

        #endregion

        #region Constructors

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

        public void Dispose()
        {
            m_process?.Dispose();
        }

        #endregion

        #region Private Methods

        private async Task<AsyncProcessResult> InternalRun()
        {
            try
            {
                if (!m_process.Start())
                {
                    return new AsyncProcessResult(AsyncProcessCompletionState.FailedToStart);
                }
            }
            catch (Win32Exception e)
            {
                var state = AsyncProcessCompletionState.Unknown;
                if (e.Message == "The system cannot find the file specified")
                {
                    state = AsyncProcessCompletionState.ProcessToRunMissing;
                }

                return new AsyncProcessResult(state, e);
            }
            catch (Exception e)
            {
                return new AsyncProcessResult(AsyncProcessCompletionState.Unknown, e);
            }

            try
            {
                while(IsRunning())
                {
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

                    if (m_taskCancellationToken.IsCancellationRequested)
                    {
                        return new AsyncProcessResult(AsyncProcessCompletionState.Cancelled);
                    }

                    if (!m_process.HasExited)
                    {
                        await Task.Delay(1, m_taskCancellationToken);
                    }
                }
            }
            catch (TaskCanceledException)
            {
                return new AsyncProcessResult(AsyncProcessCompletionState.Cancelled);
            }
            catch (Exception e)
            {
                return new AsyncProcessResult(AsyncProcessCompletionState.Unknown, e);
            }
            finally
            {
                while (!m_process.HasExited)
                {
                    m_process.Kill();
                    await Task.Delay(1, CancellationToken.None);
                }
            }

            var result = new AsyncProcessResult(AsyncProcessCompletionState.Completed);
            result.SetExitCode(m_process.ExitCode);
            return result;
        }

        private bool IsRunning()
        {
            if (!m_process.HasExited)
            {
                return true;
            }

            if (!m_startInfo.IsCapturingOutput())
            {
                return false;
            }

            if (!m_process.StandardOutput.EndOfStream)
            {
                return true;
            }

            if (!m_process.StandardError.EndOfStream)
            {
                return true;
            }

            return false;
        }

        #endregion

        #region Public Methods

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

        public static async Task<AsyncProcessResult> Run(string fileName)
        {
            return await Run(fileName, string.Empty);
        }

        public static async Task<AsyncProcessResult> Run(string fileName, string arguments)
        {
            return await Run(new AsyncProcessStartInfo(fileName, arguments));
        }

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
