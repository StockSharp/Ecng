namespace Ecng.Interop;

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Ecng.Common;

using Windows.Win32;
using Windows.Win32.System.JobObjects;

using Microsoft.Win32.SafeHandles;

/// <summary>
/// Provides extension methods for the <see cref="Process"/> class to manage processor affinity, memory limits, and job behavior.
/// </summary>
public unsafe static class ProcessExtensions
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

	private static int GetCurrentProcessId() => Process.GetCurrentProcess().Id;

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
				if (!PInvoke.SetInformationJobObject(jobHandle, JOBOBJECTINFOCLASS.JobObjectExtendedLimitInformation, extendedInfoPtr.ToPointer(), (uint)length))
					throw new InvalidOperationException($"Unable to set information. Error: {Marshal.GetLastWin32Error()}");

				if (!PInvoke.AssignProcessToJobObject(jobHandle, process.SafeHandle))
					throw new InvalidOperationException("Unable to add the process to the job.");

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
				if (!PInvoke.SetInformationJobObject(jobHandle, JOBOBJECTINFOCLASS.JobObjectExtendedLimitInformation, extendedInfoPtr.ToPointer(), (uint)length))
					throw new InvalidOperationException($"Unable to set information. Error: {Marshal.GetLastWin32Error()}");
				if (!PInvoke.AssignProcessToJobObject(jobHandle, process.SafeHandle))
					throw new InvalidOperationException("Unable to add the process to the job.");
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
}
