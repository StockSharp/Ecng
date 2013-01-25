namespace Ecng.Xaml
{
	using System;
	using System.Globalization;
	using System.Windows.Controls;

	using Ecng.Common;
	using Ecng.ComponentModel;

	public class NumberRangeRule<T> : ValidationRule
		where T : IComparable<T>
	{
		public T Min { get; set; }
		public T Max { get; set; }

		public override ValidationResult Validate(object value, CultureInfo cultureInfo)
		{
			var parameter = default(T);

			try
			{
				if (value.To<string>().Length > 0)
				{
					parameter = value.To<T>();
				}
			}
			catch (Exception e)
			{
				return new ValidationResult(false, "Illegal characters or " + e.Message);
			}

			if (!new Range<T>(Min, Max).Contains(parameter))
			{
				return new ValidationResult(false, "Please enter value in the range: " + Min + " - " + Max + ".");
			}

			return new ValidationResult(true, null);
		}
	}

	public class DoubleRangeRule : NumberRangeRule<double>
	{
		public DoubleRangeRule()
		{
			Min = double.MinValue;
			Max = double.MaxValue;
		}
	}

	public class IntRangeRule : NumberRangeRule<int>
	{
		public IntRangeRule()
		{
			Min = int.MinValue;
			Max = int.MaxValue;
		}
	}
}