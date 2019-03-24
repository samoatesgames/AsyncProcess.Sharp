namespace SamOatesGames.System
{
    public enum AsyncProcessCompletionState
    {
        /// <summary>
        /// The process completed successfully.
        /// The exit code will be set to the exit code of the process
        /// within the AsyncProcessResult.
        /// </summary>
        Completed,

        /// <summary>
        /// Something went wrong.
        /// The exception object of the AsyncProcessResult will be set containing the
        /// actual exception which occurred.
        /// </summary>
        Unknown,

        /// <summary>
        /// The process run was cancelled via the cancellation token provided.
        /// </summary>
        Cancelled,

        /// <summary>
        /// The process failed to start correctly for an unknown reason.
        /// </summary>
        FailedToStart,

        /// <summary>
        /// The process could not be started because the file name provided as the process
        /// could not be found.
        /// </summary>
        ProcessToRunMissing
    }
}
