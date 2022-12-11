namespace Ecng.Interop;

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

public static class ProcessExtensions
{
	private const uint JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x2000;

	public static SafeProcessHandle SetKillChildsOnClose(this Process process)
	{
		if (process is null)
			throw new ArgumentNullException(nameof(process));

		// if (!IsProcessInJob(process.SafeHandle, new SafeProcessHandle(), out var isInJob))
		// 	throw new InvalidOperationException($"IsProcessInJob returned false. err={Marshal.GetLastWin32Error()}");

		// if(isInJob)
		// 	throw new InvalidOperationException("the process is already in a job");

		var jobHandle = new SafeProcessHandle(CreateJobObject(IntPtr.Zero, null), true);

		var info = new JOBOBJECT_BASIC_LIMIT_INFORMATION { LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE };
		var extendedInfo = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION { BasicLimitInformation = info };

		var length = Marshal.SizeOf(typeof(JOBOBJECT_EXTENDED_LIMIT_INFORMATION));
		var extendedInfoPtr = Marshal.AllocHGlobal(length);
		Marshal.StructureToPtr(extendedInfo, extendedInfoPtr, false);

		if (!SetInformationJobObject(jobHandle, JobObjectInfoType.ExtendedLimitInformation, extendedInfoPtr, (uint)length))
			throw new InvalidOperationException($"Unable to set information.  Error: {Marshal.GetLastWin32Error()}");

		if(!AssignProcessToJobObject(jobHandle, process.SafeHandle))
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