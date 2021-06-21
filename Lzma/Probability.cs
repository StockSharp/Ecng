using System.Runtime.CompilerServices;

namespace Lzma
{
	/// <summary>
	/// Symbol probability.
	/// </summary>
	[System.CLSCompliant(false)]
	public struct Probability
	{
		#region Constants
		
		/// <summary>
		/// The number of bits in a probability value.
		/// Probabilities are stored in 11-bit integers.
		/// </summary>
		public const int Bits = 11;

		/// <summary>
		/// The maximum possible value for a probability.
		/// </summary>
		public const ushort MaxValue = 1 << Bits;

		/// <summary>
		/// The identity value (i.e. 0.5 or 50%).
		/// </summary>
		public const ushort IdentityValue = MaxValue / 2;

		/// <summary>
		/// The number of bits the value is shifted when increasing or decreasing a probability.
		/// </summary>
		public const int MoveBits = 5;

		/// <summary>
		/// The number of bits a probability is shifted to determine its price index.
		/// Higher values = Smaller price table.
		/// Lower values = More accuracy.
		/// </summary>
		public const int BitPriceReducingBits = 4;

		/// <summary>
		/// The "exponent" for bit prices.
		/// </summary>
		public const int BitPriceShiftBits = 4;

		#endregion

		#region Fields

		/// <summary>
		/// The encoding prices of all possible probs.
		/// </summary>
		public static readonly uint[] Prices;

		/// <summary>
		/// The identity probability (i.e. 0.5 or 50%).
		/// </summary>
		public static readonly Probability Identity;

		/// <summary>
		/// The price for a bit encoded with 50% probability.
		/// Also the price for bits encoded used the RangeEncoder.EncodeDirectBits method
		/// </summary>
		public static readonly uint IdentityPrice;

		/// <summary>
		/// The value of the probability.
		/// </summary>
		public ushort Value;

		#endregion

		#region Properties

		/// <summary>
		/// Gets the price for encoding a "0" with this probability.
		/// </summary>
		public uint Price0 => Prices[this.Value >> BitPriceReducingBits];

		/// <summary>
		/// Gets the price for encoding a "1" with this probability.
		/// The price is the same as encoding a "0" with the inverse probability (1 - this probability).
		/// </summary>
		public uint Price1 => Prices[(this.Value ^ (MaxValue - 1)) >> BitPriceReducingBits];

		#endregion

		#region Constructors

		static Probability()
		{
			// compute bit price lookup table.

			Prices = new uint[MaxValue >> BitPriceReducingBits];

			for (uint i = (1 << BitPriceReducingBits) / 2; i < MaxValue; i += (1 << BitPriceReducingBits))
			{
				uint w = i;
				uint bitCount = 0;
				
				for (int j = 0; j < BitPriceShiftBits; j++)
				{
					w = w * w;
					bitCount <<= 1;
					while (w >= (1u << 16))
					{
						w >>= 1;
						++bitCount;
					}
				}

				Prices[i >> BitPriceReducingBits] = ((Bits << BitPriceShiftBits) - 15 - bitCount);
			}

			Identity.Value = IdentityValue;
			IdentityPrice = Prices[IdentityValue >> BitPriceReducingBits];
		}

		#endregion

		#region Methods

		/// <summary>
		/// Resets the probability to the initial value of 0.5.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Reset()
		{
			this.Value = Identity.Value;
		}

		/// <summary>
		/// Increses the probability.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Increment()
		{
			this.Value = (ushort)(this.Value + ((MaxValue - this.Value) >> MoveBits));
		}

		/// <summary>
		/// Decreases the probability.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Decrement()
		{
			this.Value = (ushort)(this.Value - (this.Value >> MoveBits));
		}

		public override string ToString()
		{
			return $"{(float)this.Value / MaxValue * 100.0f:0.0}% ({this.Value})";
		}

		#endregion
	}
}
