namespace Ecng.Net.BBCodes
{
	using System.Text;
	using System.Text.RegularExpressions;

	/// <summary>
	/// The i replace blocks extensions.
	/// </summary>
	public static class IReplaceBlocksExtensions
  {
    #region Constants and Fields

    /// <summary>
    /// The _options.
    /// </summary>
    private static readonly RegexOptions _options = RegexOptions.IgnoreCase | RegexOptions.Multiline;

    /// <summary>
    /// The _reg ex html.
    /// </summary>
    private static readonly Regex _regExHtml =
      new Regex(@"</?\w+((\s+\w+(\s*=\s*(?:"".*?""|'.*?'|[^'"">\s]+))?)+\s*|\s*)/?>", _options | RegexOptions.Compiled);

    #endregion

    #region Public Methods

    /// <summary>
    /// Pull replacement blocks from the text
    /// </summary>
    /// <param name="replaceBlocks">
    /// The replace Blocks.
    /// </param>
    /// <param name="strText">
    /// The str Text.
    /// </param>
    public static void ReplaceHtmlFromText(this IReplaceBlocks replaceBlocks, ref string strText)
    {
      var sb = new StringBuilder(strText);

      ReplaceHtmlFromText(replaceBlocks, ref sb);

      strText = sb.ToString();
    }

    /// <summary>
    /// The get replacements from text.
    /// </summary>
    /// <param name="replaceBlocks">
    /// The replace Blocks.
    /// </param>
    /// <param name="sb">
    /// The sb.
    /// </param>
    public static void ReplaceHtmlFromText(this IReplaceBlocks replaceBlocks, ref StringBuilder sb)
    {
      Match m = _regExHtml.Match(sb.ToString());

      while (m.Success)
      {
        // add it to the list...
        int index = replaceBlocks.Add(m.Groups[0].Value);

        // replacement lookup code
        string replace = replaceBlocks.Get(index);

        // remove the replaced item...
        sb.Remove(m.Groups[0].Index, m.Groups[0].Length);

        // insert the replaced value back in...
        sb.Insert(m.Groups[0].Index, replace);

        // text = text.Substring( 0, m.Groups [0].Index ) + replace + text.Substring( m.Groups [0].Index + m.Groups [0].Length );
        m = _regExHtml.Match(sb.ToString());
      }
    }

    #endregion
  }
}