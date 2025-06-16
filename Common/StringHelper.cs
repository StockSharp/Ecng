namespace Ecng.Common;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Reflection;

using SmartFormat;
using SmartFormat.Core.Extensions;

/// <summary>
/// Provides helper methods for string operations.
/// </summary>
public static class StringHelper
{
	private class DictionarySourceEx : ISource
	{
		private readonly SyncObject _sync = new();
		private readonly Dictionary<Type, Type> _genericTypes = [];
		private readonly Dictionary<string, object> _keys = [];

		bool ISource.TryEvaluateSelector(ISelectorInfo selectorInfo)
		{
			if (selectorInfo.CurrentValue is not IDictionary dictionary)
				return false;

			var dictType = dictionary.GetType();

			Type type;

			lock (_sync)
			{
				if (!_genericTypes.TryGetValue(dictType, out type))
				{
					type = dictType.GetGenericType(typeof(IDictionary<,>));
					_genericTypes.Add(dictType, type);
				}
			}

			if (type is null)
				return false;

			object key;
			var text = selectorInfo.SelectorText;

			lock (_sync)
			{
				if (!_keys.TryGetValue(text, out key))
				{
					key = text.To(type.GetGenericArguments()[0]);
					_keys.Add(text, key);
				}
			}

			selectorInfo.Result = dictionary[key];
			return true;
		}
	}

	static StringHelper()
	{
		Smart.Default.AddExtensions(new DictionarySourceEx());

		// https://stackoverflow.com/a/47017180
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
	}

	/// <summary>
	/// Checks if the string is null or empty.
	/// </summary>
	/// <param name="str">The string to check.</param>
	/// <returns>True if the string is null or empty; otherwise, false.</returns>
	public static bool IsEmpty(this string str)
	{
		return string.IsNullOrEmpty(str);
	}

	/// <summary>
	/// Returns default value if the string is null or empty; otherwise, returns the original string.
	/// </summary>
	/// <param name="str">The string to check.</param>
	/// <param name="defaultValue">The value to return if string is null or empty.</param>
	/// <returns>Default value or original string.</returns>
	public static string IsEmpty(this string str, string defaultValue)
	{
		return str.IsEmpty() ? defaultValue : str;
	}

	/// <summary>
	/// Throws ArgumentNullException if the string is null or empty.
	/// </summary>
	/// <param name="str">The string to check.</param>
	/// <param name="paramName">The parameter name to use in the exception.</param>
	/// <returns>The original string if not empty.</returns>
	/// <exception cref="ArgumentNullException">Thrown when string is null or empty.</exception>
	public static string ThrowIfEmpty(this string str, string paramName)
		=> str.IsEmpty() ? throw new ArgumentNullException(paramName) : str;

	/// <summary>
	/// Checks if the string is null, empty, or consists only of white-space characters.
	/// </summary>
	/// <param name="str">The string to check.</param>
	/// <returns>True if the string is null, empty, or whitespace; otherwise, false.</returns>
	public static bool IsEmptyOrWhiteSpace(this string str)
	{
		return string.IsNullOrWhiteSpace(str);
	}

	/// <summary>
	/// Returns default value if the string is null, empty, or whitespace; otherwise, returns the original string.
	/// </summary>
	/// <param name="str">The string to check.</param>
	/// <param name="defaultValue">The value to return if string is null, empty, or whitespace.</param>
	/// <returns>Default value or original string.</returns>
	public static string IsEmptyOrWhiteSpace(this string str, string defaultValue)
	{
		return str.IsEmptyOrWhiteSpace() ? defaultValue : str;
	}

	/// <summary>
	/// Formats the string using standard string.Format with the provided arguments.
	/// </summary>
	/// <param name="str">The format string.</param>
	/// <param name="args">The arguments to format the string with.</param>
	/// <returns>A formatted string.</returns>
	/// <exception cref="ArgumentNullException">Thrown when args is null.</exception>
	public static string Put(this string str, params object[] args)
	{
		if (args is null)
			throw new ArgumentNullException(nameof(args));
		return args.Length == 0 ? str : string.Format(str, args);
	}

	/// <summary>
	/// Formats the string using Smart.Format with the provided arguments.
	/// </summary>
	/// <param name="str">The format string.</param>
	/// <param name="args">The arguments to format the string with.</param>
	/// <returns>A formatted string.</returns>
	/// <exception cref="ArgumentNullException">Thrown when args is null.</exception>
	public static string PutEx(this string str, params object[] args)
	{
		if (args is null)
			throw new ArgumentNullException(nameof(args));

		return args.Length == 0 ? str : Smart.Format(str, args);
	}

	/// <summary>
	/// Asynchronously formats the string using Smart.Format with the provided arguments.
	/// </summary>
	/// <param name="str">The format string.</param>
	/// <param name="args">The arguments to format the string with.</param>
	/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
	/// <returns>A ValueTask containing the formatted string.</returns>
	/// <exception cref="ArgumentNullException">Thrown when args is null.</exception>
	public static ValueTask<string> PutExAsync(this string str, object[] args, CancellationToken cancellationToken)
	{
		if (args is null)
			throw new ArgumentNullException(nameof(args));

		return args.Length == 0 ? new(str) : Smart.FormatAsync(str, args, cancellationToken);
	}

