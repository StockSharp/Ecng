namespace Ecng.Interop;

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;

using Ecng.Common;

using Windows.Win32;
using Windows.Win32.System.JobObjects;

using Microsoft.Win32.SafeHandles;

public unsafe static class ProcessExtensions
{
#pragma warning disable CA1416 // Validate platform compatibility

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

	public static SafeFileHandle LimitByMemory(this Process process, long limit)
	{
		if (process is null)
			throw new ArgumentNullException(nameof(process));

		if (limit <= 0)
			throw new ArgumentOutOfRangeException(nameof(limit));

		if (!OperatingSystemEx.IsWindows())
		{
			// TODO implement for Linux
			throw new PlatformNotSupportedException();
		}

		var jobHandle = PInvoke.CreateJobObject(default, default(string));

		var extendedInfo = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION
		{
			BasicLimitInformation = new()
			{
				LimitFlags =
						JOB_OBJECT_LIMIT.JOB_OBJECT_LIMIT_PROCESS_MEMORY |
						JOB_OBJECT_LIMIT.JOB_OBJECT_LIMIT_BREAKAWAY_OK |
						JOB_OBJECT_LIMIT.JOB_OBJECT_LIMIT_SILENT_BREAKAWAY_OK,
			},

			ProcessMemoryLimit = (UIntPtr)limit
		};

		var (extendedInfoPtr, length) = extendedInfo.StructToPtrEx();

		if (!PInvoke.SetInformationJobObject(jobHandle, JOBOBJECTINFOCLASS.JobObjectExtendedLimitInformation, extendedInfoPtr.ToPointer(), (uint)length))
			throw new InvalidOperationException($"Unable to set information.  Error: {Marshal.GetLastWin32Error()}");

		if (!PInvoke.AssignProcessToJobObject(jobHandle, process.SafeHandle))
			throw new InvalidOperationException("Unable to add the this process to the job");

		return jobHandle;
	}

	public static SafeFileHandle SetKillChildsOnClose(this Process process)
	{
		if (process is null)
			throw new ArgumentNullException(nameof(process));

		if (!OperatingSystemEx.IsWindows())
			throw new PlatformNotSupportedException();

		var jobHandle = PInvoke.CreateJobObject(default, default(string));

		var extendedInfo = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION
		{
			BasicLimitInformation = new()
			{
				LimitFlags = JOB_OBJECT_LIMIT.JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE,
			}
		};

		var (extendedInfoPtr, length) = extendedInfo.StructToPtrEx();

		if (!PInvoke.SetInformationJobObject(jobHandle, JOBOBJECTINFOCLASS.JobObjectExtendedLimitInformation, extendedInfoPtr.ToPointer(), (uint)length))
			throw new InvalidOperationException($"Unable to set information.  Error: {Marshal.GetLastWin32Error()}");

		if (!PInvoke.AssignProcessToJobObject(jobHandle, process.SafeHandle))
			throw new InvalidOperationException("Unable to add the this process to the job");

		return jobHandle;
	}

	// for non .NET Core
	public static void LoadWmiLightNative(string currDir)
	{
		var architecture = Constants.GetArchitecture();
		var os = Constants.GetOS();
		var path = Path.Combine(Path.GetDirectoryName(currDir), Constants.Runtimes, $"{os}-{architecture}", Constants.Native, "WmiLight.Native.dll");
		PInvoke.LoadLibrary(path);
	}
}