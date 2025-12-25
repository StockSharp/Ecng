namespace Ecng.Common;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Extension methods for <see cref="IFileSystem"/>.
/// </summary>
public static class FileSystemExtensions
{
	/// <summary>
	/// Reads all text from a file.
	/// </summary>
	/// <param name="fs">File system.</param>
	/// <param name="path">Path to the file.</param>
	/// <param name="encoding">Text encoding. Defaults to UTF8.</param>
	/// <returns>File contents as string.</returns>
	public static string ReadAllText(this IFileSystem fs, string path, Encoding encoding = null)
	{
		encoding ??= Encoding.UTF8;

		using var stream = fs.OpenRead(path);
		using var reader = new StreamReader(stream, encoding);

		return reader.ReadToEnd();
	}

	/// <summary>
	/// Reads all text from a file asynchronously.
	/// </summary>
	/// <param name="fs">File system.</param>
	/// <param name="path">Path to the file.</param>
	/// <param name="encoding">Text encoding. Defaults to UTF8.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>File contents as string.</returns>
	public static async Task<string> ReadAllTextAsync(this IFileSystem fs, string path, Encoding encoding = null, CancellationToken cancellationToken = default)
	{
		encoding ??= Encoding.UTF8;

		using var stream = fs.OpenRead(path);
		using var reader = new StreamReader(stream, encoding);

#if NETSTANDARD2_0
		cancellationToken.ThrowIfCancellationRequested();
		return await reader.ReadToEndAsync();
#else
		return await reader.ReadToEndAsync(cancellationToken);
#endif
	}

	/// <summary>
	/// Writes text to a file.
	/// </summary>
	/// <param name="fs">File system.</param>
	/// <param name="path">Path to the file.</param>
	/// <param name="content">Text content.</param>
	/// <param name="encoding">Text encoding. Defaults to UTF8.</param>
	public static void WriteAllText(this IFileSystem fs, string path, string content, Encoding encoding = null)
	{
		encoding ??= Encoding.UTF8;

		using var stream = fs.OpenWrite(path);
		using var writer = new StreamWriter(stream, encoding);

		writer.Write(content);
	}

	/// <summary>
	/// Writes text to a file asynchronously.
	/// </summary>
	/// <param name="fs">File system.</param>
	/// <param name="path">Path to the file.</param>
	/// <param name="content">Text content.</param>
	/// <param name="encoding">Text encoding. Defaults to UTF8.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	public static async Task WriteAllTextAsync(this IFileSystem fs, string path, string content, Encoding encoding = null, CancellationToken cancellationToken = default)
	{
		encoding ??= Encoding.UTF8;

		using var stream = fs.OpenWrite(path);
		using var writer = new StreamWriter(stream, encoding);

#if NETSTANDARD2_0
		cancellationToken.ThrowIfCancellationRequested();
		await writer.WriteAsync(content);
		await writer.FlushAsync();
#else
		await writer.WriteAsync(content.AsMemory(), cancellationToken);
		await writer.FlushAsync(cancellationToken);
#endif
	}

	/// <summary>
	/// Appends text to a file.
	/// </summary>
	/// <param name="fs">File system.</param>
	/// <param name="path">Path to the file.</param>
	/// <param name="content">Text content.</param>
	/// <param name="encoding">Text encoding. Defaults to UTF8.</param>
	public static void AppendAllText(this IFileSystem fs, string path, string content, Encoding encoding = null)
	{
		encoding ??= Encoding.UTF8;

		using var stream = fs.OpenWrite(path, append: true);
		using var writer = new StreamWriter(stream, encoding);

		writer.Write(content);
	}

	/// <summary>
	/// Appends text to a file asynchronously.
	/// </summary>
	/// <param name="fs">File system.</param>
	/// <param name="path">Path to the file.</param>
	/// <param name="content">Text content.</param>
	/// <param name="encoding">Text encoding. Defaults to UTF8.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	public static async Task AppendAllTextAsync(this IFileSystem fs, string path, string content, Encoding encoding = null, CancellationToken cancellationToken = default)
	{
		encoding ??= Encoding.UTF8;

		using var stream = fs.OpenWrite(path, append: true);
		using var writer = new StreamWriter(stream, encoding);

#if NETSTANDARD2_0
		cancellationToken.ThrowIfCancellationRequested();
		await writer.WriteAsync(content);
		await writer.FlushAsync();
#else
		await writer.WriteAsync(content.AsMemory(), cancellationToken);
		await writer.FlushAsync(cancellationToken);
#endif
	}

	/// <summary>
	/// Reads all bytes from a file.
	/// </summary>
	/// <param name="fs">File system.</param>
	/// <param name="path">Path to the file.</param>
	/// <returns>File contents as byte array.</returns>
	public static byte[] ReadAllBytes(this IFileSystem fs, string path)
	{
		using var stream = fs.OpenRead(path);
		using var ms = new MemoryStream();

		stream.CopyTo(ms);

		return ms.ToArray();
	}

