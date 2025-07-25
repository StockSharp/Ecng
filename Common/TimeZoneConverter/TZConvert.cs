﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Ecng.Common.TimeZoneConverter;

/// <summary>
/// Converts time zone identifiers from various sources.
/// </summary>
public static class TZConvert
{
	private static readonly Dictionary<string, string> _ianaMap = new(StringComparer.OrdinalIgnoreCase);
	private static readonly Dictionary<string, string> _windowsMap = new(StringComparer.OrdinalIgnoreCase);
	private static readonly Dictionary<string, string> _railsMap = new(StringComparer.OrdinalIgnoreCase);
	private static readonly Dictionary<string, IList<string>> _inverseRailsMap = new(StringComparer.OrdinalIgnoreCase);

	private static readonly bool _isWindows = OperatingSystemEx.IsWindows();
	private static readonly Dictionary<string, TimeZoneInfo> _systemTimeZones;

	static TZConvert()
	{
		DataLoader.Populate(_ianaMap, _windowsMap, _railsMap, _inverseRailsMap);

		KnownIanaTimeZoneNames = new HashSet<string>(_ianaMap.Select(x => x.Key));
		KnownWindowsTimeZoneIds = new HashSet<string>(_windowsMap.Keys.Select(x => x.Split('|')[1]).Distinct());
		KnownRailsTimeZoneNames = new HashSet<string>(_railsMap.Select(x => x.Key));

		_systemTimeZones = GetSystemTimeZones();
	}

	/// <summary>
	/// Gets a collection of all IANA time zone names known to this library.
	/// </summary>
	public static ICollection<string> KnownIanaTimeZoneNames { get; }

	/// <summary>
	/// Gets a collection of all Windows time zone IDs known to this library.
	/// </summary>
	public static ICollection<string> KnownWindowsTimeZoneIds { get; }

	/// <summary>
	/// Gets a collection of all Rails time zone names known to this library.
	/// </summary>
	public static ICollection<string> KnownRailsTimeZoneNames { get; }

	/// <summary>
	/// Converts an IANA time zone name to the equivalent Windows time zone ID.
	/// </summary>
	/// <param name="ianaTimeZoneName">The IANA time zone name to convert.</param>
	/// <returns>A Windows time zone ID.</returns>
	/// <exception cref="InvalidTimeZoneException">Thrown if the input string was not recognized or has no equivalent Windows zone.</exception>
	public static string IanaToWindows(string ianaTimeZoneName)
	{
		if (TryIanaToWindows(ianaTimeZoneName, out var windowsTimeZoneId))
			return windowsTimeZoneId;

		throw new InvalidTimeZoneException($"\"{ianaTimeZoneName}\" was not recognized as a valid IANA time zone name, or has no equivalent Windows time zone.");
	}

	/// <summary>
	/// Attempts to convert an IANA time zone name to the equivalent Windows time zone ID.
	/// </summary>
	/// <param name="ianaTimeZoneName">The IANA time zone name to convert.</param>
	/// <param name="windowsTimeZoneId">A Windows time zone ID.</param>
	/// <returns><c>true</c> if successful, <c>false</c> otherwise.</returns>
	public static bool TryIanaToWindows(string ianaTimeZoneName, out string windowsTimeZoneId)
	{
		return _ianaMap.TryGetValue(ianaTimeZoneName, out windowsTimeZoneId);
	}

	/// <summary>
	/// Converts a Windows time zone ID to an equivalent IANA time zone name.
	/// </summary>
	/// <param name="windowsTimeZoneId">The Windows time zone ID to convert.</param>
	/// <param name="territoryCode">
	/// An optional two-letter ISO Country/Region code, used to get a a specific mapping.
	/// Defaults to "001" if not specified, which means to get the "golden zone" - the one that is most prevalent.
	/// </param>
	/// <returns>An IANA time zone name.</returns>
	/// <exception cref="InvalidTimeZoneException">Thrown if the input string was not recognized or has no equivalent IANA zone.</exception>
	public static string WindowsToIana(string windowsTimeZoneId, string territoryCode = "001")
	{
		if (TryWindowsToIana(windowsTimeZoneId, territoryCode, out var ianaTimeZoneName))
			return ianaTimeZoneName;

		throw new InvalidTimeZoneException($"\"{windowsTimeZoneId}\" was not recognized as a valid Windows time zone ID.");
	}

