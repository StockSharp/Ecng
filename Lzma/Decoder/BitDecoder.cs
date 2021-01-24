namespace Lzma
{
	/// <summary>
	/// Represents a decoder for single bits.
	/// </summary>
	internal struct BitDecoder
	{
		#region Fields

		public Probability Probability;

		#endregion

		#region Methods

		/// <summary>
		/// Initializes the bit decoder, resetting the bit probability.
		/// </summary>
		public void Initialize()
		{
			this.Probability.Reset();
		}

		/// <summary>
		/// Decodes a bit from the range decoder.
		/// </summary>
		/// <param name="rangeDecoder">The range decoder to decode from.</param>
		/// <returns>The decoded bit (either 0 or 1).</returns>
		public uint Decode(RangeDecoder rangeDecoder)
		{
			uint bound = (rangeDecoder.range >> Probability.Bits) * this.Probability.Value;
			uint symbol;

			if (rangeDecoder.low < bound)
			{
				this.Probability.Increment();
				rangeDecoder.range = bound;
				symbol = 0;
			}
			else
			{
				this.Probability.Decrement();
				rangeDecoder.low -= bound;
				rangeDecoder.range -= bound;
				symbol = 1;
			}

			rangeDecoder.Normalize();

			return symbol;
		}

		#endregion
	}
}
