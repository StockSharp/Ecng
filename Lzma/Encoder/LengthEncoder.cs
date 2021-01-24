using System.Diagnostics.Contracts;

namespace Lzma
{
	/// <summary>
	/// Represents the encoder for match length values.
	/// </summary>
	internal sealed class LengthEncoder
	{
		#region Fields

		// the bits that determine which of the encoders is used. 0 = low, 10 = mid, 11 = high.
		private BitEncoder choice;
		private BitEncoder choice2;

		// encoders for different ranges of lengths.
		private readonly BitTreeEncoder[] lowEncoder;
		private readonly BitTreeEncoder[] midEncoder;
		private readonly BitTreeEncoder highEncoder;
	
		#endregion

		#region Properties

		#endregion

		#region Constructors

		/// <summary>
		/// Creates a new length encoder.
		/// </summary>
		/// <param name="numPositionBits">The number of position bits (i.e. the "pb" property).</param>
		public LengthEncoder(int numPositionBits)
		{
			this.choice = new BitEncoder();
			this.choice2 = new BitEncoder();

			this.lowEncoder = new BitTreeEncoder[1 << numPositionBits];
			for (int i = 0; i < this.lowEncoder.Length; i++)
				this.lowEncoder[i] = new BitTreeEncoder(Constants.LengthLowBits);

			this.midEncoder = new BitTreeEncoder[1 << numPositionBits];
			for (int i = 0; i < this.midEncoder.Length; i++)
				this.midEncoder[i] = new BitTreeEncoder(Constants.LengthMidBits);

			this.highEncoder = new BitTreeEncoder(Constants.LengthHighBits);
		}

		#endregion

		#region Methods

		/// <summary>
		/// Initializes the length encoder.
		/// </summary>
		public void Initialize()
		{
			this.choice.Initialize();
			this.choice2.Initialize();

			for (int i = 0; i < this.lowEncoder.Length; i++)
				this.lowEncoder[i].Initialize();

			for (int i = 0; i < this.midEncoder.Length; i++)
				this.midEncoder[i].Initialize();

			this.highEncoder.Initialize();
		}

		/// <summary>
		/// Encodes a length.
		/// </summary>
		/// <param name="rangeEncoder">The range encoder to write to.</param>
		/// <param name="posState">The position state.</param>
		/// <param name="length">The value to encode.</param>
		public void Encode(RangeEncoder rangeEncoder, uint posState, uint length)
		{
			length -= Constants.MinMatchLength;

			if (length < Constants.LengthLowMax)
			{
				this.choice.Encode0(rangeEncoder);
				this.lowEncoder[posState].Encode(rangeEncoder, length);
			}
			else
			{
				this.choice.Encode1(rangeEncoder);
				if (length < Constants.LengthLowMax + Constants.LengthMidMax)
				{
					this.choice2.Encode0(rangeEncoder);
					this.midEncoder[posState].Encode(rangeEncoder, length - Constants.LengthLowMax);
				}
				else
				{
					this.choice2.Encode1(rangeEncoder);
					this.highEncoder.Encode(rangeEncoder, length - Constants.LengthLowMax - Constants.LengthMidMax);
				}
			}
		}

		/// <summary>
		/// Gets the price for encoding the specified length.
		/// </summary>
		/// <param name="posState">The position state.</param>
		/// <param name="length">The value to encode.</param>
		/// <returns>The price to encode that length.</returns>
		[Pure]
		public uint GetPrice(uint posState, uint length)
		{
			length -= Constants.MinMatchLength;

			if (length < Constants.LengthLowMax)
				return this.choice.Price0 + this.lowEncoder[posState].GetPrice(length);

			if (length < Constants.LengthLowMax + Constants.LengthMidMax)
				return this.choice.Price1 + this.choice2.Price0 + this.midEncoder[posState].GetPrice(length - Constants.LengthLowMax);

			return this.choice.Price1 + this.choice2.Price1 + this.highEncoder.GetPrice(length - Constants.LengthLowMax - Constants.LengthMidMax);
		}
		
		#endregion
	}
}
