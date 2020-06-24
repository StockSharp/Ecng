namespace Ecng.Xaml
{
	using System.Globalization;
	using System.IO;
	using System.Windows.Controls;

	using Ecng.Common;
	using Ecng.Localization;

	class FolderValidationRule : ValidationRule
	{
		public override ValidationResult Validate(object value, CultureInfo cultureInfo)
		{
			var path = (string)value;

			if (!path.IsEmpty() && !Directory.Exists(path))
				return new ValidationResult(false, "Invalid folder path.".Translate());

			return ValidationResult.ValidResult;
		}
	}
}