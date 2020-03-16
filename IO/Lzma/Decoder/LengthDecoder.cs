namespace Lzma
{
	/// <summary>
	/// Represents the decoder for length values.
	/// </summary>
	internal sealed class LengthDecoder
	{
		#region Fields

		private BitDecoder choice; // 0 = len < 8 (lowDecoder); 1 = decode choice2
		private BitDecoder choice2; // 0 = len >= 8 && len < 16 (midDecoder); 1 = len > 16 (highDecoder).
		private readonly BitTreeDecoder[] lowDecoder;
		private readonly BitTreeDecoder[] midDecoder;
		private readonly BitTreeDecoder highDecoder;

		#endregion

		#region Constructors

		/// <summary>
		/// Creates a new length decoder.
		/// </summary>
		/// <param name="numPositionBits">The number of position bits (i.e. the "pb" property).</param>
		public LengthDecoder(int numPositionBits)
		{
			this.choice = new BitDecoder();
			this.choice2 = new BitDecoder();

			this.lowDecoder = new BitTreeDecoder[1 << numPositionBits]; // was Constants.NumPosBitsMax.
			for (int i = 0; i < this.lowDecoder.Length; i++)
				this.lowDecoder[i] = new BitTreeDecoder(Constants.LengthLowBits);

			this.midDecoder = new BitTreeDecoder[1 << numPositionBits]; // was Constants.NumPosBitsMax.
			for (int i = 0; i < this.midDecoder.Length; i++)
				this.midDecoder[i] = new BitTreeDecoder(Constants.LengthMidBits);

			this.highDecoder = new BitTreeDecoder(Constants.LengthHighBits);
		}

		#endregion

		/// <summary>
		/// Initializes the length decoder.
		/// </summary>
		public void Initialize()
		{
			this.choice.Initialize();
			this.choice2.Initialize();

			for (int i = 0; i < this.lowDecoder.Length; i++)
				this.lowDecoder[i].Initialize();

			for (int i = 0; i < this.midDecoder.Length; i++)
				this.midDecoder[i].Initialize();

			this.highDecoder.Initialize();
		}

		/// <summary>
		/// Decodes a length.
		/// </summary>
		/// <param name="rangeDecoder">The range decoder to read from.</param>
		/// <param name="posState">The position state.</param>
		/// <returns>The decoded length.</returns>
		public uint Decode(RangeDecoder rangeDecoder, uint posState)
		{
			// length is < LengthLowMax.
			if (this.choice.Decode(rangeDecoder) == 0)
				return Constants.MinMatchLength + this.lowDecoder[posState].Decode(rangeDecoder);

			// length is >= LengthLowMax and < LengthLowMax + LengthMidMax.
			if (this.choice2.Decode(rangeDecoder) == 0)
				return Constants.MinMatchLength + Constants.LengthLowMax + this.midDecoder[posState].Decode(rangeDecoder);

			// length is > 16.
			return Constants.MinMatchLength + Constants.LengthLowMax + Constants.LengthMidMax + this.highDecoder.Decode(rangeDecoder); 
		}
	}
}
