namespace Ecng.Interop
{
	using System;
	using System.Linq;
	using System.Management;
	using System.Collections.Generic;
	using System.Net.NetworkInformation;
	using System.Text.RegularExpressions;
	using System.Threading;
	using System.Threading.Tasks;

	using Ecng.Common;
	using Ecng.Localization;

#if !__STOCKSHARP__
	public
#endif
	static class HardwareInfo
	{
		public static async Task<string> GetIdAsync(CancellationToken cancellationToken = default)
		{
			string id;

			if (OperatingSystemEx.IsWindows())
				id = await GetIdWindows(cancellationToken);
			else
				id = await GetIdLinuxAsync(cancellationToken);

			if (id.IsEmpty())
				throw new InvalidOperationException("Cannot generate HDDID.".Translate());

			return id;
		}

		private static async Task<string> GetWMIIdAsync(string table, string field, CancellationToken cancellationToken)
		{
			using var mbs = new ManagementObjectSearcher("Select * From {0}".Put(table));
			using var list = await Task.Run(() => mbs.Get(), cancellationToken);
			return list.Cast<ManagementObject>().Select(o => (string)o[field]).FirstOrDefault(f => !f.IsEmptyOrWhiteSpace());
		}

		private static async Task<string> GetIdWindows(CancellationToken cancellationToken)
		{
			var cpuid = await GetWMIIdAsync("Win32_processor", "ProcessorID", cancellationToken);
			var mbId = await GetWMIIdAsync("Win32_BaseBoard", "SerialNumber", cancellationToken);

			if (
				mbId.EqualsIgnoreCase("none") ||
				mbId.EqualsIgnoreCase("n/a") ||
				mbId.EqualsIgnoreCase("invalid") ||
				mbId.EqualsIgnoreCase("To be filled by O.E.M.") ||
				mbId.EqualsIgnoreCase("Not Applicable")
			)
				mbId = null;

			var netId = await GetWMIIdAsync("Win32_NetworkAdapter", "MACAddress", cancellationToken);

			if (mbId.IsEmpty() && netId.IsEmpty())
				throw new InvalidOperationException("MotherBoard and Network are both is empty.".Translate());

			return cpuid + (mbId.IsEmpty() ? netId : mbId);
		}

		private static async Task<string> GetIdLinuxAsync(CancellationToken cancellationToken)
		{
			var macs = GetLinuxMacs();
			var volId = await GetLinuxVolumeIdAsync(cancellationToken);

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

		private static async Task<string> GetLinuxVolumeIdAsync(CancellationToken cancellationToken)
		{
			var errors = new List<string>();
			var result = new List<string>();

			var res = await IOHelper.ExecuteAsync("lsblk", "-r -o MOUNTPOINT,UUID", str =>
				{
					var m = _lsblkRegex.Match(str);
					if (m.Success)
						result.Add(m.Groups[1].Value);
				},
				errStr => errors.Add(errStr),
				info => info.EnvironmentVariables["PATH"] = "/bin:/sbin:/usr/bin:/usr/sbin", cancellationToken: cancellationToken);

			if (res != 0 || errors.Any())
				throw new InvalidOperationException("Unable to execute lsblk. Return code {0}.\n{1}".Translate().Put(res, errors.Join(Environment.NewLine)));

			//if (result.Count != 1)
			//	throw new InvalidOperationException($"invalid lsblk result. got {result.Count} values: {result.JoinComma()}");

			return result.FirstOrDefault()?.Remove("-").ToLowerInvariant();
		}
	}
}