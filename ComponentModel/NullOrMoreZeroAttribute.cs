namespace Ecng.ComponentModel;

public class NullOrMoreZeroAttribute : DecimalValidationAttribute
{
	protected override bool Validate(decimal? value)
		=> value is null or > 0;
}