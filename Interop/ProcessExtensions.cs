namespace Ecng.Interop;

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using Windows.Win32;
using Windows.Win32.System.JobObjects;

using Ecng.Common;

using Microsoft.Win32.SafeHandles;

using Nito.AsyncEx;

/// <summary>
/// Provides extension methods for the <see cref="Process"/> class to manage processor affinity, memory limits, and job behavior.
/// </summary>
public static class ProcessExtensions
{
#pragma warning disable CA1416 // Validate platform compatibility

	// Linux interop declarations (resolved only if called at runtime on Linux)
	private const int RLIMIT_AS = 9;              // Virtual memory (address space) limit
	private const int PR_SET_PDEATHSIG = 1;       // prctl option to set parent-death signal
	private const int SIGKILL = 9;

	[StructLayout(LayoutKind.Sequential)]
	private struct RLimit
	{
		public ulong rlim_cur; // soft limit
		public ulong rlim_max; // hard limit
	}

	[DllImport("libc", SetLastError = true)]
	private static extern int setrlimit(int resource, ref RLimit rlim);

	[DllImport("libc", SetLastError = true)]
	private static extern int prctl(int option, ulong arg2, ulong arg3, ulong arg4, ulong arg5);

	private static int GetCurrentProcessId() =>
#if NET6_0_OR_GREATER
		Environment.ProcessId
#else
		Process.GetCurrentProcess().Id
#endif
	;

	/// <summary>
	/// Sets the processor affinity for the process, allowing control over which CPU cores the process can execute on.
	/// </summary>
	/// <param name="process">The process whose processor affinity is to be set.</param>
	/// <param name="cpu">
	/// The bitmask representing the allowed CPU cores. The mask is applied to the process's current affinity.
	/// </param>
	/// <exception cref="ArgumentNullException">Thrown when the provided process is null.</exception>
	/// <exception cref="PlatformNotSupportedException">
	/// Thrown when the current operating system is not Windows or Linux.
	/// </exception>
	public static void SetProcessorAffinity(this Process process, long cpu)
	{
		if (process is null)
			throw new ArgumentNullException(nameof(process));

		if (OperatingSystemEx.IsWindows() || OperatingSystemEx.IsLinux())
		{
			var mask = (long)process.ProcessorAffinity;
			mask &= cpu;
			process.ProcessorAffinity = (IntPtr)mask;
		}
		else
			throw new PlatformNotSupportedException();
	}

	/// <summary>
	/// Limits the process memory usage. Windows: Job Object. Linux: setrlimit(RLIMIT_AS) for current process only.
	/// </summary>
	/// <param name="process">The process to limit memory usage for.</param>
	/// <param name="limit">The maximum allowed memory in bytes.</param>
	/// <returns>
	/// A <see cref="SafeFileHandle"/> representing the job object that manages the memory limit.
	/// </returns>
	/// <exception cref="ArgumentNullException">Thrown when the provided process is null.</exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when the provided limit is less than or equal to zero.</exception>
	/// <exception cref="InvalidOperationException">
	/// Thrown when setting the job object information or assigning the process to the job object fails.
	/// </exception>
	public static SafeFileHandle LimitByMemory(this Process process, long limit)
	{
		if (process is null)
			throw new ArgumentNullException(nameof(process));
		if (limit <= 0)
			throw new ArgumentOutOfRangeException(nameof(limit));

		if (OperatingSystemEx.IsWindows())
		{
			var jobHandle = PInvoke.CreateJobObject(default, default(string));

			var extendedInfo = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION
			{
				BasicLimitInformation = new()
				{
					LimitFlags = JOB_OBJECT_LIMIT.JOB_OBJECT_LIMIT_PROCESS_MEMORY |
							JOB_OBJECT_LIMIT.JOB_OBJECT_LIMIT_BREAKAWAY_OK |
							JOB_OBJECT_LIMIT.JOB_OBJECT_LIMIT_SILENT_BREAKAWAY_OK,
				},
				ProcessMemoryLimit = (UIntPtr)limit
			};

			var (extendedInfoPtr, length) = extendedInfo.StructToPtrEx();
			try
			{
				unsafe
				{
					if (!PInvoke.SetInformationJobObject(jobHandle, JOBOBJECTINFOCLASS.JobObjectExtendedLimitInformation, extendedInfoPtr.ToPointer(), (uint)length))
						throw new InvalidOperationException($"Unable to set information. Error: {Marshal.GetLastWin32Error()}");

					if (!PInvoke.AssignProcessToJobObject(jobHandle, process.SafeHandle))
						throw new InvalidOperationException("Unable to add the process to the job.");
				}

				return jobHandle;
			}
			finally
			{
				extendedInfoPtr.FreeHGlobal();
			}
		}
		else if (OperatingSystemEx.IsLinux())
		{
			if (process.Id != GetCurrentProcessId())
				throw new InvalidOperationException("On Linux memory limit can be applied only to current process.");

			var rl = new RLimit { rlim_cur = (ulong)limit, rlim_max = (ulong)limit };
			if (setrlimit(RLIMIT_AS, ref rl) != 0)
				throw new InvalidOperationException($"setrlimit failed. errno={Marshal.GetLastWin32Error()}");

			return new SafeFileHandle(IntPtr.Zero, ownsHandle: false);
		}

		throw new PlatformNotSupportedException();
	}

