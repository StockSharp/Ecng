namespace Ecng.Interop;

using System;
using System.Linq;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;

using Nito.AsyncEx;

using WmiLight;

/// <summary>
/// Provides methods to generate a hardware-based identifier.
/// </summary>
public static class HardwareInfo
{
	/// <summary>
	/// Gets the hardware identifier.
	/// </summary>
	/// <returns>A string representing the hardware identifier.</returns>
	public static string GetId()
		=> AsyncContext.Run(() => GetIdAsync());

	/// <summary>
	/// Asynchronously gets the hardware identifier.
	/// </summary>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains a string representing the hardware identifier.</returns>
	public static async Task<string> GetIdAsync(CancellationToken cancellationToken = default)
	{
		string id;

		if (OperatingSystemEx.IsWindows())
			id = await GetIdWindows(cancellationToken);
		else if (OperatingSystemEx.IsMacOS())
			id = await GetIdMacOSAsync(cancellationToken);
		else if (OperatingSystemEx.IsLinux())
			id = await GetIdLinuxAsync(cancellationToken);
		else
			throw new PlatformNotSupportedException($"Platform {Environment.OSVersion.Platform} is not supported.");

		if (id.IsEmpty())
			throw new InvalidOperationException("Cannot generate HDDID.");

		return id;
	}

	private static async Task<string> GetWMIIdAsync(string table, string field, CancellationToken cancellationToken)
	{
		using var con = new WmiConnection();
		var query = con.CreateQuery($"Select * From {table}");
		var list = await Task.Run(() => query.ToArray(), cancellationToken).NoWait();
		return list.Select(o => (string)o[field]).FirstOrDefault(f => !f.IsEmptyOrWhiteSpace());
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
			throw new InvalidOperationException("MotherBoard and Network are both is empty.");

		return cpuid + (mbId.IsEmpty() ? netId : mbId);
	}

	private static async Task<string> GetIdLinuxAsync(CancellationToken cancellationToken)
	{
		var macs = GetNetworkMacs();
		var volId = await GetLinuxVolumeIdAsync(cancellationToken);

		return macs + volId;
	}

	private static string GetNetworkMacs()
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

		return list.Join(string.Empty);
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
		errors.Add,
		info => info.EnvironmentVariables["PATH"] = "/bin:/sbin:/usr/bin:/usr/sbin", cancellationToken: cancellationToken).NoWait();

		if (res != 0 || errors.Any())
			throw new InvalidOperationException($"Unable to execute lsblk. Return code {res}.\n{errors.JoinNL()}");

		//if (result.Count != 1)
		//	throw new InvalidOperationException($"invalid lsblk result. got {result.Count} values: {result.JoinComma()}");

		return result.FirstOrDefault()?.Remove("-").ToLowerInvariant();
	}

	private static async Task<string> GetIdMacOSAsync(CancellationToken cancellationToken)
	{
		var macs = GetNetworkMacs();
		var volId = await GetMacOSVolumeIdAsync(cancellationToken);

		return macs + volId;
	}

	private static async Task<string> GetMacOSVolumeIdAsync(CancellationToken cancellationToken)
	{
		var errors = new List<string>();
		var result = new List<string>();

		var res = await IOHelper.ExecuteAsync("diskutil", "info /", str =>
		{
			if (!str.ContainsIgnoreCase("Volume UUID:"))
				return;

			var uuid = str.Split(':')[1].Trim();

			if (!uuid.IsEmpty())
				result.Add(uuid);
		},
		errors.Add,
		cancellationToken: cancellationToken).NoWait();

		if (res != 0 || errors.Any())
			throw new InvalidOperationException($"Unable to execute diskutil. Return code {res}.\n{errors.JoinNL()}");

		return result.FirstOrDefault()?.Remove("-").ToLowerInvariant();
	}
}