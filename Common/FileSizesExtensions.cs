namespace Ecng.Common;

using System;

/// <summary>
/// Provides extension methods for converting file sizes between bytes, kilobytes, and megabytes.
/// </summary>
public static class FileSizesExtensions
{
	/// <summary>
	/// Converts the specified number of bytes to kilobytes.
	/// </summary>
	/// <param name="bytes">The number of bytes.</param>
	/// <returns>The equivalent number of kilobytes.</returns>
	public static long ToKB(this long bytes) => (long)((double)bytes / FileSizes.KB);

	/// <summary>
	/// Converts the specified number of bytes to megabytes.
	/// </summary>
	/// <param name="bytes">The number of bytes.</param>
	/// <returns>The equivalent number of megabytes.</returns>
	public static long ToMB(this long bytes) => (long)((double)bytes / FileSizes.MB);

	/// <summary>
	/// Converts the specified number of kilobytes to bytes.
	/// </summary>
	/// <param name="kbytes">The number of kilobytes.</param>
	/// <returns>The equivalent number of bytes.</returns>
	public static long FromKB(this long kbytes) => kbytes * FileSizes.KB;

	/// <summary>
	/// Converts the specified number of megabytes to bytes.
	/// </summary>
	/// <param name="mbytes">The number of megabytes.</param>
	/// <returns>The equivalent number of bytes.</returns>
	public static long FromMB(this long mbytes) => mbytes * FileSizes.MB;

	private static readonly string[] _suf = ["B", "KB", "MB", "GB", "TB", "PB", "EB"]; //Longs run out around EB

	/// <summary>
	/// Converts a byte count to a human-readable file size string.
	/// </summary>
	/// <param name="byteCount">The number of bytes.</param>
	/// <returns>A formatted string representing the file size.</returns>
	public static string ToHumanReadableFileSize(this long byteCount)
	{
		int place;
		int num;

		if (byteCount == 0)
		{
			num = 0;
			place = 0;
		}
		else
		{
			var bytes = byteCount.Abs();
			place = (int)Math.Log(bytes, FileSizes.KB).Floor();
			num = (int)(Math.Sign(byteCount) * Math.Round(bytes / Math.Pow(FileSizes.KB, place), 1));
		}

		return num + " " + _suf[place];
	}
}