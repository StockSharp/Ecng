namespace Ecng.Interop;

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;

using Newtonsoft.Json.Linq;

using Nito.AsyncEx;

using WmiLight;

/// <summary>
/// Provides methods to generate a hardware-based identifier.
/// </summary>
#if NET7_0_OR_GREATER
public static partial class HardwareInfo
#else
public static class HardwareInfo
#endif
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

#if NET7_0_OR_GREATER
	[GeneratedRegex(@"^\s*/\s+([\da-f-]+)\s*$", RegexOptions.IgnoreCase)]
	private static partial Regex LsblkRegex();
#else
	private static readonly Regex _lsblkRegex = new(@"^\s*/\s+([\da-f-]+)\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
	private static Regex LsblkRegex() => _lsblkRegex;
#endif

	private static async Task<string> GetIdLinuxAsync(CancellationToken cancellationToken)
	{
		var errors = new List<string>();
		var result = new List<string>();

		var res = await ProcessExtensions.ExecuteAsync("lsblk", "-r -o MOUNTPOINT,UUID", str =>
		{
			var m = LsblkRegex().Match(str);
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
		var errors = new List<string>();
		var output = new StringBuilder();

		// Execute system_profiler with JSON output format
		// -json flag is available since macOS High Sierra (10.13)
		var res = await ProcessExtensions.ExecuteAsync(
			"system_profiler",
			"SPHardwareDataType -json",
			str => output.AppendLine(str),
			errors.Add,
			cancellationToken: cancellationToken).NoWait();

		if (res != 0 || errors.Any())
		{
			throw new InvalidOperationException(
				$"Unable to execute system_profiler. Return code {res}.\n{errors.JoinNL()}");
		}

		try
		{
			// Parse the JSON output
			var json = JObject.Parse(output.ToString());

			// Navigate through the JSON structure
			// The path is: SPHardwareDataType[0].platform_UUID
			var hardwareData = json["SPHardwareDataType"] as JArray;
			if (hardwareData?.Count > 0)
			{
				var platformUuid = hardwareData[0]["platform_UUID"]?.ToString();

				if (!platformUuid.IsEmpty())
				{
					return platformUuid.Remove("-").ToLowerInvariant();
				}
			}

			// Fallback: try alternative JSON paths that might exist on different macOS versions
			var alternativePaths = new[]
			{
				"SPHardwareDataType[0]._items[0].platform_UUID",
				"SPHardwareDataType[0].Hardware.platform_UUID"
			};

			foreach (var path in alternativePaths)
			{
				var uuid = json.SelectToken(path)?.ToString();
				if (!uuid.IsEmpty())
				{
					return uuid.Remove("-").ToLowerInvariant();
				}
			}
		}
		catch (Exception ex)
		{
			throw new InvalidOperationException(
				$"Failed to parse system_profiler JSON output: {ex.Message}", ex);
		}

		throw new InvalidOperationException(
			"Hardware UUID not found in system_profiler output");
	}
}