namespace Ecng.Serialization
{
	using System;
	using System.Diagnostics;
	using System.IO;

	using Ecng.Common;

	/// <summary>
	/// Represents a transactional file stream that writes data to a temporary file and, upon disposal, commits the changes to the target file.
	/// </summary>
	public class TransactionFileStream : Stream
	{
		private readonly string _name;
		private readonly string _nameTemp;

		private FileStream _temp;

		/// <summary>
		/// Initializes a new instance of the <see cref="TransactionFileStream"/> class.
		/// </summary>
		/// <param name="name">The name of the target file.</param>
		/// <param name="mode">The file mode that specifies the type of operations to be performed on the file.</param>
		public TransactionFileStream(string name, FileMode mode)
		{
			_name = name.ThrowIfEmpty(nameof(name));
			_nameTemp = _name + ".tmp";

			switch (mode)
			{
				case FileMode.CreateNew:
				{
					if (File.Exists(_name))
						throw new IOException();

					break;
				}
				case FileMode.Create:
					break;
				case FileMode.Open:
				{
					File.Copy(_name, _nameTemp, true);
					break;
				}
				case FileMode.OpenOrCreate:
				{
					if (File.Exists(_name))
						File.Copy(_name, _nameTemp, true);

					break;
				}
				case FileMode.Truncate:
					break;
				case FileMode.Append:
				{
					if (File.Exists(_name))
						File.Copy(_name, _nameTemp, true);

					break;
				}
				default:
					throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
			}

			_temp = new FileStream(_nameTemp, mode, FileAccess.Write);
		}

		/// <summary>
		/// Releases the unmanaged resources used by the <see cref="TransactionFileStream"/> and optionally releases the managed resources.
		/// Copies the temporary file to the target file and then deletes the temporary file.
		/// </summary>
		/// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
		protected override void Dispose(bool disposing)
		{
			if (_temp is null)
				return;

			_temp.Dispose();
			_temp = null;

			base.Dispose(disposing);

			File.Copy(_nameTemp, _name, true);

			// SSD can copy file async
			try
			{
				File.Delete(_nameTemp);
			}
			catch (Exception ex)
			{
				Trace.WriteLine(ex);
			}
		}

		/// <summary>
		/// Clears all buffers for the current stream and causes any buffered data to be written to the underlying file.
		/// </summary>
		public override void Flush()
		{
			_temp.Flush();
		}

		/// <summary>
		/// Sets the position within the temporary file stream.
		/// </summary>
		/// <param name="offset">A byte offset relative to the origin parameter.</param>
		/// <param name="origin">A value of type <see cref="SeekOrigin"/> indicating the reference point used to obtain the new position.</param>
		/// <returns>The new position within the temporary file stream.</returns>
		public override long Seek(long offset, SeekOrigin origin)
		{
			return _temp.Seek(offset, origin);
		}

		/// <summary>
		/// Sets the length of the underlying temporary file stream.
		/// </summary>
		/// <param name="value">The desired length of the current stream in bytes.</param>
		public override void SetLength(long value)
		{
			_temp.SetLength(value);
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
			_temp.Write(buffer, offset, count);
		}

		/// <summary>
		/// Gets a value indicating whether the temporary file stream supports reading.
		/// Always returns false.
		/// </summary>
		public override bool CanRead => false;

		/// <summary>
		/// Gets a value indicating whether the underlying temporary file stream supports seeking.
		/// </summary>
		public override bool CanSeek => _temp.CanSeek;

		/// <summary>
		/// Gets a value indicating whether the underlying temporary file stream supports writing.
		/// </summary>
		public override bool CanWrite => _temp.CanWrite;

		/// <summary>
		/// Gets the length in bytes of the underlying temporary file stream.
		/// </summary>
		public override long Length => _temp.Length;

		/// <summary>
		/// Gets or sets the current position within the underlying temporary file stream.
		/// </summary>
		public override long Position
		{
			get => _temp.Position;
			set => _temp.Position = value;
		}
	}
}