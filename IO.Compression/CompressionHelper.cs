namespace Ecng.IO.Compression;

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;

using Nito.AsyncEx;

using SharpCompress.Compressors.LZMA;

/// <summary>
/// Provides helper methods for compressing and decompressing data using various algorithms.
/// </summary>
public static class CompressionHelper
{
	#region ZipEntries

	private class ZipEntries(Stream input, bool leaveOpen, Func<string, bool> filter) : IEnumerable<(string name, Stream body)>
	{
		private class Enumerator(Stream input, bool leaveOpen, Func<string, bool> filter) : IEnumerator<(string name, Stream body)>
		{
			private ZipArchive _archive;
			private IEnumerator<ZipArchiveEntry> _entries;
			private (string name, Stream body) _current;

			public (string name, Stream body) Current => _current;
			object IEnumerator.Current => _current;

			public bool MoveNext()
			{
				if (_archive is null)
				{
					_archive = new ZipArchive(input, ZipArchiveMode.Read, leaveOpen);
					_entries = _archive.Entries.Where(e => filter(e.Name)).GetEnumerator();
				}

				if (!_entries.MoveNext())
					return false;

				var entry = _entries.Current;
				_current = (entry.FullName, entry.Open());
				return true;
			}

			public void Reset() => throw new NotSupportedException();

			public void Dispose()
			{
				_entries?.Dispose();
				_archive?.Dispose();
			}
		}

		public IEnumerator<(string name, Stream body)> GetEnumerator()
			=> new Enumerator(input, leaveOpen, filter);

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}

	#endregion

	/// <summary>
	/// Creates a ZIP archive from a collection of entries and writes it to the specified stream.
	/// </summary>
	/// <param name="entries">The entries to include in the archive. Each entry is a tuple of (name, stream).</param>
	/// <param name="output">The output stream to write the ZIP archive to.</param>
	/// <param name="level">The compression level to use.</param>
	/// <param name="leaveOpen">Whether to leave the output stream open after creating the archive.</param>
	public static void Zip(this IEnumerable<(string name, Stream body)> entries, Stream output, CompressionLevel level = CompressionLevel.Optimal, bool leaveOpen = true)
		=> AsyncContext.Run(() => entries.ZipAsync(output, level, leaveOpen));

	/// <summary>
	/// Creates a ZIP archive from a collection of entries and writes it to the specified stream asynchronously.
	/// </summary>
	/// <param name="entries">The entries to include in the archive. Each entry is a tuple of (name, stream).</param>
	/// <param name="output">The output stream to write the ZIP archive to.</param>
	/// <param name="level">The compression level to use.</param>
	/// <param name="leaveOpen">Whether to leave the output stream open after creating the archive.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	public static async Task ZipAsync(this IEnumerable<(string name, Stream body)> entries, Stream output, CompressionLevel level = CompressionLevel.Optimal, bool leaveOpen = true, CancellationToken cancellationToken = default)
	{
		if (entries is null)
			throw new ArgumentNullException(nameof(entries));

		if (output is null)
			throw new ArgumentNullException(nameof(output));

		using var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen);

		foreach (var (name, body) in entries)
		{
			cancellationToken.ThrowIfCancellationRequested();

			var entry = archive.CreateEntry(name, level);

			using var entryStream = entry.Open();
			await body.CopyToAsync(entryStream, cancellationToken);
		}
	}

	/// <summary>
	/// Extracts entries from a ZIP archive contained in the specified byte array.
	/// </summary>
	/// <param name="input">The byte array containing the ZIP archive.</param>
	/// <param name="filter">A function to filter entries by name. Only entries returning true will be processed.</param>
	/// <returns>An enumerable of entries. The enumerator must be disposed after use (foreach does this automatically).</returns>
	public static IEnumerable<(string name, Stream body)> Unzip(this byte[] input, Func<string, bool> filter = null)
	{
		return input.To<MemoryStream>().Unzip(filter: filter);
	}

	/// <summary>
	/// Extracts entries from a ZIP archive contained in the specified stream.
	/// </summary>
	/// <param name="input">The stream containing the ZIP archive.</param>
	/// <param name="leaveOpen">Whether to leave the underlying stream open after processing.</param>
	/// <param name="filter">A function to filter entries by name. Only entries returning true will be processed.</param>
	/// <returns>An enumerable of entries. The enumerator must be disposed after use (foreach does this automatically).</returns>
	public static IEnumerable<(string name, Stream body)> Unzip(this Stream input, bool leaveOpen = false, Func<string, bool> filter = null)
	{
		if (input is null)
			throw new ArgumentNullException(nameof(input));

		return new ZipEntries(input, leaveOpen, filter ?? (_ => true));
	}

	/// <summary>
	/// Represents the default buffer size used in compression and decompression operations.
	/// </summary>
	public const int DefaultBufferSize = FileSizes.KB * 80;

	/// <summary>
	/// Decompresses a GZip-compressed byte array into a UTF-8 encoded string.
	/// </summary>
	/// <param name="input">The byte array containing GZip-compressed data.</param>
	/// <returns>A string resulting from decompression.</returns>
	public static string UnGZip(this byte[] input)
		=> input.UnGZip(0, input.Length);

