namespace Ecng.Net.BBCodes
{
	using System;

	using Ecng.Common;

	public class BBCodesContext
	{
		public BBCodesContext(bool preventScaling, bool allowHtml, string langCode, bool isUrlLocalizeDisabled, string scheme)
		{
			if (langCode.IsEmpty())
				throw new ArgumentNullException(nameof(langCode));

			if (scheme.IsEmpty())
				throw new ArgumentNullException(nameof(scheme));

			PreventScaling = preventScaling;
			AllowHtml = allowHtml;
			LangCode = langCode;
			IsUrlLocalizeDisabled = isUrlLocalizeDisabled;
			Scheme = scheme;
		}

		public readonly bool PreventScaling;
		public readonly bool AllowHtml;
		public readonly string LangCode;
		public readonly bool IsUrlLocalizeDisabled;
		public readonly string Scheme;
	}
}