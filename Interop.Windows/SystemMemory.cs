namespace Ecng.Interop
{
	using System;
	using System.Diagnostics;

	using Microsoft.VisualBasic.Devices;

	public class SystemMemory
	{
		private SystemMemory()
		{
			try
			{
				var info = new ComputerInfo();

				TotalPhysicalMemory = (long)info.TotalPhysicalMemory;
				TotalVirtualMemory = (long)info.TotalVirtualMemory;
				AvailableVirtualMemory = (long)info.AvailableVirtualMemory;
				AvailablePhysicalMemory = (long)info.AvailablePhysicalMemory;
			}
			catch (Exception e)
			{
				Trace.WriteLine(e);
			}
		}

		/// <summary>
		/// Gets the total amount of physical memory for the computer.
		/// </summary>
		public long TotalPhysicalMemory { get; }

		/// <summary>
		/// Gets the total amount of virtual address space available for the computer.
		/// </summary>
		public long TotalVirtualMemory { get; }

		/// <summary>
		/// Gets the total amount of the computer's free virtual address space.
		/// </summary>
		public long AvailableVirtualMemory { get; }

		/// <summary>
		/// Gets the total amount of free physical memory for the computer.
		/// </summary>
		public long AvailablePhysicalMemory { get; }

		private static readonly Lazy<SystemMemory> _instance = new Lazy<SystemMemory>(() => new SystemMemory());

		public static SystemMemory Instance = _instance.Value;
	}
}