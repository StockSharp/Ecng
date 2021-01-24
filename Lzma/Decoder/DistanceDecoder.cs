namespace Lzma
{
	/// <summary>
	/// Represents the decoder for match distance values.
	/// </summary>
	internal sealed class DistanceDecoder
	{
		#region Fields

		private readonly BitTreeDecoder[] prefixDecoders;
		private readonly BitTreeDecoder alignDecoder;
		private readonly BitDecoder[] posDecoders;

		#endregion

		#region Constructors

		/// <summary>
		/// Creates a new distance decoder.
		/// </summary>
		public DistanceDecoder()
		{
			this.prefixDecoders = new BitTreeDecoder[Constants.LengthToDistanceStates];
			for (int i = 0; i < this.prefixDecoders.Length; i++)
				this.prefixDecoders[i] = new BitTreeDecoder(6);

			this.alignDecoder = new BitTreeDecoder(Constants.DistanceAlignBits);
			this.posDecoders = new BitDecoder[1 + Constants.FullDistances - Constants.DistanceModelEndIndex];
		}

		#endregion

		#region Methods

		/// <summary>
		/// Initializes the decoder, resetting the probabilities.
		/// </summary>
		public void Initialize()
		{
			for (int i = 0; i < this.prefixDecoders.Length; i++)
				this.prefixDecoders[i].Initialize();
			this.alignDecoder.Initialize();
			this.posDecoders.InitializeAll();
		}

		/// <summary>
		/// Decodes a match distance.
		/// </summary>
		/// <param name="rangeDecoder">The range decoder to read from.</param>
		/// <param name="length">The length of the match.</param>
		/// <returns>The decoded distance.</returns>
		public uint Decode(RangeDecoder rangeDecoder, uint length)
		{
			length -= Constants.MinMatchLength;
			uint lenToPosState = length;
			if (lenToPosState > Constants.LengthToDistanceStates - 1)
				lenToPosState = Constants.LengthToDistanceStates - 1;

			// decode the distance prefix.
			uint prefix = this.prefixDecoders[lenToPosState].Decode(rangeDecoder);
			if (prefix < Constants.DistanceModelStartIndex)
				return prefix;

			// decode the rest of the distance.
			int numFooterBits = (int)((prefix >> 1) - 1);
			uint baseValue = ((2 | (prefix & 1)) << numFooterBits);

			if (prefix < Constants.DistanceModelEndIndex)
				return baseValue + BitTreeDecoder.DecodeReverse(rangeDecoder, this.posDecoders, baseValue - prefix, numFooterBits);
			
			return baseValue + (rangeDecoder.DecodeDirectBits(numFooterBits - Constants.DistanceAlignBits) << Constants.DistanceAlignBits) + this.alignDecoder.DecodeReverse(rangeDecoder);
		}

		#endregion
	}
}
