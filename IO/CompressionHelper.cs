namespace Ecng.IO
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.IO.Compression;
	using System.Threading;
	using System.Threading.Tasks;

	using Ecng.Common;

	using Nito.AsyncEx;

	public static class CompressionHelper
	{
		public static IEnumerable<(string name, Stream body)> Unzip(this byte[] input, bool leaveOpen = false, Func<string, bool> filter = null)
		{
			return input.To<MemoryStream>().Unzip(leaveOpen, filter);
		}

		public static IEnumerable<(string name, Stream body)> Unzip(this Stream input, bool leaveOpen = false, Func<string, bool> filter = null)
		{
			using (var zip = new ZipArchive(input, ZipArchiveMode.Read, leaveOpen))
			{
				foreach (var entry in zip.Entries)
				{
					if (filter?.Invoke(entry.Name) == false)
						continue;

					using var stream = entry.Open();
					yield return (entry.FullName, stream);
				}
			}

			if (!leaveOpen)
				input.Close();
		}

		public const int DefaultBufferSize = FileSizes.KB * 80;

		public static string UnGZip(this byte[] input)
			=> input.UnGZip(0, input.Length);

		public static string UnGZip(this ArraySegment<byte> v)
			=> v.Array.UnGZip(v.Offset, v.Count);

		public static string UnGZip(this byte[] input, int index, int count)
			=> input.Uncompress<GZipStream>(index, count).UTF8();

		public static int UnGZip(this ArraySegment<byte> input, byte[] destination)
			=> UnGZip(input.Array, input.Offset, input.Count, destination);

		public static int UnGZip(this byte[] input, int index, int count, byte[] destination)
		{
			using var inputStream = new MemoryStream(input, index, count);
			using var outputStream = new MemoryStream(destination);
			AsyncContext.Run(() => inputStream.UncompressAsync<GZipStream>(outputStream, true));
			return (int)outputStream.Position;
		}

		public static string UnDeflate(this byte[] input)
			=> input.UnDeflate(0, input.Length);

		public static string UnDeflate(this ArraySegment<byte> v)
			=> v.Array.UnDeflate(v.Offset, v.Count);

		public static string UnDeflate(this byte[] input, int index, int count)
			=> input.DeflateFrom(index, count).UTF8();

		public static int UnDeflate(this ArraySegment<byte> input, byte[] destination)
			=> UnDeflate(input.Array, input.Offset, input.Count, destination);

		public static int UnDeflate(this byte[] input, int index, int count, byte[] destination)
		{
			using var inputStream = new MemoryStream(input, index, count);
			using var outputStream = new MemoryStream(destination);
			AsyncContext.Run(() => inputStream.UncompressAsync<DeflateStream>(outputStream, true));
			return (int)outputStream.Position;
		}

		public static byte[] DeflateTo(this byte[] input)
			=> input.Compress<DeflateStream>();

		public static byte[] DeflateFrom(this ArraySegment<byte> v)
			=> v.Array.DeflateFrom(v.Offset, v.Count);

		public static byte[] DeflateFrom(this byte[] input, int? index = default, int? count = default, int bufferSize = DefaultBufferSize)
			=> input.Uncompress<DeflateStream>(index, count, bufferSize);

		public static byte[] Do7Zip(this byte[] input)
			=> input.Compress<Lzma.LzmaStream>();

		public static byte[] Un7Zip(this byte[] input)
			=> input.Uncompress<Lzma.LzmaStream>();

		public static byte[] Compress<TCompressStream>(this byte[] input, int? index = default, int? count = default, CompressionLevel level = CompressionLevel.Optimal, int bufferSize = DefaultBufferSize)
			where TCompressStream : Stream
			=> AsyncContext.Run(() => input.CompressAsync<TCompressStream>(index, count, level, bufferSize));

		public static byte[] Uncompress<TCompressStream>(this byte[] input, int? index = default, int? count = default, int bufferSize = DefaultBufferSize)
			where TCompressStream : Stream
			=> AsyncContext.Run(() => input.UncompressAsync<TCompressStream>(index, count, bufferSize));

		public static Task<byte[]> CompressAsync<TCompressStream>(this ArraySegment<byte> v, CompressionLevel level = CompressionLevel.Optimal, int bufferSize = DefaultBufferSize)
			where TCompressStream : Stream
			=> v.Array.CompressAsync<TCompressStream>(v.Offset, v.Count, level, bufferSize);

		public static async Task<byte[]> CompressAsync<TCompressStream>(this byte[] input, int? index = default, int? count = default, CompressionLevel level = CompressionLevel.Optimal, int bufferSize = DefaultBufferSize, CancellationToken cancellationToken = default)
			where TCompressStream : Stream
		{
			if (input is null)
				throw new ArgumentNullException(nameof(input));

			using var inputStream = new MemoryStream(input, index ?? 0, count ?? input.Length);
			using var outputStream = new MemoryStream();
			await inputStream.CompressAsync<TCompressStream>(outputStream, level, true, bufferSize, cancellationToken);
			return outputStream.To<byte[]>();
		}

		public static async Task<byte[]> UncompressAsync<TCompressStream>(this byte[] input, int? index = default, int? count = default, int bufferSize = DefaultBufferSize, CancellationToken cancellationToken = default)
			where TCompressStream : Stream
		{
			if (input is null)
				throw new ArgumentNullException(nameof(input));

			using var inputStream = new MemoryStream(input, index ?? 0, count ?? input.Length);
			using var outputStream = new MemoryStream();
			await inputStream.UncompressAsync<TCompressStream>(outputStream, true, bufferSize, cancellationToken);
			return outputStream.To<byte[]>();
		}

		public static async Task CompressAsync<TCompressStream>(this Stream input, Stream output, CompressionLevel level = CompressionLevel.Optimal, bool leaveOpen = true, int bufferSize = DefaultBufferSize, CancellationToken cancellationToken = default)
			where TCompressStream : Stream
		{
			if (input is null)
				throw new ArgumentNullException(nameof(input));

			using var compress = (TCompressStream)Activator.CreateInstance(typeof(TCompressStream), output, level, leaveOpen);
			await input.CopyToAsync(compress, bufferSize, cancellationToken);
		}

		public static async Task UncompressAsync<TCompressStream>(this Stream input, Stream output, bool leaveOpen = true, int bufferSize = DefaultBufferSize, CancellationToken cancellationToken = default)
			where TCompressStream : Stream
		{
			if (input is null)
				throw new ArgumentNullException(nameof(input));

			using var compress = (TCompressStream)Activator.CreateInstance(typeof(TCompressStream), input, CompressionMode.Decompress, leaveOpen);
			await compress.CopyToAsync(output, bufferSize, cancellationToken);
		}
	}
}