	/// <summary>
	/// Configures the process to automatically kill its child processes when the process is closed by
	/// assigning it to a Windows Job Object with the kill-on-job-close flag.
	/// </summary>
	/// <param name="process">The process whose child processes should be terminated on close.</param>
	/// <returns>
	/// A <see cref="SafeFileHandle"/> representing the job object that enforces the kill-on-job-close behavior.
	/// </returns>
	/// <exception cref="ArgumentNullException">Thrown when the provided process is null.</exception>
	/// <exception cref="InvalidOperationException">
	/// Thrown when setting the job object information or assigning the process to the job object fails.
	/// </exception>
	public static SafeFileHandle SetKillChildsOnClose(this Process process)
	{
		if (process is null)
			throw new ArgumentNullException(nameof(process));

		if (OperatingSystemEx.IsWindows())
		{
			var jobHandle = PInvoke.CreateJobObject(default, default(string));
			var extendedInfo = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION
			{
				BasicLimitInformation = new() { LimitFlags = JOB_OBJECT_LIMIT.JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE }
			};

			var (extendedInfoPtr, length) = extendedInfo.StructToPtrEx();
			try
			{
				unsafe
				{
					if (!PInvoke.SetInformationJobObject(jobHandle, JOBOBJECTINFOCLASS.JobObjectExtendedLimitInformation, extendedInfoPtr.ToPointer(), (uint)length))
						throw new InvalidOperationException($"Unable to set information. Error: {Marshal.GetLastWin32Error()}");
					if (!PInvoke.AssignProcessToJobObject(jobHandle, process.SafeHandle))
						throw new InvalidOperationException("Unable to add the process to the job.");
				}

				return jobHandle;
			}
			finally
			{
				extendedInfoPtr.FreeHGlobal();
			}
		}
		else if (OperatingSystemEx.IsLinux())
		{
			if (process.Id != GetCurrentProcessId())
				throw new InvalidOperationException("On Linux child-kill behavior can be set only for current process.");

			if (prctl(PR_SET_PDEATHSIG, SIGKILL, 0, 0, 0) != 0)
				throw new InvalidOperationException($"prctl(PR_SET_PDEATHSIG) failed. errno={Marshal.GetLastWin32Error()}");

			return new SafeFileHandle(IntPtr.Zero, ownsHandle: false);
		}

		throw new PlatformNotSupportedException();
	}

