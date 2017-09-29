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

				TotalPhysicalMemory = info.TotalPhysicalMemory;
				TotalVirtualMemory = info.TotalVirtualMemory;
				AvailableVirtualMemory = info.AvailableVirtualMemory;
				AvailablePhysicalMemory = info.AvailablePhysicalMemory;
			}
			catch (Exception e)
			{
				Trace.WriteLine(e);
			}
		}

		/// <summary>
		/// Gets the total amount of physical memory for the computer.
		/// </summary>
		public ulong TotalPhysicalMemory { get; }

		/// <summary>
		/// Gets the total amount of virtual address space available for the computer.
		/// </summary>
		public ulong TotalVirtualMemory { get; }

		/// <summary>
		/// Gets the total amount of the computer's free virtual address space.
		/// </summary>
		public ulong AvailableVirtualMemory { get; }

		/// <summary>
		/// Gets the total amount of free physical memory for the computer.
		/// </summary>
		public ulong AvailablePhysicalMemory { get; }

		private static readonly Lazy<SystemMemory> _instance = new Lazy<SystemMemory>(() => new SystemMemory());

		public static SystemMemory Instance = _instance.Value;
	}
}