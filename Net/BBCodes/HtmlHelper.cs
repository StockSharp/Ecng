namespace Ecng.Net.BBCodes
{
	using System.Collections.Generic;
	using System.Linq;
	using System.Text.RegularExpressions;

	/// <summary>
	/// The html helper.
	/// </summary>
	public static class HtmlHelper
  {
    /// <summary>
    /// The strip html.
    /// </summary>
    /// <param name="text">
    /// The text.
    /// </param>
    /// <returns>
    /// The strip html.
    /// </returns>
    public static string StripHtml(this string text)
    {
      return Regex.Replace(text, @"<(.|\n)*?>", string.Empty);
    }

    /// <summary>
    /// The clean html string.
    /// </summary>
    /// <param name="text">
    /// The text.
    /// </param>
    /// <returns>
    /// The clean html string.
    /// </returns>
    public static string CleanHtmlString(this string text)
    {
      text = text.Replace("<br />", " ");
      text = text.Replace("&quot;", "\"");
      text = text.Replace("&nbsp;", " ");

      return text;
    }

    /// <summary>
    /// Validates an html tag against the allowedTags. Also check that
    /// it doesn't have any "extra" features such as javascript in it.
    /// </summary>
    /// <param name="tag">
    /// </param>
    /// <param name="allowedTags">
    /// </param>
    /// <returns>
    /// The is valid tag.
    /// </returns>
    public static bool IsValidTag(string tag, IEnumerable<string> allowedTags)
    {
      if (tag.IndexOf("javascript") >= 0)
      {
        return false;
      }

      if (tag.IndexOf("vbscript") >= 0)
      {
        return false;
      }

      if (tag.IndexOf("onclick") >= 0)
      {
        return false;
      }

      var endchars = new[]
        {
          ' ', '>', '/', '\t'
        };

      int pos = tag.IndexOfAny(endchars, 1);
      if (pos > 0)
      {
        tag = tag.Substring(0, pos);
      }

      if (tag[0] == '/')
      {
        tag = tag.Substring(1);
      }

      // check if it's a valid tag
      return allowedTags.Any(allowedTag => tag == allowedTag);
    }
  }
}