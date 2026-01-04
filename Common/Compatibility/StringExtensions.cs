#if NETSTANDARD2_0
namespace System;

using System.Text.RegularExpressions;

/// <summary>
/// Compatibility extensions for <see cref="string"/>.
/// </summary>
public static class StringExtensions
{
	/// <summary>
	/// Returns a value indicating whether a specified string occurs within this string, using the specified comparison rules.
	/// </summary>
	/// <param name="str">The string to search in.</param>
	/// <param name="value">The string to seek.</param>
	/// <param name="comparisonType">One of the enumeration values that specifies the rules for the search.</param>
	/// <returns><c>true</c> if the value parameter occurs within this string; otherwise, <c>false</c>.</returns>
	public static bool Contains(this string str, string value, StringComparison comparisonType)
	{
		return str.IndexOf(value, comparisonType) >= 0;
	}

	/// <summary>
	/// Returns a new string in which all occurrences of a specified string in the current instance are replaced with another specified string, using the provided comparison type.
	/// </summary>
	/// <param name="str">The string to search in.</param>
	/// <param name="oldValue">The string to be replaced.</param>
	/// <param name="newValue">The string to replace all occurrences of oldValue.</param>
	/// <param name="comparisonType">One of the enumeration values that specifies the rules for the search.</param>
	/// <returns>A string that is equivalent to the current string except that all instances of oldValue are replaced with newValue.</returns>
	public static string Replace(this string str, string oldValue, string newValue, StringComparison comparisonType)
	{
		if (comparisonType == StringComparison.InvariantCultureIgnoreCase || comparisonType == StringComparison.OrdinalIgnoreCase || comparisonType == StringComparison.CurrentCultureIgnoreCase)
			return Regex.Replace(str, Regex.Escape(oldValue), newValue.Replace("$", "$$"), RegexOptions.IgnoreCase);

		// For case-sensitive comparisons, use simple string replace
		return str.Replace(oldValue, newValue);
	}
}
#endif
