using System.Diagnostics.Contracts;

namespace Lzma
{
	/// <summary>
	/// Literal encoder structure for contextual literal encoding. 
	/// It is used for one specific state of the LiteralEncoder.
	/// </summary>
	internal struct LiteralSubEncoder
	{
		#region Fields

		private BitEncoder[] bitEncoders;
		
		#endregion

		#region Methods

		/// <summary>
		/// Resets all probabilities.
		/// </summary>
		public void Initialize()
		{
			if (this.bitEncoders == null)
				this.bitEncoders = new BitEncoder[0x300];
			
			this.bitEncoders.InitializeAll();
		}
		
		/// <summary>
		/// Encodes a literal.
		/// </summary>
		/// <param name="rangeEncoder">The range encode to write to.</param>
		/// <param name="symbol">The 8-bit literal to encode.</param>
		public void Encode(RangeEncoder rangeEncoder, byte symbol)
		{
			uint context = 1;
			for (int i = 7; i >= 0; i--)
			{
				// fetch one bit from the symbol.
				uint bit = (uint)((symbol >> i) & 1);

				// encode it.
				this.bitEncoders[context].Encode(rangeEncoder, bit);

				// append bit to context.
				context = (context << 1) | bit;
			}
		}

		/// <summary>
		/// Encodes a literal using delta encoding.
		/// </summary>
		/// <param name="rangeEncoder">The range encode to write to.</param>
		/// <param name="symbol">The 8-bit literal to encode.</param>
		/// <param name="matchByte">The byte at the last distance.</param>
		public void EncodeDelta(RangeEncoder rangeEncoder, byte symbol, byte matchByte)
		{
			uint context = 1;
			bool same = true;
			for (int i = 7; i >= 0; i--)
			{
				// fetch one bit from the symbol.
				uint bit = (uint)((symbol >> i) & 1);
				uint state = context;

				if (same)
				{
					// fetch one bit from the match byte.
					uint matchBit = (uint)((matchByte >> i) & 1);

					// append match bit to state using the higher indices (0x100 - 0x200 for matchBit == 0, 0x200 - 0x300 for matchBit == 1).
					state += ((1 + matchBit) << 8);

					// determine whether the bits still match. if not, delta encoding stops and the remaining bits are encoded normally.
					same = (matchBit == bit);
				}
				
				// encode it.
				this.bitEncoders[state].Encode(rangeEncoder, bit);
				
				// append bit to context.
				context = (context << 1) | bit;
			}
		}

		/// <summary>
		/// Gets the price for encoding the specified literal.
		/// </summary>
		/// <param name="symbol">The 8-bit literal.</param>
		/// <returns>The price.</returns>
		[Pure]
		public uint GetPrice(byte symbol)
		{
			uint price = 0;
			uint context = 1;

			// sum of all bit probability prices.
			for (int i = 7; i >= 0; i--)
			{
				uint bit = (uint)(symbol >> i) & 1;
				price += this.bitEncoders[context].GetPrice(bit);
				context = (context << 1) | bit;
			}

			return price;
		}

		/// <summary>
		/// Gets the price for delta encoding the specified literal.
		/// </summary>
		/// <param name="symbol">The 8-bit literal.</param>
		/// <param name="matchByte">The byte at the last distance.</param>
		/// <returns>The price.</returns>
		[Pure]
		public uint GetPriceDelta(byte symbol, byte matchByte)
		{
			uint price = 0;
			uint context = 1;
			int i = 7;

			// sum of all matched bit probability prices.
			for (; i >= 0; i--)
			{
				uint matchBit = (uint)(matchByte >> i) & 1;
				uint bit = (uint)(symbol >> i) & 1;

				price += this.bitEncoders[((1 + matchBit) << 8) + context].GetPrice(bit);
				context = (context << 1) | bit;

				if (matchBit != bit)
				{
					i--;
					break;
				}
			}

			// sum of all bit probability prices.
			for (; i >= 0; i--)
			{
				uint bit = (uint)(symbol >> i) & 1;
				price += this.bitEncoders[context].GetPrice(bit);
				context = (context << 1) | bit;
			}

			return price;
		}

		#endregion
	}
}
