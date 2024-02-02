namespace Ecng.Common
{
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

	public static class StringHelper
	{
		static StringHelper()
		{
			Smart.Default.AddExtensions(new DictionarySourceEx());

			// https://stackoverflow.com/a/47017180
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		}

		public static bool IsEmpty(this string str)
		{
			return string.IsNullOrEmpty(str);
		}

		public static string IsEmpty(this string str, string defaultValue)
		{
			return str.IsEmpty() ? defaultValue : str;
		}

		public static string ThrowIfEmpty(this string str, string paramName)
			=> str.IsEmpty() ? throw new ArgumentNullException(paramName) : str;

		public static bool IsEmptyOrWhiteSpace(this string str)
		{
			return string.IsNullOrWhiteSpace(str);
		}

		public static string IsEmptyOrWhiteSpace(this string str, string defaultValue)
		{
			return str.IsEmptyOrWhiteSpace() ? defaultValue : str;
		}

		public static string Put(this string str, params object[] args)
		{
			if (args is null)
				throw new ArgumentNullException(nameof(args));

			return args.Length == 0 ? str : string.Format(str, args);
		}

		private class DictionarySourceEx : ISource
		{
			private readonly SyncObject _sync = new();
			private readonly Dictionary<Type, Type> _genericTypes = new();
			private readonly Dictionary<string, object> _keys = new();

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

		public static string PutEx(this string str, params object[] args)
		{
			if (args is null)
				throw new ArgumentNullException(nameof(args));

			return args.Length == 0 ? str : Smart.Format(str, args);
		}

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

		public const string N = "\n";
		public const string R = "\r";
		public const string RN = "\r\n";

		public static string[] SplitByLineSeps(this string str, bool removeEmptyEntries = true)
			// https://stackoverflow.com/a/1547483/8029915
			=> str.Split(
				new[] { RN, R, N },
				removeEmptyEntries ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None
			);

		public static string[] SplitByR(this string str, bool removeEmptyEntries = true)
			=> str.SplitBySep(R, removeEmptyEntries);

		public static string[] SplitByRN(this string str, bool removeEmptyEntries = true)
			=> str.SplitBySep(RN, removeEmptyEntries);

		public static string[] SplitByN(this string str, bool removeEmptyEntries = true)
			=> str.SplitBySep(N, removeEmptyEntries);

		[Obsolete("Use SplitByRN or SplitByN methods.")]
		public static string[] SplitLines(this string str, bool removeEmptyEntries = true)
		{
			return str.SplitBySep(Environment.NewLine, removeEmptyEntries);
		}

		[Obsolete("Use SplitBySep method.")]
		public static string[] Split(this string str, string separator, bool removeEmptyEntries = true)
		{
			return str.SplitBySep(separator, removeEmptyEntries);
		}

		public static string[] SplitBySep(this string str, string separator, bool removeEmptyEntries = true)
		{
			if (str is null)
				throw new ArgumentNullException(nameof(str));

			if (str.Length == 0)
				return Array.Empty<string>();

			return str.Split(new[] { separator }, removeEmptyEntries ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None);
		}

		public static string[] SplitByComma(this string str, bool removeEmptyEntries = false)
			=> str.SplitBySep(",", removeEmptyEntries);

		public static string[] SplitByDot(this string str, bool removeEmptyEntries = false)
			=> str.SplitBySep(".", removeEmptyEntries);

		public static string[] SplitByDotComma(this string str, bool removeEmptyEntries = false)
			=> str.SplitBySep(";", removeEmptyEntries);

		public static string[] SplitByColon(this string str, bool removeEmptyEntries = true)
			=> str.SplitBySep(":", removeEmptyEntries);

		public static string[] SplitBySpace(this string str, bool removeEmptyEntries = true)
			=> str.SplitBySep(" ", removeEmptyEntries);

		public static string[] SplitByEqual(this string str, bool removeEmptyEntries = true)
			=> str.SplitBySep("=", removeEmptyEntries);

		[Obsolete("Use SplitByN methods.")]
		public static string[] SplitByLine(this string str, bool removeEmptyEntries = false)
			=> str.SplitByN(removeEmptyEntries);

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
		/// true, if is valid email address
		/// from http://www.davidhayden.com/blog/dave/archive/2006/11/30/ExtensionMethodsCSharp.aspx
		/// </summary>
		/// <param name="email">email address to test</param>
		/// <returns>true, if is valid email address</returns>
		public static bool IsValidEmailAddress(this string email)
		{
			return new Regex(@"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$").IsMatch(email);
		}

		/// <summary>
		/// Checks if <paramref name="url"/> is valid. 
		/// from http://www.osix.net/modules/article/?id=586
		/// and changed to match http://localhost
		/// 
		/// complete (not only http) <paramref name="url"/> regex can be found 
		/// at http://internet.ls-la.net/folklore/url-regexpr.html
		/// </summary>
		/// <param name="url"></param>
		/// <returns></returns>
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
		/// Reverse the string from http://en.wikipedia.org/wiki/Extension_method
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		public static string Reverse(this string input)
		{
			var chars = input.ToCharArray();
			Array.Reverse(chars);
			return new string(chars);
		}

		/// <summary>
		/// Reduce string to shorter preview which is optionally ended by some string (...).
		/// </summary>
		/// <param name="s">string to reduce</param>
		/// <param name="count">Length of returned string including endings.</param>
		/// <param name="endings">optional endings of reduced text</param>
		/// <example>
		/// string description = "This is very long description of something";
		/// string preview = description.Reduce(20,"...");
		/// produce -> "This is very long..."
		/// </example>
		/// <returns></returns>
		public static string Reduce(this string s, int count, string endings)
		{
			if (endings.IsEmpty())
				throw new ArgumentNullException(nameof(endings));

			if (count < endings.Length || count >= endings.Length)
				throw new ArgumentOutOfRangeException(nameof(count));

			return s.Substring(0, count - endings.Length) + endings;
		}

		public static string ReplaceWhiteSpaces(this string s)
		{
			return s.ReplaceWhiteSpaces(' ');
		}

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
		/// Remove white space, not line end useful when parsing user input such phone,
		/// price int.Parse("1 000 000".RemoveSpaces(),.....
		/// </summary>
		/// <param name="s"></param>
		/// <returns>string without spaces</returns>
		public static string RemoveSpaces(this string s)
		{
			return s.Remove(" ");
		}

		public static string Remove(this string s, string what, bool ignoreCase = false)
		{
			if (ignoreCase)
				return s.ReplaceIgnoreCase(what, string.Empty);
			else
				return s.Replace(what, string.Empty);
		}

		/// <summary>
		/// true, if the string can be parse as Double respective Int32 spaces are not considered.
		/// </summary>
		/// <param name="s">input string</param>
		/// <param name="floatPoint">true, if Double is considered, otherwise Int32 is considered.</param>
		/// <returns>true, if the string contains only digits or float-point</returns>
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
		/// true, if the string contains only digits or float-point.
		/// Spaces are not considered.
		/// </summary>
		/// <param name="s">input string</param>
		/// <param name="floatPoint">true, if float-point is considered</param>
		/// <returns>true, if the string contains only digits or float-point</returns>
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

		public static bool IsDigit(this char c)
			=> char.IsDigit(c);

		/// <summary>
		/// Remove accent from strings 
		/// </summary>
		/// <example>
		///  input:  "Příliš žluťoučký kůň úpěl ďábelské ódy."
		///  result: "Prilis zlutoucky kun upel dabelske ody."
		/// </example>
		/// <param name="s"></param>
		/// <remarks>found at http://stackoverflow.com/questions/249087/how-do-i-remove-diacritics-accents-from-a-string-in-net </remarks>
		/// <returns>string without accents</returns>
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
		/// Replace \r\n or \n by <br /> from http://weblogs.asp.net/gunnarpeipman/archive/2007/11/18/c-extension-methods.aspx
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		public static string Nl2Br(this string s)
		{
			return s.Replace(RN, "<br />").Replace(N, "<br />");
		}

		public static string Trim(this string value, int maxLength)
		{
			if (value != null && value.Length > maxLength)
				return value.Substring(0, maxLength) + "...";
			else
				return value;
		}

		public static string JoinComma(this IEnumerable<string> parts)
			=> parts.Join(",");

		public static string JoinDotComma(this IEnumerable<string> parts)
			=> parts.Join(";");

		public static string JoinDot(this IEnumerable<string> parts)
			=> parts.Join(".");

		public static string JoinCommaSpace(this IEnumerable<string> parts)
			=> parts.Join(", ");

		public static string JoinSpace(this IEnumerable<string> parts)
			=> parts.Join(" ");

		public static string JoinPipe(this IEnumerable<string> parts)
			=> parts.Join("|");

		public static string JoinColon(this IEnumerable<string> parts)
			=> parts.Join(":");

		public static string JoinEqual(this IEnumerable<string> parts)
			=> parts.Join("=");

		public static string JoinAnd(this IEnumerable<string> parts)
			=> parts.Join("&");

		public static string JoinN(this IEnumerable<string> parts)
			=> parts.Join(N);

		public static string JoinRN(this IEnumerable<string> parts)
			=> parts.Join(RN);

		public static string JoinNL(this IEnumerable<string> parts)
			=> parts.Join(Environment.NewLine);

		public static string Join(this IEnumerable<string> parts, string separator)
		{
			return string.Join(separator, parts.ToArray());
		}

		public static bool EqualsIgnoreCase(this string str1, string str2)
		{
			return string.Equals(str1, str2, StringComparison.InvariantCultureIgnoreCase);
		}

		[Obsolete("Use EqualsIgnoreCase.")]
		public static bool CompareIgnoreCase(this string str1, string str2)
		{
			return string.Compare(str1, str2, StringComparison.InvariantCultureIgnoreCase) == 0;
		}

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

		public static StringBuilder ReplaceIgnoreCase(this StringBuilder builder, string oldValue, string newValue)
		{
			if (builder is null)
				throw new ArgumentNullException(nameof(builder));

			var str = builder.ToString().ReplaceIgnoreCase(oldValue, newValue);
			return builder
				.Clear()
				.Append(str);
		}

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
		public static string ToTitleCase(this string value)
		{
			var ti = Thread.CurrentThread.CurrentCulture.TextInfo;
			return ti.ToTitleCase(value);
		}

		//
		// http://ppetrov.wordpress.com/2008/06/13/useful-method-1-of-n/
		//
		public static string Times(this string value, int n)
		{
			return value.Times(n, string.Empty);
		}

		public static string Times(this string value, int n, string separator)
		{
			if (value is null)
				throw new ArgumentNullException(nameof(value));

			if (n < 0)
				throw new ArgumentException("Must be a positive number.", nameof(n));

			if (value.Length > 0 && n > 0)
				return Enumerable.Repeat(value, n).Join(separator);

			return value;
		}

		//
		// http://www.extensionmethod.net/Details.aspx?ID=123
		//

		public static string Truncate(this string text, int maxLength)
		{
			return text.Truncate(maxLength, "...");
		}

		/// <summary>
		/// Truncates the string to a specified length and replace the truncated to a ...
		/// </summary>
		/// <param name="text">string that will be truncated</param>
		/// <param name="maxLength">total length of characters to maintain before the truncate happens</param>
		/// <param name="suffix"></param>
		/// <returns>truncated string</returns>
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

		public static string RemoveTrailingZeros(this string s)
		{
			return s.RemoveTrailingZeros(Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator);
		}

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

		public static byte[] Base64(this string value)
		{
			return Convert.FromBase64String(value);
		}

		public static string Base64(this byte[] value)
		{
			return Convert.ToBase64String(value);
		}

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

		public static string TrimStart(this string str, string sStartValue)
		{
			return str.StartsWith(sStartValue) ? str.Remove(0, sStartValue.Length) : str;
		}

		public static string TrimEnd(this string str, string sEndValue)
		{
			return str.EndsWith(sEndValue) ? str.Remove(str.Length - sEndValue.Length, sEndValue.Length) : str;
		}

		public static bool CheckBrackets(this string str, string sStart, string sEnd)
		{
			return str.StartsWith(sStart) && str.EndsWith(sEnd);
		}

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

		public static string LightScreening(this string text)
			=> text?.Replace(' ', '-').Remove(".").Remove("#").Remove("?").Remove(":");

		public static bool ComparePaths(this string path1, string path2)
		{
			// http://stackoverflow.com/questions/2281531/how-can-i-compare-directory-paths-in-c
			return Path.GetFullPath(path1).TrimEnd('\\').EqualsIgnoreCase(Path.GetFullPath(path2).TrimEnd('\\'));
		}

		public static bool Like(this string toSearch, string toFind, bool ignoreCase = true)
		{
			var option = RegexOptions.Singleline;

			if (ignoreCase)
				option = RegexOptions.IgnoreCase;

			return new Regex(@"\A" + new Regex(@"\.|\$|\^|\{|\[|\(|\||\)|\*|\+|\?|\\").Replace(toFind, ch => @"\" + ch).Replace('_', '.').Replace("%", ".*") + @"\z", option).IsMatch(toSearch);
		}

		[CLSCompliant(false)]
		public static bool IsEmpty(this SecureString secureString)
		{
			return secureString is null	|| secureString.Length == 0;
		}

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

		public static string Digest(this byte[] digest)
		{
			return digest.Digest(digest.Length);
		}

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

		public static Encoding WindowsCyrillic => Encoding.GetEncoding(1251);
		public static readonly HexEncoding HexEncoding = new();

		public static IEnumerable<string> Duplicates(this IEnumerable<string> items)
			=> items.GroupBy(s => s, s => StringComparer.InvariantCultureIgnoreCase).Where(g => g.Count() > 1).Select(g => g.Key);

		public static byte[] Default(this string v) => Encoding.Default.GetBytes(v);
		public static string Default(this byte[] v) => Default(v, 0, v.Length);
		public static string Default(this byte[] v, int index, int count) => Encoding.Default.GetString(v, index, count);
		public static string Default(this ArraySegment<byte> v) => v.Array.Default(v.Offset, v.Count);
		[CLSCompliant(false)]
		public static string Default(this byte[] v, uint count, int index = 0) => Default(v, index, (int)count);

		public static byte[] ASCII(this string v) => Encoding.ASCII.GetBytes(v);
		public static string ASCII(this byte[] v) => ASCII(v, 0, v.Length);
		public static string ASCII(this byte[] v, int index, int count) => Encoding.ASCII.GetString(v, index, count);
		public static string ASCII(this ArraySegment<byte> v) => v.Array.ASCII(v.Offset, v.Count);
		[CLSCompliant(false)]
		public static string ASCII(this byte[] v, uint count, int index = 0) => ASCII(v, index, (int)count);

		public static byte[] UTF8(this string v) => Encoding.UTF8.GetBytes(v);
		public static string UTF8(this byte[] v) => UTF8(v, 0, v.Length);
		public static string UTF8(this byte[] v, int index, int count) => Encoding.UTF8.GetString(v, index, count);
		public static string UTF8(this ArraySegment<byte> v) => v.Array.UTF8(v.Offset, v.Count);
		[CLSCompliant(false)]
		public static string UTF8(this byte[] v, uint count, int index = 0) => UTF8(v, index, (int)count);

		public static byte[] Unicode(this string v) => Encoding.Unicode.GetBytes(v);
		public static string Unicode(this byte[] v) => Unicode(v, 0, v.Length);
		public static string Unicode(this byte[] v, int index, int count) => Encoding.Unicode.GetString(v, index, count);
		public static string Unicode(this ArraySegment<byte> v) => v.Array.Unicode(v.Offset, v.Count);
		[CLSCompliant(false)]
		public static string Unicode(this byte[] v, uint count, int index = 0) => Unicode(v, index, (int)count);

		public static byte[] Cyrillic(this string v) => WindowsCyrillic.GetBytes(v);
		public static string Cyrillic(this byte[] v) => Cyrillic(v, 0, v.Length);
		public static string Cyrillic(this byte[] v, int index, int count) => WindowsCyrillic.GetString(v, index, count);
		public static string Cyrillic(this ArraySegment<byte> v) => v.Array.Cyrillic(v.Offset, v.Count);
		[CLSCompliant(false)]
		public static string Cyrillic(this byte[] v, uint count, int index = 0) => Cyrillic(v, index, (int)count);

		public static byte[] Hex(this string v) => HexEncoding.GetBytes(v);
		public static string Hex(this byte[] v) => Hex(v, 0, v.Length);
		public static string Hex(this byte[] v, int index, int count) => HexEncoding.GetString(v, index, count);
		public static string Hex(this ArraySegment<byte> v) => v.Array.Hex(v.Offset, v.Count);
		[CLSCompliant(false)]
		public static string Hex(this byte[] v, uint count, int index = 0) => Hex(v, index, (int)count);

		public static SecureString Secure(this string str)
			=> str?.ToCharArray().TypedTo<char[], SecureString>();

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
		/// Convert key to numeric identifier.
		/// </summary>
		/// <param name="key">Key.</param>
		/// <returns>Identifier.</returns>
		public static int? ToId(this SecureString key) => key?.UnSecure().GetDeterministicHashCode();

		[CLSCompliant(false)]
		public static string ToString(this char[] arr, uint count, int index = 0)
			=> arr.ToString((int)count, index);

		public static string ToString(this char[] arr, int count, int index = 0)
			=> count == 0 ? string.Empty : new string(arr, index, count);

		public static string ToBitString(this ArraySegment<byte> buffer, char separator = ' ')
			=> buffer.Array.ToBitString(buffer.Offset, buffer.Count, separator);

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

		public static byte[] ToByteArray(this string bitString, char separator = ' ')
		{
			if (bitString is null)
				throw new ArgumentNullException(nameof(bitString));

			if (bitString.Length == 0)
				return Array.Empty<byte>();

			var bitStrings = bitString.Split(separator);
			var bytes = new byte[bitStrings.Length];

			for (var i = 0; i < bitStrings.Length; i++)
			{
				bytes[i] = (byte)Convert.ToInt32(bitStrings[i], 2);
			}

			return bytes;
		}

		public static unsafe int GetDeterministicHashCode(this string value)
		{
			if (value is null)
				throw new ArgumentNullException(nameof(value));

			// decompiled code from .NET FW
			// reason - make in stable in FW and CORE
			// https://andrewlock.net/why-is-string-gethashcode-different-each-time-i-run-my-program-in-net-core/

			fixed (char* str = value)
			{
				char* chPtr = str;
				
				if (Environment.Is64BitProcess)
				{
					int hash1 = 5381;
                    int hash2 = hash1;

                    int c;
                    while ((c = chPtr[0]) != 0)
					{
                        hash1 = ((hash1 << 5) + hash1) ^ c;
                        c = chPtr[1];

                        if (c == 0)
                            break;

                        hash2 = ((hash2 << 5) + hash2) ^ c;
                        chPtr += 2;
                    }

                    return hash1 + (hash2 * 1566083941);
				}
				else
				{
					int num = 352654597;
					int num2 = num;
					int* numPtr = (int*)chPtr;
					int length = value.Length;

					while (length > 2)
					{
						num = (((num << 5) + num) + (num >> 27)) ^ numPtr[0];
						num2 = (((num2 << 5) + num2) + (num2 >> 27)) ^ numPtr[1];
						numPtr += 2;
						length -= 4;
					}

					if (length > 0)
					{
						num = (((num << 5) + num) + (num >> 27)) ^ numPtr[0];
					}

					return (num + (num2 * 1566083941));
				}
			}
		}

		public static long? TryToLong(this string str)
		{
			return long.TryParse(str, out var l) ? l : (long?)null;
		}

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

			// Store the first 2 characters of "pattern"
			char c0 = pattern[0];
			char c1 = pattern[1];

			// Find the first occurrence of the first character
			int first = source.IndexOf(c0, 0, limit);
			while (first != -1)
			{
				// Check if the following character is the same like
				// the 2nd character of "pattern"
				if (source[first + 1] != c1)
				{
					first = source.IndexOf(c0, ++first, limit - first);
					continue;
				}

				// Check the rest of "pattern" (starting with the 3rd character)
				bool found = true;
				for (int j = 2; j < pattern.Length; j++)
				{
					if (source[first + j] != pattern[j])
					{
						found = false;
						break;
					}
				}

				// If the whole word was found, return its index, otherwise try again
				if (found)
				{
					return first;
				}

				first = source.IndexOf(c0, ++first, limit - first);
			}

			return -1;
		}

		/// <summary>
		/// Removes multiple whitespace characters from a string.
		/// </summary>
		/// <param name="text">
		/// </param>
		/// <returns>
		/// The remove multiple whitespace.
		/// </returns>
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

		[Obsolete]
		public static string UrlEscape(this string url)
			=> Uri.EscapeUriString(url);

		public static string DataEscape(this string url)
			=> Uri.EscapeDataString(url);

		public static string DataUnEscape(this string url)
			=> Uri.UnescapeDataString(url);

		public static char ToLower(this char c, bool invariant = true)
			=> invariant ? char.ToLowerInvariant(c) : char.ToLower(c);

		public static char ToUpper(this char c, bool invariant = true)
			=> invariant ? char.ToUpperInvariant(c) : char.ToUpper(c);

		public static string GetLangCode(this string cultureName)
			=> cultureName.To<CultureInfo>().TwoLetterISOLanguageName;

		public static void RemoveLast(this StringBuilder builder, int count)
			=> builder.Remove(builder.Length - count, count);

		public static bool IsEmpty(this StringBuilder builder)
			=> builder.CheckOnNull(nameof(builder)).Length == 0;

		public static string GetAndClear(this StringBuilder builder)
		{
			if (builder is null)
				throw new ArgumentNullException(nameof(builder));

			var str = builder.ToString();
			builder.Clear();
			return str;
		}
	}
}