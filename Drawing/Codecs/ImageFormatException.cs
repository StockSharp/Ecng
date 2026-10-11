namespace Ecng.Drawing.Codecs;

using System;

internal class ImageFormatException : Exception
{
	public ImageFormatException(string message, string format)
		: base($"{format}: {message}")
	{
	}

	public ImageFormatException(string message, string format, Exception innerException)
		: base($"{format}: {message}", innerException)
	{
	}
}
