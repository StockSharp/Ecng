namespace Ecng.Net.BBCodes
{
	using System;
	using System.Text.RegularExpressions;
	using System.Threading;
	using System.Threading.Tasks;

	/// <summary>
	/// Simple code block regular express replace
	/// </summary>
	public class CodeRegexReplaceRule<TContext, TDomain> : SimpleRegexReplaceRule<TContext, TDomain>
		where TContext : BBCodesContext<TDomain>
	{
    #region Constructors and Destructors

    /// <summary>
    /// Initializes a new instance of the <see cref="CodeRegexReplaceRule"/> class.
    /// </summary>
    /// <param name="regExSearch">
    /// The reg ex search.
    /// </param>
    /// <param name="regExReplace">
    /// The reg ex replace.
    /// </param>
    public CodeRegexReplaceRule(Regex regExSearch, Func<TDomain, string> regExReplace)
      : base(regExSearch, regExReplace)
    {
      // default high rank...
      RuleRank = 2;
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
      Match m = RegExSearch.Match(text);
      while (m.Success)
      {
	    cancellationToken.ThrowIfCancellationRequested();

        string replaceItem = RegExReplace(context.Domain).Replace("${inner}", GetInnerValue(m.Groups["inner"].Value));

        int replaceIndex = replacement.Add(replaceItem);
        text = text.Substring(0, m.Groups[0].Index) + replacement.Get(replaceIndex) +
               text.Substring(m.Groups[0].Index + m.Groups[0].Length);

        m = RegExSearch.Match(text);
      }

	  return Task.FromResult(text);
    }

    #endregion

    #region Methods

    /// <summary>
    /// This just overrides how the inner value is handled
    /// </summary>
    /// <param name="innerValue">
    /// </param>
    /// <returns>
    /// The get inner value.
    /// </returns>
    protected override string GetInnerValue(string innerValue)
    {
      innerValue = innerValue.Replace("\t", "&nbsp; &nbsp;&nbsp;");
      innerValue = innerValue.Replace("[", "&#91;");
      innerValue = innerValue.Replace("]", "&#93;");
      innerValue = innerValue.Replace("<", "&lt;");
      innerValue = innerValue.Replace(">", "&gt;");
      innerValue = innerValue.Replace("\r\n", "<br />");
      // TODO: vzrus there should not be contsructions with string.Replace and double whitespace to replace.
      // it can lead to server overloads in some situations. Seems OK.
      // TODO : tha_watcha _this creates duplicated whitespace, in simple texts its not really needed, to replace it.
      //innerValue = Regex.Replace(innerValue, @"\s+", " &nbsp;").Trim();
      // vzrus: No matter I mean comstructions like .Replace("  "," ")
      return innerValue;
    }

    #endregion
  }
}