	/// <summary>
	/// Attempts to convert a Windows time zone ID to an equivalent IANA time zone name.
	/// Uses the "golden zone" - the one that is the most prevalent.
	/// </summary>
	/// <param name="windowsTimeZoneId">The Windows time zone ID to convert.</param>
	/// <param name="ianaTimeZoneName">An IANA time zone name.</param>
	/// <returns><c>true</c> if successful, <c>false</c> otherwise.</returns>
	public static bool TryWindowsToIana(string windowsTimeZoneId, out string ianaTimeZoneName)
	{
		return TryWindowsToIana(windowsTimeZoneId, "001", out ianaTimeZoneName);
	}

	/// <summary>
	/// Attempts to convert a Windows time zone ID to an equivalent IANA time zone name.
	/// </summary>
	/// <param name="windowsTimeZoneId">The Windows time zone ID to convert.</param>
	/// <param name="territoryCode">
	/// An optional two-letter ISO Country/Region code, used to get a a specific mapping.
	/// Defaults to "001" if not specified, which means to get the "golden zone" - the one that is most prevalent.
	/// </param>
	/// <param name="ianaTimeZoneName">An IANA time zone name.</param>
	/// <returns><c>true</c> if successful, <c>false</c> otherwise.</returns>
	public static bool TryWindowsToIana(string windowsTimeZoneId, string territoryCode, out string ianaTimeZoneName)
	{
		if (_windowsMap.TryGetValue($"{territoryCode}|{windowsTimeZoneId}", out ianaTimeZoneName))
			return true;

		// use the golden zone when not found with a particular region
		return territoryCode != "001" && _windowsMap.TryGetValue($"001|{windowsTimeZoneId}", out ianaTimeZoneName);
	}

	/// <summary>
	/// Retrieves a <see cref="TimeZoneInfo"/> object given a valid Windows or IANA time zone identifier,
	/// regardless of which platform the application is running on.
	/// </summary>
	/// <param name="windowsOrIanaTimeZoneId">A valid Windows or IANA time zone identifier.</param>
	/// <returns>A <see cref="TimeZoneInfo"/> object.</returns>
	public static TimeZoneInfo GetTimeZoneInfo(string windowsOrIanaTimeZoneId)
	{
		if (TryGetTimeZoneInfo(windowsOrIanaTimeZoneId, out var timeZoneInfo))
			return timeZoneInfo;

		throw new TimeZoneNotFoundException();
	}

	/// <summary>
	/// Attempts to retrieve a <see cref="TimeZoneInfo"/> object given a valid Windows or IANA time zone identifier,
	/// regardless of which platform the application is running on.
	/// </summary>
	/// <param name="windowsOrIanaTimeZoneId">A valid Windows or IANA time zone identifier.</param>
	/// <param name="timeZoneInfo">A <see cref="TimeZoneInfo"/> object.</param>
	/// <returns><c>true</c> if successful, <c>false</c> otherwise.</returns>
	public static bool TryGetTimeZoneInfo(string windowsOrIanaTimeZoneId, out TimeZoneInfo timeZoneInfo)
	{
		if (string.Equals(windowsOrIanaTimeZoneId, "UTC", StringComparison.OrdinalIgnoreCase))
		{
			timeZoneInfo = TimeZoneInfo.Utc;
			return true;
		}

		// Try a direct approach 
		if (_systemTimeZones.TryGetValue(windowsOrIanaTimeZoneId, out timeZoneInfo))
			return true;

		// Convert to the opposite platform and try again
		return (_isWindows && TryIanaToWindows(windowsOrIanaTimeZoneId, out var tzid) ||
				TryWindowsToIana(windowsOrIanaTimeZoneId, out tzid)) &&
			   _systemTimeZones.TryGetValue(tzid, out timeZoneInfo);
	}

	/// <summary>
	/// Converts an IANA time zone name to one or more equivalent Rails time zone names.
	/// </summary>
	/// <param name="ianaTimeZoneName">The IANA time zone name to convert.</param>
	/// <returns>One or more equivalent Rails time zone names.</returns>
	/// <exception cref="InvalidTimeZoneException">Thrown if the input string was not recognized or has no equivalent Rails zone.</exception>
	public static IList<string> IanaToRails(string ianaTimeZoneName)
	{
		if (TryIanaToRails(ianaTimeZoneName, out var railsTimeZoneNames))
			return railsTimeZoneNames;

		throw new InvalidTimeZoneException($"\"{ianaTimeZoneName}\" was not recognized as a valid IANA time zone name, or has no equivalent Rails time zone.");
	}

