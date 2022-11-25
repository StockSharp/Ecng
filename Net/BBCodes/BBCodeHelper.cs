namespace Ecng.Net.BBCodes;

using System.Text.RegularExpressions;

/// <summary>
/// The bb code helper.
/// </summary>
public static class BBCodeHelper
{
	/// <summary>
	/// The strip bb code.
	/// </summary>
	/// <param name="text">
	/// The text.
	/// </param>
	/// <returns>
	/// The strip bb code.
	/// </returns>
	public static string StripBBCode(this string text)
	{
		return Regex.Replace(text, @"\[(.|\n)*?\]", string.Empty);
	}

	/// <summary>
	/// Strip Quote BB Code Quotes including the quoted text
	/// </summary>
	/// <param name="text">Text to check
	/// </param>
	/// <returns>The Cleaned Text
	/// </returns>
	public static string StripBBCodeQuotes(this string text)
	{
		return Regex.Replace(text, @"\[quote\b[^>]*](.|\n)*?\[/quote\]", string.Empty, RegexOptions.Multiline);
	}

#if NETSTANDARD2_0
	public static bool TryGetValue(this GroupCollection groups, string name, out Group g)
	{
		g = groups[name];

		if (g.Success)
			return true;

		g = null;
		return false;
	}
#endif
}