	private static Type GetGenericType(this Type targetType, Type genericType)
	{
		if (targetType is null)
			throw new ArgumentNullException(nameof(targetType));

		if (genericType is null)
			throw new ArgumentNullException(nameof(genericType));

		if (!genericType.IsGenericTypeDefinition)
			throw new ArgumentException(nameof(genericType));

		if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == genericType)
			return targetType;
		else
		{
			if (genericType.IsInterface)
			{
				var findedInterfaces = targetType.GetInterfaces()
					.Where(@interface => @interface.IsGenericType && @interface.GetGenericTypeDefinition() == genericType)
					.ToList();

				if (findedInterfaces.Count > 1)
					throw new AmbiguousMatchException("Too many interfaces were found.");
				else if (findedInterfaces.Count == 1)
					return findedInterfaces[0];
				else
					return null;
			}
			else
			{
				return targetType.BaseType != null ? GetGenericType(targetType.BaseType, genericType) : null;
			}
		}
	}


	/// <summary>
	/// Represents the newline character "\n".
	/// </summary>
	public const string N = "\n";

	/// <summary>
	/// Represents the carriage return character "\r".
	/// </summary>
	public const string R = "\r";

	/// <summary>
	/// Represents the carriage return and newline characters "\r\n".
	/// </summary>
	public const string RN = "\r\n";

	/// <summary>
	/// Splits the string by line separators (RN, R, or N).
	/// </summary>
	/// <param name="str">The string to split.</param>
	/// <param name="removeEmptyEntries">If true, removes empty entries from the result.</param>
	/// <returns>An array of substrings.</returns>
	public static string[] SplitByLineSeps(this string str, bool removeEmptyEntries = true)
		// https://stackoverflow.com/a/1547483/8029915
		=> str.Split(
			[RN, R, N],
			removeEmptyEntries ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None
		);

	/// <summary>
	/// Splits the string using the carriage return as the separator.
	/// </summary>
	/// <param name="str">The string to split.</param>
	/// <param name="removeEmptyEntries">If true, removes empty entries.</param>
	/// <returns>An array of substrings.</returns>
	public static string[] SplitByR(this string str, bool removeEmptyEntries = true)
		=> str.SplitBySep(R, removeEmptyEntries);

	/// <summary>
	/// Splits the string using the carriage return and newline as the separator.
	/// </summary>
	/// <param name="str">The string to split.</param>
	/// <param name="removeEmptyEntries">If true, removes empty entries.</param>
	/// <returns>An array of substrings.</returns>
	public static string[] SplitByRN(this string str, bool removeEmptyEntries = true)
		=> str.SplitBySep(RN, removeEmptyEntries);

	/// <summary>
	/// Splits the string using the newline character as the separator.
	/// </summary>
	/// <param name="str">The string to split.</param>
	/// <param name="removeEmptyEntries">If true, removes empty entries.</param>
	/// <returns>An array of substrings.</returns>
	public static string[] SplitByN(this string str, bool removeEmptyEntries = true)
		=> str.SplitBySep(N, removeEmptyEntries);

	/// <summary>
	/// Splits the string by Environment.NewLine.
	/// </summary>
	/// <param name="str">The string to split.</param>
	/// <param name="removeEmptyEntries">If true, removes empty entries.</param>
	/// <returns>An array of substrings.</returns>
	[Obsolete("Use SplitByRN or SplitByN methods.")]
	public static string[] SplitLines(this string str, bool removeEmptyEntries = true)
	{
		return str.SplitBySep(Environment.NewLine, removeEmptyEntries);
	}

	/// <summary>
	/// Splits the string by the specified separator.
	/// </summary>
	/// <param name="str">The string to split.</param>
	/// <param name="separator">The separator string.</param>
	/// <param name="removeEmptyEntries">If true, removes empty entries.</param>
	/// <returns>An array of substrings.</returns>
	[Obsolete("Use SplitBySep method.")]
	public static string[] Split(this string str, string separator, bool removeEmptyEntries = true)
	{
		return str.SplitBySep(separator, removeEmptyEntries);
	}

	/// <summary>
	/// Splits the string by the specified separator.
	/// </summary>
	/// <param name="str">The string to split.</param>
	/// <param name="separator">The separator string.</param>
	/// <param name="removeEmptyEntries">If true, removes empty entries.</param>
	/// <returns>An array of substrings.</returns>
	public static string[] SplitBySep(this string str, string separator, bool removeEmptyEntries = true)
	{
		if (str is null)
			throw new ArgumentNullException(nameof(str));

		if (str.Length == 0)
			return [];

		return str.Split([separator], removeEmptyEntries ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None);
	}

	/// <summary>
	/// Splits the string using a comma as the separator.
	/// </summary>
	/// <param name="str">The string to split.</param>
	/// <param name="removeEmptyEntries">If true, removes empty entries.</param>
	/// <returns>An array of substrings.</returns>
	public static string[] SplitByComma(this string str, bool removeEmptyEntries = false)
		=> str.SplitBySep(",", removeEmptyEntries);

	/// <summary>
	/// Splits the string using a dot as the separator.
	/// </summary>
	/// <param name="str">The string to split.</param>
	/// <param name="removeEmptyEntries">If true, removes empty entries.</param>
	/// <returns>An array of substrings.</returns>
	public static string[] SplitByDot(this string str, bool removeEmptyEntries = false)
		=> str.SplitBySep(".", removeEmptyEntries);

	/// <summary>
	/// Splits the string using a semicolon as the separator.
	/// </summary>
	/// <param name="str">The string to split.</param>
	/// <param name="removeEmptyEntries">If true, removes empty entries.</param>
	/// <returns>An array of substrings.</returns>
	public static string[] SplitByDotComma(this string str, bool removeEmptyEntries = false)
		=> str.SplitBySep(";", removeEmptyEntries);

	/// <summary>
	/// Splits the string using a colon as the separator.
	/// </summary>
	/// <param name="str">The string to split.</param>
	/// <param name="removeEmptyEntries">If true, removes empty entries.</param>
	/// <returns>An array of substrings.</returns>
	public static string[] SplitByColon(this string str, bool removeEmptyEntries = true)
		=> str.SplitBySep(":", removeEmptyEntries);

	/// <summary>
	/// Splits the string using a space as the separator.
	/// </summary>
	/// <param name="str">The string to split.</param>
	/// <param name="removeEmptyEntries">If true, removes empty entries.</param>
	/// <returns>An array of substrings.</returns>
	public static string[] SplitBySpace(this string str, bool removeEmptyEntries = true)
		=> str.SplitBySep(" ", removeEmptyEntries);

	/// <summary>
	/// Splits the string using the equal sign as the separator.
	/// </summary>
	/// <param name="str">The string to split.</param>
	/// <param name="removeEmptyEntries">If true, removes empty entries.</param>
	/// <returns>An array of substrings.</returns>
	public static string[] SplitByEqual(this string str, bool removeEmptyEntries = true)
		=> str.SplitBySep("=", removeEmptyEntries);

	/// <summary>
	/// Splits the string using a tab character as the separator.
	/// </summary>
	/// <param name="str">The string to split.</param>
	/// <param name="removeEmptyEntries">If true, removes empty entries.</param>
	/// <returns>An array of substrings.</returns>
	public static string[] SplitByTab(this string str, bool removeEmptyEntries = true)
		=> str.SplitBySep("\t", removeEmptyEntries);

	/// <summary>
	/// Splits the string using the "@" symbol as the separator.
	/// </summary>
	/// <param name="str">The string to split.</param>
	/// <param name="removeEmptyEntries">If true, removes empty entries.</param>
	/// <returns>An array of substrings.</returns>
	public static string[] SplitByAt(this string str, bool removeEmptyEntries = true)
		=> str.SplitBySep("@", removeEmptyEntries);

	/// <summary>
	/// Splits the string using newline as the separator.
	/// </summary>
	/// <param name="str">The string to split.</param>
	/// <param name="removeEmptyEntries">If true, removes empty entries.</param>
	/// <returns>An array of substrings.</returns>
	[Obsolete("Use SplitByN methods.")]
	public static string[] SplitByLine(this string str, bool removeEmptyEntries = false)
		=> str.SplitByN(removeEmptyEntries);

	/// <summary>
	/// Finds the last index of a specified character in a StringBuilder.
	/// </summary>
	/// <param name="builder">The StringBuilder to search.</param>
	/// <param name="value">The character to locate.</param>
	/// <returns>The zero-based index position of the character if found; otherwise, -1.</returns>
	public static int LastIndexOf(this StringBuilder builder, char value)
	{
		if (builder is null)
			throw new ArgumentNullException(nameof(builder));

		for (var i = builder.Length - 1; i > 0; i--)
		{
			if (builder[i] == value)
				return i;
		}

		return -1;
	}

	/// <summary>
	/// Determines whether the specified string is a valid email address.
	/// </summary>
	/// <param name="email">The email address to test.</param>
	/// <returns>True if the email address is valid; otherwise, false.</returns>
	public static bool IsValidEmailAddress(this string email)
	{
		return new Regex(@"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$").IsMatch(email);
	}

	/// <summary>
	/// Determines whether the specified string is a valid URL.
	/// </summary>
	/// <param name="url">The URL to test.</param>
	/// <returns>True if the URL is valid; otherwise, false.</returns>
	public static bool IsValidUrl(this string url)
	{
		const string strRegex = "^(https?://)"
								+ "?(([0-9a-z_!~*'().&=+$%-]+: )?[0-9a-z_!~*'().&=+$%-]+@)?" //user@
								+ @"(([0-9]{1,3}\.){3}[0-9]{1,3}" // IP- 199.194.52.184
								+ "|" // allows either IP or domain
								+ @"([0-9a-z_!~*'()-]+\.)*" // tertiary domain(s)- www.
								+ @"([0-9a-z][0-9a-z-]{0,61})?[0-9a-z]" // second level domain
								+ @"(\.[a-z]{2,6})?)" // first level domain- .com or .museum is optional
								+ "(:[0-9]{1,5})?" // port number- :80
								+ "((/?)|" // a slash isn't required if there is no file name
								+ "(/[0-9a-z_!~*'().;?:@&=+$,%#-]+)+/?)$";
		return new Regex(strRegex).IsMatch(url);
	}

	/// <summary>
	/// Reverses the specified string.
	/// </summary>
	/// <param name="input">The string to reverse.</param>
	/// <returns>The reversed string.</returns>
	public static string Reverse(this string input)
	{
		var chars = input.ToCharArray();
		Array.Reverse(chars);
		return new string(chars);
	}

	/// <summary>
	/// Reduces the string to a specified length and appends the specified ending.
	/// </summary>
	/// <param name="s">The string to reduce.</param>
	/// <param name="count">The total length of the returned string including the ending.</param>
	/// <param name="endings">The ending to append.</param>
	/// <returns>A reduced version of the string.</returns>
	public static string Reduce(this string s, int count, string endings)
	{
		if (s is null)
			throw new ArgumentNullException(nameof(s));

		if (endings.IsEmpty())
			return s.Substring(0, count);

		if (count < endings.Length)
			throw new ArgumentOutOfRangeException(nameof(count));

		return s.Substring(0, count - endings.Length) + endings;
	}

	/// <summary>
	/// Replaces all white space characters in the string with a single space.
	/// </summary>
	/// <param name="s">The string in which to replace white spaces.</param>
	/// <returns>The modified string.</returns>
	public static string ReplaceWhiteSpaces(this string s)
	{
		return s.ReplaceWhiteSpaces(' ');
	}

	/// <summary>
	/// Replaces all white space characters in the string with the specified character.
	/// </summary>
	/// <param name="s">The string in which to replace white spaces.</param>
	/// <param name="c">The character to replace white spaces with.</param>
	/// <returns>The modified string.</returns>
	public static string ReplaceWhiteSpaces(this string s, char c)
	{
		if (s is null)
			return null;

		var sb = new StringBuilder(s);

		for (var i = 0; i < sb.Length; i++)
		{
			if (char.IsWhiteSpace(sb[i]))
				sb[i] = c;
		}

		return sb.ToString();
	}

	/// <summary>
	/// Removes all spaces from the string.
	/// </summary>
	/// <param name="s">The string from which to remove spaces.</param>
	/// <returns>The string without spaces.</returns>
	public static string RemoveSpaces(this string s)
	{
		return s.Remove(" ");
	}

	/// <summary>
	/// Removes all occurrences of the specified substring from the string.
	/// </summary>
	/// <param name="s">The string from which to remove.</param>
	/// <param name="what">The substring to remove.</param>
	/// <param name="ignoreCase">If true, performs a case-insensitive removal.</param>
	/// <returns>The modified string.</returns>
	public static string Remove(this string s, string what, bool ignoreCase = false)
	{
		if (ignoreCase)
			return s.ReplaceIgnoreCase(what, string.Empty);
		else
			return s.Replace(what, string.Empty);
	}

	/// <summary>
	/// Determines whether the string represents a number.
	/// </summary>
	/// <param name="s">The string to test.</param>
	/// <param name="floatPoint">If true, considers floating point numbers; otherwise, integers.</param>
	/// <returns>True if the string can be parsed as a number; otherwise, false.</returns>
	public static bool IsNumber(this string s, bool floatPoint)
	{
		var withoutWhiteSpace = s.RemoveSpaces();

		if (floatPoint)
		{
			return double.TryParse(withoutWhiteSpace, NumberStyles.Any, CultureInfo.InvariantCulture, out _);
		}
		else
		{
			return int.TryParse(withoutWhiteSpace, out _);
		}
	}

	/// <summary>
	/// Determines whether the string contains only numeric characters (and optionally a decimal separator).
	/// </summary>
	/// <param name="s">The string to test.</param>
	/// <param name="floatPoint">If true, allows decimal points or commas.</param>
	/// <returns>True if the string contains only numbers; otherwise, false.</returns>
	public static bool IsNumberOnly(this string s, bool floatPoint)
	{
		s = s.RemoveSpaces();

		if (s.Length == 0)
			return false;

		foreach (var c in s)
		{
			if (c.IsDigit())
				continue;

			if (floatPoint && (c == '.' || c == ','))
				continue;

			return false;
		}

		return true;
	}

	/// <summary>
	/// Determines whether the specified character is a digit.
	/// </summary>
	/// <param name="c">The character to test.</param>
	/// <returns>True if the character is a digit; otherwise, false.</returns>
	public static bool IsDigit(this char c)
		=> char.IsDigit(c);

	/// <summary>
	/// Removes diacritical marks from the string.
	/// </summary>
	/// <param name="s">The string from which to remove diacritics.</param>
	/// <returns>The modified string without accents.</returns>
	public static string RemoveDiacritics(this string s)
	{
		var stFormD = s.Normalize(NormalizationForm.FormD);
		var sb = new StringBuilder();

		foreach (var t in from t in stFormD let uc = CharUnicodeInfo.GetUnicodeCategory(t) where uc != UnicodeCategory.NonSpacingMark select t)
		{
			sb.Append(t);
		}

		return sb.ToString().Normalize(NormalizationForm.FormC);
	}

	/// <summary>
	/// Replaces newline characters in the string with HTML line breaks.
	/// </summary>
	/// <param name="s">The string to modify.</param>
	/// <returns>The modified string with HTML &lt;br /&gt; tags in place of newlines.</returns>
	public static string Nl2Br(this string s)
	{
		return s.Replace(RN, "<br />").Replace(N, "<br />");
	}

	/// <summary>
	/// Trims the string to the specified maximum length and appends "..." if it exceeds that length.
	/// </summary>
	/// <param name="value">The string to trim.</param>
	/// <param name="maxLength">The maximum length of the returned string.</param>
	/// <returns>A trimmed version of the string.</returns>
	public static string Trim(this string value, int maxLength)
	{
		if (value != null && value.Length > maxLength)
			return value.Substring(0, maxLength) + "...";
		else
			return value;
	}

	/// <summary>
	/// Joins the collection of strings using "@" as the separator.
	/// </summary>
	/// <param name="parts">The collection of strings to join.</param>
	/// <returns>A string resulting from the join.</returns>
	public static string JoinAt(this IEnumerable<string> parts)
		=> parts.Join("@");

	/// <summary>
	/// Joins the collection of strings using a tab character as the separator.
	/// </summary>
	/// <param name="parts">The collection of strings to join.</param>
	/// <returns>A string resulting from the join.</returns>
	public static string JoinTab(this IEnumerable<string> parts)
		=> parts.Join("\t");

	/// <summary>
	/// Joins the collection of strings using a comma as the separator.
	/// </summary>
	/// <param name="parts">The collection of strings to join.</param>
	/// <returns>A string resulting from the join.</returns>
	public static string JoinComma(this IEnumerable<string> parts)
		=> parts.Join(",");

	/// <summary>
	/// Joins the collection of strings using a semicolon as the separator.
	/// </summary>
	/// <param name="parts">The collection of strings to join.</param>
	/// <returns>A string resulting from the join.</returns>
	public static string JoinDotComma(this IEnumerable<string> parts)
		=> parts.Join(";");

	/// <summary>
	/// Joins the collection of strings using a dot as the separator.
	/// </summary>
	/// <param name="parts">The collection of strings to join.</param>
	/// <returns>A string resulting from the join.</returns>
	public static string JoinDot(this IEnumerable<string> parts)
		=> parts.Join(".");

	/// <summary>
	/// Joins the collection of strings using a comma and a space as the separator.
	/// </summary>
	/// <param name="parts">The collection of strings to join.</param>
	/// <returns>A string resulting from the join.</returns>
	public static string JoinCommaSpace(this IEnumerable<string> parts)
		=> parts.Join(", ");

	/// <summary>
	/// Joins the collection of strings using a space as the separator.
	/// </summary>
	/// <param name="parts">The collection of strings to join.</param>
	/// <returns>A string resulting from the join.</returns>
	public static string JoinSpace(this IEnumerable<string> parts)
		=> parts.Join(" ");

	/// <summary>
	/// Joins the collection of strings using the pipe character as the separator.
	/// </summary>
	/// <param name="parts">The collection of strings to join.</param>
	/// <returns>A string resulting from the join.</returns>
	public static string JoinPipe(this IEnumerable<string> parts)
		=> parts.Join("|");

	/// <summary>
	/// Joins the collection of strings using a colon as the separator.
	/// </summary>
	/// <param name="parts">The collection of strings to join.</param>
	/// <returns>A string resulting from the join.</returns>
	public static string JoinColon(this IEnumerable<string> parts)
		=> parts.Join(":");

	/// <summary>
	/// Joins the collection of strings using an equal sign as the separator.
	/// </summary>
	/// <param name="parts">The collection of strings to join.</param>
	/// <returns>A string resulting from the join.</returns>
	public static string JoinEqual(this IEnumerable<string> parts)
		=> parts.Join("=");

	/// <summary>
	/// Joins the collection of strings using the ampersand as the separator.
	/// </summary>
	/// <param name="parts">The collection of strings to join.</param>
	/// <returns>A string resulting from the join.</returns>
	public static string JoinAnd(this IEnumerable<string> parts)
		=> parts.Join("&");

	/// <summary>
	/// Joins the collection of strings using the newline character as the separator.
	/// </summary>
	/// <param name="parts">The collection of strings to join.</param>
	/// <returns>A string resulting from the join.</returns>
	public static string JoinN(this IEnumerable<string> parts)
		=> parts.Join(N);

	/// <summary>
	/// Joins the collection of strings using the carriage return and newline as the separator.
	/// </summary>
	/// <param name="parts">The collection of strings to join.</param>
	/// <returns>A string resulting from the join.</returns>
	public static string JoinRN(this IEnumerable<string> parts)
		=> parts.Join(RN);

	/// <summary>
	/// Joins the collection of strings using the system's newline as the separator.
	/// </summary>
	/// <param name="parts">The collection of strings to join.</param>
	/// <returns>A string resulting from the join.</returns>
	public static string JoinNL(this IEnumerable<string> parts)
		=> parts.Join(Environment.NewLine);

	/// <summary>
	/// Joins the collection of strings using the specified separator.
	/// </summary>
	/// <param name="parts">The collection of strings to join.</param>
	/// <param name="separator">The separator string.</param>
	/// <returns>A string resulting from the join.</returns>
	public static string Join(this IEnumerable<string> parts, string separator)
	{
		return string.Join(separator, [.. parts]);
	}

	/// <summary>
	/// Determines whether two strings are equal, ignoring case.
	/// </summary>
	/// <param name="str1">The first string to compare.</param>
	/// <param name="str2">The second string to compare.</param>
	/// <returns>True if the strings are equal ignoring case; otherwise, false.</returns>
	public static bool EqualsIgnoreCase(this string str1, string str2)
	{
		return string.Equals(str1, str2, StringComparison.InvariantCultureIgnoreCase);
	}

	/// <summary>
	/// Compares two strings for equality, ignoring case.
	/// </summary>
	/// <param name="str1">The first string to compare.</param>
	/// <param name="str2">The second string to compare.</param>
	/// <returns>True if the strings are equal ignoring case; otherwise, false.</returns>
	[Obsolete("Use EqualsIgnoreCase.")]
	public static bool CompareIgnoreCase(this string str1, string str2)
	{
		return string.Compare(str1, str2, StringComparison.InvariantCultureIgnoreCase) == 0;
	}

	/// <summary>
	/// Determines whether the first string contains the second string, ignoring case.
	/// </summary>
	/// <param name="str1">The string to search.</param>
	/// <param name="str2">The string to locate.</param>
	/// <returns>True if str1 contains str2 when ignoring case; otherwise, false.</returns>
	public static bool ContainsIgnoreCase(this string str1, string str2)
	{
		if (str1 is null)
		{
			return false;
			//throw new ArgumentNullException(nameof(str1));
		}

		if (str2 is null)
			return false;

#if NETSTANDARD2_0
		return str1.IndexOf(str2, StringComparison.InvariantCultureIgnoreCase) >= 0;
#else
		return str1.Contains(str2, StringComparison.InvariantCultureIgnoreCase);
#endif
	}

	/// <summary>
	/// Replaces occurrences of a specified substring with another string, ignoring case.
	/// </summary>
	/// <param name="original">The original string.</param>
	/// <param name="oldValue">The substring to replace.</param>
	/// <param name="newValue">The replacement string.</param>
	/// <returns>The modified string.</returns>
	public static string ReplaceIgnoreCase(this string original, string oldValue, string newValue)
	{
		if (oldValue is null)
			throw new ArgumentNullException(nameof(oldValue));

		if (newValue is null)
			throw new ArgumentNullException(nameof(newValue));

		if (original is null)
		{
			return null;
			//throw new ArgumentNullException(nameof(original));
		}

		if (oldValue.Length == 0)
			return original.IsEmpty() ? newValue : original;

#if NETSTANDARD2_0
		return Regex.Replace(original, oldValue, newValue, RegexOptions.IgnoreCase);
#else
		return original.Replace(oldValue, newValue, StringComparison.InvariantCultureIgnoreCase);
#endif
	}

	/// <summary>
	/// Replaces occurrences of a specified substring with another string in a StringBuilder, ignoring case.
	/// </summary>
	/// <param name="builder">The StringBuilder to modify.</param>
	/// <param name="oldValue">The substring to replace.</param>
	/// <param name="newValue">The replacement string.</param>
	/// <returns>The modified StringBuilder.</returns>
	public static StringBuilder ReplaceIgnoreCase(this StringBuilder builder, string oldValue, string newValue)
	{
		if (builder is null)
			throw new ArgumentNullException(nameof(builder));

		var str = builder.ToString().ReplaceIgnoreCase(oldValue, newValue);
		return builder
			.Clear()
			.Append(str);
	}

	/// <summary>
	/// Determines whether the string starts with the specified substring, ignoring case.
	/// </summary>
	/// <param name="str1">The string to test.</param>
	/// <param name="str2">The substring to compare.</param>
	/// <returns>True if str1 starts with str2 ignoring case; otherwise, false.</returns>
	public static bool StartsWithIgnoreCase(this string str1, string str2)
	{
		if (str1 is null)
		{
			return false;
			//throw new ArgumentNullException(nameof(str1));
		}

		if (str2 is null)
			return false;

		return str1.StartsWith(str2, StringComparison.InvariantCultureIgnoreCase);
	}

	/// <summary>
	/// Determines whether the string ends with the specified substring, ignoring case.
	/// </summary>
	/// <param name="str1">The string to test.</param>
	/// <param name="str2">The substring to compare.</param>
	/// <returns>True if str1 ends with str2 ignoring case; otherwise, false.</returns>
	public static bool EndsWithIgnoreCase(this string str1, string str2)
	{
		if (str1 is null)
		{
			return false;
			//throw new ArgumentNullException(nameof(str1));
		}

		if (str2 is null)
			return false;

		return str1.EndsWith(str2, StringComparison.InvariantCultureIgnoreCase);
	}

	/// <summary>
	/// Returns the zero-based index of the first occurrence of the specified substring, ignoring case.
	/// </summary>
	/// <param name="str1">The string to search.</param>
	/// <param name="str2">The substring to locate.</param>
	/// <param name="index">The starting index for the search. Defaults to -1 for beginning.</param>
	/// <returns>The index of the first occurrence, or -1 if not found.</returns>
	public static int IndexOfIgnoreCase(this string str1, string str2, int index = -1)
	{
		if (str1 is null)
		{
			return -1;
			//throw new ArgumentNullException(nameof(str1));
		}

		if (str2 is null)
			return -1;

		if (index == -1)
			return str1.IndexOf(str2, StringComparison.InvariantCultureIgnoreCase);
		else
			return str1.IndexOf(str2, index, StringComparison.InvariantCultureIgnoreCase);
	}

	/// <summary>
	/// Searches for the last occurrence of a specified string, ignoring case.
	/// </summary>
	/// <param name="str1">The string to search within.</param>
	/// <param name="str2">The string to search for.</param>
	/// <param name="index">The starting position of the search. The search is conducted from this position to the beginning. Default is -1 (entire string).</param>
	/// <returns>The index of the last occurrence of str2 in str1, or -1 if not found or if either string is null.</returns>
	public static int LastIndexOfIgnoreCase(this string str1, string str2, int index = -1)
	{
		if (str1 is null)
		{
			return -1;
			//throw new ArgumentNullException(nameof(str1));
		}

		if (str2 is null)
			return -1;

		if (index == -1)
			return str1.LastIndexOf(str2, StringComparison.InvariantCultureIgnoreCase);
		else
			return str1.LastIndexOf(str2, index, StringComparison.InvariantCultureIgnoreCase);
	}

	//
	// http://ppetrov.wordpress.com/2008/06/30/useful-method-8-of-n-string-capitalize-firsttotitlecase/
	//

	/// <summary>
	/// Converts the string to title case using the current culture.
	/// </summary>
	/// <param name="value">The string to convert.</param>
	/// <returns>The string converted to title case.</returns>
	public static string ToTitleCase(this string value)
	{
		var ti = Thread.CurrentThread.CurrentCulture.TextInfo;
		return ti.ToTitleCase(value);
	}

	//
	// http://ppetrov.wordpress.com/2008/06/13/useful-method-1-of-n/
	//

	/// <summary>
	/// Repeats the string a specified number of times.
	/// </summary>
	/// <param name="value">The string to repeat.</param>
	/// <param name="n">The number of times to repeat the string.</param>
	/// <returns>A new string containing the original string repeated n times.</returns>
	public static string Times(this string value, int n)
	{
		return value.Times(n, string.Empty);
	}

	/// <summary>
	/// Repeats the string a specified number of times with a separator between each repetition.
	/// </summary>
	/// <param name="value">The string to repeat.</param>
	/// <param name="n">The number of times to repeat the string.</param>
	/// <param name="separator">The string to use as a separator between repetitions.</param>
	/// <returns>A new string containing the original string repeated n times with the specified separator.</returns>
	/// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when n is less than 1.</exception>
	public static string Times(this string value, int n, string separator)
	{
		if (value is null)
			throw new ArgumentNullException(nameof(value));

		if (n < 1)
			throw new ArgumentOutOfRangeException(nameof(n), n, "Must be a positive number.");

		if (value.Length > 0 && n > 0)
			return Enumerable.Repeat(value, n).Join(separator);

		return value;
	}

	//
	// http://www.extensionmethod.net/Details.aspx?ID=123
	//

	/// <summary>
	/// Truncates the string to a specified length and appends "..." if truncated.
	/// </summary>
	/// <param name="text">The string to truncate.</param>
	/// <param name="maxLength">The maximum length of the resulting string.</param>
	/// <returns>The truncated string.</returns>
	public static string Truncate(this string text, int maxLength)
	{
		return text.Truncate(maxLength, "...");
	}

	/// <summary>
	/// Truncates the string to a specified length and appends a custom suffix if truncated.
	/// </summary>
	/// <param name="text">The string to truncate.</param>
	/// <param name="maxLength">The maximum length of the resulting string before adding the suffix.</param>
	/// <param name="suffix">The string to append if truncation occurs.</param>
	/// <returns>The truncated string with the suffix if truncation occurred.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when maxLength is negative.</exception>
	public static string Truncate(this string text, int maxLength, string suffix)
	{
		if (maxLength < 0)
			throw new ArgumentOutOfRangeException(nameof(maxLength), nameof(maxLength), "maxLength is negative.");
		else if (maxLength == 0)
			return suffix;
		else if (maxLength >= text.Length)
			return text;
		else
			return text.Substring(0, maxLength) + suffix;
	}

	/// <summary>
	/// Truncates a string in the middle, preserving the start and end portions while adding an ellipsis in between.
	/// </summary>
	/// <param name="input">The string to truncate.</param>
	/// <param name="limit">The maximum length of the resulting string including the ellipsis.</param>
	/// <returns>The truncated string with ellipsis in the middle if truncation was necessary, otherwise the original string.</returns>
	public static string TruncateMiddle(this string input, int limit)
	{
		if (input.IsEmpty())
			return input;

		var output = input;
		const string middle = "...";

		// Check if the string is longer than the allowed amount
		// otherwise do nothing
		if (output.Length <= limit || limit <= 0)
			return output;

		// figure out how much to make it fit...
		var left = (limit / 2) - (middle.Length / 2);
		var right = limit - left - (middle.Length / 2);

		if ((left + right + middle.Length) < limit)
		{
			right++;
		}
		else if ((left + right + middle.Length) > limit)
		{
			right--;
		}

		// cut the left side
		output = input.Substring(0, left);

		// add the middle
		output += middle;

		// add the right side...
		output += input.Substring(input.Length - right, right);

		return output;
	}

	/// <summary>
	/// Removes trailing zeros from the string using the current thread's number format decimal separator.
	/// </summary>
	/// <param name="s">The input string.</param>
	/// <returns>A string with trailing zeros removed.</returns>
	public static string RemoveTrailingZeros(this string s)
	{
		return s.RemoveTrailingZeros(Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator);
	}

	/// <summary>
	/// Removes trailing zeros from the string based on the specified separator.
	/// </summary>
	/// <param name="s">The input string.</param>
	/// <param name="separator">The separator to consider while removing zeros.</param>
	/// <returns>A string with trailing zeros removed.</returns>
	public static string RemoveTrailingZeros(this string s, string separator)
	{
		if (s.IsEmpty())
			throw new ArgumentNullException(nameof(s));

		if (separator.IsEmpty())
			throw new ArgumentNullException(nameof(separator));

		s = s.TrimStart('0').TrimEnd('0');

		//var index = s.IndexOf(separator);

		//if (index == -1)
		//    index = s.Length;

		//var endIndex = 0;

		//for (var i = 0; i < index; i++)
		//{
		//    if (s[i] == '0')
		//        endIndex = i + 1;
		//    else
		//        break;
		//}

		//if (endIndex > 0)
		//    s = s.Substring(endIndex);

		//for (var i = s.Length - 1; i > index; i--)
		//{
		//    if (s[i] == '0')
		//        endIndex = i - 1;
		//    else
		//        break;
		//}

		//s = s.TrimStart('0').TrimEnd('0', separator[0]);

		if (s.StartsWith(separator))
			s = "0" + s;

		if (s.EndsWith(separator))
			s = s.Substring(0, s.Length - 1);

		if (s.IsEmpty())
			s = "0";

		return s;
	}

	/// <summary>
	/// Converts a base64 encoded string to a byte array.
	/// </summary>
	/// <param name="value">The base64 encoded string.</param>
	/// <returns>The byte array representation.</returns>
	public static byte[] Base64(this string value)
	{
		return Convert.FromBase64String(value);
	}

	/// <summary>
	/// Converts a byte array to a base64 encoded string.
	/// </summary>
	/// <param name="value">The byte array.</param>
	/// <returns>The base64 encoded string.</returns>
	public static string Base64(this byte[] value)
	{
		return Convert.ToBase64String(value);
	}

	/// <summary>
	/// Splits the specified string into substrings of the given length.
	/// </summary>
	/// <param name="stringToSplit">The string to split.</param>
	/// <param name="length">The maximum length of each substring.</param>
	/// <returns>An enumerable collection of substrings.</returns>
	public static IEnumerable<string> SplitByLength(this string stringToSplit, int length)
	{
		while (stringToSplit.Length > length)
		{
			yield return stringToSplit.Substring(0, length);
			stringToSplit = stringToSplit.Substring(length);
		}

		if (stringToSplit.Length > 0)
			yield return stringToSplit;
	}

	/// <summary>
	/// Trims the specified start value from the beginning of the string if present.
	/// </summary>
	/// <param name="str">The input string.</param>
	/// <param name="sStartValue">The starting substring to remove.</param>
	/// <returns>The trimmed string.</returns>
	public static string TrimStart(this string str, string sStartValue)
	{
		return str.StartsWith(sStartValue) ? str.Remove(0, sStartValue.Length) : str;
	}

	/// <summary>
	/// Trims the specified end value from the end of the string if present.
	/// </summary>
	/// <param name="str">The input string.</param>
	/// <param name="sEndValue">The ending substring to remove.</param>
	/// <returns>The trimmed string.</returns>
	public static string TrimEnd(this string str, string sEndValue)
	{
		return str.EndsWith(sEndValue) ? str.Remove(str.Length - sEndValue.Length, sEndValue.Length) : str;
	}

	/// <summary>
	/// Checks whether the string starts with the specified start string and ends with the specified end string.
	/// </summary>
	/// <param name="str">The input string.</param>
	/// <param name="sStart">The starting string.</param>
	/// <param name="sEnd">The ending string.</param>
	/// <returns>true if the string is enclosed by the specified brackets; otherwise, false.</returns>
	public static bool CheckBrackets(this string str, string sStart, string sEnd)
	{
		return str.StartsWith(sStart) && str.EndsWith(sEnd);
	}

	/// <summary>
	/// Removes the enclosing brackets from the string if present.
	/// </summary>
	/// <param name="str">The input string.</param>
	/// <param name="sStart">The starting bracket string.</param>
	/// <param name="sEnd">The ending bracket string.</param>
	/// <returns>The string with brackets removed, if they were present.</returns>
	public static string StripBrackets(this string str, string sStart, string sEnd)
	{
		return str.CheckBrackets(sStart, sEnd)
			       ? str.Substring(sStart.Length, (str.Length - sStart.Length) - sEnd.Length)
			       : str;
	}

	private static readonly Dictionary<char, string> _charMap = new()
	{
		{ 'а', "a" },
		{ 'б', "b" },
		{ 'в', "v" },
		{ 'г', "g" },
		{ 'д', "d" },
		{ 'е', "e" },
		{ 'ё', "yo" },
		{ 'ж', "zh" },
		{ 'з', "z" },
		{ 'и', "i" },
		{ 'й', "i" },
		{ 'к', "k" },
		{ 'л', "l" },
		{ 'м', "m" },
		{ 'н', "n" },
		{ 'о', "o" },
		{ 'п', "p" },
		{ 'р', "r" },
		{ 'с', "s" },
		{ 'т', "t" },
		{ 'у', "u" },
		{ 'ф', "f" },
		{ 'х', "h" },
		{ 'ц', "ts" },
		{ 'ч', "ch" },
		{ 'ш', "sh" },
		{ 'щ', "shsh" },
		{ 'ы', "y" },
		{ 'э', "eh" },
		{ 'ю', "yu" },
		{ 'я', "ya" },
		{ 'ь', "'" },
		{ 'ъ', "'" },
	};

	/// <summary>
	/// Transliterates the Russian title to Latin characters.
	/// </summary>
	/// <param name="russianTitle">The Russian string.</param>
	/// <returns>The transliterated string in Latin.</returns>
	public static string ToLatin(this string russianTitle)
	{
		if (russianTitle.IsEmpty())
			return russianTitle;

		var transliter = string.Empty;

		foreach (var letter in russianTitle.ToLower())
		{
			if (_charMap.TryGetValue(letter, out var mappedLetter))
				transliter += mappedLetter;
			else
				transliter += letter;
		}

		return transliter;
	}

	/// <summary>
	/// Performs light screening on the input text by replacing spaces with hyphens and removing certain characters.
	/// </summary>
	/// <param name="text">The input text.</param>
	/// <returns>The screened text.</returns>
	public static string LightScreening(this string text)
		=> text?.Replace(' ', '-').Remove(".").Remove("#").Remove("?").Remove(":");

	/// <summary>
	/// Compares two file paths for equality after normalizing them.
	/// </summary>
	/// <param name="path1">The first file path.</param>
	/// <param name="path2">The second file path.</param>
	/// <returns>true if the paths are equal; otherwise, false.</returns>
	public static bool ComparePaths(this string path1, string path2)
	{
		// http://stackoverflow.com/questions/2281531/how-can-i-compare-directory-paths-in-c
		return Path.GetFullPath(path1).TrimEnd('\\').EqualsIgnoreCase(Path.GetFullPath(path2).TrimEnd('\\'));
	}

	/// <summary>
	/// Determines if the current string matches the specified pattern using SQL-like wildcards.
	/// </summary>
	/// <param name="toSearch">The string to search in.</param>
	/// <param name="toFind">The pattern to search for. Use '_' for single character and '%' for multiple characters.</param>
	/// <param name="ignoreCase">if set to true, the comparison ignores case.</param>
	/// <returns>true if the string matches the pattern; otherwise, false.</returns>
	public static bool Like(this string toSearch, string toFind, bool ignoreCase = true)
	{
		var option = RegexOptions.Singleline;

		if (ignoreCase)
			option = RegexOptions.IgnoreCase;

		return new Regex(@"\A" + new Regex(@"\.|\$|\^|\{|\[|\(|\||\)|\*|\+|\?|\\").Replace(toFind, ch => @"\" + ch).Replace('_', '.').Replace("%", ".*") + @"\z", option).IsMatch(toSearch);
	}

	/// <summary>
	/// Determines whether the specified secure string is empty.
	/// </summary>
	/// <param name="secureString">The secure string to check.</param>
	/// <returns>true if the secure string is null or empty; otherwise, false.</returns>
	public static bool IsEmpty(this SecureString secureString)
		=> secureString is null	|| secureString.Length == 0;

	/// <summary>
	/// Throws an ArgumentNullException if the secure string is empty.
	/// </summary>
	/// <param name="str">The secure string.</param>
	/// <param name="paramName">The name of the parameter.</param>
	/// <returns>The original secure string if not empty.</returns>
	public static SecureString ThrowIfEmpty(this SecureString str, string paramName)
		=> str.IsEmpty() ? throw new ArgumentNullException(paramName) : str;

	/// <summary>
	/// Determines whether two secure strings are equal.
	/// </summary>
	/// <param name="value1">The first secure string.</param>
	/// <param name="value2">The second secure string.</param>
	/// <returns>true if both secure strings are equal; otherwise, false.</returns>
	public static bool IsEqualTo(this SecureString value1, SecureString value2)
	{
		if (value1 is null)
			return value2 is null;

		if (value2 is null)
			return false;

		if (value1.Length != value2.Length)
			return false;

		var bstr1 = IntPtr.Zero;
		var bstr2 = IntPtr.Zero;

		try
		{
			bstr1 = Marshal.SecureStringToBSTR(value1);
			bstr2 = Marshal.SecureStringToBSTR(value2);

			var length = Marshal.ReadInt32(bstr1, -4);

			for (var x = 0; x < length; ++x)
			{
				var byte1 = Marshal.ReadByte(bstr1, x);
				var byte2 = Marshal.ReadByte(bstr2, x);

				if (byte1 != byte2)
					return false;
			}

			return true;
		}
		finally
		{
			if (bstr2 != IntPtr.Zero)
				Marshal.ZeroFreeBSTR(bstr2);

			if (bstr1 != IntPtr.Zero)
				Marshal.ZeroFreeBSTR(bstr1);
		}
	}

	/// <summary>
	/// Converts the byte array digest to its hexadecimal string representation.
	/// </summary>
	/// <param name="digest">The byte array digest.</param>
	/// <returns>The hexadecimal string.</returns>
	public static string Digest(this byte[] digest)
	{
		return digest.Digest(digest.Length);
	}

	/// <summary>
	/// Converts a portion of the byte array digest to its hexadecimal string representation.
	/// </summary>
	/// <param name="digest">The byte array digest.</param>
	/// <param name="length">The number of bytes to process.</param>
	/// <param name="index">The starting index in the array.</param>
	/// <returns>The hexadecimal string.</returns>
	public static string Digest(this byte[] digest, int? length, int index = 0)
	{
		return BitConverter.ToString(digest, index, length ?? digest.Length).Remove("-");
	}

	//private static readonly byte[] _initVectorBytes = Encoding.ASCII.GetBytes("ss14fgty650h8u82");
	//private const int _keysize = 256;

	//public static string Encrypt(this string data, string password)
	//{
	//	using (var pwd = new Rfc2898DeriveBytes(password, _initVectorBytes))
	//	{
	//		var keyBytes = pwd.GetBytes(_keysize / 8);

	//		using (var symmetricKey = new RijndaelManaged { Mode = CipherMode.CBC })
	//		using (var encryptor = symmetricKey.CreateEncryptor(keyBytes, _initVectorBytes))
	//		using (var memoryStream = new MemoryStream())
	//		using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
	//		{
	//			var bytes = data.UTF8();

	//			cryptoStream.Write(bytes, 0, bytes.Length);
	//			cryptoStream.FlushFinalBlock();

	//			return memoryStream.ToArray().Base64();
	//		}
	//	}
	//}

	//public static string Decrypt(this string data, string password)
	//{
	//	using (var pwd = new Rfc2898DeriveBytes(password, _initVectorBytes))
	//	{
	//		var cipherTextBytes = data.Base64();
	//		var keyBytes = pwd.GetBytes(_keysize / 8);

	//		using (var symmetricKey = new RijndaelManaged { Mode = CipherMode.CBC })
	//		using (var decryptor = symmetricKey.CreateDecryptor(keyBytes, _initVectorBytes))
	//		using (var memoryStream = new MemoryStream(cipherTextBytes))
	//		using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
	//		{
	//			var plainTextBytes = new byte[cipherTextBytes.Length];
	//			var decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
	//			return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
	//		}
	//	}
	//}

	/// <summary>
	/// Gets the Windows Cyrillic encoding (code page 1251).
	/// </summary>
	public static Encoding WindowsCyrillic => Encoding.GetEncoding(1251);

	/// <summary>
	/// Provides hexadecimal encoding functionality.
	/// </summary>
	public static readonly HexEncoding HexEncoding = new();

	/// <summary>
	/// Returns the duplicate strings from the provided collection, ignoring case.
	/// </summary>
	/// <param name="items">The collection of strings.</param>
	/// <returns>An enumerable collection of duplicate strings.</returns>
	public static IEnumerable<string> Duplicates(this IEnumerable<string> items)
		=> items.GroupBy(s => s, s => StringComparer.InvariantCultureIgnoreCase).Where(g => g.Count() > 1).Select(g => g.Key);

	/// <summary>
	/// Gets the default encoding bytes for the string.
	/// </summary>
	/// <param name="v">The input string.</param>
	/// <returns>The byte array in default encoding.</returns>
	public static byte[] Default(this string v) => Encoding.Default.GetBytes(v);
	/// <summary>
	/// Gets the string from a byte array using the default encoding.
	/// </summary>
	/// <param name="v">The byte array.</param>
	/// <returns>The string decoded from the byte array.</returns>
	public static string Default(this byte[] v) => Default(v, 0, v.Length);
	/// <summary>
	/// Gets the string from a portion of the byte array using the default encoding.
	/// </summary>
	/// <param name="v">The byte array.</param>
	/// <param name="index">The starting index.</param>
	/// <param name="count">The number of bytes to decode.</param>
	/// <returns>The decoded string.</returns>
	public static string Default(this byte[] v, int index, int count) => Encoding.Default.GetString(v, index, count);
	/// <summary>
	/// Gets the string from an ArraySegment using the default encoding.
	/// </summary>
	/// <param name="v">The ArraySegment of bytes.</param>
	/// <returns>The decoded string.</returns>
	public static string Default(this ArraySegment<byte> v) => v.Array.Default(v.Offset, v.Count);
	/// <summary>
	/// Gets the string from a byte array using the default encoding with a specified count.
	/// </summary>
	/// <param name="v">The byte array.</param>
	/// <param name="count">The number of bytes to decode.</param>
	/// <param name="index">The starting index.</param>
	/// <returns>The decoded string.</returns>
	[CLSCompliant(false)]
	public static string Default(this byte[] v, uint count, int index = 0) => Default(v, index, (int)count);

	/// <summary>
	/// Gets the ASCII encoded bytes for the string.
	/// </summary>
	/// <param name="v">The input string.</param>
	/// <returns>The ASCII byte array.</returns>
	public static byte[] ASCII(this string v) => Encoding.ASCII.GetBytes(v);
	/// <summary>
	/// Gets the string from a byte array using ASCII encoding.
	/// </summary>
	/// <param name="v">The byte array.</param>
	/// <returns>The ASCII decoded string.</returns>
	public static string ASCII(this byte[] v) => ASCII(v, 0, v.Length);
	/// <summary>
	/// Gets the string from a portion of the byte array using ASCII encoding.
	/// </summary>
	/// <param name="v">The byte array.</param>
	/// <param name="index">The starting index.</param>
	/// <param name="count">The number of bytes to decode.</param>
	/// <returns>The decoded string.</returns>
	public static string ASCII(this byte[] v, int index, int count) => Encoding.ASCII.GetString(v, index, count);
	/// <summary>
	/// Gets the string from an ArraySegment using ASCII encoding.
	/// </summary>
	/// <param name="v">The ArraySegment of bytes.</param>
	/// <returns>The decoded string.</returns>
	public static string ASCII(this ArraySegment<byte> v) => v.Array.ASCII(v.Offset, v.Count);
	/// <summary>
	/// Gets the string from a byte array using ASCII encoding with a specified count.
	/// </summary>
	/// <param name="v">The byte array.</param>
	/// <param name="count">The number of bytes to decode.</param>
	/// <param name="index">The starting index.</param>
	/// <returns>The decoded string.</returns>
	[CLSCompliant(false)]
	public static string ASCII(this byte[] v, uint count, int index = 0) => ASCII(v, index, (int)count);

	/// <summary>
	/// Gets the UTF8 encoded bytes for the string.
	/// </summary>
	/// <param name="v">The input string.</param>
	/// <returns>The UTF8 byte array.</returns>
	public static byte[] UTF8(this string v) => Encoding.UTF8.GetBytes(v);
	/// <summary>
	/// Gets the string from a byte array using UTF8 encoding.
	/// </summary>
	/// <param name="v">The byte array.</param>
	/// <returns>The UTF8 decoded string.</returns>
	public static string UTF8(this byte[] v) => UTF8(v, 0, v.Length);
	/// <summary>
	/// Gets the string from a portion of the byte array using UTF8 encoding.
	/// </summary>
	/// <param name="v">The byte array.</param>
	/// <param name="index">The starting index.</param>
	/// <param name="count">The number of bytes to decode.</param>
	/// <returns>The decoded string.</returns>
	public static string UTF8(this byte[] v, int index, int count) => Encoding.UTF8.GetString(v, index, count);
	/// <summary>
	/// Gets the string from an ArraySegment using UTF8 encoding.
	/// </summary>
	/// <param name="v">The ArraySegment of bytes.</param>
	/// <returns>The decoded string.</returns>
	public static string UTF8(this ArraySegment<byte> v) => v.Array.UTF8(v.Offset, v.Count);
	/// <summary>
	/// Gets the string from a byte array using UTF8 encoding with a specified count.
	/// </summary>
	/// <param name="v">The byte array.</param>
	/// <param name="count">The number of bytes to decode.</param>
	/// <param name="index">The starting index.</param>
	/// <returns>The decoded string.</returns>
	[CLSCompliant(false)]
	public static string UTF8(this byte[] v, uint count, int index = 0) => UTF8(v, index, (int)count);

	/// <summary>
	/// Gets the Unicode encoded bytes for the string.
	/// </summary>
	/// <param name="v">The input string.</param>
	/// <returns>The Unicode byte array.</returns>
	public static byte[] Unicode(this string v) => Encoding.Unicode.GetBytes(v);
	/// <summary>
	/// Gets the string from a byte array using Unicode encoding.
	/// </summary>
	/// <param name="v">The byte array.</param>
	/// <returns>The Unicode decoded string.</returns>
	public static string Unicode(this byte[] v) => Unicode(v, 0, v.Length);
	/// <summary>
	/// Gets the string from a portion of the byte array using Unicode encoding.
	/// </summary>
	/// <param name="v">The byte array.</param>
	/// <param name="index">The starting index.</param>
	/// <param name="count">The number of bytes to decode.</param>
	/// <returns>The decoded string.</returns>
	public static string Unicode(this byte[] v, int index, int count) => Encoding.Unicode.GetString(v, index, count);
	/// <summary>
	/// Gets the string from an ArraySegment using Unicode encoding.
	/// </summary>
	/// <param name="v">The ArraySegment of bytes.</param>
	/// <returns>The decoded string.</returns>
	public static string Unicode(this ArraySegment<byte> v) => v.Array.Unicode(v.Offset, v.Count);
	/// <summary>
	/// Gets the string from a byte array using Unicode encoding with a specified count.
	/// </summary>
	/// <param name="v">The byte array.</param>
	/// <param name="count">The number of bytes to decode.</param>
	/// <param name="index">The starting index.</param>
	/// <returns>The decoded string.</returns>
	[CLSCompliant(false)]
	public static string Unicode(this byte[] v, uint count, int index = 0) => Unicode(v, index, (int)count);

	/// <summary>
	/// Gets the Windows Cyrillic encoded bytes for the string.
	/// </summary>
	/// <param name="v">The input string.</param>
	/// <returns>The Windows Cyrillic byte array.</returns>
	public static byte[] Cyrillic(this string v) => WindowsCyrillic.GetBytes(v);
	/// <summary>
	/// Gets the string from a byte array using Windows Cyrillic encoding.
	/// </summary>
	/// <param name="v">The byte array.</param>
	/// <returns>The decoded string.</returns>
	public static string Cyrillic(this byte[] v) => Cyrillic(v, 0, v.Length);
	/// <summary>
	/// Gets the string from a portion of the byte array using Windows Cyrillic encoding.
	/// </summary>
	/// <param name="v">The byte array.</param>
	/// <param name="index">The starting index.</param>
	/// <param name="count">The number of bytes to decode.</param>
	/// <returns>The decoded string.</returns>
	public static string Cyrillic(this byte[] v, int index, int count) => WindowsCyrillic.GetString(v, index, count);
	/// <summary>
	/// Gets the string from an ArraySegment using Windows Cyrillic encoding.
	/// </summary>
	/// <param name="v">The ArraySegment of bytes.</param>
	/// <returns>The decoded string.</returns>
	public static string Cyrillic(this ArraySegment<byte> v) => v.Array.Cyrillic(v.Offset, v.Count);
	/// <summary>
	/// Gets the string from a byte array using Windows Cyrillic encoding with a specified count.
	/// </summary>
	/// <param name="v">The byte array.</param>
	/// <param name="count">The number of bytes to decode.</param>
	/// <param name="index">The starting index.</param>
	/// <returns>The decoded string.</returns>
	[CLSCompliant(false)]
	public static string Cyrillic(this byte[] v, uint count, int index = 0) => Cyrillic(v, index, (int)count);

	/// <summary>
	/// Gets the hexadecimal encoded bytes for the string.
	/// </summary>
	/// <param name="v">The input string.</param>
	/// <returns>The hexadecimal byte array.</returns>
	public static byte[] Hex(this string v) => HexEncoding.GetBytes(v);
	/// <summary>
	/// Gets the string from a byte array using hexadecimal encoding.
	/// </summary>
	/// <param name="v">The byte array.</param>
	/// <returns>The hexadecimal decoded string.</returns>
	public static string Hex(this byte[] v) => Hex(v, 0, v.Length);
	/// <summary>
	/// Gets the string from a portion of the byte array using hexadecimal encoding.
	/// </summary>
	/// <param name="v">The byte array.</param>
	/// <param name="index">The starting index.</param>
	/// <param name="count">The number of bytes to decode.</param>
	/// <returns>The decoded hexadecimal string.</returns>
	public static string Hex(this byte[] v, int index, int count) => HexEncoding.GetString(v, index, count);
	/// <summary>
	/// Gets the string from an ArraySegment using hexadecimal encoding.
	/// </summary>
	/// <param name="v">The ArraySegment of bytes.</param>
	/// <returns>The decoded hexadecimal string.</returns>
	public static string Hex(this ArraySegment<byte> v) => v.Array.Hex(v.Offset, v.Count);
	/// <summary>
	/// Gets the string from a byte array using hexadecimal encoding with a specified count.
	/// </summary>
	/// <param name="v">The byte array.</param>
	/// <param name="count">The number of bytes to decode.</param>
	/// <param name="index">The starting index.</param>
	/// <returns>The decoded hexadecimal string.</returns>
	[CLSCompliant(false)]
	public static string Hex(this byte[] v, uint count, int index = 0) => Hex(v, index, (int)count);

	/// <summary>
	/// Decodes an ArraySegment of bytes into a string using the specified encoding.
	/// </summary>
	/// <param name="encoding">The encoding to use.</param>
	/// <param name="buffer">The ArraySegment of bytes.</param>
	/// <returns>The decoded string.</returns>
	public static string GetString(this Encoding encoding, ArraySegment<byte> buffer)
		=> encoding.CheckOnNull(nameof(encoding)).GetString(buffer.Array, buffer.Offset, buffer.Count);

	/// <summary>
	/// Converts the string to a SecureString.
	/// </summary>
	/// <param name="str">The input string.</param>
	/// <returns>The SecureString equivalent of the input string.</returns>
	public static SecureString Secure(this string str)
		=> str?.ToCharArray().TypedTo<char[], SecureString>();

	/// <summary>
	/// Converts the SecureString to an unsecured string.
	/// </summary>
	/// <param name="str">The SecureString.</param>
	/// <returns>The unsecured string equivalent.</returns>
	public static string UnSecure(this SecureString str)
	{
		if (str is null)
			return null;

		var bstr = Marshal.SecureStringToBSTR(str);

		using (bstr.MakeDisposable(Marshal.ZeroFreeBSTR))
		{
			return Marshal.PtrToStringBSTR(bstr);
		}
	}

	/// <summary>
	/// Converts a SecureString key to a numeric identifier.
	/// </summary>
	/// <param name="key">The secure key.</param>
	/// <returns>A numeric identifier or null if the key is null.</returns>
	public static int? ToId(this SecureString key) => key?.UnSecure().GetDeterministicHashCode();

	/// <summary>
	/// Converts a character array to a string using the specified count and starting index.
	/// </summary>
	/// <param name="arr">The character array.</param>
	/// <param name="count">The number of characters to convert.</param>
	/// <param name="index">The starting index in the array.</param>
	/// <returns>A new string constructed from the array.</returns>
	[CLSCompliant(false)]
	public static string ToString(this char[] arr, uint count, int index = 0)
		=> arr.ToString((int)count, index);

	/// <summary>
	/// Converts a character array to a string using the specified count and starting index.
	/// </summary>
	/// <param name="arr">The character array.</param>
	/// <param name="count">The number of characters to convert.</param>
	/// <param name="index">The starting index in the array.</param>
	/// <returns>A new string constructed from the array.</returns>
	public static string ToString(this char[] arr, int count, int index = 0)
		=> count == 0 ? string.Empty : new string(arr, index, count);

	/// <summary>
	/// Converts an ArraySegment of bytes to its bit string representation with a specified separator.
	/// </summary>
	/// <param name="buffer">The byte array segment.</param>
	/// <param name="separator">The character separator between byte bits.</param>
	/// <returns>A string representing the bits of the bytes.</returns>
	public static string ToBitString(this ArraySegment<byte> buffer, char separator = ' ')
		=> buffer.Array.ToBitString(buffer.Offset, buffer.Count, separator);

	/// <summary>
	/// Converts a byte array to its bit string representation with a specified separator.
	/// </summary>
	/// <param name="buffer">The byte array.</param>
	/// <param name="index">Optional start index for conversion.</param>
	/// <param name="count">Optional count of bytes to convert.</param>
	/// <param name="separator">The character separator between byte bits.</param>
	/// <returns>A string representing the bits of the bytes.</returns>
	public static string ToBitString(this byte[] buffer, int? index = null, int? count = null, char separator = ' ')
	{
		if (buffer is null)
			throw new ArgumentNullException(nameof(buffer));

		var offset = index ?? 0;
		var len = count ?? buffer.Length;

		if ((offset + len) > buffer.Length)
			throw new ArgumentOutOfRangeException(nameof(index));

		if (offset == len)
			return string.Empty;

		var builder = new StringBuilder();

		for (var i = 0; i < len; i++)
		{
			var bits = Convert.ToString(buffer[i + offset] & 0xFF, 2).PadLeft(8, '0');

			builder.Append(bits).Append(separator);
		}

		if (builder.Length > 0)
			builder.Remove(builder.Length - 1, 1);

		return builder.ToString();
	}

	/// <summary>
	/// Converts a bit string representation to a byte array.
	/// </summary>
	/// <param name="bitString">The string containing bits.</param>
	/// <param name="separator">The character that separates individual byte bits.</param>
	/// <returns>A byte array created from the bit string.</returns>
	public static byte[] ToByteArray(this string bitString, char separator = ' ')
	{
		if (bitString is null)
			throw new ArgumentNullException(nameof(bitString));

		if (bitString.Length == 0)
			return [];

		var bitStrings = bitString.Split(separator);
		var bytes = new byte[bitStrings.Length];

		for (var i = 0; i < bitStrings.Length; i++)
		{
			bytes[i] = (byte)Convert.ToInt32(bitStrings[i], 2);
		}

		return bytes;
	}

	/// <summary>
	/// Calculates a deterministic hash code for the specified string.
	/// </summary>
	/// <param name="value">The string to hash.</param>
	/// <returns>An integer hash code computed deterministically.</returns>
	public static int GetDeterministicHashCode(this string value)
	{
		if (value is null)
			throw new ArgumentNullException(nameof(value));

		var hash1 = 5381;
		var hash2 = hash1;
		var i = 0;
		var len = value.Length;

		while (i < len)
		{
			hash1 = ((hash1 << 5) + hash1) ^ value[i];
			if (++i == len)
				break;
			hash2 = ((hash2 << 5) + hash2) ^ value[i];
			++i;
		}

		return hash1 + (hash2 * 1566083941);
	}

	/// <summary>
	/// Tries to parse the string to a long integer.
	/// </summary>
	/// <param name="str">The string to parse.</param>
	/// <returns>The long value if successfully parsed; otherwise, null.</returns>
	public static long? TryToLong(this string str)
	{
		return long.TryParse(str, out var l) ? l : (long?)null;
	}

	/// <summary>
	/// Searches for the specified pattern in the source string and returns the zero-based index of its first occurrence.
	/// </summary>
	/// <param name="source">The source string to search.</param>
	/// <param name="pattern">The pattern to locate.</param>
	/// <returns>The index of the first occurrence; otherwise, -1 if not found.</returns>
	public static int FastIndexOf(this string source, string pattern)
	{
		if (source is null)
			throw new ArgumentNullException(nameof(source));

		if (pattern is null)
			throw new ArgumentNullException(nameof(pattern));

		if (pattern.Length == 0)
		{
			return 0;
		}

		if (pattern.Length == 1)
		{
			return source.IndexOf(pattern[0]);
		}

		int limit = source.Length - pattern.Length + 1;
		if (limit < 1)
		{
			return -1;
		}

		// Store the first 2 characters of the pattern.
		char c0 = pattern[0];
		char c1 = pattern[1];

		// Find the first occurrence of the first character.
		int first = source.IndexOf(c0, 0, limit);
		while (first != -1)
		{
			// Check if the following character matches the second character of the pattern.
			if (source[first + 1] != c1)
			{
				first = source.IndexOf(c0, ++first, limit - first);
				continue;
			}

			// Check the rest of the pattern (starting with the 3rd character).
			bool found = true;
			for (int j = 2; j < pattern.Length; j++)
			{
				if (source[first + j] != pattern[j])
				{
					found = false;
					break;
				}
			}

			// If the entire pattern is found, return its index; otherwise, continue searching.
			if (found)
			{
				return first;
			}

			first = source.IndexOf(c0, ++first, limit - first);
		}

		return -1;
	}

	/// <summary>
	/// Removes multiple consecutive whitespace characters from the specified text.
	/// </summary>
	/// <param name="text">The input text.</param>
	/// <returns>A string with multiple whitespaces replaced by a single space.</returns>
	public static string RemoveMultipleWhitespace(this string text)
	{
		string result = string.Empty;
		if (text.IsEmptyOrWhiteSpace())
		{
			return result;
		}

		var r = new Regex(@"\s+");
		return r.Replace(text, @" ");
	}

	/// <summary>
	/// Escapes a URL string.
	/// </summary>
	/// <param name="url">The URL to escape.</param>
	/// <returns>An escaped URL string.</returns>
	[Obsolete]
	public static string UrlEscape(this string url)
		=> Uri.EscapeUriString(url);

	/// <summary>
	/// Escapes URL data.
	/// </summary>
	/// <param name="url">The URL data to escape.</param>
	/// <returns>An escaped URL data string.</returns>
	public static string DataEscape(this string url)
		=> Uri.EscapeDataString(url);

	/// <summary>
	/// Unescapes URL data.
	/// </summary>
	/// <param name="url">The URL data to unescape.</param>
	/// <returns>An unescaped URL data string.</returns>
	public static string DataUnEscape(this string url)
		=> Uri.UnescapeDataString(url);

	/// <summary>
	/// Converts the specified character to lowercase.
	/// </summary>
	/// <param name="c">The character to convert.</param>
	/// <param name="invariant">If set to true, uses culture-invariant conversion; otherwise, uses current culture.</param>
	/// <returns>The lowercase equivalent of the character.</returns>
	public static char ToLower(this char c, bool invariant = true)
		=> invariant ? char.ToLowerInvariant(c) : char.ToLower(c);

	/// <summary>
	/// Converts the specified character to uppercase.
	/// </summary>
	/// <param name="c">The character to convert.</param>
	/// <param name="invariant">If set to true, uses culture-invariant conversion; otherwise, uses current culture.</param>
	/// <returns>The uppercase equivalent of the character.</returns>
	public static char ToUpper(this char c, bool invariant = true)
		=> invariant ? char.ToUpperInvariant(c) : char.ToUpper(c);

	/// <summary>
	/// Gets the two-letter ISO language code from the provided culture name.
	/// </summary>
	/// <param name="cultureName">The culture name.</param>
	/// <returns>The two-letter ISO language code.</returns>
	public static string GetLangCode(this string cultureName)
		=> cultureName.To<CultureInfo>().TwoLetterISOLanguageName;

	/// <summary>
	/// Removes the specified number of characters from the end of the StringBuilder.
	/// </summary>
	/// <param name="builder">The StringBuilder to modify.</param>
	/// <param name="count">The number of characters to remove.</param>
	public static void RemoveLast(this StringBuilder builder, int count)
		=> builder.Remove(builder.Length - count, count);

	/// <summary>
	/// Determines whether the StringBuilder is empty.
	/// </summary>
	/// <param name="builder">The StringBuilder to evaluate.</param>
	/// <returns>True if the StringBuilder is empty; otherwise, false.</returns>
	public static bool IsEmpty(this StringBuilder builder)
		=> builder.CheckOnNull(nameof(builder)).Length == 0;

	/// <summary>
	/// Retrieves the content of the StringBuilder and then clears it.
	/// </summary>
	/// <param name="builder">The StringBuilder.</param>
	/// <returns>The content of the StringBuilder before it was cleared.</returns>
	public static string GetAndClear(this StringBuilder builder)
	{
		if (builder is null)
			throw new ArgumentNullException(nameof(builder));

		var str = builder.ToString();
		builder.Clear();
		return str;
	}

	/// <summary>
	/// Interns the specified string.
	/// </summary>
	/// <param name="str">The string to intern.</param>
	/// <returns>The interned string.</returns>
	public static string Intern(this string str)
		=> string.Intern(str);
}