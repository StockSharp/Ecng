namespace Ecng.Interop
{
	using System;
	using System.Linq;
	using System.Management;

	using Ecng.Common;

	public sealed class HardwareInfo
	{
		private HardwareInfo()
		{
			ProcessId = GetId("Win32_processor", "ProcessorID");
			MotherBoardId = GetId("Win32_BaseBoard", "SerialNumber");

			if (
					MotherBoardId.CompareIgnoreCase("none") ||
					MotherBoardId.CompareIgnoreCase("n/a") ||
					MotherBoardId.CompareIgnoreCase("invalid") ||
					MotherBoardId.CompareIgnoreCase("To be filled by O.E.M.") ||
					MotherBoardId.CompareIgnoreCase("Not Applicable")
				)
				MotherBoardId = null;

			NetworkId = GetId("Win32_NetworkAdapter", "MACAddress");
			//HddId = GetId("win32_logicaldisk", "VolumeSerialNumber");

			if (MotherBoardId.IsEmpty() && NetworkId.IsEmpty())
				throw new InvalidOperationException("MotherBoard and Network are both is empty.");

			Id = ProcessId + (MotherBoardId.IsEmpty() ? NetworkId : MotherBoardId);
		}

		private static string GetId(string table, string field)
		{
			using (var mbs = new ManagementObjectSearcher("Select * From {0}".Put(table)))
			{
				using (var list = mbs.Get())
				{
					return list.Cast<ManagementObject>().Select(o => (string)o[field]).FirstOrDefault(f => !f.IsEmptyOrWhiteSpace());
				}
			}
		}

		private static readonly Lazy<HardwareInfo> _instance = new Lazy<HardwareInfo>(() => new HardwareInfo());

		public static HardwareInfo Instance
		{
			get { return _instance.Value; }
		}

		public string ProcessId { get; private set; }
		public string MotherBoardId { get; private set; }
		public string NetworkId { get; private set; }


		public string Id { get; private set; }
	}
}