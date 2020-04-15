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
		public static IEnumerable<Tuple<string, Stream>> Unzip(this byte[] input, bool leaveOpen = false, Func<string, bool> filter = null)
		{
			return input.To<MemoryStream>().Unzip(leaveOpen, filter);
		}

		public static IEnumerable<Tuple<string, Stream>> Unzip(this Stream input, bool leaveOpen = false, Func<string, bool> filter = null)
		{
			using (var zip = new ZipArchive(input, ZipArchiveMode.Read, leaveOpen))
			{
				foreach (var entry in zip.Entries)
				{
					if (filter?.Invoke(entry.Name) == false)
						continue;

					using (var stream = entry.Open())
						yield return Tuple.Create(entry.FullName, stream);
				}
			}

			if(!leaveOpen)
				input.Close();
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

		public static string UnGZip(this byte[] input)
		{
			if (input == null)
				throw new ArgumentNullException(nameof(input));

			return input.UnGZip(0, input.Length);
		}

		public static string UnGZip(this byte[] input, int index, int count)
		{
			using (var zip = new GZipStream(new MemoryStream(input, index, count), CompressionMode.Decompress))
				return zip.StreamToString();
		}

		public static int UnGZip(this byte[] input, int index, int count, byte[] destination)
		{
			using (var zip = new GZipStream(new MemoryStream(input, index, count), CompressionMode.Decompress))
				return zip.CopyToBuffer(destination);
		}

		public static string UnDeflate(this byte[] input)
		{
			if (input == null)
				throw new ArgumentNullException(nameof(input));

			return input.UnDeflate(0, input.Length);
		}

		public static string UnDeflate(this byte[] input, int index, int count)
		{
			using (var deflate = new DeflateStream(new MemoryStream(input, index, count), CompressionMode.Decompress))
				return deflate.StreamToString();
		}

		public static int UnDeflate(this byte[] input, int index, int count, byte[] destination)
		{
			using (var deflate = new DeflateStream(new MemoryStream(input, index, count), CompressionMode.Decompress))
				return deflate.CopyToBuffer(destination);
		}

		public static byte[] DeflateTo(this byte[] input)
		{
			if (input == null)
				throw new ArgumentNullException(nameof(input));

			using (var output = new MemoryStream())
			{
				using (var deflate = new DeflateStream(output, CompressionMode.Compress, true))
					deflate.Write(input, 0, input.Length);

				return output.To<byte[]>();
			}
		}

		public static byte[] DeflateFrom(this byte[] input)
		{
			if (input == null)
				throw new ArgumentNullException(nameof(input));

			using (var output = new MemoryStream())
			{
				using (var deflate = new DeflateStream(new MemoryStream(input), CompressionMode.Decompress))
					deflate.CopyTo(output);

				return output.To<byte[]>();
			}
		}

		public static byte[] Un7Zip(this byte[] input)
		{
			if (input == null)
				throw new ArgumentNullException(nameof(input));

			return new Lzma.LzmaStream(input.To<Stream>(), CompressionMode.Decompress).To<byte[]>();
		}
	}
}