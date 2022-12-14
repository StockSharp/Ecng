namespace Ecng.Interop;

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

public static class ProcessExtensions
{
	private static class JobInformationLimitFlags
	{
		public const uint JOB_OBJECT_LIMIT_ACTIVE_PROCESS = 0x00000008;
		public const uint JOB_OBJECT_LIMIT_AFFINITY = 0x00000010;
		public const uint JOB_OBJECT_LIMIT_BREAKAWAY_OK = 0x00000800;
		public const uint JOB_OBJECT_LIMIT_DIE_ON_UNHANDLED_EXCEPTION = 0x00000400;
		public const uint JOB_OBJECT_LIMIT_JOB_MEMORY = 0x00000200;
		public const uint JOB_OBJECT_LIMIT_JOB_TIME = 0x00000004;
		public const uint JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x00002000;
		public const uint JOB_OBJECT_LIMIT_PRESERVE_JOB_TIME = 0x00000040;
		public const uint JOB_OBJECT_LIMIT_PRIORITY_CLASS = 0x00000020;
		public const uint JOB_OBJECT_LIMIT_PROCESS_MEMORY = 0x00000100;
		public const uint JOB_OBJECT_LIMIT_PROCESS_TIME = 0x00000002;
		public const uint JOB_OBJECT_LIMIT_SCHEDULING_CLASS = 0x00000080;
		public const uint JOB_OBJECT_LIMIT_SILENT_BREAKAWAY_OK = 0x00001000;
		public const uint JOB_OBJECT_LIMIT_SUBSET_AFFINITY = 0x00004000;
		public const uint JOB_OBJECT_LIMIT_WORKINGSET = 0x00000001;
	}

	public static void LimitByCPU(this Process process, long cpu)
	{
		if (process is null)
			throw new ArgumentNullException(nameof(process));

#pragma warning disable CA1416 // Validate platform compatibility
		var mask = (long)process.ProcessorAffinity;
		mask &= cpu;
		process.ProcessorAffinity = (IntPtr)mask;
#pragma warning restore CA1416 // Validate platform compatibility
	}

	public static SafeProcessHandle LimitByMemory(this Process process, long limit)
	{
		if (process is null)
			throw new ArgumentNullException(nameof(process));

		if (limit <= 0)
			throw new ArgumentOutOfRangeException(nameof(limit));

		var jobHandle = new SafeProcessHandle(CreateJobObject(IntPtr.Zero, null), true);

		var extendedInfo = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION
		{
			BasicLimitInformation = new()
			{
				LimitFlags =
						JobInformationLimitFlags.JOB_OBJECT_LIMIT_PROCESS_MEMORY |
						JobInformationLimitFlags.JOB_OBJECT_LIMIT_BREAKAWAY_OK |
						JobInformationLimitFlags.JOB_OBJECT_LIMIT_SILENT_BREAKAWAY_OK,
			},

			ProcessMemoryLimit = (UIntPtr)limit
		};

		int? length = default;
		var extendedInfoPtr = extendedInfo.StructToPtr(ref length);

		if (!SetInformationJobObject(jobHandle, JobObjectInfoType.ExtendedLimitInformation, extendedInfoPtr, (uint)length))
			throw new InvalidOperationException($"Unable to set information.  Error: {Marshal.GetLastWin32Error()}");

		if (!AssignProcessToJobObject(jobHandle, process.SafeHandle))
			throw new InvalidOperationException("Unable to add the this process to the job");

		return jobHandle;
	}

	public static SafeProcessHandle SetKillChildsOnClose(this Process process)
	{
		if (process is null)
			throw new ArgumentNullException(nameof(process));

		// if (!IsProcessInJob(process.SafeHandle, new SafeProcessHandle(), out var isInJob))
		// 	throw new InvalidOperationException($"IsProcessInJob returned false. err={Marshal.GetLastWin32Error()}");

		// if(isInJob)
		// 	throw new InvalidOperationException("the process is already in a job");

		var jobHandle = new SafeProcessHandle(CreateJobObject(IntPtr.Zero, null), true);

		var extendedInfo = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION
		{
			BasicLimitInformation = new()
			{
				LimitFlags = JobInformationLimitFlags.JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE,
			}
		};

		int? length = default;
		var extendedInfoPtr = extendedInfo.StructToPtr(ref length);

		if (!SetInformationJobObject(jobHandle, JobObjectInfoType.ExtendedLimitInformation, extendedInfoPtr, (uint)length))
			throw new InvalidOperationException($"Unable to set information.  Error: {Marshal.GetLastWin32Error()}");

		if (!AssignProcessToJobObject(jobHandle, process.SafeHandle))
			throw new InvalidOperationException("Unable to add the this process to the job");

		return jobHandle;
	}

	#region Win32

	[DllImport("kernel32", CharSet = CharSet.Unicode)]
	static extern IntPtr CreateJobObject(IntPtr a, string lpName);

	[DllImport("kernel32", SetLastError = true)]
	static extern bool SetInformationJobObject(SafeProcessHandle proc, JobObjectInfoType infoType, IntPtr lpJobObjectInfo, uint cbJobObjectInfoLength);

	[DllImport("kernel32", SetLastError = true)]
	static extern bool AssignProcessToJobObject(SafeProcessHandle job, SafeProcessHandle process);

	[DllImport("kernel32", SetLastError = true)]
	static extern bool IsProcessInJob(SafeProcessHandle proc, SafeProcessHandle job, out bool result);

	// ReSharper disable MemberCanBePrivate.Local
	// ReSharper disable FieldCanBeMadeReadOnly.Local

	[StructLayout(LayoutKind.Sequential)]
	private struct IO_COUNTERS
	{
		public ulong ReadOperationCount;
		public ulong WriteOperationCount;
		public ulong OtherOperationCount;
		public ulong ReadTransferCount;
		public ulong WriteTransferCount;
		public ulong OtherTransferCount;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct JOBOBJECT_BASIC_LIMIT_INFORMATION
	{
		public long PerProcessUserTimeLimit;
		public long PerJobUserTimeLimit;
		public uint LimitFlags;
		public UIntPtr MinimumWorkingSetSize;
		public UIntPtr MaximumWorkingSetSize;
		public uint ActiveProcessLimit;
		public UIntPtr Affinity;
		public uint PriorityClass;
		public uint SchedulingClass;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct SECURITY_ATTRIBUTES
	{
		public uint nLength;
		public IntPtr lpSecurityDescriptor;
		public int bInheritHandle;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
	{
		public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
		public IO_COUNTERS IoInfo;
		public UIntPtr ProcessMemoryLimit;
		public UIntPtr JobMemoryLimit;
		public UIntPtr PeakProcessMemoryUsed;
		public UIntPtr PeakJobMemoryUsed;
	}

	private enum JobObjectInfoType
	{
		ExtendedLimitInformation = 9,
	}

	#endregion
}