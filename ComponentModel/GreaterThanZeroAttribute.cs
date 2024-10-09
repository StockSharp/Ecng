namespace Ecng.ComponentModel;

public class GreaterThanZeroAttribute : DecimalValidationAttribute
{
	protected override bool Validate(decimal? value)
		=> value > 0;
}