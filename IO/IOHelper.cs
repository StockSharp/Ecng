namespace Ecng.IO;

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using System.Globalization;
#if !NET5_0_OR_GREATER
using System.Runtime.InteropServices;
#endif

using Ecng.Common;

/// <summary>
/// Provides helper methods for file and directory operations.
/// </summary>
public static class IOHelper
{
	/// <summary>
	/// Converts a relative or partial path to a fully qualified path.
	/// </summary>
	/// <param name="path">The input path.</param>
	/// <returns>The absolute path.</returns>
	public static string ToFullPath(this string path)
	{
		if (path is null)
			throw new ArgumentNullException(nameof(path));

		return Path.GetFullPath(path);
	}

	/// <summary>
	/// Adds a relative segment to the current path and returns the fully qualified path.
	/// </summary>
	/// <param name="path">The base path.</param>
	/// <param name="relativePart">The relative segment to add.</param>
	/// <returns>The combined full path.</returns>
	public static string AddRelative(this string path, string relativePart)
	{
		return Path.Combine(path, relativePart).ToFullPath();
	}

	/// <summary>
	/// Builds an absolute path from a potentially relative path.
	/// </summary>
	/// <param name="path">A relative or absolute path.</param>
	/// <param name="baseDir">The base directory used when <paramref name="path"/> is not rooted.</param>
	/// <returns>
	/// If <paramref name="path"/> is already rooted, the same value is returned;
	/// otherwise the path combined with <paramref name="baseDir"/> is returned.
	/// </returns>
	public static string MakeFullPath(this string path, string baseDir)
		=> Path.IsPathRooted(path) ? path : Path.Combine(baseDir, path);

