using System;
using System.Diagnostics;

namespace SamOatesGames.System
{
    public class AsyncProcessStartInfo
    {
        /// <summary>
        /// The file name of the process to run.
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// The launch arguments to use when starting the process.
        /// </summary>
        public string Arguments { get; }

        /// <summary>
        /// If true, no window will be created when the process starts.
        /// </summary>
        public bool CreateNoWindow { get; set; }

        /// <summary>
        /// A custom working directory to use when starting the process.
        /// </summary>
        public string WorkingDirectory { get; set; }

        /// <summary>
        /// A callback to invoke when a standard output message is received from the process.
        /// </summary>
        public Action<string> OnStandardOutputReceived { get; set; }

        /// <summary>
        /// A callback to invoke when a standard error message is received from the process.
        /// </summary>
        public Action<string> OnStandardErrorReceived { get; set; }

        /// <summary>
        /// If we are redirecting standard output or error, how long we should wait for the
        /// output/error pipes to be fully flushed. Time is in milliseconds, -1 means block
        /// until the pipes are closed.
        /// </summary>
        public int OutputRedirectingTimeout { get; set; } = -1;

        /// <summary>
        /// What output from the process should be captured and be stored in the process result.
        /// </summary>
        public ProcessOutputCaptureMode CaptureOutputToProcessResult { get; set; } = ProcessOutputCaptureMode.None;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="fileName">The file name of the process to run.</param>
        /// <param name="arguments">The optional launch arguments to use when starting the process.</param>
        public AsyncProcessStartInfo(string fileName, string arguments = null)
        {
            FileName = fileName;
            Arguments = arguments;
        }

        /// <summary>
        /// Convert this object to a 'ProcessStartInfo' used for starting the process.
        /// </summary>
        /// <returns></returns>
        internal ProcessStartInfo ToProcessStartInfo()
        {
            var startInfo = new ProcessStartInfo(FileName, Arguments)
            {
                RedirectStandardOutput = IsCapturingStandardOutput(),
                RedirectStandardError = IsCapturingStandardError(),
                UseShellExecute = !IsCapturingAnyOutput(),
                CreateNoWindow = CreateNoWindow,
                WorkingDirectory = WorkingDirectory
            };
            return startInfo;
        }

        /// <summary>
        /// Is the process capturing any output (standard or error).
        /// </summary>
        /// <returns>True if we are capturing output from either standard, error or both.</returns>
        public bool IsCapturingAnyOutput()
        {
            return IsCapturingStandardOutput()
                   || IsCapturingStandardError();
        }

        /// <summary>
        /// Is the process capturing standard output
        /// </summary>
        /// <returns>True if we are forwarding standard output, or capturing it for the process result</returns>
        public bool IsCapturingStandardOutput()
        {
            return OnStandardOutputReceived != null
                || CaptureOutputToProcessResult.HasFlag(ProcessOutputCaptureMode.Output);
        }

        /// <summary>
        /// Is the process capturing standard error
        /// </summary>
        /// <returns>True if we are forwarding standard error, or capturing it for the process result</returns>
        public bool IsCapturingStandardError()
        {
            return OnStandardErrorReceived != null
               || CaptureOutputToProcessResult.HasFlag(ProcessOutputCaptureMode.Error);
        }
    }
}
