using System.Diagnostics.Contracts;

namespace Lzma
{
	/// <summary>
	/// Represents the encoder for match distance values.
	/// </summary>
	internal sealed class DistanceEncoder
	{
		#region Constants

		private const uint prefixTableSize = 2048;

		#endregion

		#region Fields

		private readonly BitTreeEncoder[] prefixEncoders;
		private readonly BitTreeEncoder alignEncoder;
		private readonly BitEncoder[] posEncoders;

		// lookup table for finding the most significant set bit (used in getPrefix).
		// inspired by http://www.hackersdelight.org/hdcodetxt/nlz.c.txt (nlz10b).
		private static readonly int[] flsTable =
		[
			-1, 11, 12, 00, 00, 13, 00, 24, 21, 14, 00, 00, 17, 00, 25, 00,
			00, 22, 00, 15, 00, 00, 30, 05, 00, 18, 00, 00, 07, 26, 00, 00,
			00, 10, 00, 23, 20, 00, 16, 00, 00, 00, 00, 29, 04, 31, 06, 00,
			09, 00, 19, 00, 00, 28, 03, 00, 08, 00, 27, 02, 00, 00, 01, 00
		];

		// fast lookup table for small distances which are used more often than large distances.
		private static readonly byte[] prefixTable;

		#endregion

		#region Constructors

		/// <summary>
		/// Creates a new DistanceEncoder.
		/// </summary>
		public DistanceEncoder()
		{
			this.prefixEncoders = new BitTreeEncoder[Constants.LengthToDistanceStates];
			for (int i = 0; i < this.prefixEncoders.Length; i++)
				this.prefixEncoders[i] = new BitTreeEncoder(6);

			this.alignEncoder = new BitTreeEncoder(Constants.DistanceAlignBits);
			this.posEncoders = new BitEncoder[1 + Constants.FullDistances - Constants.DistanceModelEndIndex];
		}

		static DistanceEncoder()
		{
			prefixTable = new byte[prefixTableSize];
			prefixTable[0] = 0xFF; // invalid.
			prefixTable[1] = 1; 
			prefixTable[2] = 2; 
			prefixTable[3] = 3; 
			
			for (uint i = 4; i < prefixTableSize; i++)
			{
				// for details, see getPrefix.
				uint x = i;
				x = x | (x >> 1);
				x = x | (x >> 2);
				x = x | (x >> 4);
				x = x | (x >> 8);
				x = x & ~(x >> 16);
				x = x * 0xFD7049FF;
				int bitIndex = flsTable[x >> 26];
				prefixTable[i] = (byte)((bitIndex + bitIndex) + ((i >> (bitIndex - 1)) & 1));
			}
				
		}

		#endregion

		#region Methods

		/// <summary>
		/// Gets the prefix value for the specified distance.
		/// </summary>
		/// <param name="distance">The distance</param>
		/// <returns>The encoding prefix for the distance.</returns>
		[Pure]
		private static uint getPrefix(uint distance)
		{
			// shortcut.			
			if (distance < prefixTableSize)
				return prefixTable[distance];

			// find the index of the most significant set bit.
			// inspired by http://www.hackersdelight.org/hdcodetxt/nlz.c.txt (nlz10b).
			uint x = distance;
			x = x | (x >> 1);
			x = x | (x >> 2);
			x = x | (x >> 4);
			x = x | (x >> 8);
			x = x & ~(x >> 16);
			x = x * 0xFD7049FF;
			int bitIndex = flsTable[x >> 26]; // use fast lookup table.

			// only the two most significant bits are relevant for the prefix code.
			// the following multiplies the bit index by 2 and adds 1 if the second most significant bit is set.
			// e.g.:
			// distance -> prefix
			// 00000001 ->  1
			// 00000010 ->  2
			// 00000100 ->  4
			// 00001000 ->  6
			// 00010000 ->  8
			// 00011000 ->  9
			// 00100000 -> 10

			return (uint)(bitIndex + bitIndex) + ((distance >> (bitIndex - 1)) & 1);
		}

		/// <summary>
		/// Initializes the encoder, resetting the probabilities.
		/// </summary>
		public void Initialize()
		{
			for (int i = 0; i < this.prefixEncoders.Length; i++)
				this.prefixEncoders[i].Initialize();
			this.alignEncoder.Initialize();
			this.posEncoders.InitializeAll();
		}

		/// <summary>
		/// Encodes a match distance.
		/// </summary>
		/// <param name="rangeEncoder">The range encoder to write to.</param>
		/// <param name="length">The length of the match.</param>
		/// <param name="distance">The distance to encode.</param>
		public void Encode(RangeEncoder rangeEncoder, uint length, uint distance)
		{
			length -= Constants.MinMatchLength;
			uint lenToPosState = length;
			if (lenToPosState > Constants.LengthToDistanceStates - 1)
				lenToPosState = Constants.LengthToDistanceStates - 1;

			// encode the distance prefix.
			uint prefix = getPrefix(distance);
			this.prefixEncoders[lenToPosState].Encode(rangeEncoder, prefix);

			// if this is true, the prefix alone is enough to store the distance.
			if (prefix < Constants.DistanceModelStartIndex)
				return;

			// encode the rest of the distance.
			int numFooterBits = (int)((prefix >> 1) - 1);
			uint baseValue = ((2 | (prefix & 1)) << numFooterBits);
			uint posReduced = distance - baseValue;

			if (prefix < Constants.DistanceModelEndIndex)
			{
				BitTreeEncoder.EncodeReverse(rangeEncoder, this.posEncoders, baseValue - prefix - 1, numFooterBits, posReduced);
			}
			else
			{
				rangeEncoder.EncodeDirectBits(posReduced >> Constants.DistanceAlignBits, numFooterBits - Constants.DistanceAlignBits);
				this.alignEncoder.EncodeReverse(rangeEncoder, posReduced & Constants.DistanceAlignMask);
			}
		}

		/// <summary>
		/// Gets the price for encoding the specified distance.
		/// </summary>
		/// <param name="length">The length of the match.</param>
		/// <param name="distance">The distance of the match.</param>
		/// <returns>The price to encode the distance.</returns>
		[Pure]
		public uint GetPrice(uint length, uint distance)
		{
			uint price = 0;

			length -= Constants.MinMatchLength;
			uint lenToPosState = length;
			if (lenToPosState > Constants.LengthToDistanceStates - 1)
				lenToPosState = Constants.LengthToDistanceStates - 1;

			// get the distance prefix price.
			uint prefix = getPrefix(distance);
			price += this.prefixEncoders[lenToPosState].GetPrice(prefix);

			// get price for the rest.
			if (prefix >= Constants.DistanceModelStartIndex)
			{
				int numFooterBits = (int)((prefix >> 1) - 1);
				uint baseValue = ((2 | (prefix & 1)) << numFooterBits);
				uint posReduced = distance - baseValue;

				if (prefix < Constants.DistanceModelEndIndex)
				{
					price += BitTreeEncoder.GetPriceReverse(this.posEncoders, baseValue - prefix - 1, numFooterBits, posReduced);
				}
				else
				{
					price += ((uint)numFooterBits - Constants.DistanceAlignBits) * Probability.IdentityPrice;
					price += this.alignEncoder.GetPriceReverse(posReduced & Constants.DistanceAlignMask);
				}
			}

			return price;
		}

		#endregion
	}
}
