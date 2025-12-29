namespace Ecng.Serialization;

using System;
using System.Diagnostics;
using System.IO;

using Ecng.Common;

/// <summary>
/// Represents a transactional file stream that writes data to a temporary file.
/// Changes are only committed to the target file when <see cref="Commit"/> is called.
/// If disposed without commit, changes are rolled back (temporary file is deleted).
/// </summary>
/// <remarks>
/// Usage pattern:
/// <code>
/// using (var tfs = new TransactionFileStream("file.txt", FileMode.Create))
/// {
///     // write data...
///     tfs.Commit(); // explicitly commit changes
/// }
/// // If exception occurs before Commit(), original file is preserved (rollback)
/// </code>
/// </remarks>
public class TransactionFileStream : Stream
{
	private readonly string _name;
	private readonly string _nameTemp;

	private readonly IFileSystem _fs;
	private Stream _temp;
	private bool _disposed;
	private bool _everCommitted;

	/// <summary>
	/// Initializes a new instance of the <see cref="TransactionFileStream"/> class.
	/// </summary>
	/// <param name="name">The name of the target file.</param>
	/// <param name="mode">The file mode that specifies the type of operations to be performed on the file.</param>
	public TransactionFileStream(string name, FileMode mode)
		: this(LocalFileSystem.Instance, name, mode)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TransactionFileStream"/> class.
	/// </summary>
	/// <param name="fileSystem"><see cref="IFileSystem"/></param>
	/// <param name="name">The name of the target file.</param>
	/// <param name="mode">The file mode that specifies the type of operations to be performed on the file.</param>
	public TransactionFileStream(IFileSystem fileSystem, string name, FileMode mode)
	{
		_fs = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
		_name = name.ThrowIfEmpty(nameof(name));
		_nameTemp = _name + ".tmp";

		// Clean up any stale .tmp file from previous crashed operations
		// This is done for ALL modes to prevent inheriting garbage data
		TryDeleteTempFile();

		var preload = false;

		switch (mode)
		{
			case FileMode.CreateNew:
			{
				if (_fs.FileExists(_name))
					throw new IOException($"File '{_name}' already exists.");
				break;
			}
			case FileMode.Create:
				break;
			case FileMode.Open:
			{
				if (!_fs.FileExists(_name))
					throw new FileNotFoundException(null, _name);
				preload = true;
				break;
			}
			case FileMode.OpenOrCreate:
			{
				if (_fs.FileExists(_name))
					preload = true;
				break;
			}
			case FileMode.Truncate:
			{
				if (!_fs.FileExists(_name))
					throw new FileNotFoundException(null, _name);
				break;
			}
			case FileMode.Append:
			{
				if (_fs.FileExists(_name))
					_fs.CopyFile(_name, _nameTemp, true);
				break;
			}
			default:
				throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
		}

		var append = mode == FileMode.Append;
		_temp = _fs.OpenWrite(_nameTemp, append);

		if (preload)
		{
            // write original data into temp, position stays at end for appending
            using var rs = _fs.OpenRead(_name);
            rs.CopyTo(_temp);
        }
	}

	private void TryDeleteTempFile()
	{
		try
		{
			if (_fs.FileExists(_nameTemp))
				_fs.DeleteFile(_nameTemp);
		}
		catch (Exception ex)
		{
			Trace.WriteLine(ex);
		}
	}

	private Stream Temp
	{
		get
		{
			if (_disposed || _temp is null)
				throw new ObjectDisposedException(nameof(TransactionFileStream));

			return _temp;
		}
	}

	/// <summary>
	/// Gets a value indicating whether the transaction has ever been committed.
	/// </summary>
	public bool IsCommitted => _everCommitted;