	/// <summary>
	/// Reads all bytes from a file asynchronously.
	/// </summary>
	/// <param name="fs">File system.</param>
	/// <param name="path">Path to the file.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>File contents as byte array.</returns>
	public static async Task<byte[]> ReadAllBytesAsync(this IFileSystem fs, string path, CancellationToken cancellationToken = default)
	{
		using var stream = fs.OpenRead(path);
		using var ms = new MemoryStream();

#if NETSTANDARD2_0
		cancellationToken.ThrowIfCancellationRequested();
		await stream.CopyToAsync(ms);
#else
		await stream.CopyToAsync(ms, cancellationToken);
#endif

		return ms.ToArray();
	}

	/// <summary>
	/// Writes bytes to a file.
	/// </summary>
	/// <param name="fs">File system.</param>
	/// <param name="path">Path to the file.</param>
	/// <param name="bytes">Byte content.</param>
	public static void WriteAllBytes(this IFileSystem fs, string path, byte[] bytes)
	{
		using var stream = fs.OpenWrite(path);

		stream.Write(bytes, 0, bytes.Length);
	}

	/// <summary>
	/// Writes bytes to a file asynchronously.
	/// </summary>
	/// <param name="fs">File system.</param>
	/// <param name="path">Path to the file.</param>
	/// <param name="bytes">Byte content.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	public static async Task WriteAllBytesAsync(this IFileSystem fs, string path, byte[] bytes, CancellationToken cancellationToken = default)
	{
		using var stream = fs.OpenWrite(path);

#if NETSTANDARD2_0
		cancellationToken.ThrowIfCancellationRequested();
		await stream.WriteAsync(bytes, 0, bytes.Length);
#else
		await stream.WriteAsync(bytes, cancellationToken);
#endif
	}

	/// <summary>
	/// Reads all lines from a file.
	/// </summary>
	/// <param name="fs">File system.</param>
	/// <param name="path">Path to the file.</param>
	/// <param name="encoding">Text encoding. Defaults to UTF8.</param>
	/// <returns>File contents as string array.</returns>
	public static string[] ReadAllLines(this IFileSystem fs, string path, Encoding encoding = null)
	{
		encoding ??= Encoding.UTF8;

		using var stream = fs.OpenRead(path);
		using var reader = new StreamReader(stream, encoding);

		var lines = new List<string>();

		while (reader.ReadLine() is { } line)
			lines.Add(line);

		return [.. lines];
	}

	/// <summary>
	/// Reads all lines from a file asynchronously.
	/// </summary>
	/// <param name="fs">File system.</param>
	/// <param name="path">Path to the file.</param>
	/// <param name="encoding">Text encoding. Defaults to UTF8.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>File contents as string array.</returns>
	public static async Task<string[]> ReadAllLinesAsync(this IFileSystem fs, string path, Encoding encoding = null, CancellationToken cancellationToken = default)
	{
		encoding ??= Encoding.UTF8;

		using var stream = fs.OpenRead(path);
		using var reader = new StreamReader(stream, encoding);

		var lines = new List<string>();

#if NETSTANDARD2_0
		string line;
		while ((line = await reader.ReadLineAsync()) != null)
		{
			cancellationToken.ThrowIfCancellationRequested();
			lines.Add(line);
		}
#else
		while (await reader.ReadLineAsync(cancellationToken) is { } line)
			lines.Add(line);
#endif

		return [.. lines];
	}

	/// <summary>
	/// Writes lines to a file.
	/// </summary>
	/// <param name="fs">File system.</param>
	/// <param name="path">Path to the file.</param>
	/// <param name="lines">Lines to write.</param>
	/// <param name="encoding">Text encoding. Defaults to UTF8.</param>
	public static void WriteAllLines(this IFileSystem fs, string path, IEnumerable<string> lines, Encoding encoding = null)
	{
		encoding ??= Encoding.UTF8;

		using var stream = fs.OpenWrite(path);
		using var writer = new StreamWriter(stream, encoding);

		foreach (var line in lines)
			writer.WriteLine(line);
	}

	/// <summary>
	/// Writes lines to a file asynchronously.
	/// </summary>
	/// <param name="fs">File system.</param>
	/// <param name="path">Path to the file.</param>
	/// <param name="lines">Lines to write.</param>
	/// <param name="encoding">Text encoding. Defaults to UTF8.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	public static async Task WriteAllLinesAsync(this IFileSystem fs, string path, IEnumerable<string> lines, Encoding encoding = null, CancellationToken cancellationToken = default)
	{
		encoding ??= Encoding.UTF8;

		using var stream = fs.OpenWrite(path);
		using var writer = new StreamWriter(stream, encoding);

		foreach (var line in lines)
		{
#if NETSTANDARD2_0
			cancellationToken.ThrowIfCancellationRequested();
			await writer.WriteLineAsync(line);
#else
			await writer.WriteLineAsync(line.AsMemory(), cancellationToken);
#endif
		}

#if NETSTANDARD2_0
		await writer.FlushAsync();
#else
		await writer.FlushAsync(cancellationToken);
#endif
	}
}
