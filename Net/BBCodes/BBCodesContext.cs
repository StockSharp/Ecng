namespace Ecng.Net.BBCodes
{
	using System;
	using Ecng.Net;

	public class BBCodesContext
	{
		public BBCodesContext(bool preventScaling, bool allowHtml, bool isEnglish, bool isEmail, Url currentUrl, bool isLocalHost, string localPath)
		{
			PreventScaling = preventScaling;
			AllowHtml = allowHtml;
			IsEnglish = isEnglish;
			IsEmail = isEmail;
			CurrentUrl = currentUrl;
			IsLocalHost = isLocalHost;
			LocalPath = localPath;
		}

		public readonly bool PreventScaling;
		public readonly bool AllowHtml;
		public readonly bool IsEnglish;
		public readonly bool IsEmail;
		public readonly Url CurrentUrl;
		public readonly bool IsLocalHost;
		public readonly string LocalPath;

		public string Scheme => CurrentUrl?.Scheme ?? Uri.UriSchemeHttps;
	}
}