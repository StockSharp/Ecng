namespace Ecng.Xaml
{
	using System.Globalization;
	using System.IO;
	using System.Windows.Controls;

	using Ecng.Common;
	using Ecng.Localization;

	class FileValidationRule : ValidationRule
	{
		public bool IsActive { get; set; } = true;

		public override ValidationResult Validate(object value, CultureInfo cultureInfo)
		{
			var path = (string)value;

			if (IsActive && (!path.IsEmpty() && !File.Exists(path)))
				return new ValidationResult(false, "Invalid file path.".Translate());

			return ValidationResult.ValidResult;
		}
	}
}