#if !NETSTANDARD2_0
	/// <summary>
	/// Decompresses a GZip-compressed span of bytes into a UTF-8 encoded string.
	/// </summary>
	/// <param name="input">The span containing GZip-compressed data.</param>
	/// <returns>A string resulting from decompression.</returns>
	public static string UnGZip(this ReadOnlySpan<byte> input)
		=> input.ToArray().UnGZip(0, input.Length);
#endif

	/// <summary>
	/// Decompresses a GZip-compressed portion of a byte array into a UTF-8 encoded string.
	/// </summary>
	/// <param name="input">The byte array containing GZip-compressed data.</param>
	/// <param name="index">The starting index from which to begin decompression.</param>
	/// <param name="count">The number of bytes to decompress.</param>
	/// <returns>A string resulting from decompression.</returns>
	public static string UnGZip(this byte[] input, int index, int count)
		=> input.Uncompress<GZipStream>(index, count).UTF8();

#if !NETSTANDARD2_0
	/// <summary>
	/// Decompresses a GZip-compressed span of bytes into the provided destination span.
	/// </summary>
	/// <param name="input">The span containing GZip-compressed data.</param>
	/// <param name="destination">The span to store decompressed data.</param>
	/// <returns>The number of bytes written into the destination span.</returns>
	public static int UnGZip(this ReadOnlySpan<byte> input, Span<byte> destination)
	{
		var tempDest = new byte[destination.Length];
		var written = input.ToArray().UnGZip(0, input.Length, tempDest);
		tempDest.AsSpan(0, written).CopyTo(destination);
		return written;
	}
#endif

	/// <summary>
	/// Decompresses a portion of a GZip-compressed byte array into the provided destination buffer.
	/// </summary>
	/// <param name="input">The byte array containing GZip-compressed data.</param>
	/// <param name="index">The starting index for decompression.</param>
	/// <param name="count">The number of bytes to decompress.</param>
	/// <param name="destination">The buffer to store decompressed data.</param>
	/// <returns>The number of bytes written into the destination buffer.</returns>
	public static int UnGZip(this byte[] input, int index, int count, byte[] destination)
	{
		using var inputStream = new MemoryStream(input, index, count);
		using var outputStream = new MemoryStream(destination);
		AsyncContext.Run(() => inputStream.UncompressAsync<GZipStream>(outputStream, true));
		return (int)outputStream.Position;
	}

	/// <summary>
	/// Decompresses a Deflate-compressed byte array into a UTF-8 encoded string.
	/// </summary>
	/// <param name="input">The byte array containing Deflate-compressed data.</param>
	/// <returns>A string resulting from decompression.</returns>
	public static string UnDeflate(this byte[] input)
		=> input.UnDeflate(0, input.Length);

#if !NETSTANDARD2_0
	/// <summary>
	/// Decompresses a Deflate-compressed span of bytes into a UTF-8 encoded string.
	/// </summary>
	/// <param name="input">The span containing Deflate-compressed data.</param>
	/// <returns>A string resulting from decompression.</returns>
	public static string UnDeflate(this ReadOnlySpan<byte> input)
		=> input.ToArray().UnDeflate(0, input.Length);
