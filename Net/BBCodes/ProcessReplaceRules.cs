namespace Ecng.Net.BBCodes
{
	using System;
	using System.Collections.Generic;
	using System.Threading;
	using System.Threading.Tasks;

    using Ecng.Common;

	/// <summary>
	/// Provides a way to handle layers of replacements rules
	/// </summary>
	public class ProcessReplaceRules<TContext> : ICloneable, IProcessReplaceRules<TContext>
  {
    #region Constants and Fields

    /// <summary>
    ///   The _rules list.
    /// </summary>
    private readonly List<IReplaceRule<TContext>> _rulesList;

    /// <summary>
    ///   The _rules lock.
    /// </summary>
    private readonly object _rulesLock = new();

    /// <summary>
    ///   The _need sort.
    /// </summary>
    private bool _needSort;

    #endregion

    #region Constructors and Destructors

    /// <summary>
    ///   Initializes a new instance of the <see cref = "ProcessReplaceRules" /> class.
    /// </summary>
    public ProcessReplaceRules()
    {
      _rulesList = new List<IReplaceRule<TContext>>();
    }

    #endregion

    #region Properties

    /// <summary>
    ///   Gets a value indicating whether any rules have been added.
    /// </summary>
    public bool HasRules
    {
      get
      {
        lock (_rulesLock)
        {
          return _rulesList.Count > 0;
        }
      }
    }

    #endregion

    #region Implemented Interfaces

    #region ICloneable

    /// <summary>
    /// This clone method is a Deep Clone -- including all data.
    /// </summary>
    /// <returns>
    /// The clone.
    /// </returns>
    public object Clone()
    {
      var copyReplaceRules = new ProcessReplaceRules<TContext>();

      // move the rules over...
      var ruleArray = new IReplaceRule<TContext>[_rulesList.Count];
      _rulesList.CopyTo(ruleArray);
      copyReplaceRules._rulesList.InsertRange(0, ruleArray);
      copyReplaceRules._needSort = _needSort;

      return copyReplaceRules;
    }

    #endregion

    #region IProcessReplaceRules

    /// <summary>
    /// The add rule.
    /// </summary>
    /// <param name="newRule">
    /// The new rule.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// </exception>
    public void AddRule(IReplaceRule<TContext> newRule)
    {
      if (newRule is null)
      {
        throw new ArgumentNullException(nameof(newRule));
      }

      lock (_rulesLock)
      {
        _rulesList.Add(newRule);
        _needSort = true;
      }
    }

    /// <summary>
    /// The process.
    /// </summary>
    /// <param name="text">
    /// The text.
    /// </param>
    public async Task<string> ProcessAsync(TContext context, string text, CancellationToken cancellationToken)
    {
      if (text.IsEmptyOrWhiteSpace())
      {
        return text;
      }

      // sort the rules according to rank...
      if (_needSort)
      {
        lock (_rulesLock)
        {
          _rulesList.Sort();
          _needSort = false;
        }
      }

      // make the replacementCollection for this instance...
      var mainCollection = new ReplaceBlocksCollection();

      // get as local list...
      var localRulesList = new List<IReplaceRule<TContext>>();

      lock (_rulesLock)
      {
        localRulesList.AddRange(_rulesList);
      }

      // apply all rules...
      foreach (var rule in localRulesList)
      {
        text = await rule.ReplaceAsync(context, text, mainCollection, cancellationToken);
      }

      // reconstruct the html
      mainCollection.Reconstruct(ref text);

	  return text;
    }

    #endregion

    #endregion
  }
}