namespace Lzma
{
	/// <summary>
	/// Represents a naive match finder which simply goes through all bytes in the buffer to find matches.
	/// </summary>
	internal sealed class NaiveMatchFinder : IMatchFinder
	{
		#region Fields

		private readonly SlidingWindow window;
		private readonly uint maxDistance;
		private readonly uint minLength;
		
		#endregion

		#region Properties

		#endregion

		#region Constructors

		/// <summary>
		/// Creates a new NaiveMatchFinder.
		/// </summary>
		public NaiveMatchFinder(SlidingWindow window, uint maxDistance, uint minLength)
		{
			this.window = window;
			this.maxDistance = maxDistance;
			this.minLength = minLength;
		}

		#endregion

		#region Methods

		/// <summary>
		/// Initializes the match finder.
		/// </summary>
		public void Initialize()
		{

		}

		/// <summary>
		/// This method is called whenever the sliding window moves forward and the match finder should insert all bytes that have been processed.
		/// </summary>
		/// <param name="numBytes">The number of bytes that have been processed.</param>
		public void Insert(uint numBytes)
		{

		}

		/// <summary>
		/// Find matches. The callback is called for every match that is found.
		/// If the callback returns false, the search is aborted.
		/// </summary>
		/// <param name="position">The starting position.</param>
		/// <param name="maxLength">The maximum match length.</param>
		/// <param name="callback">The callback to use.</param>
		public void FindMatches(uint position, uint maxLength, MatchCallback callback)
		{
			uint matchPos = position == 0 ? this.window.bufferSize - 1 : position - 1;
			uint max = System.Math.Min(this.maxDistance, this.window.totalPosition);

			// go backwards through the buffer, looking for matches.
			for (uint i = 1; i < max; i++)
			{
				// decrement position and wrap around if we hit the start.
				if (matchPos == 0)
					matchPos = this.window.bufferSize - 1;
				else
					matchPos--;

				// get length of match.
				uint length = this.window.GetMatchLength(position, matchPos, maxLength);
				if (length >= this.minLength)
				{
					if (!callback(i, length))
						break;
				}
			}
		}

		#endregion
	}
}