#endif

	/// <summary>
	/// Decompresses a portion of a Deflate-compressed byte array into a UTF-8 encoded string.
	/// </summary>
	/// <param name="input">The byte array containing Deflate-compressed data.</param>
	/// <param name="index">The starting index for decompression.</param>
	/// <param name="count">The number of bytes to decompress.</param>
	/// <returns>A string resulting from decompression.</returns>
	public static string UnDeflate(this byte[] input, int index, int count)
		=> input.DeflateFrom(index, count).UTF8();

#if !NETSTANDARD2_0
	/// <summary>
	/// Decompresses a Deflate-compressed span of bytes into the provided destination span.
	/// </summary>
	/// <param name="input">The span containing Deflate-compressed data.</param>
	/// <param name="destination">The span to store decompressed data.</param>
	/// <returns>The number of bytes written into the destination span.</returns>
	public static int UnDeflate(this ReadOnlySpan<byte> input, Span<byte> destination)
	{
		var tempDest = new byte[destination.Length];
		var written = input.ToArray().UnDeflate(0, input.Length, tempDest);
		tempDest.AsSpan(0, written).CopyTo(destination);
		return written;
	}
#endif

	/// <summary>
	/// Decompresses a portion of a Deflate-compressed byte array into the provided destination buffer.
	/// </summary>
	/// <param name="input">The byte array containing Deflate-compressed data.</param>
	/// <param name="index">The starting index for decompression.</param>
	/// <param name="count">The number of bytes to decompress.</param>
	/// <param name="destination">The buffer to store decompressed data.</param>
	/// <returns>The number of bytes written into the destination buffer.</returns>
	public static int UnDeflate(this byte[] input, int index, int count, byte[] destination)
	{
		using var inputStream = new MemoryStream(input, index, count);
		using var outputStream = new MemoryStream(destination);
		AsyncContext.Run(() => inputStream.UncompressAsync<DeflateStream>(outputStream, true));
		return (int)outputStream.Position;
	}

	/// <summary>
	/// Compresses the specified byte array using the Deflate algorithm.
	/// </summary>
	/// <param name="input">The byte array to compress.</param>
	/// <returns>A compressed byte array.</returns>
	public static byte[] DeflateTo(this byte[] input)
		=> input.Compress<DeflateStream>();

#if !NETSTANDARD2_0
	/// <summary>
	/// Decompresses a span of bytes using the Deflate algorithm.
	/// </summary>
	/// <param name="input">The span to decompress.</param>
	/// <param name="bufferSize">The buffer size to use during decompression.</param>
	/// <returns>A decompressed byte array.</returns>
	public static byte[] DeflateFrom(this ReadOnlySpan<byte> input, int bufferSize = DefaultBufferSize)
		=> input.ToArray().DeflateFrom(0, input.Length, bufferSize);
