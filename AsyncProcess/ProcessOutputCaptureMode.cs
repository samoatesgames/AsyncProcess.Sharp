using System;

namespace SamOatesGames.System
{
    [Flags]
    public enum ProcessOutputCaptureMode
    {
        /// <summary>
        /// Do not capture the output and store it in
        /// the process result.
        /// </summary>
        None = 0,

        /// <summary>
        /// Capture the standard output of the process and
        /// store it in the process result.
        /// </summary>
        Output = 1,

        /// <summary>
        /// Capture the standard error of the process and
        /// store it in the process result.
        /// </summary>
        Error = 2,

        /// <summary>
        /// Capture the standard output and the standard error
        /// of the process and store it in the process result.
        /// </summary>
        Both = Output | Error
    }
}
