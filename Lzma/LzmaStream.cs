using System;
using System.IO;
using System.IO.Compression;

namespace Lzma
{
	/// <summary>
	/// Represents an LZMA stream.
	/// This class is capable of reading and writing data in the simple format used by the .lzma file format.
	/// </summary>
	[CLSCompliant(false)]
	public sealed class LzmaStream : Stream
	{
		#region Fields

		private readonly Stream stream;
		private readonly CompressionMode mode;
		private readonly bool leaveOpen;

		private bool hasHeader;
		private readonly DecoderStream decoderStream;

		private readonly EncoderProperties encoderProperties;
		private readonly EncoderStream encoderStream;
		private long encodedBytes;
		private long headerPosition;

		#endregion

		#region Properties

		/// <summary>
		/// Gets a value indicating whether the stream supports reading.
		/// Returns true if the stream is in decompression mode, false otherwise.
		/// </summary>
		public override bool CanRead => this.mode == CompressionMode.Decompress;

		/// <summary>
		/// Gets a value indicating whether the stream supports writing.
		/// Returns true if the stream is in compression mode, false otherwise.
		/// </summary>
		public override bool CanWrite => this.mode == CompressionMode.Compress;

		/// <summary>
		/// Gets a value indicating whether the stream supports seeking.
		/// Always returns false.
		/// </summary>
		public override bool CanSeek => false;

		/// <summary>
		/// Gets or sets the position in the stream.
		/// Not supported.
		/// </summary>
		public override long Position
		{
			get { throw new NotSupportedException(); }
			set { throw new NotSupportedException(); }
		}