#endif

	/// <summary>
	/// Decompresses a portion of a byte array using the Deflate algorithm.
	/// </summary>
	/// <param name="input">The byte array to decompress.</param>
	/// <param name="index">The starting index from which to begin decompression.</param>
	/// <param name="count">The number of bytes to decompress.</param>
	/// <param name="bufferSize">The buffer size to use during decompression.</param>
	/// <returns>A decompressed byte array.</returns>
	public static byte[] DeflateFrom(this byte[] input, int? index = default, int? count = default, int bufferSize = DefaultBufferSize)
		=> input.Uncompress<DeflateStream>(index, count, bufferSize);

	/// <summary>
	/// Compresses the specified byte array using the 7Zip (LZMA) algorithm.
	/// </summary>
	/// <param name="input">The byte array to compress.</param>
	/// <returns>A compressed byte array with LZMA header (5 bytes properties + 8 bytes size + compressed data).</returns>
	public static byte[] Do7Zip(this byte[] input)
	{
		if (input is null)
			throw new ArgumentNullException(nameof(input));

		using var compressedStream = new MemoryStream();
		var props = new LzmaEncoderProperties(eos: true, dictionary: 1 << 20, numFastBytes: 32);
		byte[] properties;

		using (var lzma = new LzmaStream(props, false, compressedStream))
		{
			properties = lzma.Properties;
			lzma.Write(input, 0, input.Length);
		}

		var compressedData = compressedStream.ToArray();

		// Build LZMA file format: 5 bytes props + 8 bytes size + compressed data
		using var result = new MemoryStream();
		result.Write(properties, 0, 5);
		result.Write(BitConverter.GetBytes((long)input.Length), 0, 8);
		result.Write(compressedData, 0, compressedData.Length);

		return result.ToArray();
	}

	/// <summary>
	/// Decompresses a 7Zip (LZMA) compressed byte array.
	/// </summary>
	/// <param name="input">The byte array containing 7Zip-compressed data with LZMA header.</param>
	/// <returns>A decompressed byte array.</returns>
	public static byte[] Un7Zip(this byte[] input)
	{
		if (input is null)
			throw new ArgumentNullException(nameof(input));

		if (input.Length < 13)
			throw new ArgumentException("Input too short for LZMA format (minimum 13 bytes header).", nameof(input));

		// Parse LZMA header: 5 bytes properties + 8 bytes uncompressed size
		var properties = new byte[5];
		Array.Copy(input, 0, properties, 0, 5);
		var uncompressedSize = BitConverter.ToInt64(input, 5);

		using var inputStream = new MemoryStream(input, 13, input.Length - 13);
		using var outputStream = new MemoryStream();

		using (var lzma = new LzmaStream(properties, inputStream, input.Length - 13, uncompressedSize, null, false))
		{
			lzma.CopyTo(outputStream);
		}

		return outputStream.ToArray();
	}

	/// <summary>
	/// Compresses a portion of a byte array using the specified compression stream.
	/// </summary>
	/// <typeparam name="TCompressStream">The type of compression stream to use.</typeparam>
	/// <param name="input">The byte array to compress.</param>
	/// <param name="index">The starting index for compression.</param>
	/// <param name="count">The number of bytes to compress.</param>
	/// <param name="level">The compression level to use.</param>
	/// <param name="bufferSize">The buffer size to use during compression.</param>
	/// <returns>A compressed byte array.</returns>
	public static byte[] Compress<TCompressStream>(this byte[] input, int? index = default, int? count = default, CompressionLevel level = CompressionLevel.Optimal, int bufferSize = DefaultBufferSize)
		where TCompressStream : Stream
		=> AsyncContext.Run(() => input.CompressAsync<TCompressStream>(index, count, level, bufferSize));

	/// <summary>
	/// Decompresses a portion of a byte array using the specified compression stream.
	/// </summary>
	/// <typeparam name="TCompressStream">The type of compression stream to use.</typeparam>
	/// <param name="input">The byte array containing compressed data.</param>
	/// <param name="index">The starting index for decompression.</param>
	/// <param name="count">The number of bytes to decompress.</param>
	/// <param name="bufferSize">The buffer size to use during decompression.</param>
	/// <returns>A decompressed byte array.</returns>
	public static byte[] Uncompress<TCompressStream>(this byte[] input, int? index = default, int? count = default, int bufferSize = DefaultBufferSize)
		where TCompressStream : Stream
		=> AsyncContext.Run(() => input.UncompressAsync<TCompressStream>(index, count, bufferSize));

#if !NETSTANDARD2_0
	/// <summary>
	/// Asynchronously compresses a read-only memory of bytes using the specified compression stream.
	/// </summary>
	/// <typeparam name="TCompressStream">The type of compression stream to use.</typeparam>
	/// <param name="input">The read-only memory containing data to compress.</param>
	/// <param name="level">The compression level to use.</param>
	/// <param name="bufferSize">The buffer size to use during compression.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A task representing the asynchronous operation, with a compressed byte array as the result.</returns>
	public static Task<byte[]> CompressAsync<TCompressStream>(this ReadOnlyMemory<byte> input, CompressionLevel level = CompressionLevel.Optimal, int bufferSize = DefaultBufferSize, CancellationToken cancellationToken = default)
		where TCompressStream : Stream
		=> input.ToArray().CompressAsync<TCompressStream>(0, input.Length, level, bufferSize, cancellationToken);
