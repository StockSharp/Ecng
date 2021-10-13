namespace Ecng.Net.BBCodes
{
	using System.Text.RegularExpressions;

	/// <summary>
	/// For the font size with replace
	/// </summary>
	public class FontSizeRegexReplaceRule<TContext, TDomain> : VariableRegexReplaceRule<TContext, TDomain>
		where TContext : BB2HtmlContext<TDomain>
  {
    #region Constructors and Destructors

    /// <summary>
    /// Initializes a new instance of the <see cref="FontSizeRegexReplaceRule"/> class.
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
    public FontSizeRegexReplaceRule(string regExSearch, string regExReplace, RegexOptions regExOptions)
      : base(regExSearch, regExReplace, regExOptions, new[] { "size" }, new[] { "5" })
    {
      RuleRank = 25;
    }

    #endregion

    #region Methods

    /// <summary>
    /// The manage variable value.
    /// </summary>
    /// <param name="variableName">
    /// The variable name.
    /// </param>
    /// <param name="variableValue">
    /// The variable value.
    /// </param>
    /// <param name="handlingValue">
    /// The handling value.
    /// </param>
    /// <returns>
    /// The manage variable value.
    /// </returns>
    protected override string ManageVariableValue(TContext context, string variableName, string variableValue, string handlingValue)
    {
      if (variableName == "size")
      {
        return GetFontSize(variableValue);
      }

      return variableValue;
    }

    /// <summary>
    /// The get font size.
    /// </summary>
    /// <param name="inputStr">
    /// The input str.
    /// </param>
    /// <returns>
    /// The get font size.
    /// </returns>
    private string GetFontSize(string inputStr)
    {
      int[] sizes = { 50, 70, 80, 90, 100, 120, 140, 160, 180 };

      // try to parse the input string...
      int.TryParse(inputStr, out var size);

      if (size < 1)
      {
        size = 1;
      }

      if (size > sizes.Length)
      {
        size = 5;
      }

      return sizes[size - 1] + "%";
    }

    #endregion
  }
}