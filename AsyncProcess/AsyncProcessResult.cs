using System;

namespace SamOatesGames.System
{
    /// <summary>
    /// An object describing the result of a process run.
    /// All run methods of an Async Process will return one of these.
    /// </summary>
    public class AsyncProcessResult
    {
        /// <summary>
        /// The state in which the process resulted in.
        /// </summary>
        public AsyncProcessCompletionState CompletionState { get; }

        /// <summary>
        /// If the state denotes an error, the exception which caused that state.
        /// Otherwise this will be null.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// If the process completed, this will be set to the exit code of the process.
        /// Otherwise this will be set to the minimum value possible for an integer.
        /// </summary>
        public int ExitCode { get; private set; } = int.MinValue;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="completionState">The state the result should be set to</param>
        /// <param name="exception">An optional exception that the result should contain</param>
        internal AsyncProcessResult(AsyncProcessCompletionState completionState, Exception exception = null)
        {
            CompletionState = completionState;
            Exception = exception;
        }

        /// <summary>
        /// Sets the exit code of the result to the specified value.
        /// </summary>
        /// <param name="exitCode">The exit code to use as the exit code of this result</param>
        internal void SetExitCode(int exitCode)
        {
            ExitCode = exitCode;
        }
    }
}
