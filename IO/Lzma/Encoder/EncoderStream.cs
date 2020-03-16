using System;
using System.IO;

namespace Lzma
{
	/// <summary>
	/// Represents an encoder stream for raw LZMA streams.
	/// </summary>
	/// <remarks>
	/// Note that this class does NOT handle/support any of the LZMA container streams/headers.
	/// </remarks>
	public sealed class EncoderStream : Stream
	{
		#region Fields

		private readonly Stream stream;
		private Encoder encoder;

		#endregion

		#region Properties

		/// <summary>
		/// Gets a value indicating whether the stream supports reading.
		/// Always returns false.
		/// </summary>
		public override bool CanRead => false;

		/// <summary>
		/// Gets a value indicating whether the stream supports writing.
		/// Always returns true.
		/// </summary>
		public override bool CanWrite => true;

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
		/// Not supported.
		/// </summary>
		public override long Length
		{
			get { throw new NotSupportedException(); }
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Creates a new uninitialized EncoderStream.
		/// </summary>
		public EncoderStream(Stream stream)
		{
			this.stream = stream;
		}

		/// <summary>
		/// Creates a new EncoderStream and initializes it with the specified properties.
		/// </summary>
		public EncoderStream(Stream stream, EncoderProperties properties)
		{
			this.stream = stream;
			this.Initialize(properties);
		}

		#endregion

		#region Methods

		/// <summary>
		/// Initializes the encoder.
		/// </summary>
		/// <param name="properties">The encoder properties.</param>
		public void Initialize(EncoderProperties properties)
		{
			if (this.encoder == null || !this.encoder.Properties.Equals(properties))
				this.encoder = new FastEncoder(this.stream, properties);

			this.encoder.Initialize();
		}

		/// <summary>
		/// Reads data from the stream.
		/// Not supported.
		/// </summary>
		/// <param name="buffer">The buffer to store the data.</param>
		/// <param name="offset">The offset in the buffer at which data is written.</param>
		/// <param name="count">The number of bytes to read.</param>
		/// <returns>The number of bytes read.</returns>
		public override int Read(byte[] buffer, int offset, int count)
		{
			throw new NotSupportedException();
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
			if (this.encoder == null)
				throw new InvalidOperationException("Encoder is not initialized. Please initialize it using the \"Initialize\" method first.");

			while (true)
			{
				int numBytesWritten = (int)this.encoder.Write(buffer, (uint)offset, (uint)count);

				count -= numBytesWritten;
				if (count == 0)
					break;
				
				this.encoder.Encode(false);

				offset += numBytesWritten;
			}
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
			this.encoder.Encode(true);
			this.stream.Flush();
		}

		/// <summary>
		/// Flushes the stream and byte-aligns the output.
		/// </summary>
		public void FlushAndAlign()
		{
			this.encoder.FlushAndAlign();
			this.stream.Flush();
		}

		/// <summary>
		/// Closes the stream and the encoder.
		/// </summary>
		public override void Close()
		{
			this.encoder.Close();
		}

		/// <summary>
		/// Sets the length of the stream. Not supported.
		/// </summary>
		/// <param name="value"></param>
		public override void SetLength(long value)
		{
			throw new NotSupportedException();
		}

		#endregion
	}
}
