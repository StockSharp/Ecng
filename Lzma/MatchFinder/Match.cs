namespace Lzma
{
	/// <summary>
	/// Represents a match.
	/// </summary>
	[System.CLSCompliant(false)]
	public struct Match
	{
		#region Fields

		/// <summary>
		/// The distance of the match.
		/// </summary>
		public uint Distance;

		/// <summary>
		/// The length of the match.
		/// </summary>
		public uint Length;

		#endregion

		#region Properties

		#endregion

		#region Constructors

		/// <summary>
		/// Creates a new Match.
		/// </summary>
		/// <param name="distance">The match distance.</param>
		/// <param name="length">The match length.</param>
		public Match(uint distance, uint length)
		{
			this.Distance = distance;
			this.Length = length;
		}

		#endregion

		#region Methods

		/// <summary>
		/// Converts the match to a string.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return $"{this.Distance}, {this.Length}";
		}

		#endregion
	}
}
