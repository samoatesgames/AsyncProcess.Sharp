using System;
using System.Diagnostics;

namespace SamOatesGames.System
{
    public class AsyncProcessStartInfo
    {
        public string FileName { get; }
        public string Arguments { get; }

        public bool CreateNoWindow { get; set; }
        public string WorkingDirectory { get; set; }

        public Action<string> OnStandardOutputReceived { get; set; }
        public Action<string> OnStandardErrorReceived { get; set; }

        public AsyncProcessStartInfo(string fileName, string arguments = null)
        {
            FileName = fileName;
            Arguments = arguments;
        }

        public ProcessStartInfo ToProcessStartInfo()
        {
            var startInfo = new ProcessStartInfo(FileName, Arguments)
            {
                RedirectStandardOutput = IsCapturingOutput(),
                RedirectStandardError = IsCapturingOutput(),
                UseShellExecute = !IsCapturingOutput(),
                CreateNoWindow = CreateNoWindow,
                WorkingDirectory = WorkingDirectory
            };
            return startInfo;
        }

        public bool IsCapturingOutput()
        {
            return OnStandardOutputReceived != null
                   || OnStandardErrorReceived != null;
        }
    }
}
