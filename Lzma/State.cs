namespace Lzma
{
	/// <summary>
	/// State index of the encoder or decoder.
	/// </summary>
	public struct State
	{
		#region Constants

		/// <summary>
		/// The number of possible states.
		/// </summary>
		public const uint NumStates = 12;

		#endregion

		#region Fields

		/// <summary>
		/// The integer value of the state.
		/// </summary>
		public uint Value;

		#endregion

		#region Properties

		/// <summary>
		/// Gets a value that determines whether the last operation was a literal.
		/// </summary>
		public bool IsLiteral => this.Value < 7u;

		#endregion

		#region Methods

		/// <summary>
		/// Updates the state.
		/// Called after a literal has been encoded/decoded.
		/// </summary>
		public void UpdateLiteral()
		{
			if (this.Value < 4u)
				this.Value = 0u;
			else if (this.Value < 10u)
				this.Value -= 3;
			else
				this.Value -= 6;
		}

		/// <summary>
		/// Updates the state.
		/// Called after a match has been encoded/decoded.
		/// </summary>
		public void UpdateMatch()
		{
			this.Value = this.Value < 7u ? 7u : 10u;
		}

		/// <summary>
		/// Updates the state.
		/// Called after a rep has been encoded/decoded.
		/// </summary>
		public void UpdateRep()
		{
			this.Value = this.Value < 7u ? 8u : 11u;
		}

		/// <summary>
		/// Updates the state.
		/// Called after a short rep has been encoded/decoded.
		/// </summary>
		public void UpdateShortRep()
		{
			this.Value = this.Value < 7u ? 9u : 11u;
		}

		public override string ToString()
		{
			return this.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
		}

		#endregion
	}
}
