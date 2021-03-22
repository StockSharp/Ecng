namespace Ecng.Net.BBCodes
{
	using System;

	/// <summary>
	/// Base Replace Rules Interface
	/// </summary>
	public interface IReplaceRule<TContext>
  {
    #region Properties

    /// <summary>
    ///   Gets RuleDescription.
    /// </summary>
    string RuleDescription { get; }

    #endregion

    #region Public Methods

    /// <summary>
    /// The replace.
    /// </summary>
    /// <param name="text">
    /// The text.
    /// </param>
    /// <param name="replacement">
    /// The replacement.
    /// </param>
    /// <exception cref="NotImplementedException">
    /// </exception>
    void Replace(TContext context, ref string text, IReplaceBlocks replacement);

    #endregion
  }
}