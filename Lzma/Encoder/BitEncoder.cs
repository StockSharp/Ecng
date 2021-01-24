using System.Diagnostics.Contracts;

namespace Lzma
{
	/// <summary>
	/// Represents a encoder for single bits.
	/// </summary>
	internal struct BitEncoder
	{
		#region Fields

		public Probability Probability;

		#endregion

		#region Properties

		/// <summary>
		/// Gets the price for encoding a "0" with this probability.
		/// </summary>
		public uint Price0 => this.Probability.Price0;

		/// <summary>
		/// Gets the price for encoding a "1" with this probability.
		/// The price is the same as encoding a "0" with the inverse probability (1 - this probability).
		/// </summary>
		public uint Price1 => this.Probability.Price1;

		#endregion

		#region Methods

		/// <summary>
		/// Initializes the bit encoder, resetting the bit probability.
		/// </summary>
		public void Initialize()
		{
			this.Probability.Reset();
		}

		/// <summary>
		/// Encodes a bit.
		/// </summary>
		/// <param name="rangeEncoder">The range encoder to write to.</param>
		/// <param name="symbol">The bit to encode.</param>
		public void Encode(RangeEncoder rangeEncoder, uint symbol)
		{
			uint bound = (rangeEncoder.range >> Probability.Bits) * this.Probability.Value;

			if (symbol == 0)
			{
				this.Probability.Increment();
				rangeEncoder.range = bound;
			}
			else
			{
				this.Probability.Decrement();
				rangeEncoder.low += bound;
				rangeEncoder.range -= bound;
			}

			rangeEncoder.Normalize();
		}

		/// <summary>
		/// Encodes a low bit (i.e. 0) with the specified probability.
		/// </summary>
		/// <param name="rangeEncoder">The range encoder to write to.</param>
		public void Encode0(RangeEncoder rangeEncoder)
		{
			rangeEncoder.range = (rangeEncoder.range >> Probability.Bits) * this.Probability.Value;

			this.Probability.Increment();

			rangeEncoder.Normalize();
		}

		/// <summary>
		/// Encodes a high bit (i.e. 1) with the specified probability.
		/// </summary>
		/// <param name="rangeEncoder">The range encoder to write to.</param>
		public void Encode1(RangeEncoder rangeEncoder)
		{
			uint bound = (rangeEncoder.range >> Probability.Bits) * this.Probability.Value;
			rangeEncoder.low += bound;
			rangeEncoder.range -= bound;

			this.Probability.Decrement();

			rangeEncoder.Normalize();
		}

		/// <summary>
		/// Gets the encoding price for the specified symbol.
		/// </summary>
		/// <param name="symbol">The symbol.</param>
		/// <returns>The price.</returns>
		[Pure]
		public uint GetPrice(uint symbol)
		{
			return Probability.Prices[(((this.Probability.Value - symbol) ^ ((-(int)symbol))) & (Probability.MaxValue - 1)) >> Probability.BitPriceReducingBits];
		}

		#endregion
	}
}
