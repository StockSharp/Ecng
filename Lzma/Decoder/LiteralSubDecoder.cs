namespace Lzma
{
	/// <summary>
	/// Literal decoder structure for contextual literal decoding. 
	/// It is used for one specific state of the LiteralDecoder.
	/// </summary>
	internal struct LiteralSubDecoder
	{
		#region Fields

		private BitDecoder[] bitDecoders;

		#endregion

		#region Properties

		#endregion

		#region Constructors

		#endregion

		#region Methods

		/// <summary>
		/// Resets all probabilities.
		/// </summary>
		public void Initialize()
		{
			if(this.bitDecoders == null)
				this.bitDecoders = new BitDecoder[0x300];

			this.bitDecoders.InitializeAll();
		}

		/// <summary>
		/// Decodes a literal.
		/// </summary>
		/// <param name="rangeDecoder">The range decoder to read from.</param>
		/// <returns></returns>
		public byte Decode(RangeDecoder rangeDecoder)
		{
			// decode until symbol is completed.
			uint symbol = 1;
			do symbol = (symbol << 1) | this.bitDecoders[symbol].Decode(rangeDecoder); while (symbol < 0x100);
			return unchecked((byte)symbol);
		}

		/// <summary>
		/// Decodes a literal using a specific match byte to perform a delta decode.
		/// </summary>
		/// <param name="rangeDecoder">The range decoder to read from.</param>
		/// <param name="matchByte">The match byte.</param>
		/// <returns></returns>
		public byte DecodeDelta(RangeDecoder rangeDecoder, byte matchByte)
		{
			uint symbol = 1;
			do
			{
				// get match bit and decoded bit.
				uint matchBit = (uint)(matchByte >> 7) & 1;
				uint state = ((1 + matchBit) << 8) + symbol;
				uint bit = this.bitDecoders[state].Decode(rangeDecoder);

				// append bit.
				matchByte <<= 1;
				symbol = (symbol << 1) | bit;

				// if the decoded bit does not match the bit in the match byte, decode in normal way.
				if (matchBit != bit)
				{
					while (symbol < 0x100)
						symbol = (symbol << 1) | this.bitDecoders[symbol].Decode(rangeDecoder);

					break;
				}
			} while (symbol < 0x100);

			return unchecked((byte)symbol);
		}

		#endregion
	}
}
