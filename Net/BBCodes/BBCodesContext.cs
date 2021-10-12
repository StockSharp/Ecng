namespace Ecng.Net.BBCodes
{
	using System;

	using Ecng.Net;

	public class BBCodesContext
	{
		public BBCodesContext(bool preventScaling, bool allowHtml, string langCode, bool isEmail, Url currentUrl, bool isLocalHost, string localPath)
		{
			PreventScaling = preventScaling;
			AllowHtml = allowHtml;
			LangCode = langCode;
			IsEmail = isEmail;
			CurrentUrl = currentUrl;
			IsLocalHost = isLocalHost;
			LocalPath = localPath;
		}

		public bool IsEnglish => LangCode == "en";

		public readonly bool PreventScaling;
		public readonly bool AllowHtml;
		public readonly string LangCode;
		public readonly bool IsEmail;
		public readonly Url CurrentUrl;
		public readonly bool IsLocalHost;
		public readonly string LocalPath;

		public string Scheme => CurrentUrl?.Scheme ?? Uri.UriSchemeHttps;
	}
}