	/// <summary>
	/// Gets the relative path from a folder to a file.
	/// </summary>
	/// <param name="fileFull">The full file path.</param>
	/// <param name="folder">The base folder.</param>
	/// <returns>The relative file path.</returns>
	public static string GetRelativePath(this string fileFull, string folder)
	{
		var pathUri = new Uri(fileFull);

		if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString()))
			folder += Path.DirectorySeparatorChar;

		var folderUri = new Uri(folder);
		return folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar).DataUnEscape();
	}

	/// <summary>
	/// Gets the available free space on the specified drive.
	/// </summary>
	/// <param name="driveName">The drive name (e.g., "C:").</param>
	/// <returns>The amount of free space in bytes.</returns>
	public static long GetDiskFreeSpace(string driveName)
	{
		return new DriveInfo(driveName).TotalFreeSpace;
	}

	/// <summary>
	/// The %Documents% variable.
	/// </summary>
	public const string DocsVar = "%Documents%";

	/// <summary>
	/// Replaces the %Documents% variable in the path with the actual Documents folder path.
	/// </summary>
	/// <param name="path">The path containing the %Documents% variable.</param>
	/// <returns>The fully qualified path with the Documents folder.</returns>
	public static string ToFullPathIfNeed(this string path)
	{
		if (path is null)
			throw new ArgumentNullException(nameof(path));

		return path.ReplaceIgnoreCase(DocsVar, Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
	}

	/// <summary>
	/// Gets the timestamp of the specified assembly.
	/// </summary>
	/// <param name="assembly">The assembly.</param>
	/// <returns>The timestamp when the assembly was built.</returns>
	public static DateTime GetTimestamp(this Assembly assembly)
	{
		if (assembly is null)
			throw new ArgumentNullException(nameof(assembly));

		return LocalFileSystem.Instance.GetTimestamp(assembly.Location);
	}

	/// <summary>
	/// Writes the specified bytes to a stream.
	/// </summary>
	/// <param name="stream">The output stream.</param>
	/// <param name="bytes">The byte array.</param>
	/// <param name="len">The number of bytes to write.</param>
	/// <param name="pos">The position in the array to start writing from.</param>
	public static void WriteBytes(this Stream stream, byte[] bytes, int len, int pos = 0)
	{
		stream.Write(bytes, pos, len);
	}

	/// <summary>
	/// Reads a specified number of bytes from a stream.
	/// </summary>
	/// <param name="stream">The input stream.</param>
	/// <param name="buffer">The buffer to fill.</param>
	/// <param name="len">The number of bytes to read.</param>
	/// <param name="pos">The position in the buffer to start filling.</param>
	/// <returns>The buffer containing the read bytes.</returns>
	public static byte[] ReadBytes(this Stream stream, byte[] buffer, int len, int pos = 0)
	{
		ReadBytes(stream, buffer.AsMemory(pos, len));
		return buffer;
	}

	/// <summary>
	/// Reads a specified number of bytes from a stream.
	/// </summary>
	/// <param name="stream">The input stream.</param>
	/// <param name="buffer">The buffer to fill.</param>
	public static void ReadBytes(this Stream stream, Memory<byte> buffer)
	{
		if (stream is null)
			throw new ArgumentNullException(nameof(stream));

		if (buffer.IsEmpty)
			return;

		var totalRead = 0;

#if NET5_0_OR_GREATER
		while (totalRead < buffer.Length)
		{
			var read = stream.Read(buffer[totalRead..].Span);

			if (read <= 0)
				throw new IOException($"Stream returned '{read}' bytes. Expected {buffer.Length - totalRead} more bytes.");

			totalRead += read;
		}
#else
		if (MemoryMarshal.TryGetArray<byte>(buffer, out var segment))
		{
			while (totalRead < buffer.Length)
            {
                var read = stream.Read(segment.Array, segment.Offset + totalRead, buffer.Length - totalRead);

                if (read <= 0)
					throw new IOException($"Stream returned '{read}' bytes. Expected {buffer.Length - totalRead} more bytes.");

                totalRead += read;
            }
		}
		else
		{
			var tempBuffer = new byte[Math.Min(8192, buffer.Length)];

            while (totalRead < buffer.Length)
            {
                var bytesToRead = Math.Min(tempBuffer.Length, buffer.Length - totalRead);

                var read = stream.Read(tempBuffer, 0, bytesToRead);

				if (read <= 0)
					throw new IOException($"Stream returned '{read}' bytes. Expected {buffer.Length - totalRead} more bytes.");

                tempBuffer.AsSpan(0, read).CopyTo(buffer.Span.Slice(totalRead));
                totalRead += read;
            }
		}
#endif
	}

	/// <summary>
	/// Enumerates the lines in the stream using the specified encoding.
	/// </summary>
	/// <param name="stream">The stream to read lines from.</param>
	/// <param name="encoding">The encoding to use when reading the stream. Defaults to UTF8 if null.</param>
	/// <param name="leaveOpen">Indicates whether to leave the stream open after reading.</param>
	/// <returns>An enumerable collection of strings, each representing a line from the stream.</returns>
	public static IEnumerable<string> EnumerateLines(this Stream stream, Encoding encoding = null, bool leaveOpen = true)
	{
		if (stream is null)
			throw new ArgumentNullException(nameof(stream));

		using var sr = new StreamReader(stream, encoding ?? Encoding.UTF8, true, -1, leaveOpen);

		while (!sr.EndOfStream)
			yield return sr.ReadLine();
	}

	/// <summary>
	/// Writes an extended representation of the provided object to the stream, prefixing its length.
	/// </summary>
	/// <param name="stream">The stream to write to.</param>
	/// <param name="value">The object to write.</param>
	public static void WriteEx(this Stream stream, object value)
	{
		if (stream is null)
			throw new ArgumentNullException(nameof(stream));

		if (value is null)
			throw new ArgumentNullException(nameof(value));

		if (value is Stream s)
			stream.WriteEx((int)s.Length);
		else if (value is byte[] a1)
			stream.WriteEx(a1.Length);
		else if (value is string str)
			stream.WriteEx(str.Length);

		stream.WriteRaw(value);
	}

	/// <summary>
	/// Writes the raw byte representation of the provided object to the stream.
	/// </summary>
	/// <param name="stream">The stream to write to.</param>
	/// <param name="value">The object to write. Its byte representation will be determined.</param>
	public static void WriteRaw(this Stream stream, object value)
	{
		stream.WriteRaw(value.To<byte[]>());
	}

	/// <summary>
	/// Writes a raw byte array to the stream.
	/// </summary>
	/// <param name="stream">The stream to write to.</param>
	/// <param name="buffer">The byte array to write.</param>
	public static void WriteRaw(this Stream stream, byte[] buffer)
	{
		if (stream is null)
			throw new ArgumentNullException(nameof(stream));

		if (buffer is null)
			throw new ArgumentNullException(nameof(buffer));

		stream.Write(buffer, 0, buffer.Length);
	}

	#region Read

	/// <summary>
	/// Reads an object of type T from the stream.
	/// </summary>
	/// <typeparam name="T">The type of object to read.</typeparam>
	/// <param name="stream">The stream to read from.</param>
	/// <returns>The object read from the stream.</returns>
	public static T Read<T>(this Stream stream)
	{
		return (T)stream.Read(typeof(T));
	}

	/// <summary>
	/// Reads an object of the specified type from the stream.
	/// </summary>
	/// <param name="stream">The stream to read from.</param>
	/// <param name="type">The type of object to read.</param>
	/// <returns>The object read from the stream.</returns>
	public static object Read(this Stream stream, Type type)
	{
		int size;

		if (type == typeof(byte[]) || type == typeof(string) || type == typeof(Stream))
			size = stream.Read<int>();
		else
			size = type.SizeOf();

		return stream.Read(type, size);
	}

	/// <summary>
	/// Reads an object of the specified type from the stream using the provided size.
	/// </summary>
	/// <param name="stream">The stream to read from.</param>
	/// <param name="type">The type of object to read.</param>
	/// <param name="size">The size in bytes to read.</param>
	/// <returns>The object read from the stream.</returns>
	public static object Read(this Stream stream, Type type, int size)
	{
		if (stream is null)
			throw new ArgumentNullException(nameof(stream));

		if (type is null)
			throw new ArgumentNullException(nameof(type));

		if (size < 0)
			throw new ArgumentOutOfRangeException(nameof(size), $"Size has negative value '{size}'.");

		if (size == 0 && !(type == typeof(string) || type == typeof(byte[]) || type == typeof(Stream)))
			throw new ArgumentOutOfRangeException(nameof(size), "Size has zero value.");

		if (type == typeof(string))
			size *= 2;

		if (size > int.MaxValue / 10)
			throw new ArgumentOutOfRangeException(nameof(size), $"Size has too big value {size}.");

		byte[] buffer;

		if (size > 0)
		{
			buffer = new byte[size];
			stream.ReadExactly(buffer, 0, size);
		}
		else
		{
			buffer = [];
		}

		if (type == typeof(byte[]))
			return buffer;
		else
			return buffer.To(type);
	}

	#endregion

	/// <summary>
	/// Truncates the underlying stream used by the StreamWriter by clearing its content.
	/// </summary>
	/// <param name="writer">The StreamWriter whose stream is to be truncated.</param>
	public static void Truncate(this StreamWriter writer)
	{
		if (writer is null)
			throw new ArgumentNullException(nameof(writer));

		writer.Flush();
		writer.BaseStream.SetLength(0);
	}

	/// <summary>
	/// Gets the actual buffer of the MemoryStream that contains the written data.
	/// </summary>
	/// <param name="stream">The MemoryStream to retrieve the buffer from.</param>
	/// <returns>An ArraySegment containing the actual data from the MemoryStream.</returns>
#if !NETSTANDARD2_0
	[Obsolete("Use Span/Memory overloads instead.")]
#endif
	public static ArraySegment<byte> GetActualBuffer(this MemoryStream stream)
	{
		if (stream is null)
			throw new ArgumentNullException(nameof(stream));

		return new(stream.GetBuffer(), 0, (int)stream.Position);
	}

	/// <summary>
	/// Normalizes the provided file path for comparison purposes without converting to lowercase.
	/// </summary>
	/// <param name="path">The file path to normalize.</param>
	/// <returns>The normalized file path, or null if the input is empty or whitespace.</returns>
	public static string NormalizePathNoLowercase(this string path)
	{
		if (path.IsEmptyOrWhiteSpace())
			return null;

		path = Path.GetFullPath(path);

		return Path.GetFullPath(new Uri(path).LocalPath)
					.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
					.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
	}

	/// <summary>
	/// Normalizes the provided file path for comparison purposes and converts it to lowercase.
	/// Uses invariant culture for case conversion to ensure consistent behavior across all locales.
	/// </summary>
	/// <param name="path">The file path to normalize.</param>
	/// <param name="culture">Ignored. Kept for backward compatibility. Always uses invariant culture.</param>
	/// <returns>The normalized and lowercased file path.</returns>
	public static string NormalizePath(this string path, CultureInfo culture = null)
	{
		// Always use invariant culture for path normalization to avoid issues with Turkish I/i
		// and other culture-specific case mappings that break file path comparisons.
		return path.NormalizePathNoLowercase()?.ToLowerInvariant();
	}

	/// <summary>
	/// Compares two file paths for equality after normalization.
	/// </summary>
	/// <param name="path1">The first file path to compare.</param>
	/// <param name="path2">The second file path to compare.</param>
	/// <returns>True if both paths are equal; otherwise, false.</returns>
	public static bool IsPathsEqual(string path1, string path2) => path1.NormalizePath() == path2.NormalizePath();

}
