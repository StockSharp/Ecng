namespace Lzma
{
	/// <summary>
	/// Represents the decoder for literals.
	/// </summary>
	internal sealed class LiteralDecoder
	{
		#region Fields

		private readonly uint positionMask;
		private readonly int numContextBits;
		private readonly LiteralSubDecoder[] decoders;

		#endregion

		#region Constructors

		/// <summary>
		/// Creates a new literal decoder using the specified parameters.
		/// </summary>
		/// <param name="numPositionBits">The number of literal position bits.</param>
		/// <param name="numContextBits">The number of literal context bits.</param>
		public LiteralDecoder(int numPositionBits, int numContextBits)
		{
			// compute a mask for the stream position.
			// e.g. this is useful when compressing structured/aligned data with a lot of consecutive 2^pb byte groups (think int-array, float-array, etc.).
			this.positionMask = (1u << numPositionBits) - 1u;
			this.numContextBits = numContextBits;

			// create an array of sub-decoders responsible for specific states.
			this.decoders = new LiteralSubDecoder[1u << (numPositionBits + numContextBits)];
		}

		#endregion

		#region Methods

		/// <summary>
		/// Initializes the literal decoder, resetting all probabilities.
		/// </summary>
		public void Initialize()
		{
			for (int i = 0; i < this.decoders.Length; i++)
				this.decoders[i].Initialize();
		}

		#endregion

		#region Methods

		/// <summary>
		/// Decodes a literal (i.e. one byte) from the compressed stream.
		/// </summary>
		/// <param name="rangeDecoder">The range decoder to read from.</param>
		/// <param name="position">The decoder total position.</param>
		/// <param name="previousByte">The previous byte that provides the context for this byte.</param>
		/// <returns>The decoded literal.</returns>
		public byte Decode(RangeDecoder rangeDecoder, uint position, byte previousByte)
		{
			// determine offset in the decoder array.
			// include some bits of the previous byte as determined by "lc".
			// this is useful for e.g. text, where some bits are almost always the same (see for example the literal values of "A" to "Z").
			uint index = ((position & this.positionMask) << this.numContextBits) + ((uint)previousByte >> (8 - this.numContextBits));

			// decode literal.
			return this.decoders[index].Decode(rangeDecoder);
		}

		/// <summary>
		/// Decodes a literal (i.e. one byte) from the compressed stream using delta decoding.
		/// </summary>
		/// <param name="rangeDecoder">The range decoder to read from.</param>
		/// <param name="position">The decoder total position.</param>
		/// <param name="previousByte">The previous byte that provides the context for this byte.</param>
		/// <param name="matchByte">The reference byte for delta decoding.</param>
		/// <returns>The decoded literal.</returns>
		public byte DecodeDelta(RangeDecoder rangeDecoder, uint position, byte previousByte, byte matchByte)
		{
			// determine offset in the decoder array.
			// include some bits of the previous byte as determined by "lc".
			// this is useful for e.g. text, where some bits are almost always the same (see for example the literal values of "A" to "Z").
			uint index = ((position & this.positionMask) << this.numContextBits) + ((uint)previousByte >> (8 - this.numContextBits));

			// decode literal.
			return this.decoders[index].DecodeDelta(rangeDecoder, matchByte);
		}


		#endregion
	}
}
