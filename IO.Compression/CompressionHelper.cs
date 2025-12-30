namespace Ecng.IO.Compression;

using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;

using Nito.AsyncEx;

/// <summary>
/// Provides helper methods for compressing and decompressing data using various algorithms.
/// </summary>
public static class CompressionHelper
{
	/// <summary>
	/// Extracts entries from a ZIP archive contained in the specified byte array.
	/// </summary>
	/// <param name="input">The byte array containing the ZIP archive.</param>
	/// <param name="filter">A function to filter entries by name. Only entries returning true will be processed.</param>
	/// <returns>A disposable enumerable of entries.</returns>
	public static ZipEntries Unzip(this byte[] input, Func<string, bool> filter = null)
	{
		return input.To<MemoryStream>().Unzip(filter: filter);
	}

	/// <summary>
	/// Extracts entries from a ZIP archive contained in the specified stream.
	/// </summary>
	/// <param name="input">The stream containing the ZIP archive.</param>
	/// <param name="leaveOpen">Whether to leave the underlying stream open after processing.</param>
	/// <param name="filter">A function to filter entries by name. Only entries returning true will be processed.</param>
	/// <returns>A disposable enumerable of entries that must be disposed when finished.</returns>
	public static ZipEntries Unzip(this Stream input, bool leaveOpen = false, Func<string, bool> filter = null)
	{
		if (input is null)
			throw new ArgumentNullException(nameof(input));

		filter ??= _ => true;

		return new ZipEntries(new(input, ZipArchiveMode.Read, leaveOpen), filter);
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

	/// <summary>
	/// Decompresses a GZip-compressed segment of bytes into a UTF-8 encoded string.
	/// </summary>
	/// <param name="v">The byte array segment containing GZip-compressed data.</param>
	/// <returns>A string resulting from decompression.</returns>
#if !NETSTANDARD2_0
	[Obsolete("Use ReadOnlySpan<byte> overload instead for better performance.")]
#endif
	public static string UnGZip(this ArraySegment<byte> v)
		=> v.Array.UnGZip(v.Offset, v.Count);

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

	/// <summary>
	/// Decompresses a GZip-compressed segment of bytes into the provided destination buffer.
	/// </summary>
	/// <param name="input">The byte array segment containing GZip-compressed data.</param>
	/// <param name="destination">The buffer to store decompressed data.</param>
	/// <returns>The number of bytes written into the destination buffer.</returns>
#if !NETSTANDARD2_0
	[Obsolete("Use ReadOnlySpan<byte> overload instead for better performance.")]
#endif
	public static int UnGZip(this ArraySegment<byte> input, byte[] destination)
		=> UnGZip(input.Array, input.Offset, input.Count, destination);

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

	/// <summary>
	/// Decompresses a segment of Deflate-compressed bytes into a UTF-8 encoded string.
	/// </summary>
	/// <param name="v">The byte array segment containing Deflate-compressed data.</param>
	/// <returns>A string resulting from decompression.</returns>
#if !NETSTANDARD2_0
	[Obsolete("Use ReadOnlySpan<byte> overload instead for better performance.")]
#endif
	public static string UnDeflate(this ArraySegment<byte> v)
		=> v.Array.UnDeflate(v.Offset, v.Count);

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

	/// <summary>
	/// Decompresses a Deflate-compressed segment of bytes into the provided destination buffer.
	/// </summary>
	/// <param name="input">The byte array segment containing Deflate-compressed data.</param>
	/// <param name="destination">The buffer to store decompressed data.</param>
	/// <returns>The number of bytes written into the destination buffer.</returns>
#if !NETSTANDARD2_0
	[Obsolete("Use ReadOnlySpan<byte> overload instead for better performance.")]
#endif
	public static int UnDeflate(this ArraySegment<byte> input, byte[] destination)
		=> UnDeflate(input.Array, input.Offset, input.Count, destination);

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

	/// <summary>
	/// Decompresses a segment of a byte array using the Deflate algorithm.
	/// </summary>
	/// <param name="v">The byte array segment to decompress.</param>
	/// <returns>A decompressed byte array.</returns>
#if !NETSTANDARD2_0
	[Obsolete("Use ReadOnlySpan<byte> overload instead for better performance.")]
#endif
	public static byte[] DeflateFrom(this ArraySegment<byte> v)
		=> v.Array.DeflateFrom(v.Offset, v.Count);

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
	/// <returns>A compressed byte array.</returns>
	public static byte[] Do7Zip(this byte[] input)
		=> input.Compress<Lzma.LzmaStream>();

	/// <summary>
	/// Decompresses a 7Zip (LZMA) compressed byte array.
	/// </summary>
	/// <param name="input">The byte array containing 7Zip-compressed data.</param>
	/// <returns>A decompressed byte array.</returns>
	public static byte[] Un7Zip(this byte[] input)
		=> input.Uncompress<Lzma.LzmaStream>();

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

	/// <summary>
	/// Asynchronously compresses a segment of a byte array using the specified compression stream.
	/// </summary>
	/// <typeparam name="TCompressStream">The type of compression stream to use.</typeparam>
	/// <param name="v">The segment of the byte array to compress.</param>
	/// <param name="level">The compression level to use.</param>
	/// <param name="bufferSize">The buffer size to use during compression.</param>
	/// <returns>A task representing the asynchronous operation, with a compressed byte array as the result.</returns>
#if !NETSTANDARD2_0
	[Obsolete("Use ReadOnlyMemory<byte> overload instead for better performance.")]
#endif
	public static Task<byte[]> CompressAsync<TCompressStream>(this ArraySegment<byte> v, CompressionLevel level = CompressionLevel.Optimal, int bufferSize = DefaultBufferSize)
		where TCompressStream : Stream
		=> v.Array.CompressAsync<TCompressStream>(v.Offset, v.Count, level, bufferSize);

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