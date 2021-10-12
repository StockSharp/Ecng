namespace Ecng.Net.BBCodes
{
	using System.Text;
	using System.Text.RegularExpressions;
	using System.Threading;
	using System.Threading.Tasks;

	/// <summary>
	/// For basic regex with no variables
	/// </summary>
	public class SingleRegexReplaceRule<TContext> : SimpleRegexReplaceRule<TContext>
		where TContext : BBCodesContext
  {
    #region Constructors and Destructors

    /// <summary>
    /// Initializes a new instance of the <see cref="SingleRegexReplaceRule"/> class.
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
    public SingleRegexReplaceRule(string regExSearch, string regExReplace, RegexOptions regExOptions)
      : base(regExSearch, regExReplace, regExOptions)
    {
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

        // just replaces with no "inner"
        int replaceIndex = replacement.Add(RegExReplace(context.LangCode));

        // remove old bbcode...
        sb.Remove(m.Groups[0].Index, m.Groups[0].Length);

        // insert replaced value(s)
        sb.Insert(m.Groups[0].Index, replacement.Get(replaceIndex));

        // text = text.Substring( 0, m.Groups [0].Index ) + replacement.GetReplaceValue( replaceIndex ) + text.Substring( m.Groups [0].Index + m.Groups [0].Length );
        m = RegExSearch.Match(sb.ToString());
      }

      return Task.FromResult(sb.ToString());
    }

    #endregion
  }
}