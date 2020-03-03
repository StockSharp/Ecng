namespace Ecng.Interop
{
	using System;
	using System.Linq;
	using System.Management;
	using System.Collections.Generic;
	using System.Net.NetworkInformation;
	using System.Text.RegularExpressions;
#if NETCOREAPP
	using System.Runtime.InteropServices;
#endif

	using Ecng.Common;

#if !__STOCKSHARP__
	public
#endif
	sealed class HardwareInfo
	{
		private static readonly Lazy<HardwareInfo> _instance = new Lazy<HardwareInfo>(() => new HardwareInfo());

		public static HardwareInfo Instance => _instance.Value;

		public string Id { get; private set; }

		private HardwareInfo()
		{
#if NETFRAMEWORK
			InitIdWindows();
#else
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				InitIdWindows();
			else
				InitIdLinux();
#endif
		}

		private static string GetWMIId(string table, string field)
		{
			using (var mbs = new ManagementObjectSearcher("Select * From {0}".Put(table)))
			{
				using (var list = mbs.Get())
				{
					return list.Cast<ManagementObject>().Select(o => (string) o[field]).FirstOrDefault(f => !f.IsEmptyOrWhiteSpace());
				}
			}
		}

		private void InitIdWindows()
		{
			var cpuid = GetWMIId("Win32_processor", "ProcessorID");
			var mbId = GetWMIId("Win32_BaseBoard", "SerialNumber");

			if (
				mbId.CompareIgnoreCase("none") ||
				mbId.CompareIgnoreCase("n/a") ||
				mbId.CompareIgnoreCase("invalid") ||
				mbId.CompareIgnoreCase("To be filled by O.E.M.") ||
				mbId.CompareIgnoreCase("Not Applicable")
			)
				mbId = null;

			var netId = GetWMIId("Win32_NetworkAdapter", "MACAddress");

			if (mbId.IsEmpty() && netId.IsEmpty())
				throw new InvalidOperationException("MotherBoard and Network are both is empty.");

			Id = cpuid + (mbId.IsEmpty() ? netId : mbId);
		}

		private void InitIdLinux()
		{
			var macs = GetLinuxMacs();
			var volId = GetLinuxVolumeId();

			Id = string.Join("", macs) + volId;
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

		private static readonly Regex _lsblkRegex = new Regex(@"^\s*/\s+([\da-f-]+)\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		private static string GetLinuxVolumeId()
		{
			var errors = new List<string>();
			var result = new List<string>();

			var res = IOHelper.Execute("lsblk", "-o MOUNTPOINT,UUID", str =>
				{
					var m = _lsblkRegex.Match(str);
					if (m.Success)
						result.Add(m.Groups[1].Value);
				},
				errStr => errors.Add(errStr),
				info => info.EnvironmentVariables["PATH"] = "/bin:/sbin:/usr/bin:/usr/sbin");

			if (res != 0 || errors.Any())
				throw new InvalidOperationException($"unable to execute lsblk. return code {res}.\n{string.Join("\n", errors)}");

			if (result.Count != 1)
				throw new InvalidOperationException($"invalid lsblk result. got {result.Count} values: {string.Join(",", result)}");

			return result[0].Replace("-", "").ToLowerInvariant();
		}
	}
}