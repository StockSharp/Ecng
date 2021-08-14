namespace Ecng.Interop
{
	using System;
	using System.Linq;
	using System.Management;
	using System.Collections.Generic;
	using System.Net.NetworkInformation;
	using System.Text.RegularExpressions;

	using Ecng.Common;
	using Ecng.Localization;

#if !__STOCKSHARP__
	public
#endif
	sealed class HardwareInfo
	{
		private static readonly Lazy<HardwareInfo> _instance = new(() => new HardwareInfo());

		public static HardwareInfo Instance => _instance.Value;

		public string Id { get; }

		private HardwareInfo()
		{
			string id;

			if (OperatingSystemEx.IsWindows())
				id = GetIdWindows();
			else
				id = GetIdLinux();

			if (id.IsEmpty())
				throw new InvalidOperationException("Cannot generate HDDID.".Translate());

			Id = id;
		}

		private static string GetWMIId(string table, string field)
		{
			using (var mbs = new ManagementObjectSearcher("Select * From {0}".Put(table)))
			using (var list = mbs.Get())
				return list.Cast<ManagementObject>().Select(o => (string) o[field]).FirstOrDefault(f => !f.IsEmptyOrWhiteSpace());
		}

		private static string GetIdWindows()
		{
			var cpuid = GetWMIId("Win32_processor", "ProcessorID");
			var mbId = GetWMIId("Win32_BaseBoard", "SerialNumber");

			if (
				mbId.EqualsIgnoreCase("none") ||
				mbId.EqualsIgnoreCase("n/a") ||
				mbId.EqualsIgnoreCase("invalid") ||
				mbId.EqualsIgnoreCase("To be filled by O.E.M.") ||
				mbId.EqualsIgnoreCase("Not Applicable")
			)
				mbId = null;

			var netId = GetWMIId("Win32_NetworkAdapter", "MACAddress");

			if (mbId.IsEmpty() && netId.IsEmpty())
				throw new InvalidOperationException("MotherBoard and Network are both is empty.".Translate());

			return cpuid + (mbId.IsEmpty() ? netId : mbId);
		}

		private static string GetIdLinux()
		{
			var macs = GetLinuxMacs();
			var volId = GetLinuxVolumeId();

			return macs.Join(string.Empty) + volId;
		}

		private static List<string> GetLinuxMacs()
		{
			var result = new HashSet<string>();
			var ifaces = NetworkInterface
				.GetAllNetworkInterfaces()
				.Where(i => i.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
							i.NetworkInterfaceType != NetworkInterfaceType.Tunnel &&
							i.OperationalStatus == OperationalStatus.Up);

			foreach (var iface in ifaces)
				result.Add(iface.GetPhysicalAddress().ToString().ToLowerInvariant());

			var list = result
				.OrderBy(s => s)
				.Where(s => s != "ffffffffffff" && s != "000000000000")
				.ToList();

			return list;
		}

		private static readonly Regex _lsblkRegex = new(@"^\s*/\s+([\da-f-]+)\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		private static string GetLinuxVolumeId()
		{
			var errors = new List<string>();
			var result = new List<string>();

			var res = IOHelper.Execute("lsblk", "-r -o MOUNTPOINT,UUID", str =>
				{
					var m = _lsblkRegex.Match(str);
					if (m.Success)
						result.Add(m.Groups[1].Value);
				},
				errStr => errors.Add(errStr),
				info => info.EnvironmentVariables["PATH"] = "/bin:/sbin:/usr/bin:/usr/sbin");

			if (res != 0 || errors.Any())
				throw new InvalidOperationException("Unable to execute lsblk. Return code {0}.\n{1}".Translate().Put(res, errors.Join(Environment.NewLine)));

			//if (result.Count != 1)
			//	throw new InvalidOperationException($"invalid lsblk result. got {result.Count} values: {result.JoinComma()}");

			return result.FirstOrDefault()?.Remove("-").ToLowerInvariant();
		}
	}
}