using System;

namespace Ecng.Backup.Mega.Native;

public sealed class MegaApiException : InvalidOperationException
{
	public MegaApiException(int code)
		: base($"Mega API error: {MegaErrorCodeHelper.GetDisplayName(code)}.")
	{
		Code = code;
		ErrorCode = MegaErrorCodeHelper.GetKnownOrUnknown(code);
	}

	public int Code { get; }
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
