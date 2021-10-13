namespace Ecng.Net.BBCodes
{
	using System.Text;
	using System.Text.RegularExpressions;
	using System.Web;
	using System.Threading;
	using System.Threading.Tasks;

	using Ecng.Common;

	/// <summary>
	/// For complex regex with variable/default and truncate support
	/// </summary>
	public class VariableRegexReplaceRule<TContext, TDomain> : SimpleRegexReplaceRule<TContext, TDomain>
		where TContext : BBCodesContext<TDomain>
  {
    #region Constants and Fields

    /// <summary>
    ///   The _truncate length.
    /// </summary>
    protected readonly int TruncateLength;

    /// <summary>
    ///   The _variable defaults.
    /// </summary>
    protected readonly string[] VariableDefaults;

    /// <summary>
    ///   The _variables.
    /// </summary>
    protected readonly string[] Variables;

    #endregion

    #region Constructors and Destructors

    /// <summary>
    /// Initializes a new instance of the <see cref="VariableRegexReplaceRule"/> class.
    /// </summary>
    /// <param name="regExSearch">
    /// The reg ex search.
    /// </param>
    /// <param name="regExReplace">
    /// The reg ex replace.
    /// </param>
    /// <param name="variables">
    /// The variables.
    /// </param>
    /// <param name="varDefaults">
    /// The var defaults.
    /// </param>
    /// <param name="truncateLength">
    /// The truncate length.
    /// </param>
    public VariableRegexReplaceRule(
      Regex regExSearch, string regExReplace, string[] variables, string[] varDefaults, int truncateLength)
      : base(regExSearch, regExReplace)
    {
      Variables = variables;
      VariableDefaults = varDefaults;
      TruncateLength = truncateLength;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VariableRegexReplaceRule"/> class.
    /// </summary>
    /// <param name="regExSearch">
    /// The reg ex search.
    /// </param>
    /// <param name="regExReplace">
    /// The reg ex replace.
    /// </param>
    /// <param name="variables">
    /// The variables.
    /// </param>
    /// <param name="varDefaults">
    /// The var defaults.
    /// </param>
    public VariableRegexReplaceRule(Regex regExSearch, string regExReplace, string[] variables, string[] varDefaults)
      : this(regExSearch, regExReplace, variables, varDefaults, 0)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VariableRegexReplaceRule"/> class.
    /// </summary>
    /// <param name="regExSearch">
    /// The reg ex search.
    /// </param>
    /// <param name="regExReplace">
    /// The reg ex replace.
    /// </param>
    /// <param name="variables">
    /// The variables.
    /// </param>
    public VariableRegexReplaceRule(Regex regExSearch, string regExReplace, string[] variables)
      : this(regExSearch, regExReplace, variables, null, 0)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VariableRegexReplaceRule"/> class.
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
    /// <param name="variables">
    /// The variables.
    /// </param>
    /// <param name="varDefaults">
    /// The var defaults.
    /// </param>
    /// <param name="truncateLength">
    /// The truncate length.
    /// </param>
    public VariableRegexReplaceRule(
      string regExSearch, 
      string regExReplace, 
      RegexOptions regExOptions, 
      string[] variables, 
      string[] varDefaults, 
      int truncateLength)
      : base(regExSearch, regExReplace, regExOptions)
    {
      Variables = variables;
      VariableDefaults = varDefaults;
      TruncateLength = truncateLength;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VariableRegexReplaceRule"/> class.
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
    /// <param name="variables">
    /// The variables.
    /// </param>
    /// <param name="varDefaults">
    /// The var defaults.
    /// </param>
    public VariableRegexReplaceRule(
      string regExSearch, string regExReplace, RegexOptions regExOptions, string[] variables, string[] varDefaults)
      : this(regExSearch, regExReplace, regExOptions, variables, varDefaults, 0)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VariableRegexReplaceRule"/> class.
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
    /// <param name="variables">
    /// The variables.
    /// </param>
    public VariableRegexReplaceRule(
      string regExSearch, string regExReplace, RegexOptions regExOptions, string[] variables)
      : this(regExSearch, regExReplace, regExOptions, variables, null, 0)
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

        var innerReplace = new StringBuilder(RegExReplace(context.Domain));
        int i = 0;

        foreach (string tVar in Variables)
        {
          string varName = tVar;
          string handlingValue = string.Empty;

          if (varName.Contains(":"))
          {
            // has handling section
            string[] tmpSplit = varName.Split(':');
            varName = tmpSplit[0];
            handlingValue = tmpSplit[1];
          }

          string tValue = m.Groups[varName].Value;

          if (VariableDefaults != null && tValue.Length == 0)
          {
            // use default instead
            tValue = VariableDefaults[i];
          }

          innerReplace.Replace("${" + varName + "}", ManageVariableValue(context, varName, tValue, handlingValue));
          i++;
        }

        innerReplace.Replace("${inner}", m.Groups["inner"].Value);

        if (TruncateLength > 0)
        {
          // special handling to truncate urls
          innerReplace.Replace(
            "${innertrunc}", m.Groups["inner"].Value.TruncateMiddle(TruncateLength));
        }

        // pulls the htmls into the replacement collection before it's inserted back into the main text
        replacement.ReplaceHtmlFromText(ref innerReplace, cancellationToken);

        // remove old bbcode...
        sb.Remove(m.Groups[0].Index, m.Groups[0].Length);

        // insert replaced value(s)
        sb.Insert(m.Groups[0].Index, innerReplace.ToString());

        // text = text.Substring( 0, m.Groups [0].Index ) + tStr + text.Substring( m.Groups [0].Index + m.Groups [0].Length );
        m = RegExSearch.Match(sb.ToString());
      }

      return Task.FromResult(sb.ToString());
    }

    #endregion

    #region Methods

    /// <summary>
    /// Override to change default variable handling...
    /// </summary>
    /// <param name="variableName">
    /// </param>
    /// <param name="variableValue">
    /// </param>
    /// <param name="handlingValue">
    /// variable transfermation desired
    /// </param>
    /// <returns>
    /// The manage variable value.
    /// </returns>
    protected virtual string ManageVariableValue(TContext context, string variableName, string variableValue, string handlingValue)
    {
      if (!handlingValue.IsEmptyOrWhiteSpace())
      {
        switch (handlingValue.ToLower())
        {
          case "decode":
            variableValue = HttpUtility.HtmlDecode(variableValue);
            break;
          case "encode":
            variableValue = HttpUtility.HtmlEncode(variableValue);
            break;
        }
      }

      return variableValue;
    }

    #endregion
  }
}