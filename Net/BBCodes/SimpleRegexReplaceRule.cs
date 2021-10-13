namespace Ecng.Net.BBCodes
{
	using System;
	using System.Text;
	using System.Text.RegularExpressions;
	using System.Threading;
	using System.Threading.Tasks;

	/// <summary>
	/// For basic regex with no variables
	/// </summary>
	public class SimpleRegexReplaceRule<TContext, TDomain> : BaseReplaceRule<TContext>
		where TContext : BBCodesContext<TDomain>
  {
    #region Constants and Fields

    /// <summary>
    ///   The _reg ex replace.
    /// </summary>
    protected readonly Func<TDomain, string> RegExReplace;

    /// <summary>
    ///   The _reg ex search.
    /// </summary>
    protected readonly Regex RegExSearch;

    #endregion

    #region Constructors and Destructors

    /// <summary>
    /// Initializes a new instance of the <see cref="SimpleRegexReplaceRule"/> class.
    /// </summary>
    /// <param name="regExSearch">
    /// The reg ex search.
    /// </param>
    /// <param name="regExReplace">
    /// The reg ex replace.
    /// </param>
    /// <param name="regExOptions">
    /// The reg ex options.
    /// </param>
    public SimpleRegexReplaceRule(string regExSearch, string regExReplace, RegexOptions regExOptions)
		: this(regExSearch, c => regExReplace, regExOptions)
	{
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SimpleRegexReplaceRule"/> class.
    /// </summary>
    /// <param name="regExSearch">
    /// The reg ex search.
    /// </param>
    /// <param name="regExReplace">
    /// The reg ex replace.
    /// </param>
    public SimpleRegexReplaceRule(Regex regExSearch, string regExReplace)
		: this(regExSearch, c => regExReplace)
    {
    }

	/// <summary>
    /// Initializes a new instance of the <see cref="SimpleRegexReplaceRule"/> class.
    /// </summary>
    /// <param name="regExSearch">
    /// The reg ex search.
    /// </param>
    /// <param name="regExReplace">
    /// The reg ex replace.
    /// </param>
    /// <param name="regExOptions">
    /// The reg ex options.
    /// </param>
    public SimpleRegexReplaceRule(string regExSearch, Func<TDomain, string> regExReplace, RegexOptions regExOptions)
    {
      RegExSearch = new Regex(regExSearch, regExOptions);
      RegExReplace = regExReplace;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SimpleRegexReplaceRule"/> class.
    /// </summary>
    /// <param name="regExSearch">
    /// The reg ex search.
    /// </param>
    /// <param name="regExReplace">
    /// The reg ex replace.
    /// </param>
    public SimpleRegexReplaceRule(Regex regExSearch, Func<TDomain, string> regExReplace)
    {
      RegExSearch = regExSearch;
      RegExReplace = regExReplace;
    }

    #endregion

    #region Properties

    /// <summary>
    ///   Gets RuleDescription.
    /// </summary>
    public override string RuleDescription
    {
      get
      {
        return $"RegExSearch = \"{RegExSearch}\"";
      }
    }

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
    public override Task<string> ReplaceAsync(TContext context, string text, IReplaceBlocks replacement, CancellationToken cancellationToken)
    {
      var sb = new StringBuilder(text);

      Match m = RegExSearch.Match(text);
      while (m.Success)
      {
	    cancellationToken.ThrowIfCancellationRequested();

        string replaceString = RegExReplace(context.Domain).Replace("${inner}", GetInnerValue(m.Groups["inner"].Value));

        // pulls the htmls into the replacement collection before it's inserted back into the main text
        replacement.ReplaceHtmlFromText(ref replaceString, cancellationToken);

        // remove old bbcode...
        sb.Remove(m.Groups[0].Index, m.Groups[0].Length);

        // insert replaced value(s)
        sb.Insert(m.Groups[0].Index, replaceString);

        // text = text.Substring( 0, m.Groups [0].Index ) + tStr + text.Substring( m.Groups [0].Index + m.Groups [0].Length );
        m = RegExSearch.Match(sb.ToString());
      }

      return Task.FromResult(sb.ToString());
    }

    #endregion

    #region Methods

    /// <summary>
    /// The get inner value.
    /// </summary>
    /// <param name="innerValue">
    /// The inner value.
    /// </param>
    /// <returns>
    /// The get inner value.
    /// </returns>
    protected virtual string GetInnerValue(string innerValue)
    {
      return innerValue;
    }

    #endregion
  }
}