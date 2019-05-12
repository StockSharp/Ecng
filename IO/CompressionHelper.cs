namespace Ecng.IO
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.IO.Compression;

	using Ecng.Common;

	public static class CompressionHelper
    {
	    public static IEnumerable<Stream> Unzip(this byte[] input, bool leaveOpen = false, Func<string, bool> filter = null)
	    {
		    return input.To<MemoryStream>().Unzip(leaveOpen, filter);
	    }

	    public static IEnumerable<Stream> Unzip(this Stream input, bool leaveOpen = false, Func<string, bool> filter = null)
	    {
		    using (var zip = new ZipArchive(input, ZipArchiveMode.Read, leaveOpen))
		    {
			    foreach (var entry in zip.Entries)
			    {
					if (filter?.Invoke(entry.Name) == false)
						continue;

					using (var stream = entry.Open())
						yield return stream;
			    }
		    }
	    }
    }
}
