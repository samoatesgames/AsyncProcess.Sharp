using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SamOatesGames.System;

namespace SamOatesGames.Tests
{
    [TestClass]
    public class AsyncProcessTests
    {
        [TestMethod]
        [TestCategory("Basic Execute Tests")]
        public async Task ExecutePingAsyncViaRunMethod()
        {
            var result = await AsyncProcess.Run("ping.exe");
            Assert.AreEqual(AsyncProcessCompletionState.Completed, result.CompletionState);
        }

        [TestMethod]
        [TestCategory("Basic Execute Tests")]
        public async Task ExecutePingAsyncViaClassAndCheckExitCode()
        {
            using (var process = new AsyncProcess(new ProcessStartInfo("ping.exe")))
            {
                var result = await process.Run();
                Assert.AreEqual(AsyncProcessCompletionState.Completed, result.CompletionState);
                Assert.AreEqual(1, result.ExitCode);
            }
        }

        [TestMethod]
        [TestCategory("Basic Execute Tests")]
        public async Task ExecuteMissingFileAsyncViaRunMethod()
        {
            var result = await AsyncProcess.Run($"{Path.GetRandomFileName()}.bloop");
            Assert.AreEqual(AsyncProcessCompletionState.ProcessToRunMissing, result.CompletionState);
            Assert.AreEqual(typeof(Win32Exception), result.Exception.GetType());
        }

        [TestMethod]
        [TestCategory("Task Cancelling")]
        public async Task CancelRunningProcess()
        {
            var cancellationSource = new CancellationTokenSource();
            var token = cancellationSource.Token;

            using (var process = new AsyncProcess(new ProcessStartInfo("cmd.exe"), token))
            {
                var runTask = process.Run();
                await Task.Delay(100, token);

                cancellationSource.Cancel();
                var result = await runTask;
                Assert.AreEqual(AsyncProcessCompletionState.Cancelled, result.CompletionState);
                Assert.AreEqual(int.MinValue, result.ExitCode);
            }
        }
    }
}
