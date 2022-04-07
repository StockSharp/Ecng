namespace Ecng.Net.BBCodes
{
	using System;
	using System.Threading;
	using System.Threading.Tasks;

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
    ValueTask<string> ReplaceAsync(TContext context, string text, IReplaceBlocks replacement, CancellationToken cancellationToken = default);

    #endregion
  }
}