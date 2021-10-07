namespace Ecng.Common
{
	using System;
	using System.Threading.Tasks;
	using System.Collections.Generic;
	using System.Threading;
	using System.Linq;
	using System.Diagnostics;
	using System.IO;
	using System.Reflection;
	using System.ComponentModel;
	using System.Runtime.InteropServices;

	using Nito.AsyncEx;

	public static class IOHelper
	{
		public static DirectoryInfo ClearDirectory(string releasePath, Func<string, bool> filter = null)
			=> AsyncContext.Run(() => ClearDirectoryAsync(releasePath, filter));
		
		public static Task<DirectoryInfo> ClearDirectoryAsync(string releasePath, Func<string, bool> filter = null, CancellationToken cancellationToken = default)
		{
			var releaseDir = new DirectoryInfo(releasePath);

			foreach (var file in releaseDir.EnumerateFiles())
			{
				if (filter != null && !filter(file.FullName))
					continue;

				file.Delete();

				cancellationToken.ThrowIfCancellationRequested();
			}

			foreach (var dir in releaseDir.EnumerateDirectories())
			{
				dir.Delete(true);

				cancellationToken.ThrowIfCancellationRequested();
			}

			return Task.FromResult(releaseDir);
		}

		public static void CopyDirectory(string sourcePath, string destPath)
			=> AsyncContext.Run(() => CopyDirectoryAsync(sourcePath, destPath));

		public static Task CopyDirectoryAsync(string sourcePath, string destPath, CancellationToken cancellationToken = default)
		{
			Directory.CreateDirectory(destPath);

			foreach (var fileName in Directory.GetFiles(sourcePath))
			{
				CopyAndMakeWritable(fileName, destPath);

				cancellationToken.ThrowIfCancellationRequested();
			}

			foreach (var directory in Directory.GetDirectories(sourcePath))
			{
				CopyDirectory(directory, Path.Combine(destPath, Path.GetFileName(directory)));

				cancellationToken.ThrowIfCancellationRequested();
			}

			return Task.CompletedTask;
		}

		public static string CopyAndMakeWritable(string fileName, string destPath)
		{
			var destFile = Path.Combine(destPath, Path.GetFileName(fileName));

			File.Copy(fileName, destFile, true);
			new FileInfo(destFile).IsReadOnly = false;

			return destFile;
		}

		public static string ToFullPath(this string path)
		{
			if (path is null)
				throw new ArgumentNullException(nameof(path));

			return Path.GetFullPath(path);
		}

		public static string AddRelative(this string path, string relativePart)
		{
			return (path + relativePart).ToFullPath();
		}

		public static int Execute(string fileName, string arg, Action<string> output, Action<string> error, Action<ProcessStartInfo> infoHandler = null, TimeSpan waitForExit = default, string stdInput = null, ProcessPriorityClass? priority = null)
		{
			var source = new CancellationTokenSource();

			if (waitForExit != default)
				source.CancelAfter(waitForExit);

			return AsyncContext.Run(() => ExecuteAsync(fileName, arg, output, error, infoHandler, stdInput, priority, source.Token));
		}

		public static async Task<int> ExecuteAsync(string fileName, string arg, Action<string> output, Action<string> error, Action<ProcessStartInfo> infoHandler = null, string stdInput = null, ProcessPriorityClass? priority = null, CancellationToken cancellationToken = default)
		{
			if (output is null)
				throw new ArgumentNullException(nameof(output));

			if (error is null)
				throw new ArgumentNullException(nameof(error));

			var input = !stdInput.IsEmpty();

			var procInfo = new ProcessStartInfo(fileName, arg)
			{
				UseShellExecute = false,
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				RedirectStandardInput = input,
				CreateNoWindow = true,
				WindowStyle = ProcessWindowStyle.Hidden
			};

			infoHandler?.Invoke(procInfo);

			using var process = new Process
			{
				EnableRaisingEvents = true,
				StartInfo = procInfo
			};

			if (priority is not null)
				process.PriorityClass = priority.Value;

			process.Start();

			var locker = new object();

			if (input)
			{
				process.StandardInput.WriteLine(stdInput);
				process.StandardInput.Close();
			}

			async Task ReadProcessOutput(TextReader reader, Action<string> action)
			{
				do
				{
					var str = await reader.ReadLineAsync().WithCancellation(cancellationToken);
					if (str is null)
						break;

					if (!str.IsEmptyOrWhiteSpace())
					{
						lock (locker)
							action(str);
					}

					cancellationToken.ThrowIfCancellationRequested();
				}
				while (true);
			}

			var task1 = ReadProcessOutput(process.StandardOutput, output);
			var task2 = ReadProcessOutput(process.StandardError, error);

			await task1;
			await task2;

			await process.WaitForExitAsync(cancellationToken);

			return process.ExitCode;
		}

		public static bool CreateDirIfNotExists(this string fullPath)
		{
			var directory = Path.GetDirectoryName(fullPath);

			if (directory.IsEmpty() || Directory.Exists(directory))
				return false;

			Directory.CreateDirectory(directory);
			return true;
		}

		private static readonly string[] _suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB

		public static string ToHumanReadableFileSize(this long byteCount)
		{
			int place;
			int num;

			if (byteCount == 0)
			{
				num = 0;
				place = 0;
			}
			else
			{
				var bytes = byteCount.Abs();
				place = (int)Math.Log(bytes, 1024).Floor();
				num = (int)(Math.Sign(byteCount) * Math.Round(bytes / Math.Pow(1024, place), 1));
			}

			return num + " " + _suf[place];
		}

		public static void SafeDeleteDir(this string path)
		{
			if (!Directory.Exists(path))
				return;

			Directory.Delete(path, true);
		}

		public static string CreateTempDir()
		{
			var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString().Remove("-"));

			if (!Directory.Exists(path))
				Directory.CreateDirectory(path);

			return path;
		}

		public static bool CheckInstallation(string path)
		{
			if (path.IsEmpty())
				return false;

			if (!Directory.Exists(path))
				return false;

			var files = Directory.GetFiles(path);
			var directories = Directory.GetDirectories(path);
			return files.Any() || directories.Any();
		}

		public static string GetRelativePath(this string fileFull, string folder)
		{
			var pathUri = new Uri(fileFull);

			if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString()))
				folder += Path.DirectorySeparatorChar;

			var folderUri = new Uri(folder);
			return folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar).DataUnEscape();
		}

		public static long GetDiskFreeSpace(string driveName)
		{
			return new DriveInfo(driveName).TotalFreeSpace;
		}

		public static void CreateFile(string rootPath, string relativePath, string fileName, byte[] content)
		{
			if (relativePath.IsEmpty())
			{
				File.WriteAllBytes(Path.Combine(rootPath, fileName), content);
			}
			else
			{
				var fullPath = Path.Combine(rootPath, relativePath, fileName);
				var fileInfo = new FileInfo(fullPath);
				fileInfo.Directory.Create();
				File.WriteAllBytes(fullPath, content);
			}
		}

		// https://stackoverflow.com/a/2811746/8029915
		public static void DeleteEmptyDirs(string dir)
		{
			if (dir.IsEmpty())
				throw new ArgumentNullException(nameof(dir));

			try
			{
				foreach (var d in Directory.EnumerateDirectories(dir))
				{
					DeleteEmptyDirs(d);
				}

				var entries = Directory.EnumerateFileSystemEntries(dir);

				if (!entries.Any())
				{
					try
					{
						Directory.Delete(dir);
					}
					catch (UnauthorizedAccessException) { }
					catch (DirectoryNotFoundException) { }
				}
			}
			catch (UnauthorizedAccessException) { }
		}

		public static string ToFullPathIfNeed(this string path)
		{
			if (path is null)
				throw new ArgumentNullException(nameof(path));

			return path.ReplaceIgnoreCase("%Documents%", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
		}

		// http://social.msdn.microsoft.com/Forums/eu/windowssearch/thread/55582d9d-77ea-47d9-91ce-cff7ca7ef528
		public static bool BlockDeleteDir(string dir, bool isRecursive = false, int iterCount = 1000, int sleep = 0)
		{
			if (isRecursive)
			{
				// https://stackoverflow.com/a/329502/8029915

				var files = Directory.GetFiles(dir);
				var dirs = Directory.GetDirectories(dir);

				foreach (var file in files)
				{
					File.SetAttributes(file, FileAttributes.Normal);
					File.Delete(file);
				}

				foreach (var sub in dirs)
				{
					BlockDeleteDir(sub, true, iterCount, sleep);
				}
			}

			// https://stackoverflow.com/a/1703799/8029915

			try
			{
				Directory.Delete(dir, false);
			}
			catch (IOException)
			{
				Directory.Delete(dir, false);
			}
			catch (UnauthorizedAccessException)
			{
				Directory.Delete(dir, false);
			}

			var limit = iterCount;

			while (Directory.Exists(dir) && limit-- > 0)
				Thread.Sleep(sleep);

			return Directory.Exists(dir);
		}

		public static bool OpenLink(this string url, bool raiseError)
		{
			if (url.IsEmpty())
				throw new ArgumentNullException(nameof(url));

			// https://stackoverflow.com/a/21836079

			try
			{
				// https://github.com/dotnet/wpf/issues/2566

				var procInfo = new ProcessStartInfo(url)
				{
					UseShellExecute = true,
				};

				Process.Start(procInfo);
				return true;
			}
			catch (Win32Exception)
			{
				try
				{
					var launcher = url.StartsWithIgnoreCase("http") ? "IExplore.exe" : "explorer.exe";
					Process.Start(launcher, url);
					return true;
				}
				catch
				{
					if (raiseError)
						throw;

					return false;
				}
			}
		}

		public static IEnumerable<string> GetDirectories(string path,
			string searchPattern = "*",
			SearchOption searchOption = SearchOption.TopDirectoryOnly)
		{
			return !Directory.Exists(path)
				? Enumerable.Empty<string>()
				: Directory.EnumerateDirectories(path, searchPattern, searchOption);
		}

		public static DateTime GetTimestamp(this Assembly assembly)
		{
			if (assembly is null)
				throw new ArgumentNullException(nameof(assembly));

			return GetTimestamp(assembly.Location);
		}

		public static DateTime GetTimestamp(string filePath)
		{
			var b = new byte[2048];

			using (var s = new FileStream(filePath, FileMode.Open, FileAccess.Read))
				s.Read(b, 0, b.Length);

			const int peHeaderOffset = 60;
			const int linkerTimestampOffset = 8;
			var i = BitConverter.ToInt32(b, peHeaderOffset);
			var secondsSince1970 = (long)BitConverter.ToInt32(b, i + linkerTimestampOffset);

			return secondsSince1970.FromUnix().ToLocalTime();
		}

		public static bool IsDirectory(this string path) => File.GetAttributes(path).HasFlag(FileAttributes.Directory);

		public static void WriteBytes(this Stream stream, byte[] bytes, int len, int pos = 0)
		{
			stream.Write(bytes, pos, len);
		}

		public static byte[] ReadBytes(this Stream stream, byte[] buffer, int len, int pos = 0)
		{
			var left = len;

			while (left > 0)
			{
				var read = stream.Read(buffer, pos + (len - left), left);

				if (read <= 0)
					throw new IOException("Stream returned '{0}' bytes.".Put(read));

				left -= read;
			}

			return buffer;
		}

		public static byte ReadByteEx(this Stream stream, byte[] buffer)
		{
			return stream.ReadBytes(buffer, 1)[0];
		}

		public static void WriteByteEx(this Stream stream, byte[] buffer, byte value)
		{
			buffer[0] = value;
			stream.Write(buffer, 0, 1);
		}

		public static unsafe void WriteShort(this Stream stream, byte[] buffer, short value, bool isLittleEndian)
		{
			fixed (byte* b = buffer)
				*((short*)b) = value;

			stream.WriteBytes(buffer.ChangeOrder(2, isLittleEndian), 2);
		}

		public static short ReadShort(this Stream stream, byte[] buffer, bool isLittleEndian)
		{
			return BitConverter.ToInt16(stream.ReadBytes(buffer, 2).ChangeOrder(2, isLittleEndian), 0);
		}

		[CLSCompliant(false)]
		public static unsafe void WriteUShort(this Stream stream, byte[] buffer, ushort value, bool isLittleEndian)
		{
			fixed (byte* b = buffer)
				*((ushort*)b) = value;

			stream.WriteBytes(buffer.ChangeOrder(2, isLittleEndian), 2);
		}

		[CLSCompliant(false)]
		public static ushort ReadUShort(this Stream stream, byte[] buffer, bool isLittleEndian)
		{
			return BitConverter.ToUInt16(stream.ReadBytes(buffer, 2).ChangeOrder(2, isLittleEndian), 0);
		}

		public static unsafe void WriteInt(this Stream stream, byte[] buffer, int value, bool isLittleEndian)
		{
			fixed (byte* b = buffer)
				*((int*)b) = value;

			stream.WriteBytes(buffer.ChangeOrder(4, isLittleEndian), 4);
		}

		public static int ReadInt(this Stream stream, byte[] buffer, bool isLittleEndian)
		{
			return BitConverter.ToInt32(stream.ReadBytes(buffer, 4).ChangeOrder(4, isLittleEndian), 0);
		}

		[CLSCompliant(false)]
		public static unsafe void WriteUInt(this Stream stream, byte[] buffer, uint value, bool isLittleEndian)
		{
			fixed (byte* b = buffer)
				*((uint*)b) = value;

			stream.WriteBytes(buffer.ChangeOrder(4, isLittleEndian), 4);
		}

		[CLSCompliant(false)]
		public static uint ReadUInt(this Stream stream, byte[] buffer, bool isLittleEndian)
		{
			return BitConverter.ToUInt32(stream.ReadBytes(buffer, 4).ChangeOrder(4, isLittleEndian), 0);
		}

		public static unsafe void WriteLong(this Stream stream, byte[] buffer, long value, bool isLittleEndian, int len = 8)
		{
			fixed (byte* b = buffer)
				*((long*)b) = value;

			stream.WriteBytes(buffer.ChangeOrder(len, isLittleEndian), 8);
		}

		public static long ReadLong(this Stream stream, byte[] buffer, bool isLittleEndian, int len = 8)
		{
			return BitConverter.ToInt64(stream.ReadBytes(buffer, len).ChangeOrder(len, isLittleEndian), 0);
		}

		[CLSCompliant(false)]
		public static unsafe void WriteULong(this Stream stream, byte[] buffer, ulong value, bool isLittleEndian, int len = 8)
		{
			fixed (byte* b = buffer)
				*((ulong*)b) = value;

			stream.WriteBytes(buffer.ChangeOrder(len, isLittleEndian), 8);
		}

		[CLSCompliant(false)]
		public static ulong ReadULong(this Stream stream, byte[] buffer, bool isLittleEndian, int len = 8)
		{
			return BitConverter.ToUInt64(stream.ReadBytes(buffer, len).ChangeOrder(4, isLittleEndian), 0);
		}

		public static void CopySync(this Stream source, Stream destination, int count)
		{
			if (source is null)
				throw new ArgumentNullException(nameof(source));

			if (destination is null)
				throw new ArgumentNullException(nameof(destination));

			if (count < 0)
				throw new ArgumentOutOfRangeException(nameof(count));

			var buffer = new byte[count.Min(short.MaxValue)];
			int read;

			while (count > 0 && (read = source.Read(buffer, 0, buffer.Length.Min(count))) > 0)
			{
				destination.Write(buffer, 0, read);
				count -= read;
			}
		}

		public static async Task CopyAsync(this Stream source, Stream destination, long skip, long? count, Action<int> progress, CancellationToken cancellationToken = default)
		{
			if (source is null)
				throw new ArgumentNullException(nameof(source));

			if (destination is null)
				throw new ArgumentNullException(nameof(destination));

			if (count < 0)
				throw new ArgumentOutOfRangeException(nameof(count));

			var buffer = new byte[4096];
			int offset;

			checked
			{
				offset = (int)skip;
			}

			var left = count;

			while (!cancellationToken.IsCancellationRequested)
			{
				var need = left is null ? buffer.Length : (int)left.Value.Min(buffer.Length);
				var len = await source.ReadAsync(buffer, offset, need, cancellationToken);

				if (left != null)
					left -= len;

				await destination.WriteAsync(buffer, offset, len, cancellationToken);

				// TODO
				progress?.Invoke(0);
			}

			if (!cancellationToken.IsCancellationRequested)
				progress?.Invoke(100);
		}

		public static byte[] ReadBuffer(this Stream stream)
		{
			if (stream is null)
				throw new ArgumentNullException(nameof(stream));

			return stream.ReadBuffer((int)(stream.Length - stream.Position));
		}

		public static byte[] ReadBuffer(this Stream stream, int size)
		{
			if (stream is null)
				throw new ArgumentNullException(nameof(stream));

			if (size < 0)
				throw new ArgumentOutOfRangeException(nameof(size), "Size has negative value '{0}'.".Put(size));

			var buffer = new byte[size];

			if (size == 1)
			{
				var b = stream.ReadByte();

				if (b == -1)
					throw new ArgumentException("Insufficient stream size '{0}'.".Put(size), nameof(stream));

				buffer[0] = (byte)b;
			}
			else
			{
				var offset = 0;
				do
				{
					var readBytes = stream.Read(buffer, offset, size - offset);

					if (readBytes == 0)
						throw new ArgumentException("Insufficient stream size '{0}'.".Put(size), nameof(stream));

					offset += readBytes;
				}
				while (offset < size);
			}

			return buffer;
		}

		public static IEnumerable<string> ReadLines(this Stream stream)
		{
			if (stream is null)
				throw new ArgumentNullException(nameof(stream));

			using (var reader = new StreamReader(stream))
			{
				while (reader.Peek() >= 0)
				{
					yield return reader.ReadLine();
				}
			}
		}

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

		public static void WriteRaw(this Stream stream, object value)
		{
			stream.WriteRaw(value.To<byte[]>());
		}

		public static void WriteRaw(this Stream stream, byte[] buffer)
		{
			if (stream is null)
				throw new ArgumentNullException(nameof(stream));

			if (buffer is null)
				throw new ArgumentNullException(nameof(buffer));

			stream.Write(buffer, 0, buffer.Length);
		}

		#region Read

		public static T Read<T>(this Stream stream)
		{
			return (T)stream.Read(typeof(T));
		}

		public static object Read(this Stream stream, Type type)
		{
			int size;

			if (type == typeof(byte[]) || type == typeof(string) || type == typeof(Stream))
				size = stream.Read<int>();
			else
				size = type.SizeOf();

			return stream.Read(type, size);
		}

		public static object Read(this Stream stream, Type type, int size)
		{
			if (stream is null)
				throw new ArgumentNullException(nameof(stream));

			if (type is null)
				throw new ArgumentNullException(nameof(type));

			if (size < 0)
				throw new ArgumentOutOfRangeException(nameof(size), "Size has negative value '{0}'.".Put(size));

			if (size == 0 && !(type == typeof(string) || type == typeof(byte[]) || type == typeof(Stream)))
				throw new ArgumentOutOfRangeException(nameof(size), "Size has zero value.");

			if (type == typeof(string))
				size *= 2;

			if (size > int.MaxValue / 10)
				throw new ArgumentOutOfRangeException(nameof(size), "Size has too big value {0}.".Put(size));

			var buffer = size > 0 ? stream.ReadBuffer(size) : Array.Empty<byte>();

			if (type == typeof(byte[]))
				return buffer;
			else
				return buffer.To(type);
		}

		#endregion

		/// <summary>
		/// Returns the size of an unmanaged type in bytes.
		/// </summary>
		/// <typeparam name="T">The Type whose size is to be returned.</typeparam>
		/// <returns>The size of the structure parameter in unmanaged code.</returns>
		public static int SizeOf<T>()
		{
			return SizeOf(typeof(T));
		}

		/// <summary>
		/// Returns the size of an unmanaged type in bytes.
		/// </summary>
		/// <param name="type">The Type whose size is to be returned.</param>
		/// <returns>The size of the structure parameter in unmanaged code.</returns>
		public static int SizeOf(this Type type)
		{
			if (type.IsDateTime())
				type = typeof(long);
			else if (type == typeof(TimeSpan))
				type = typeof(long);
			else if (type.IsEnum())
				type = type.GetEnumBaseType();
			else if (type == typeof(bool))
				type = typeof(byte);
			else if (type == typeof(char))
				type = typeof(short);

			return Marshal.SizeOf(type);
		}

		public static Stream Save(this Stream stream, string fileName)
		{
			var pos = stream.CanSeek ? stream.Position : 0;

			using (var file = File.Create(fileName))
				stream.CopyTo(file);

			if (stream.CanSeek)
				stream.Position = pos;

			return stream;
		}

		public static byte[] Save(this byte[] data, string fileName)
		{
			data.To<Stream>().Save(fileName);
			return data;
		}

		public static bool TrySave(this byte[] data, string fileName, Action<Exception> errorHandler)
		{
			if (errorHandler is null)
				throw new ArgumentNullException(nameof(errorHandler));

			try
			{
				data.To<Stream>().Save(fileName);
				return true;
			}
			catch (Exception e)
			{
				errorHandler(e);
				return false;
			}
		}

		public static void Truncate(this StreamWriter writer)
		{
			if (writer is null)
				throw new ArgumentNullException(nameof(writer));

			writer.Flush();
			writer.BaseStream.SetLength(0);
		}
	}
}