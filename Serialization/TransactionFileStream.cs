namespace Ecng.Serialization
{
	using System;
	using System.IO;

	using Ecng.Common;

	public class TransactionFileStream : Stream
	{
		private readonly string _name;
		private readonly string _nameTemp;

		private FileStream _temp;

		public TransactionFileStream(string name, FileMode mode)
		{
			if (name.IsEmpty())
				throw new ArgumentNullException(nameof(name));

			_name = name;
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

		protected override void Dispose(bool disposing)
		{
			if (_temp == null)
				return;

			_temp.Dispose();
			_temp = null;

			base.Dispose(disposing);

			File.Copy(_nameTemp, _name, true);
			File.Delete(_nameTemp);
		}

		public override void Flush()
		{
			_temp.Flush();
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			return _temp.Seek(offset, origin);
		}

		public override void SetLength(long value)
		{
			_temp.SetLength(value);
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			throw new NotSupportedException();
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			_temp.Write(buffer, offset, count);
		}

		public override bool CanRead => false;

		public override bool CanSeek => _temp.CanSeek;

		public override bool CanWrite => _temp.CanWrite;

		public override long Length => _temp.Length;

		public override long Position
		{
			get => _temp.Position;
			set => _temp.Position = value;
		}
	}
}