namespace Ecng.Common;

public static class FileSizesExtensions
{
	public static long ToKB(this long bytes) => (long)((double)bytes / FileSizes.KB);
	public static long ToMB(this long bytes) => (long)((double)bytes / FileSizes.MB);

	public static long FromKB(this long kbytes) => kbytes * FileSizes.KB;
	public static long FromMB(this long mbytes) => mbytes * FileSizes.MB;
}