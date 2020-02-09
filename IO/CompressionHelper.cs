namespace Ecng.IO
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.IO.Compression;
	using System.Text;

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

		public static int CopyToBuffer(this Stream stream, byte[] destination)
		{
			var output = new MemoryStream(destination);
			stream.CopyTo(output);
			return (int)output.Position;
		}

		public static string StreamToString(this Stream stream)
		{
			return stream.StreamToString(Encoding.UTF8);
		}

		public static string StreamToString(this Stream stream, Encoding encoding)
		{
			using (var streamReader = new StreamReader(stream, encoding))
				return streamReader.ReadToEnd();
		}

		public static string UnGZip(this byte[] archive)
		{
			if (archive == null)
				throw new ArgumentNullException(nameof(archive));

			return archive.UnGZip(0, archive.Length);
		}

		public static string UnGZip(this byte[] archive, int index, int count)
		{
			using (var zip = new GZipStream(new MemoryStream(archive, index, count), CompressionMode.Decompress))
				return zip.StreamToString();
		}

		public static int UnGZip(this byte[] archive, int index, int count, byte[] destination)
		{
			using (var zip = new GZipStream(new MemoryStream(archive, index, count), CompressionMode.Decompress))
				return zip.CopyToBuffer(destination);
		}

		public static string UnDeflate(this byte[] archive)
		{
			if (archive == null)
				throw new ArgumentNullException(nameof(archive));

			return archive.UnDeflate(0, archive.Length);
		}

		public static string UnDeflate(this byte[] archive, int index, int count)
		{
			using (var deflate = new DeflateStream(new MemoryStream(archive, index, count), CompressionMode.Decompress))
				return deflate.StreamToString();
		}

		public static int UnDeflate(this byte[] archive, int index, int count, byte[] destination)
		{
			using (var deflate = new DeflateStream(new MemoryStream(archive, index, count), CompressionMode.Decompress))
				return deflate.CopyToBuffer(destination);
		}

		public static byte[] DeflateTo(this byte[] content)
		{
			if (content == null)
				throw new ArgumentNullException(nameof(content));

			using (var output = new MemoryStream())
			{
				using (var deflate = new DeflateStream(output, CompressionMode.Compress, true))
					deflate.Write(content, 0, content.Length);

				return output.To<byte[]>();
			}
		}

		public static byte[] DeflateFrom(this byte[] content)
		{
			if (content == null)
				throw new ArgumentNullException(nameof(content));

			using (var output = new MemoryStream())
			{
				using (var deflate = new DeflateStream(new MemoryStream(content), CompressionMode.Decompress))
					deflate.CopyTo(output);

				return output.To<byte[]>();
			}
		}
	}
}