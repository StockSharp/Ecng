using System.IO;

namespace Lzma
{
	/// <summary>
	/// Represents a range encoder. 
	/// It encodes bits and symbols using an arithmetic coding algorithm.
	/// The output can be decoded by the range decoder.
	/// </summary>
	internal sealed class RangeEncoder
	{
		#region Fields

		private readonly Stream stream;
		internal uint range;
		internal ulong low;

		private byte cache;
		private ulong cacheSize;

		#endregion

		#region Constructors

		/// <summary>
		/// Creates a new range encoder that writes to the specified stream.
		/// </summary>
		/// <param name="stream">The stream to write to.</param>
		public RangeEncoder(Stream stream)
		{
			this.stream = stream;
		}

		#endregion

		#region Methods

		/// <summary>
		/// Initializes the range encoder.
		/// </summary>
		public void Initialize()
		{
			this.range = uint.MaxValue;
			this.low = 0;

			this.cacheSize = 1;
			this.cache = 0;
		}

		/// <summary>
		/// Renormalizes the encoder.
		/// </summary>
		internal void Normalize()
		{
			if (this.range < Constants.MaxRange)
			{
				this.range <<= 8;
				this.shiftLow();
			}
		}

		private void shiftLow()
		{
			if ((uint)this.low < 0xFF000000u || (int)(this.low >> 32) != 0)
			{
				byte temp = this.cache;
				do
				{
					this.stream.WriteByte((byte)(temp + (this.low >> 32)));
					temp = 0xFF;
				} while (--this.cacheSize > 0);
				this.cache = (byte)(((uint)this.low) >> 24);
			}
			this.cacheSize++;
			this.low = ((uint)this.low) << 8;
		}

		/// <summary>
		/// Flushes the data of the range encoder.
		/// </summary>
		public void Flush()
		{
			for (int i = 0; i < 5; i++)
				this.shiftLow();
		}

		/// <summary>
		/// Encodes bits without probabilities.
		/// </summary>
		/// <param name="value">An integer containing the bit values.</param>
		/// <param name="numBits">The number of bits to encode</param>
		public void EncodeDirectBits(uint value, int numBits)
		{
			for (int i = numBits - 1; i >= 0; i--)
			{
				this.range >>= 1;

				// is the bit set?
				if (((value >> i) & 1) == 1)
					this.low += this.range;

				this.Normalize();
			} 
		}

		#endregion
	}
}
