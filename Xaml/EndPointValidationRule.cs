namespace Ecng.Xaml
{
	using System;
	using System.Globalization;
	using System.Net;
	using System.Windows.Controls;

	using Ecng.Common;
	using Ecng.Localization;

	using MoreLinq;

	public class EndPointValidationRule : ValidationRule
	{
		public bool Multi { get; set; }

		public override ValidationResult Validate(object value, CultureInfo cultureInfo)
		{
			if (value == null)
				return new ValidationResult(false, "Incorrect address.".Translate());

			try
			{
				if (Multi)
					value.To<string>().SplitBySep(",").ForEach(v => v.To<EndPoint>());
				else
					value.To<EndPoint>();

				return ValidationResult.ValidResult;
			}
			catch (Exception)
			{
				return new ValidationResult(false, "Incorrect address.".Translate());
			}
		}
	}
}