	/// <summary>
	/// Attempts to convert an IANA time zone name to one or more equivalent Rails time zone names.
	/// </summary>
	/// <param name="ianaTimeZoneName">The IANA time zone name to convert.</param>
	/// <param name="railsTimeZoneNames">One or more equivalent Rails time zone names.</param>
	/// <returns><c>true</c> if successful, <c>false</c> otherwise.</returns>
	public static bool TryIanaToRails(string ianaTimeZoneName, out IList<string> railsTimeZoneNames)
	{
		// try directly first
		if (_inverseRailsMap.TryGetValue(ianaTimeZoneName, out railsTimeZoneNames))
			return true;

		// try again with the golden zone
		return TryIanaToWindows(ianaTimeZoneName, out var windowsTimeZoneId) &&
			   TryWindowsToIana(windowsTimeZoneId, out var ianaGoldenZone) &&
			   _inverseRailsMap.TryGetValue(ianaGoldenZone, out railsTimeZoneNames);
	}

	/// <summary>
	/// Converts a Rails time zone name to an equivalent IANA time zone name.
	/// </summary>
	/// <param name="railsTimeZoneName">The Rails time zone name to convert.</param>
	/// <returns>An IANA time zone name.</returns>
	/// <exception cref="InvalidTimeZoneException">Thrown if the input string was not recognized or has no equivalent IANA zone.</exception>
	public static string RailsToIana(string railsTimeZoneName)
	{
		if (TryRailsToIana(railsTimeZoneName, out var ianaTimeZoneName))
			return ianaTimeZoneName;

		throw new InvalidTimeZoneException($"\"{railsTimeZoneName}\" was not recognized as a valid Rails time zone name.");
	}

	/// <summary>
	/// Attempts to convert a Rails time zone name to an equivalent IANA time zone name.
	/// </summary>
	/// <param name="railsTimeZoneName">The Rails time zone name to convert.</param>
	/// <param name="ianaTimeZoneName">An IANA time zone name.</param>
	/// <returns><c>true</c> if successful, <c>false</c> otherwise.</returns>
	public static bool TryRailsToIana(string railsTimeZoneName, out string ianaTimeZoneName)
	{
		return _railsMap.TryGetValue(railsTimeZoneName, out ianaTimeZoneName);
	}

	/// <summary>
	/// Converts a Rails time zone name to an equivalent Windows time zone ID.
	/// </summary>
	/// <param name="railsTimeZoneName">The Rails time zone name to convert.</param>
	/// <returns>A Windows time zone ID.</returns>
	/// <exception cref="InvalidTimeZoneException">Thrown if the input string was not recognized or has no equivalent Windows zone.</exception>
	public static string RailsToWindows(string railsTimeZoneName)
	{
		if (TryRailsToWindows(railsTimeZoneName, out var windowsTimeZoneId))
			return windowsTimeZoneId;

		throw new InvalidTimeZoneException($"\"{railsTimeZoneName}\" was not recognized as a valid Rails time zone name.");
	}

	/// <summary>
	/// Attempts to convert a Rails time zone name to an equivalent Windows time zone ID.
	/// </summary>
	/// <param name="railsTimeZoneName">The Rails time zone name to convert.</param>
	/// <param name="windowsTimeZoneId">A Windows time zone ID.</param>
	/// <returns><c>true</c> if successful, <c>false</c> otherwise.</returns>
	public static bool TryRailsToWindows(string railsTimeZoneName, out string windowsTimeZoneId)
	{
		if (TryRailsToIana(railsTimeZoneName, out var ianaTimeZoneName) &&
			TryIanaToWindows(ianaTimeZoneName, out windowsTimeZoneId))
			return true;

		windowsTimeZoneId = null;
		return false;
	}

