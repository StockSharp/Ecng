namespace Ecng.Net.BBCodes
{
	using System;
	using System.Threading;
	using System.Threading.Tasks;

	/// <summary>
	/// Base class for all replacement rules.
	///   Provides compare functionality based on the rule rank.
	///   Override replace to handle replacement differently.
	/// </summary>
	public abstract class BaseReplaceRule<TContext> : IComparable, IReplaceRule<TContext>
  {
    #region Constants and Fields

    /// <summary>
    ///   The rule rank.
    /// </summary>
    public int RuleRank = 50;

    #endregion

    #region Properties

    /// <summary>
    ///   Gets RuleDescription.
    /// </summary>
    public virtual string RuleDescription
    {
      get
      {
        return string.Empty;
      }
    }

    #endregion

    #region Implemented Interfaces

    #region IBaseReplaceRule

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
    public abstract ValueTask<string> ReplaceAsync(TContext context, string text, IReplaceBlocks replacement, CancellationToken cancellationToken);

    #endregion

    #region IComparable

    /// <summary>
    /// The compare to.
    /// </summary>
    /// <param name="obj">
    /// The obj.
    /// </param>
    /// <returns>
    /// The compare to.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// </exception>
    public int CompareTo(object obj)
    {
      if (obj is BaseReplaceRule<TContext>)
      {
        var otherRule = obj as BaseReplaceRule<TContext>;

        if (RuleRank > otherRule.RuleRank)
        {
          return 1;
        }
        else if (RuleRank < otherRule.RuleRank)
        {
          return -1;
        }

        return 0;
      }
      else
      {
        throw new ArgumentException("Object is not of type BaseReplaceRule.");
      }
    }

    #endregion

    #endregion
  }
}