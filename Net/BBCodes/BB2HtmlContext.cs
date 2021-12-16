namespace Ecng.Net.BBCodes
{
	using System;

	using Ecng.Common;

	public class BB2HtmlContext
	{
		public BB2HtmlContext(bool preventScaling, bool allowHtml, string scheme)
		{
			if (scheme.IsEmpty())
				throw new ArgumentNullException(nameof(scheme));

			PreventScaling = preventScaling;
			AllowHtml = allowHtml;
			//IsUrlLocalizeDisabled = isUrlLocalizeDisabled;
			Scheme = scheme;
		}

		public readonly bool PreventScaling;
		public readonly bool AllowHtml;
		//public readonly bool IsUrlLocalizeDisabled;
		public readonly string Scheme;

		public virtual string GetLocString(string key) => key;
	}
}