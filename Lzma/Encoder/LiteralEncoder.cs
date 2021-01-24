using System.Diagnostics.Contracts;

namespace Lzma
{
	/// <summary>
	/// Represents the encoder for literals.
	/// </summary>
	internal sealed class LiteralEncoder
	{
		#region Fields

		private readonly uint positionMask; 
		private readonly int numContextBits;
		private readonly LiteralSubEncoder[] encoders;
		
		#endregion

		#region Properties

		#endregion

		#region Constructors

		/// <summary>
		/// Creates a new literal encoder.
		/// </summary>
		/// <param name="numPositionBits">The number of literal position bits.</param>
		/// <param name="numContextBits">The number of literal context bits.</param>
		public LiteralEncoder(int numPositionBits, int numContextBits)
		{
			// compute a mask for the stream position.
			// e.g. this is useful when compressing structured/aligned data with a lot of consecutive 2^pb byte groups (think int-array, float-array, etc.).
			this.positionMask = (1u << numPositionBits) - 1u;
			this.numContextBits = numContextBits;

			// create an array of sub-decoders responsible for specific states.
			this.encoders = new LiteralSubEncoder[1u << (numPositionBits + numContextBits)];
		}

		#endregion

		#region Methods

		/// <summary>
		/// Initializes the literal encoder, resetting all probabilities.
		/// </summary>
		public void Initialize()
		{
			for (int i = 0; i < this.encoders.Length; i++)
				this.encoders[i].Initialize();
		}

		/// <summary>
		/// Encodes a literal (i.e. one byte).
		/// </summary>
		/// <param name="rangeEncoder">The range encoder to write to.</param>
		/// <param name="position">The position in the stream.</param>
		/// <param name="previousByte">The previous encoded byte.</param>
		/// <param name="symbol">The literal symbol to encode.</param>
		public void Encode(RangeEncoder rangeEncoder, uint position, byte previousByte, byte symbol)
		{
			// determine offset in the encoder array.
			// include some bits of the previous byte as determined by "lc".
			// this is useful for e.g. text, where some bits are almost always the same (see for example the literal values of "A" to "Z").
			uint index = ((position & this.positionMask) << this.numContextBits) + ((uint)previousByte >> (8 - this.numContextBits));

			// encode literal.
			this.encoders[index].Encode(rangeEncoder, symbol);
		}

		/// <summary>
		/// Encodes a literal (i.e. one byte) using delta encoding..
		/// </summary>
		/// <param name="rangeEncoder">The range encoder to write to.</param>
		/// <param name="position">The position in the stream.</param>
		/// <param name="previousByte">The previous encoded byte.</param>
		/// <param name="symbol">The literal symbol to encode.</param>
		/// <param name="matchByte">The byte used as reference for delta encoding.</param>
		public void EncodeDelta(RangeEncoder rangeEncoder, uint position, byte previousByte, byte symbol, byte matchByte)
		{
			// determine offset in the encoder array.
			// include some bits of the previous byte as determined by "lc".
			// this is useful for e.g. text, where some bits are almost always the same (see for example the literal values of "A" to "Z").
			uint index = ((position & this.positionMask) << this.numContextBits) + ((uint)previousByte >> (8 - this.numContextBits));
			
			// encode literal.
			this.encoders[index].EncodeDelta(rangeEncoder, symbol, matchByte);
		}

		/// <summary>
		/// Gets the price for encoding the specified symbol.
		/// </summary>
		/// <param name="posState">The position state.</param>
		/// <param name="previousByte">The previous encoded byte.</param>
		/// <param name="symbol">The literal symbol.</param>
		/// <returns>The price for encoding the literal.</returns>
		[Pure]
		public uint GetPrice(uint posState, byte previousByte, byte symbol)
		{
			uint encoderIndex = (posState << this.numContextBits) + previousByte >> (8 - this.numContextBits);
			return this.encoders[encoderIndex].GetPrice(symbol);
		}

		/// <summary>
		/// Gets the price for encoding the specified symbol using delta encoding.
		/// </summary>
		/// <param name="posState">The position state.</param>
		/// <param name="previousByte">The previous encoded byte.</param>
		/// <param name="symbol">The literal symbol.</param>
		/// <param name="matchByte">The byte used as reference for delta encoding.</param>
		/// <returns>The price for encoding the literal.</returns>
		[Pure]
		public uint GetPriceDelta(uint posState, byte previousByte, byte symbol, byte matchByte)
		{
			uint encoderIndex = (posState << this.numContextBits) + previousByte >> (8 - this.numContextBits);
			return this.encoders[encoderIndex].GetPriceDelta(symbol, matchByte);
		}

		#endregion
	}
}
