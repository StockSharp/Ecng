namespace Ecng.Web
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.IO;
	using System.Security;

	using Ecng.Common;
	using Ecng.Collections;

	public static class WebHelper
	{
		public static IWebUser TryGetByNameOrEmail(this IWebUserCollection users, string id)
		{
			if (users == null)
				throw new ArgumentNullException(nameof(users));

			return users.GetByName(id) ?? users.GetByEmail(id);
		}

		public static string XmlEscape(this string content)
		{
			return SecurityElement.Escape(content);
		}

		public static string ClearUrl(this string url)
		{
			var chars = new List<char>(url);

			var count = chars.Count;

			for (var i = 0; i < count; i++)
			{
				if (!IsUrlSafeChar(chars[i]))
				{
					chars.RemoveAt(i);
					count--;
					i--;
				}
			}

			return new string(chars.ToArray());
		}

		public static bool IsUrlSafeChar(this char ch)
		{
			if (((ch < 'a') || (ch > 'z')) && ((ch < 'A') || (ch > 'Z')) && ((ch < '0') || (ch > '9')))
			{
				switch (ch)
				{
					case '(':
					case ')':
					//case '*':
					case '-':
					//case '.':
					case '!':
						break;

					case '+':
					case ',':
					case '.':
					case '%':
					case '*':
						return false;

					default:
						if (ch != '_')
							return false;

						break;
				}
			}

			return true;
		}

		private static readonly SynchronizedSet<string> _imgExts = new SynchronizedSet<string>
		{
			".png", ".jpg", ".jpeg", ".bmp", ".gif", ".svg"
		};

		public static bool IsImage(this IWebFile file)
		{
			if (file == null)
				throw new ArgumentNullException(nameof(file));

			var ext = Path.GetExtension(file.Name);

			if (ext.IsEmpty())
				return false;

			return _imgExts.Contains(ext.ToLowerInvariant());
		}

		private static readonly string[] _urlParts = { "href=", "http:", "https:", "ftp:" };

		public static bool CheckContainsUrl(this string url)
		{
			return !url.IsEmpty() && _urlParts.Any(url.ContainsIgnoreCase);
		}
	}
}