		/// <summary>
		/// Gets the length of the stream.
		/// This returns the length of the decoded data.
		/// If the size of the decoded data is unknown, a negative value is returned.
		/// </summary>
		public override long Length
		{
			get
			{
				if (this.mode == CompressionMode.Decompress)
					return this.decoderStream.Length;
				throw new NotSupportedException();
			}
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Creates a new LzmaStream.
		/// </summary>
		public LzmaStream(Stream stream, CompressionMode mode, bool leaveOpen)
		{
			this.stream = stream;
			this.mode = mode;
			this.leaveOpen = leaveOpen;

			this.hasHeader = false;

			if (this.mode == CompressionMode.Decompress)
			{
				if (!stream.CanRead)
					throw new ArgumentException("Decompression mode requires a readable stream.");

				this.decoderStream = new DecoderStream(stream);
			}
			else
			{
				if (!stream.CanWrite)
					throw new ArgumentException("Compression mode requires a writable stream.");

				this.encoderStream = new EncoderStream(stream);
				this.encoderProperties = EncoderProperties.Default;
			}
		}

		/// <summary>
		/// Creates a new <see cref="LzmaStream"/> in decompression mode.
		/// </summary>
		/// <param name="stream">The stream to read from or write to.</param>
		/// <param name="level"><see cref="CompressionLevel"/></param>
		/// <param name="leaveOpen">true to leave the stream open after the <see cref="LzmaStream"/> object is disposed; otherwise, false.</param>
		public LzmaStream(Stream stream, CompressionLevel level, bool leaveOpen)
			: this(stream, CompressionMode.Compress, leaveOpen)
		{
		}

		/// <summary>
		/// Creates a new <see cref="LzmaStream"/> and initializes it with the specified properties.
		/// </summary>
		/// <param name="stream">The stream to read from or write to.</param>
		/// <param name="properties">The encoder properties.</param>
		public LzmaStream(Stream stream, EncoderProperties properties)
			: this(stream, CompressionMode.Compress, false)
		{
			this.encoderProperties = properties;
		}

		#endregion

		#region Methods

		/// <summary>
		/// Reads properties and decoded size from the stream.
		/// </summary>
		/// <param name="properties"></param>
		/// <param name="decodedSize"></param>
		private bool readHeader(out DecoderProperties properties, out long decodedSize)
		{
			byte[] header = new byte[13];
			int pos = 0;

			properties = default(DecoderProperties);
			decodedSize = 0;

			while (pos < header.Length)
			{
				int numBytes = this.stream.Read(header, pos, header.Length - pos);
				if (numBytes == 0)
				{
					if (pos == 0)
						return false;

					throw new EndOfStreamException("Incomplete LZMA header.");
				}
					
				pos += numBytes;
			}

			byte d = header[0];

			if (d > 9 * 5 * 5)
				throw new InvalidDataException("Invalid properties.");

			properties = new DecoderProperties();
			properties.LC = d % 9; d /= 9;
			properties.LP = d % 5;
			properties.PB = d / 5;
			properties.DictionarySize = ((uint)header[1] << 0) | ((uint)header[2] << 8) | ((uint)header[3] << 16) | ((uint)header[4] << 24);

			decodedSize = ((long)header[5] << 0) | ((long)header[6] << 8) | ((long)header[7] << 16) | ((long)header[8] << 24) | ((long)header[9] << 32) | ((long)header[10] << 40) | ((long)header[11] << 48) | ((long)header[12] << 56);

			return true;
		}

		/// <summary>
		/// Writes properties and decoded size to the stream.
		/// </summary>
		/// <param name="decodedSize"></param>
		private void writeHeader(long decodedSize)
		{
			byte[] header = new byte[13];

			unchecked
			{

				header[0] = (byte)(this.encoderProperties.LC + this.encoderProperties.LP * 9 + this.encoderProperties.PB * 9 * 5);

				header[1] = (byte)(this.encoderProperties.DictionarySize >> 0);
				header[2] = (byte)(this.encoderProperties.DictionarySize >> 8);
				header[3] = (byte)(this.encoderProperties.DictionarySize >> 16);
				header[4] = (byte)(this.encoderProperties.DictionarySize >> 24);

				header[5] = (byte)(decodedSize >> 0);
				header[6] = (byte)(decodedSize >> 8);
				header[7] = (byte)(decodedSize >> 16);
				header[8] = (byte)(decodedSize >> 24);
				header[9] = (byte)(decodedSize >> 32);
				header[10] = (byte)(decodedSize >> 40);
				header[11] = (byte)(decodedSize >> 48);
				header[12] = (byte)(decodedSize >> 56);
			}

			if (this.stream.CanSeek)
			{
				this.headerPosition = this.stream.Position;
			}

			this.stream.Write(header, 0, header.Length);
		}

		/// <summary>
		/// Reads and decodes data from the stream.
		/// </summary>
		/// <param name="buffer">The buffer to store the decoded data.</param>
		/// <param name="offset">The offset in the buffer at which decoded data is written.</param>
		/// <param name="count">The number of bytes to decode.</param>
		/// <returns>The number of decoded bytes.</returns>
		public override int Read(byte[] buffer, int offset, int count)
		{
			if (this.mode != CompressionMode.Decompress)
				throw new InvalidOperationException("Stream is not in decompress mode.");

			if (!this.hasHeader)
			{
				DecoderProperties props;
				long length;

				if (!this.readHeader(out props, out length))
					return 0;

				this.decoderStream.Initialize(props);
				this.decoderStream.SetLength(length);

				this.hasHeader = true;
			}

			return this.decoderStream.Read(buffer, offset, count);
		}

		/// <summary>
		/// Writes data to the stream.
		/// Not supported.
		/// </summary>
		/// <param name="buffer">The buffer written to the stream.</param>
		/// <param name="offset">The offset in the buffer from where reading starts.</param>
		/// <param name="count">The number of bytes to write.</param>
		public override void Write(byte[] buffer, int offset, int count)
		{
			if (this.mode != CompressionMode.Compress)
				throw new InvalidOperationException("Stream is not in compress mode.");

			if (!this.hasHeader)
			{
				this.writeHeader(-1);

				this.encoderStream.Initialize(this.encoderProperties);
				this.encodedBytes = 0;

				this.hasHeader = true;
			}

			this.encodedBytes += count;

			this.encoderStream.Write(buffer, offset, count);
		}

		/// <summary>
		/// Seeks in the stream.
		/// Not supported.
		/// </summary>
		/// <param name="offset">The seek offset.</param>
		/// <param name="origin">The seek origin.</param>
		/// <returns>The new position.</returns>
		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Flushes the stream.
		/// </summary>
		public override void Flush()
		{
			if (this.mode == CompressionMode.Compress && this.encodedBytes > 0)
			{
				this.encoderStream.Close();

				if (this.stream.CanSeek)
				{
					long posBackup = this.stream.Position;

					this.stream.Position = this.headerPosition;
					this.writeHeader(this.encodedBytes);

					this.stream.Position = posBackup;
				}
				
				this.hasHeader = false;
			}
		}

		/// <summary>
		/// Closes the stream.
		/// </summary>
		public override void Close()
		{
			this.Flush();

			if (!this.leaveOpen)
				this.stream.Close();

			base.Close();
		}

		/// <summary>
		/// Sets the length of the decoded data.
		/// </summary>
		/// <param name="value"></param>
		public override void SetLength(long value)
		{
			if (this.mode == CompressionMode.Decompress)
				this.decoderStream.SetLength(value);
			else
				throw new NotSupportedException();
		}

		#endregion
	}
}
