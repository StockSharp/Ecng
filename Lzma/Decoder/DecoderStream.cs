using System;
using System.IO;

namespace Lzma
{
	/// <summary>
	/// Represents a decoder stream for raw LZMA streams.
	/// </summary>
	/// <remarks>
	/// Note that this class does NOT handle/support any of the LZMA container streams/headers.
	/// </remarks>
	public sealed class DecoderStream : Stream
	{
		#region Fields

		private readonly Stream stream;
		private Decoder decoder;
		private long decodedBytes;
		private long decodedSize;

		#endregion

		#region Properties

		/// <summary>
		/// Gets a value indicating whether the stream supports reading.
		/// Always returns true.
		/// </summary>
		public override bool CanRead => true;

		/// <summary>
		/// Gets a value indicating whether the stream supports writing.
		/// Always returns false.
		/// </summary>
		public override bool CanWrite => false;

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
		public override long Length => this.decodedSize;

		#endregion

		#region Constructors

		/// <summary>
		/// Creates a new uninitialized DecoderStream.
		/// </summary>
		public DecoderStream(Stream stream)
		{
			this.stream = stream;
		}

		/// <summary>
		/// Creates a new DecoderStream and initializes it.
		/// </summary>
		/// <param name="stream"></param>
		/// <param name="properties"></param>
		public DecoderStream(Stream stream, DecoderProperties properties)
		{
			this.stream = stream;
			this.Initialize(properties);
		}

		#endregion

		#region Methods

		/// <summary>
		/// Initializes the decoder.
		/// </summary>
		/// <param name="properties">The decoder properties.</param>
		/// <remarks>
		/// Please note that this also reads data from the stream in order to initialize the range decoder.
		/// </remarks>
		public void Initialize(DecoderProperties properties)
		{
			if(this.decoder == null || !this.decoder.Properties.Compare(properties))
				this.decoder = new Decoder(this.stream, properties);

			this.decoder.Initialize();

			this.decodedSize = -1;
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
			if (this.decoder == null)
				throw new InvalidOperationException("Decoder is not initialized. Please initialize it using the \"Initialize\" method first.");

			if (this.decodedSize >= 0)
			{
				long remaining = this.decodedSize - this.decodedBytes;
				if (count > remaining)
					count = (int)remaining;
			}

			uint numBytes = this.decoder.Decode(buffer, (uint)offset, (uint)count);
			if (numBytes == 0)
				return 0;

			this.decodedBytes += numBytes;

			return (int)numBytes;
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
			throw new NotSupportedException();
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
			
		}

		/// <summary>
		/// Restarts the range decoder. Must be called when the encoder stream was aligned.
		/// </summary>
		public void Align()
		{
			this.decoder.Align();
		}

		/// <summary>
		/// Sets the length of the decoded data.
		/// </summary>
		/// <param name="value"></param>
		public override void SetLength(long value)
		{
			this.decodedSize = value;
		}

		#endregion
	}
}
