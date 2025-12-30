namespace Ecng.IO;

using System;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// File system abstraction. Implementations can be backed by the local OS or an in-memory store.
/// The interface defines a minimal stream-based contract.
/// </summary>
public interface IFileSystem
{
	/// <summary>
	/// Checks whether a file exists at the specified path.
	/// </summary>
	/// <param name="path">Path to the file.</param>
	/// <returns>True if the file exists; otherwise, false.</returns>
	bool FileExists(string path);

	/// <summary>
	/// Checks whether a directory exists at the specified path.
	/// </summary>
	/// <param name="path">Path to the directory.</param>
	/// <returns>True if the directory exists; otherwise, false.</returns>
	bool DirectoryExists(string path);

	/// <summary>
	/// Opens a stream for the file with specified mode and access.
	/// </summary>
	/// <param name="path">Path to the file.</param>
	/// <param name="mode">File open mode.</param>
	/// <param name="access">File access type.</param>
	/// <param name="share">File sharing mode.</param>
	/// <returns>A stream with the specified access.</returns>
	Stream Open(string path, FileMode mode, FileAccess access = FileAccess.ReadWrite, FileShare share = FileShare.None);

	/// <summary>
	/// Creates a directory including all missing intermediate directories.
	/// </summary>
	/// <param name="path">Path to the directory.</param>
	void CreateDirectory(string path);

	/// <summary>
	/// Deletes a directory.
	/// </summary>
	/// <param name="path">Path to the directory.</param>
	/// <param name="recursive">If true, deletes contents recursively.</param>
	void DeleteDirectory(string path, bool recursive = false);

	/// <summary>
	/// Deletes a file.
	/// </summary>
	/// <param name="path">Path to the file.</param>
	void DeleteFile(string path);

	/// <summary>
	/// Moves a file to a new location.
	/// </summary>
	/// <param name="sourceFileName">Source file path.</param>
	/// <param name="destFileName">Destination file path.</param>
	/// <param name="overwrite">If true, overwrites destination.</param>
	void MoveFile(string sourceFileName, string destFileName, bool overwrite = false);

	/// <summary>
	/// Moves a directory to a new location.
	/// </summary>
	/// <param name="sourceDirName">Source directory path.</param>
	/// <param name="destDirName">Destination directory path.</param>
	void MoveDirectory(string sourceDirName, string destDirName);

	/// <summary>
	/// Copies a file to a new location.
	/// </summary>
	/// <param name="sourceFileName">Source file path.</param>
	/// <param name="destFileName">Destination file path.</param>
	/// <param name="overwrite">If true, overwrites destination.</param>
	void CopyFile(string sourceFileName, string destFileName, bool overwrite = false);

	/// <summary>
	/// Enumerates files in a directory.
	/// </summary>
	/// <param name="path">Directory path.</param>
	/// <param name="searchPattern">Search pattern (supports * and ?).</param>
	/// <param name="searchOption">Search scope.</param>
	/// <returns>Sequence of file paths.</returns>
	IEnumerable<string> EnumerateFiles(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly);

	/// <summary>
	/// Enumerates subdirectories in a directory.
	/// </summary>
	/// <param name="path">Directory path.</param>
	/// <param name="searchPattern">Search pattern (supports * and ?).</param>
	/// <param name="searchOption">Search scope.</param>
	/// <returns>Sequence of directory paths.</returns>
	IEnumerable<string> EnumerateDirectories(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly);

	/// <summary>
	/// Gets the creation time (UTC) of a file or directory.
	/// </summary>
	/// <param name="path">Path.</param>
	/// <returns>Creation time in UTC.</returns>
	DateTime GetCreationTimeUtc(string path);

	/// <summary>
	/// Gets the last write time (UTC) of a file or directory.
	/// </summary>
	/// <param name="path">Path.</param>
	/// <returns>Last write time in UTC.</returns>
	DateTime GetLastWriteTimeUtc(string path);

	/// <summary>
	/// Gets file length in bytes.
	/// </summary>
	/// <param name="path">Path to the file.</param>
	/// <returns>File size in bytes.</returns>
	long GetFileLength(string path);
}