	/// <summary>
	/// Executes a process with specified arguments and output handlers.
	/// </summary>
	/// <param name="fileName">The process file name.</param>
	/// <param name="arg">Arguments for the process.</param>
	/// <param name="output">Action to handle standard output.</param>
	/// <param name="error">Action to handle standard error.</param>
	/// <param name="infoHandler">Optional action to modify process start info.</param>
	/// <param name="waitForExit">TimeSpan to wait for process exit.</param>
	/// <param name="stdInput">Standard input to write to the process.</param>
	/// <param name="priority">Optional process priority.</param>
	/// <returns>The process exit code.</returns>
	public static int Execute(string fileName, string arg, Action<string> output, Action<string> error, Action<ProcessStartInfo> infoHandler = null, TimeSpan waitForExit = default, string stdInput = null, ProcessPriorityClass? priority = null)
	{
		var source = new CancellationTokenSource();

		if (waitForExit != default)
			source.CancelAfter(waitForExit);

		return AsyncContext.Run(() => ExecuteAsync(fileName, arg, output, error, infoHandler, stdInput, priority, source.Token));
	}

	/// <summary>
	/// Asynchronously executes a process with specified arguments and output handlers.
	/// </summary>
	/// <param name="fileName">The file name to execute.</param>
	/// <param name="arg">Arguments for the process.</param>
	/// <param name="output">Action to handle standard output.</param>
	/// <param name="error">Action to handle standard error.</param>
	/// <param name="infoHandler">Optional action to modify process start info.</param>
	/// <param name="stdInput">Standard input to send to the process.</param>
	/// <param name="priority">Optional process priority.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>A task that represents the asynchronous operation, containing the process exit code.</returns>
	public static async Task<int> ExecuteAsync(string fileName, string arg, Action<string> output, Action<string> error, Action<ProcessStartInfo> infoHandler = null, string stdInput = null, ProcessPriorityClass? priority = null, CancellationToken cancellationToken = default)
	{
		if (output is null)
			throw new ArgumentNullException(nameof(output));

		if (error is null)
			throw new ArgumentNullException(nameof(error));

		var input = !stdInput.IsEmpty();

		var procInfo = new ProcessStartInfo(fileName, arg)
		{
			UseShellExecute = false,
			RedirectStandardError = true,
			RedirectStandardOutput = true,
			RedirectStandardInput = input,
			CreateNoWindow = true,
			WindowStyle = ProcessWindowStyle.Hidden
		};

		infoHandler?.Invoke(procInfo);

		using var process = new Process
		{
			EnableRaisingEvents = true,
			StartInfo = procInfo
		};

		process.Start();

		// Set process priority if provided.
		// https://stackoverflow.com/a/1010377/8029915
		if (priority is not null)
			process.PriorityClass = priority.Value;

		var locker = new Lock();

		if (input)
		{
			process.StandardInput.WriteLine(stdInput);
			process.StandardInput.Close();
		}

		async Task ReadProcessOutput(TextReader reader, Action<string> action)
		{
			do
			{
				var str = await reader.ReadLineAsync(cancellationToken);
				if (str is null)
					break;

				if (!str.IsEmptyOrWhiteSpace())
				{
					using (locker.EnterScope())
						action(str);
				}

				cancellationToken.ThrowIfCancellationRequested();
			}
			while (true);
		}

		var task1 = ReadProcessOutput(process.StandardOutput, output);
		var task2 = ReadProcessOutput(process.StandardError, error);

		await task1;
		await task2;

		await process.WaitForExitAsync(cancellationToken);

		return process.ExitCode;
	}

	/// <summary>
	/// Opens the specified URL or file path using the default system launcher.
	/// </summary>
	/// <param name="url">The URL or file path to open.</param>
	/// <param name="raiseError">Determines if an exception should be raised if opening fails.</param>
	/// <returns>True if the operation is successful; otherwise, false.</returns>
	public static bool OpenLink(this string url, bool raiseError)
	{
		if (url.IsEmpty())
			throw new ArgumentNullException(nameof(url));

		// https://stackoverflow.com/a/21836079

		try
		{
			// https://github.com/dotnet/wpf/issues/2566

			var procInfo = new ProcessStartInfo(url)
			{
				UseShellExecute = true,
			};

			Process.Start(procInfo);
			return true;
		}
		catch (Win32Exception)
		{
			try
			{
				var launcher = url.StartsWithIgnoreCase("http") ? "IExplore.exe" : "explorer.exe";
				Process.Start(launcher, url);
				return true;
			}
			catch
			{
				if (raiseError)
					throw;

				return false;
			}
		}
	}
}
