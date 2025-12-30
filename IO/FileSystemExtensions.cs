namespace Ecng.IO;

#if NET10_0_OR_GREATER
using System;
#endif
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
	/// Opens a readable stream for the file at the specified path.
	/// </summary>
	/// <param name="fs">File system.</param>
	/// <param name="path">Path to the file.</param>
	/// <returns>A read-only stream.</returns>
	public static Stream OpenRead(this IFileSystem fs, string path)
		=> fs.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);

	/// <summary>
	/// Opens a writable stream for the file at the specified path.
	/// </summary>
	/// <param name="fs">File system.</param>
	/// <param name="path">Path to the file.</param>
	/// <param name="append">If true, appends to the file; otherwise overwrites.</param>
	/// <returns>A write-capable stream.</returns>
	public static Stream OpenWrite(this IFileSystem fs, string path, bool append = false)
		=> fs.Open(path, append ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.None);

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

		return await reader.ReadToEndAsync(cancellationToken);
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

		await writer.WriteAsync(content
#if NET10_0_OR_GREATER
			.AsMemory()
#endif
			, cancellationToken);
		await writer.FlushAsync(cancellationToken);
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

		await writer.WriteAsync(content
#if NET10_0_OR_GREATER
			.AsMemory()
#endif
			, cancellationToken);
		await writer.FlushAsync(cancellationToken);
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

		await stream.CopyToAsync(ms, cancellationToken);

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

		await stream.WriteAsync(bytes, cancellationToken);
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

		while (await reader.ReadLineAsync(cancellationToken) is { } line)
			lines.Add(line);

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
			await writer.WriteLineAsync(line
#if NET10_0_OR_GREATER
				.AsMemory()
#endif
				, cancellationToken);
		}

		await writer.FlushAsync(cancellationToken);
	}
}
