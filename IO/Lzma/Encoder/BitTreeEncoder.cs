using System;
using System.Diagnostics.Contracts;

namespace Lzma
{
	/// <summary>
	/// Represents a bit tree encoder.
	/// </summary>
	internal sealed class BitTreeEncoder
	{
		#region Fields

		private readonly int numBits;
		private readonly BitEncoder[] bitEncoders;

		#endregion

		#region Properties

		#endregion

		#region Constructors

		/// <summary>
		/// Creates a new BitTreeEncoder.
		/// </summary>
		public BitTreeEncoder(int numBits)
		{
			if (numBits < 1 || numBits > 32)
				throw new ArgumentException("numBits must not be less than 1 or greater than 32.");

			this.numBits = numBits;
			this.bitEncoders = new BitEncoder[1 << numBits];
		}

		#endregion

		#region Methods

		/// <summary>
		/// Initializes the bit tree encoder, resetting all probabilities.
		/// </summary>
		public void Initialize()
		{
			this.bitEncoders.InitializeAll();
		}

		/// <summary>
		/// Encodes a symbol.
		/// </summary>
		/// <param name="rangeEncoder">The range encoder to write to.</param>
		/// <param name="symbol">The symbol to encode.</param>
		public void Encode(RangeEncoder rangeEncoder, uint symbol)
		{
			uint enodersIndex = 1;
			int bitIndex = this.numBits;

			while (bitIndex-- > 0)
			{
				uint bit = (symbol >> bitIndex) & 1;
				this.bitEncoders[enodersIndex].Encode(rangeEncoder, bit);
				enodersIndex = (enodersIndex << 1) | bit;
			}
		}

		/// <summary>
		/// Encodes a symbol in reversed bit order.
		/// </summary>
		/// <param name="rangeEncoder">The range encoder to write to.</param>
		/// <param name="symbol">The symbol to encode.</param>
		public void EncodeReverse(RangeEncoder rangeEncoder, uint symbol)
		{
			EncodeReverse(rangeEncoder, this.bitEncoders, 0, this.numBits, symbol);
		}

		/// <summary>
		/// Gets the price for encoding the specified symbol.
		/// </summary>
		/// <param name="symbol">The symbol to encode.</param>
		/// <returns>The price.</returns>
		[Pure]
		public uint GetPrice(uint symbol)
		{
			uint price = 0;
			uint enodersIndex = 1;
			int bitIndex = this.numBits;

			while(bitIndex-- > 0)
			{
				uint bit = (symbol >> bitIndex) & 1;
				price += this.bitEncoders[enodersIndex].GetPrice(bit);
				enodersIndex = (enodersIndex << 1) | bit;
			}

			return price;
		}

		/// <summary>
		/// Gets the price for encoding the specified symbol in reversed bit order.
		/// </summary>
		/// <param name="symbol">The symbol to encode.</param>
		/// <returns>The price.</returns>
		[Pure]
		public uint GetPriceReverse(uint symbol)
		{
			return GetPriceReverse(this.bitEncoders, 0, this.numBits, symbol);
		}

		/// <summary>
		/// Encodes a symbol in reversed order.
		/// </summary>
		/// <param name="rangeEncoder">The range encode to write to.</param>
		/// <param name="encoders">An array of the bit encoders.</param>
		/// <param name="encodersOffset">An offset in the probabilities array.</param>
		/// <param name="numBits">The number of bits to encode.</param>
		/// <param name="symbol">The symbol to encode.</param>
		public static void EncodeReverse(RangeEncoder rangeEncoder, BitEncoder[] encoders, uint encodersOffset, int numBits, uint symbol)
		{
			uint enodersIndex = 1;

			for (int i = 0; i < numBits; i++)
			{
				uint bit = symbol & 1;
				encoders[encodersOffset + enodersIndex].Encode(rangeEncoder, bit);
				enodersIndex = (enodersIndex << 1) | bit;
				symbol >>= 1;
			}
		}

		/// <summary>
		/// Gets the price for encoding the specified symbol in reversed bit order.
		/// </summary>
		/// <param name="encoders">An array of the bit encoders.</param>
		/// <param name="encodersOffset">An offset in the probabilities array.</param>
		/// <param name="numBits">The number of bits to encode.</param>
		/// <param name="symbol">The symbol to encode.</param>
		/// <returns>The price.</returns>
		[Pure]
		public static uint GetPriceReverse(BitEncoder[] encoders, uint encodersOffset, int numBits, uint symbol)
		{
			uint price = 0;
			uint enodersIndex = 1;

			for (int i = numBits; i > 0; i--)
			{
				uint bit = symbol & 1;
				symbol >>= 1;
				price += encoders[encodersOffset + enodersIndex].GetPrice(bit);
				enodersIndex = (enodersIndex << 1) | bit;
			}

			return price;
		}

		#endregion
	}
}
