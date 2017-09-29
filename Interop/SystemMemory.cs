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

		public ulong TotalPhysicalMemory { get; }
		public ulong TotalVirtualMemory { get; }
		public ulong AvailableVirtualMemory { get; }
		public ulong AvailablePhysicalMemory { get; }

		private static readonly Lazy<SystemMemory> _instance = new Lazy<SystemMemory>(() => new SystemMemory());

		public static SystemMemory Instance = _instance.Value;
	}
}