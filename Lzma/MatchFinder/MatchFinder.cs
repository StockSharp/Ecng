namespace Lzma
{
	/// <summary>
	/// Match callback.
	/// </summary>
	/// <param name="distance"></param>
	/// <param name="length"></param>
	/// <returns></returns>
	internal delegate bool MatchCallback(uint distance, uint length);

	/// <summary>
	/// An abstract string match finding algorithm.
	/// </summary>
	internal interface IMatchFinder
	{
		/// <summary>
		/// Initializes the match finder.
		/// </summary>
		void Initialize();

		/// <summary>
		/// This method is called whenever the sliding window moves forward and the match finder should insert all bytes that have been processed.
		/// </summary>
		/// <param name="numBytes">The number of bytes that have been processed.</param>
		void Insert(uint numBytes);

		/// <summary>
		/// Find matches. The callback is called for every match that is found.
		/// If the callback returns false, the search is aborted.
		/// </summary>
		/// <param name="position">The starting position.</param>
		/// <param name="maxLength">The maximum match length.</param>
		/// <param name="callback">The callback to use.</param>
		void FindMatches(uint position, uint maxLength, MatchCallback callback);
	}
}
