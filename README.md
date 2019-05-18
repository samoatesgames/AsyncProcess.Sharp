# AsyncProcess


## About

This library is a very simple wrapper around `System.Diagnostics.Process`, allowing for async/await operations.
Rather than creating and starting a `Process`, you instead create an `AsyncProcess` and await on the running of said process.

Currently the library does not support all operations possible with `System.Diagnostics.Process`, but it does allow for the following:

 * Running a process and async awaiting for the process to end.
 * Capturing of standard output streams.
 * Capturing of standard error streams.
 * Setting of the `CreateNoWindow` property.
 * Setting of launch arguments
 * Setting of the current working directory.
 * Cancelling of async processes.

All run methods for the `AsyncProcess` return an `AsyncProcessResult`.
This result contains the state of the result (i.e. did the process complete or did an error occur).
If the process completed successfully, the result will contain the exit code of the process.
If the process failed, an `Exception` property will be set to the exception which occurred whilst running the process.
 
When constructing an `AsyncProcess` object you may pass an option cancellation token which will be respected by the async process.
However, the process may not exit immediately. When a cancellation token is set the `Kill` method will be executed on the internal 
`System.Diagnostics.Process` and we will wait until the processes `HasExited` property returns true.
 
[![GitHub](https://img.shields.io/github/license/samoatesgames/AsyncProcess.Sharp.svg?style=flat-square)](https://github.com/samoatesgames/AsyncProcess.Sharp/blob/master/LICENSE)
[![Nuget](https://img.shields.io/nuget/dt/AsyncProcess.Sharp.svg?label=Nuget&style=flat-square)](https://www.nuget.org/packages/AsyncProcess.Sharp/)
 
## Example

### Running a process and using the exit code of the process
```csharp
using (var process = new AsyncProcess(new AsyncProcessStartInfo("ping.exe", "www.github.com")))
{
    var result = await process.Run();
    Console.WriteLine($"The pinging of github resulted in the exit code: {result.ExitCode}");
}
```

### Running a process and handling an error
```csharp
using (var process = new AsyncProcess(new AsyncProcessStartInfo("missing-file.exe")))
{
    var result = await process.Run();
    if (result.Exception != null)
    {
    	throw new Exception($"Failed to run 'missing-file.exe', result: ${result.CompletionState}",
							result.Exception);
    }
}
```

### Capturing output from a process using callbacks
```csharp
var startInfo = new AsyncProcessStartInfo("ping.exe", "www.github.com")
{
    OnStandardOutputReceived = message =>
    {
        Console.WriteLine($"Info: {message}");
    },
    OnStandardErrorReceived = message =>
    {
        Console.WriteLine($"Error: {message}");
    }
};

using (var process = new AsyncProcess(startInfo))
{
    var result = await process.Run();
    Console.WriteLine($"The pinging of github resulted in the exit code: {result.ExitCode}");
}
```

### Capturing output from a process storing it in the result
```csharp
var startInfo = new AsyncProcessStartInfo("ping.exe", "www.github.com")
{
    CaptureOutputToProcessResult = ProcessOutputCaptureMode.Both
};

using (var process = new AsyncProcess(startInfo))
{
    var result = await process.Run();
    Console.WriteLine($"The pinging of github resulted in the exit code: {result.ExitCode}");
	Console.WriteLine($"Standard Output: {result.StandardOutput}");	
	Console.WriteLine($"Standard Error: {result.StandardError}");	
}
```


## Helper Methods

There are a few helper methods which allow for single line usage of the AsyncProcess class

###### await AsyncProcess.Run("notepad.exe")
```csharp
/// <summary>
/// Create a new process, executing the specified file.
/// </summary>
/// <param name="fileName">The file to use when launching the process.</param>
/// <returns>An awaitable task containing the result of the process run.</returns>
public static async Task<AsyncProcessResult> Run(string fileName)
```

###### await AsyncProcess.Run("notepad.exe", @"C:\Windows\System32\drivers\etc\hosts")
```csharp
/// <summary>
/// Create a new process, executing the specified file with the provided launch arguments.
/// </summary>
/// <param name="fileName">The file to use when launching the process.</param>
/// <param name="arguments">The arguments to use when launching the process.</param>
/// <returns>An awaitable task containing the result of the process run.</returns>
public static async Task<AsyncProcessResult> Run(string fileName, string arguments)
```

###### await AsyncProcess.Run(new AsyncProcessStartInfo("notepad.exe"))
```csharp
/// <summary>
/// Create a new process, executing using the provided start info.
/// </summary>
/// <param name="startInfo">The information used when starting the new process.</param>
/// <returns>An awaitable task containing the result of the process run.</returns>
public static async Task<AsyncProcessResult> Run(AsyncProcessStartInfo startInfo)
```
