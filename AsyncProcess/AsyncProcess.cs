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

        private readonly Process m_process;
        private readonly CancellationToken m_taskCancellationToken;

        #endregion

        #region Constructors

        public AsyncProcess(ProcessStartInfo startInfo, CancellationToken? cancellationToken = null)
        {
            m_process = new Process
            {
                StartInfo = startInfo
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
                while (!m_process.HasExited)
                {
                    if (m_taskCancellationToken.IsCancellationRequested)
                    {
                        return new AsyncProcessResult(AsyncProcessCompletionState.Cancelled);
                    }

                    await Task.Delay(1, m_taskCancellationToken);
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
            return await Run(new ProcessStartInfo(fileName, arguments));
        }

        public static async Task<AsyncProcessResult> Run(ProcessStartInfo startInfo)
        {
            using (var asyncProcess = new AsyncProcess(startInfo))
            {
                return await asyncProcess.Run();
            }
        }

        #endregion
    }
}