	/// <summary>
	/// Commits the transaction, moving the temporary file to the target location.
	/// Can be called multiple times. After commit, stream continues appending.
	/// </summary>
	/// <exception cref="ObjectDisposedException">The stream has been disposed.</exception>
	public void Commit()
	{
		if (_disposed)
			throw new ObjectDisposedException(nameof(TransactionFileStream));

		_temp.Flush();
		_temp.Dispose();
		_temp = null;

		_fs.MoveFile(_nameTemp, _name, overwrite: true);
		_everCommitted = true;

		// Reopen temp with committed data for continued appending
		_temp = _fs.OpenWrite(_nameTemp, append: false);

        using var rs = _fs.OpenRead(_name);
        rs.CopyTo(_temp);
    }

	/// <summary>
	/// Releases the unmanaged resources used by the <see cref="TransactionFileStream"/> and optionally releases the managed resources.
	/// If <see cref="Commit"/> was never called, performs rollback by deleting the temporary file.
	/// If Commit() failed, preserves .tmp file for manual recovery.
	/// </summary>
	/// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
	protected override void Dispose(bool disposing)
	{
		if (_disposed)
			return;

		_disposed = true;

		if (disposing)
		{
			var temp = _temp;
			_temp = null;

			try
			{
				temp?.Flush();
			}
			catch (Exception ex)
			{
				Trace.WriteLine(ex);
			}

			temp?.Dispose();

			// Only delete temp file if we had an active stream
			// If temp was null, Commit() failed mid-way - preserve .tmp for recovery
			if (temp != null)
				TryDeleteTempFile();
		}

		base.Dispose(disposing);
	}

	/// <summary>
	/// Clears all buffers for the current stream and causes any buffered data to be written to the underlying file.
	/// </summary>
	public override void Flush()
	{
		Temp.Flush();
	}

	/// <summary>
	/// Seeking is not supported. This is an append-only stream.
	/// </summary>
	/// <exception cref="NotSupportedException">Always thrown.</exception>
	public override long Seek(long offset, SeekOrigin origin)
	{
		throw new NotSupportedException();
	}

	/// <summary>
	/// Setting length is not supported. This is an append-only stream.
	/// </summary>
	/// <exception cref="NotSupportedException">Always thrown.</exception>
	public override void SetLength(long value)
	{
		throw new NotSupportedException();
	}

	/// <summary>
	/// Reading is not supported by the <see cref="TransactionFileStream"/>.
	/// </summary>
	/// <param name="buffer">The buffer to read data into.</param>
	/// <param name="offset">The byte offset in buffer at which to begin storing the data read from the underlying stream.</param>
	/// <param name="count">The maximum number of bytes to be read from the underlying stream.</param>
	/// <returns>This method always throws a <see cref="NotSupportedException"/>.</returns>
	/// <exception cref="NotSupportedException">Always thrown.</exception>
	public override int Read(byte[] buffer, int offset, int count)
	{
		throw new NotSupportedException();
	}

	/// <summary>
	/// Writes a sequence of bytes to the temporary file stream.
	/// </summary>
	/// <param name="buffer">An array of bytes. This is the buffer that contains the data to write to the temporary file.</param>
	/// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the temporary file.</param>
	/// <param name="count">The number of bytes to be written to the temporary file.</param>
	public override void Write(byte[] buffer, int offset, int count)
	{
		Temp.Write(buffer, offset, count);
	}

	/// <summary>
	/// Gets a value indicating whether the temporary file stream supports reading.
	/// Always returns false.
	/// </summary>
	public override bool CanRead => false;

	/// <summary>
	/// Gets a value indicating whether the stream supports seeking.
	/// Always returns false - this is an append-only stream.
	/// </summary>
	public override bool CanSeek => false;

	/// <summary>
	/// Gets a value indicating whether the underlying temporary file stream supports writing.
	/// </summary>
	public override bool CanWrite => !_disposed && _temp?.CanWrite == true;

	/// <summary>
	/// Gets the length in bytes of the underlying temporary file stream.
	/// </summary>
	public override long Length => Temp.Length;

	/// <summary>
	/// Gets the current position within the stream.
	/// Setting position is not supported - this is an append-only stream.
	/// </summary>
	/// <exception cref="NotSupportedException">Thrown when attempting to set position.</exception>
	public override long Position
	{
		get => Temp.Position;
		set => throw new NotSupportedException();
	}
}
