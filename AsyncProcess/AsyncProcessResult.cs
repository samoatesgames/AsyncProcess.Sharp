using System;

namespace SamOatesGames.System
{
    public class AsyncProcessResult
    {
        public AsyncProcessCompletionState CompletionState { get; }
        public Exception Exception { get; }
        public int ExitCode { get; private set; } = int.MinValue;

        internal AsyncProcessResult(AsyncProcessCompletionState completionState, Exception exception)
        {
            CompletionState = completionState;
            Exception = exception;
        }

        internal AsyncProcessResult(AsyncProcessCompletionState completionState) : this(completionState, null)
        {
        }

        internal void SetExitCode(int exitCode)
        {
            ExitCode = exitCode;
        }
    }
}
