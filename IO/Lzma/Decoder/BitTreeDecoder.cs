using System;

namespace Lzma
{
	/// <summary>
	/// Represents a bit tree decoder.
	/// </summary>
	internal sealed class BitTreeDecoder
	{
		#region Fields

		private readonly int numBits;
		private readonly BitDecoder[] bitDecoders;

		#endregion

		#region Constructors

		/// <summary>
		/// Creates a new bit tree decoder with the specified number of bits.
		/// </summary>
		/// <param name="numBits">The number of bits in a symbol [1..32].</param>
		public BitTreeDecoder(int numBits)
		{
			if (numBits < 1 || numBits > 32)
				throw new ArgumentException("numBits must not be less than 1 or greater than 32.");

			this.numBits = numBits;
			this.bitDecoders = new BitDecoder[1 << numBits];
		}

		#endregion

		#region Methods

		/// <summary>
		/// Initializes the decoder, resetting all probabilites.
		/// </summary>
		public void Initialize()
		{
			this.bitDecoders.InitializeAll();
		}

		/// <summary>
		/// Decodes a symbol.
		/// </summary>
		/// <param name="rangeDecoder">The range decoder to decode from.</param>
		/// <returns>The decoded symbol.</returns>
		public uint Decode(RangeDecoder rangeDecoder)
		{
			uint probsIndex = 1;
			for (int i = 0; i < this.numBits; i++)
				probsIndex = (probsIndex << 1) + this.bitDecoders[probsIndex].Decode(rangeDecoder);
			return probsIndex - (1u << this.numBits);
		}

		/// <summary>
		/// Decodes a symbol in reversed order.
		/// </summary>
		/// <param name="rangeDecoder">The range decoder to decode from.</param>
		/// <returns>The decoded symbol.</returns>
		public uint DecodeReverse(RangeDecoder rangeDecoder)
		{
			return DecodeReverse(rangeDecoder, this.bitDecoders, 0, this.numBits);
		}

		/// <summary>
		/// Performs a reverse bit tree decode.
		/// </summary>
		/// <param name="rangeDecoder">The range decoder to read from.</param>
		/// <param name="decoders">The bit decoder array.</param>
		/// <param name="decodersOffset">An offset in the array.</param>
		/// <param name="numBits">The number of bits to decode.</param>
		/// <returns>The decoded symbol.</returns>
		public static uint DecodeReverse(RangeDecoder rangeDecoder, BitDecoder[] decoders, uint decodersOffset, int numBits)
		{
			uint probsIndex = 1;
			uint symbol = 0;
			for (int i = 0; i < numBits; i++)
			{
				uint bit = decoders[decodersOffset + probsIndex].Decode(rangeDecoder);
				probsIndex <<= 1;
				probsIndex += bit;
				symbol |= (bit << i);
			}

			return symbol;
		}


		#endregion
	}
}
