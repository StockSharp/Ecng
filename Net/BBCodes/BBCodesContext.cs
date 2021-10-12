namespace Ecng.Net.BBCodes
{
	using System;

	using Ecng.Net;

	public class BBCodesContext
	{
		public BBCodesContext(bool preventScaling, bool allowHtml, string langCode, bool isUrlLocalizeDisabled, Url currentUrl, bool isLocalHost, string localPath)
		{
			PreventScaling = preventScaling;
			AllowHtml = allowHtml;
			LangCode = langCode;
			IsUrlLocalizeDisabled = isUrlLocalizeDisabled;
			CurrentUrl = currentUrl;
			IsLocalHost = isLocalHost;
			LocalPath = localPath;
		}

		public bool IsEnglish => LangCode == "en";

		public readonly bool PreventScaling;
		public readonly bool AllowHtml;
		public readonly string LangCode;
		public readonly bool IsUrlLocalizeDisabled;
		public readonly Url CurrentUrl;
		public readonly bool IsLocalHost;
		public readonly string LocalPath;

		public string Scheme => CurrentUrl?.Scheme ?? Uri.UriSchemeHttps;
	}
}