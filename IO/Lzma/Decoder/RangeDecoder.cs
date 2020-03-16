using System.IO;

namespace Lzma
{
	/// <summary>
	/// Represents a range decoder, responsible for the arithmetic coding.
	/// </summary>
	internal sealed class RangeDecoder
	{
		#region Fields

		private readonly Stream stream;
		internal uint range;
		internal uint low;

		#endregion

		#region Properties

		/// <summary>
		/// Gets a value indicating whether the range decoder is finished.
		/// </summary>
		public bool Finished => this.low == 0;

		#endregion

		#region Constructors

		/// <summary>
		/// Creates a new range decoder reading from the specified stream.
		/// </summary>
		/// <param name="stream"></param>
		public RangeDecoder(Stream stream)
		{
			this.stream = stream;
			this.range = 0;
			this.low = 0;
		}

		#endregion

		#region Methods

		/// <summary>
		/// Initializes the range decoder, reading a total of 5 bytes from the input stream where the first byte must be 0.
		/// </summary>
		public void Initialize()
		{
			int input = this.stream.ReadByte();
			if (input < 0)
				throw new EndOfStreamException();

			if (input != 0)
				throw new InvalidDataException("Range decoder corrupted.");

			this.range = 0xFFFFFFFFu;
			this.low = 0;

			for (int i = 0; i < 4; i++)
			{
				input = this.stream.ReadByte();
				if (input < 0)
					throw new EndOfStreamException();
				this.low = (this.low << 8) | (byte)input;
			}

			if (this.low == this.range)
				throw new InvalidDataException("Range decoder corrupted.");
		}

		/// <summary>
		/// Renormalizes the range decoder.
		/// </summary>
		internal void Normalize()
		{
			if (this.range < Constants.MaxRange)
			{
				int input = this.stream.ReadByte();
				if (input < 0)
					throw new EndOfStreamException();

				this.range <<= 8;
				this.low = (this.low << 8) | (byte)input;
			}
		}

		/// <summary>
		/// Decodes the specified number of bits without probabilities.
		/// </summary>
		/// <param name="numBits">The number of bits to decode, max. 32.</param>
		/// <returns>The decoded value.</returns>
		public uint DecodeDirectBits(int numBits)
		{
			uint result = 0;

			do
			{
				this.range >>= 1;
				this.low -= this.range;
				uint t = 0 - (this.low >> 31);
				this.low += this.range & t;

				if (this.low == this.range)
					throw new InvalidDataException("Range decoder corrupted.");

				this.Normalize();

				result <<= 1;
				result += t + 1;
			} while (--numBits > 0);

			return result;
		}

		#endregion
	}
}
