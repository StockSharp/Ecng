namespace Ecng.Net.BBCodes
{
	using System;

	/// <summary>
	/// Replace Rules Interface
	/// </summary>
	public interface IProcessReplaceRules<TContext>
  {
    #region Properties

    /// <summary>
    ///   Gets a value indicating whether any rules have been added.
    /// </summary>
    bool HasRules { get; }

    #endregion

    #region Public Methods

    /// <summary>
    /// The add rule.
    /// </summary>
    /// <param name="newRule">
    /// The new rule.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// </exception>
    void AddRule(IReplaceRule<TContext> newRule);

    /// <summary>
    /// Process text using the rules.
    /// </summary>
    /// <param name="text">
    /// The text.
    /// </param>
    void Process(TContext context, ref string text);

    #endregion
  }
}