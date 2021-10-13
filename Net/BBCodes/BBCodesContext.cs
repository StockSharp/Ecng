namespace Ecng.Net.BBCodes
{
	using System;

	using Ecng.Common;

	public class BBCodesContext<TDomain>
	{
		public BBCodesContext(bool preventScaling, bool allowHtml, TDomain domain, bool isUrlLocalizeDisabled, string scheme)
		{
			if (domain.IsDefault())
				throw new ArgumentNullException(nameof(domain));

			if (scheme.IsEmpty())
				throw new ArgumentNullException(nameof(scheme));

			PreventScaling = preventScaling;
			AllowHtml = allowHtml;
			Domain = domain;
			IsUrlLocalizeDisabled = isUrlLocalizeDisabled;
			Scheme = scheme;
		}

		public readonly bool PreventScaling;
		public readonly bool AllowHtml;
		public readonly TDomain Domain;
		public readonly bool IsUrlLocalizeDisabled;
		public readonly string Scheme;
	}
}