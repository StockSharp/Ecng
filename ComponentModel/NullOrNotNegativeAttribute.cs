namespace Ecng.ComponentModel;

public class NullOrNotNegativeAttribute : DecimalValidationAttribute
{
	protected override bool Validate(decimal? value)
		=> value is null or >= 0;
}