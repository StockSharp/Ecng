using System;

namespace Ecng.Backup.Mega.Native;

/// <summary>
/// Exception thrown when MEGA API returns an error code.
/// </summary>
public sealed class MegaApiException : InvalidOperationException
{
	/// <summary>
	/// Initializes a new instance of <see cref="MegaApiException"/>.
	/// </summary>
	/// <param name="code">Raw MEGA API error code.</param>
	public MegaApiException(int code)
		: base($"Mega API error: {MegaErrorCodeHelper.GetDisplayName(code)}.")
	{
		Code = code;
		ErrorCode = MegaErrorCodeHelper.GetKnownOrUnknown(code);
	}

	/// <summary>
	/// Raw MEGA API error code.
	/// </summary>
	public int Code { get; }

	/// <summary>
	/// Parsed error code if it is a known <see cref="MegaErrorCode"/> value; otherwise <see cref="MegaErrorCode.Unknown"/>.
	/// </summary>
	public MegaErrorCode ErrorCode { get; }
}

internal static class MegaErrorCodeHelper
{
	public static MegaErrorCode GetKnownOrUnknown(int code)
	{
		var value = (MegaErrorCode)code;
		return Enum.IsDefined(typeof(MegaErrorCode), value) ? value : MegaErrorCode.Unknown;
	}

	public static string GetDisplayName(int code)
	{
		var value = (MegaErrorCode)code;
		return Enum.IsDefined(typeof(MegaErrorCode), value) ? value.ToString() : MegaErrorCode.Unknown.ToString();
	}
}
