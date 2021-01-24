namespace Lzma
{
	/// <summary>
	/// LZMA constants.
	/// </summary>
	internal static class Constants
	{
		#region Range Coding

		/// <summary>
		/// Maximum range for the range coder.
		/// </summary>
		public const uint MaxRange = (1 << 24);

		#endregion

		#region Length

		/// <summary>
		/// The minimum length (in bytes) of matches.
		/// </summary>
		public const int MinMatchLength = 2;

		/// <summary>
		/// The number of bits used for encoding small lengths.
		/// </summary>
		public const int LengthLowBits = 3;

		/// <summary>
		/// The number of bits used for encoding lengths greater than "low".
		/// </summary>
		public const int LengthMidBits = 3;

		/// <summary>
		/// The number of bits used for encoding lengths greater than "mid".
		/// </summary>
		public const int LengthHighBits = 8;

		/// <summary>
		/// The exclusive maximum length that can be encoded with the low length encoder.
		/// </summary>
		public const uint LengthLowMax = 1u << LengthLowBits;

		/// <summary>
		/// The exclusive maximum length that can be encoded with the mid length encoder.
		/// </summary>
		public const uint LengthMidMax = 1u << LengthMidBits;

		/// <summary>
		/// The exclusive maximum length that can be encoded with the high length encoder.
		/// </summary>
		public const uint LengthHighMax = 1u << LengthHighBits;

		/// <summary>
		/// The exclusive maxmium length that can be encoded with the length encoder.
		/// </summary>
		public const uint LengthMax = LengthLowMax + LengthMidMax + LengthHighMax;

		#endregion

		#region Distance

		/// <summary>
		/// The upper bound of a range of lengths that influence the distance encoder state.
		/// </summary>
		public const int LengthToDistanceStates = 4;

		public const int DistanceAlignBits = 4;

		public const uint DistanceAlignMask = (1u << DistanceAlignBits) - 1u;
		
		/// <summary>
		/// The start index of the distance model.
		/// </summary>
		public const int DistanceModelStartIndex = 4;

		/// <summary>
		/// The end index of the distance model.
		/// </summary>
		public const int DistanceModelEndIndex = 14;

		public const int FullDistances = (1 << (DistanceModelEndIndex >> 1));

		#endregion
	}
}