	/// <summary>
	/// Converts a Windows time zone ID to one ore more equivalent Rails time zone names.
	/// </summary>
	/// <param name="windowsTimeZoneId">The Windows time zone ID to convert.</param>
	/// <param name="territoryCode">
	/// An optional two-letter ISO Country/Region code, used to get a a specific mapping.
	/// Defaults to "001" if not specified, which means to get the "golden zone" - the one that is most prevalent.
	/// </param>
	/// <returns>One or more equivalent Rails time zone names.</returns>
	/// <exception cref="InvalidTimeZoneException">Thrown if the input string was not recognized or has no equivalent Rails zone.</exception>
	public static IList<string> WindowsToRails(string windowsTimeZoneId, string territoryCode = "001")
	{
		if (TryWindowsToRails(windowsTimeZoneId, territoryCode, out var railsTimeZoneNames))
			return railsTimeZoneNames;

		throw new InvalidTimeZoneException($"\"{windowsTimeZoneId}\" was not recognized as a valid Windows time zone ID, or has no equivalent Rails time zone.");
	}

	/// <summary>
	/// Attempts to convert a Windows time zone ID to one ore more equivalent Rails time zone names.
	/// Uses the "golden zone" - the one that is the most prevalent.
	/// </summary>
	/// <param name="windowsTimeZoneId">The Windows time zone ID to convert.</param>
	/// <param name="railsTimeZoneNames">One or more equivalent Rails time zone names.</param>
	/// <returns><c>true</c> if successful, <c>false</c> otherwise.</returns>
	public static bool TryWindowsToRails(string windowsTimeZoneId, out IList<string> railsTimeZoneNames)
	{
		return TryWindowsToRails(windowsTimeZoneId, "001", out railsTimeZoneNames);
	}

	/// <summary>
	/// Attempts to convert a Windows time zone ID to one ore more equivalent Rails time zone names.
	/// </summary>
	/// <param name="windowsTimeZoneId">The Windows time zone ID to convert.</param>
	/// <param name="territoryCode">
	/// An optional two-letter ISO Country/Region code, used to get a a specific mapping.
	/// Defaults to "001" if not specified, which means to get the "golden zone" - the one that is most prevalent.
	/// </param>
	/// <param name="railsTimeZoneNames">One or more equivalent Rails time zone names.</param>
	/// <returns><c>true</c> if successful, <c>false</c> otherwise.</returns>
	public static bool TryWindowsToRails(string windowsTimeZoneId, string territoryCode, out IList<string> railsTimeZoneNames)
	{
		if (TryWindowsToIana(windowsTimeZoneId, territoryCode, out var ianaTimeZoneName) &&
			TryIanaToRails(ianaTimeZoneName, out railsTimeZoneNames))
			return true;

		railsTimeZoneNames = [];
		return false;
	}

	private static Dictionary<string, TimeZoneInfo> GetSystemTimeZones()
	{
		IEnumerable<TimeZoneInfo> timeZones;

#if NETSTANDARD2_0 || NETSTANDARD1_3
		if (_isWindows)
			timeZones = TimeZoneInfo.GetSystemTimeZones();
		else
			timeZones = GetSystemTimeZonesLinux();
#else
		timeZones = TimeZoneInfo.GetSystemTimeZones();
#endif
		var dict = new Dictionary<string, TimeZoneInfo>(StringComparer.OrdinalIgnoreCase);

		foreach (var tz in timeZones)
		{
			if (!dict.ContainsKey(tz.Id))
				dict.Add(tz.Id, tz);
		}

#if NETSTANDARD2_0 || NETSTANDARD1_3
		if (!_isWindows)
		{
			const string caSka = "Canada/East-Saskatchewan";
			// Include special case to resolve deleted link
			if (!dict.ContainsKey(caSka))
			{
				try
				{
					dict.Add(caSka, TimeZoneInfo.FindSystemTimeZoneById("Canada/Saskatchewan"));
				}
				catch
				{
				}
			}
		}
#endif
		return dict;
	}

#if NETSTANDARD2_0 || NETSTANDARD1_3
	private static IEnumerable<TimeZoneInfo> GetSystemTimeZonesLinux()
	{
		// Don't trust TimeZoneInfo.GetSystemTimeZones on Non-Windows
		// Because it doesn't return any links, or any Etc zones

		foreach (var name in KnownIanaTimeZoneNames)
		{
			TimeZoneInfo tzi = null;

			try
			{
				tzi = TimeZoneInfo.FindSystemTimeZoneById(name);
			}
			catch 
			{
			}

			if (tzi != null)
				yield return tzi;
		}
	}
#endif
}