#endif

	/// <summary>
	/// Asynchronously compresses a portion of a byte array using the specified compression stream.
	/// </summary>
	/// <typeparam name="TCompressStream">The type of compression stream to use.</typeparam>
	/// <param name="input">The byte array to compress.</param>
	/// <param name="index">The starting index for compression.</param>
	/// <param name="count">The number of bytes to compress.</param>
	/// <param name="level">The compression level to use.</param>
	/// <param name="bufferSize">The buffer size to use during compression.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A task representing the asynchronous operation, with a compressed byte array as the result.</returns>
	public static async Task<byte[]> CompressAsync<TCompressStream>(this byte[] input, int? index = default, int? count = default, CompressionLevel level = CompressionLevel.Optimal, int bufferSize = DefaultBufferSize, CancellationToken cancellationToken = default)
		where TCompressStream : Stream
	{
		if (input is null)
			throw new ArgumentNullException(nameof(input));

		using var inputStream = new MemoryStream(input, index ?? 0, count ?? input.Length);
		using var outputStream = new MemoryStream();
		await inputStream.CompressAsync<TCompressStream>(outputStream, level, true, bufferSize, cancellationToken).NoWait();
		return outputStream.To<byte[]>();
	}

	/// <summary>
	/// Asynchronously decompresses a portion of a byte array using the specified compression stream.
	/// </summary>
	/// <typeparam name="TCompressStream">The type of compression stream to use.</typeparam>
	/// <param name="input">The byte array containing compressed data.</param>
	/// <param name="index">The starting index for decompression.</param>
	/// <param name="count">The number of bytes to decompress.</param>
	/// <param name="bufferSize">The buffer size to use during decompression.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A task representing the asynchronous decompression operation, with a decompressed byte array as the result.</returns>
	public static async Task<byte[]> UncompressAsync<TCompressStream>(this byte[] input, int? index = default, int? count = default, int bufferSize = DefaultBufferSize, CancellationToken cancellationToken = default)
		where TCompressStream : Stream
	{
		if (input is null)
			throw new ArgumentNullException(nameof(input));

		using var inputStream = new MemoryStream(input, index ?? 0, count ?? input.Length);
		using var outputStream = new MemoryStream();
		await inputStream.UncompressAsync<TCompressStream>(outputStream, true, bufferSize, cancellationToken).NoWait();
		return outputStream.To<byte[]>();
	}

	/// <summary>
	/// Asynchronously compresses data from the input stream and writes the compressed data to the output stream using the specified compression stream.
	/// </summary>
	/// <typeparam name="TCompressStream">The type of compression stream to use.</typeparam>
	/// <param name="input">The input stream containing data to compress.</param>
	/// <param name="output">The output stream to write compressed data to.</param>
	/// <param name="level">The compression level to use.</param>
	/// <param name="leaveOpen">Whether to leave the output stream open after compression.</param>
	/// <param name="bufferSize">The buffer size to use during compression.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A task representing the asynchronous compression operation.</returns>
	public static async Task CompressAsync<TCompressStream>(this Stream input, Stream output, CompressionLevel level = CompressionLevel.Optimal, bool leaveOpen = true, int bufferSize = DefaultBufferSize, CancellationToken cancellationToken = default)
		where TCompressStream : Stream
	{
		if (input is null)
			throw new ArgumentNullException(nameof(input));

		using var compress = (TCompressStream)Activator.CreateInstance(typeof(TCompressStream), output, level, leaveOpen);
		await input.CopyToAsync(compress, bufferSize, cancellationToken);
	}

	/// <summary>
	/// Asynchronously decompresses data from the input stream and writes the decompressed data to the output stream using the specified compression stream.
	/// </summary>
	/// <typeparam name="TCompressStream">The type of compression stream to use.</typeparam>
	/// <param name="input">The input stream containing compressed data.</param>
	/// <param name="output">The output stream to write decompressed data to.</param>
	/// <param name="leaveOpen">Whether to leave the input stream open after decompression.</param>
	/// <param name="bufferSize">The buffer size to use during decompression.</param>
	/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
	/// <returns>A task representing the asynchronous decompression operation.</returns>
	public static async Task UncompressAsync<TCompressStream>(this Stream input, Stream output, bool leaveOpen = true, int bufferSize = DefaultBufferSize, CancellationToken cancellationToken = default)
		where TCompressStream : Stream
	{
		if (input is null)
			throw new ArgumentNullException(nameof(input));

		using var compress = (TCompressStream)Activator.CreateInstance(typeof(TCompressStream), input, CompressionMode.Decompress, leaveOpen);
		await compress.CopyToAsync(output, bufferSize, cancellationToken);
	}
}