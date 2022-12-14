namespace Ecng.Interop;

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Windows.Win32;
using Windows.Win32.System.JobObjects;

using Microsoft.Win32.SafeHandles;

public unsafe static class ProcessExtensions
{
	public static void SetProcessorAffinity(this Process process, long cpu)
	{
		if (process is null)
			throw new ArgumentNullException(nameof(process));

		var mask = (long)process.ProcessorAffinity;
		mask &= cpu;
		process.ProcessorAffinity = (IntPtr)mask;
	}

	public static SafeFileHandle LimitByMemory(this Process process, long limit)
	{
		if (process is null)
			throw new ArgumentNullException(nameof(process));

		if (limit <= 0)
			throw new ArgumentOutOfRangeException(nameof(limit));

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

		int? length = default;
		var extendedInfoPtr = extendedInfo.StructToPtr(ref length);

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

		var jobHandle = PInvoke.CreateJobObject(default, default(string));

		var extendedInfo = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION
		{
			BasicLimitInformation = new()
			{
				LimitFlags = JOB_OBJECT_LIMIT.JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE,
			}
		};

		int? length = default;
		var extendedInfoPtr = extendedInfo.StructToPtr(ref length);

		if (!PInvoke.SetInformationJobObject(jobHandle, JOBOBJECTINFOCLASS.JobObjectExtendedLimitInformation, extendedInfoPtr.ToPointer(), (uint)length))
			throw new InvalidOperationException($"Unable to set information.  Error: {Marshal.GetLastWin32Error()}");

		if (!PInvoke.AssignProcessToJobObject(jobHandle, process.SafeHandle))
			throw new InvalidOperationException("Unable to add the this process to the job");

		return jobHandle;
	}
}