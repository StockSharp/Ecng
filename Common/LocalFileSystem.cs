namespace Ecng.Common;

using System;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Implementation of <see cref="IFileSystem"/> over the local file system (<see cref="System.IO"/>).
/// </summary>
public class LocalFileSystem : IFileSystem
{
	/// <inheritdoc />
	public bool FileExists(string path) => File.Exists(path);
	/// <inheritdoc />
	public bool DirectoryExists(string path) => Directory.Exists(path);

	/// <inheritdoc />
	public Stream Open(string path, FileMode mode, FileAccess access = FileAccess.ReadWrite)
		=> File.Open(path, mode, access, access == FileAccess.Read ? FileShare.Read : FileShare.None);

	/// <inheritdoc />
	public void CreateDirectory(string path) => Directory.CreateDirectory(path);
	/// <inheritdoc />
	public void DeleteDirectory(string path, bool recursive = false) => Directory.Delete(path, recursive);

	/// <inheritdoc />
	public void DeleteFile(string path) => File.Delete(path);

	/// <inheritdoc />
	public void MoveFile(string sourceFileName, string destFileName, bool overwrite = false)
	{
		var dir = Path.GetDirectoryName(destFileName);

		if (!dir.IsEmpty() && !Directory.Exists(dir))
			Directory.CreateDirectory(dir);

		if (overwrite && File.Exists(destFileName))
			File.Delete(destFileName);

		File.Move(sourceFileName, destFileName);
	}

	/// <inheritdoc />
	public void MoveDirectory(string sourceDirName, string destDirName)
	{
		var parentDir = Path.GetDirectoryName(destDirName);

		if (!parentDir.IsEmpty() && !Directory.Exists(parentDir))
			Directory.CreateDirectory(parentDir);

		Directory.Move(sourceDirName, destDirName);
	}

	/// <inheritdoc />
	public void CopyFile(string sourceFileName, string destFileName, bool overwrite = false)
	{
		var dir = Path.GetDirectoryName(destFileName);

		if (!dir.IsEmpty() && !Directory.Exists(dir))
			Directory.CreateDirectory(dir);

		File.Copy(sourceFileName, destFileName, overwrite);
	}

	/// <inheritdoc />
	public IEnumerable<string> EnumerateFiles(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
		=> Directory.EnumerateFiles(path, searchPattern, searchOption);

	/// <inheritdoc />
	public IEnumerable<string> EnumerateDirectories(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
		=> Directory.EnumerateDirectories(path, searchPattern, searchOption);

	/// <inheritdoc />
	public DateTime GetCreationTimeUtc(string path) => File.GetCreationTimeUtc(path);
	/// <inheritdoc />
	public DateTime GetLastWriteTimeUtc(string path) => File.GetLastWriteTimeUtc(path);
	/// <inheritdoc />
	public long GetFileLength(string path) => new FileInfo(path).Length;
}