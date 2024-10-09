namespace Ecng.ComponentModel;

using System;
using System.ComponentModel.DataAnnotations;

using Ecng.Common;

public abstract class DecimalValidationAttribute : ValidationAttribute
{
	public override bool IsValid(object value)
	{
		try
		{
			return Validate(value.To<decimal?>());
		}
		catch (Exception)
		{
			return false;
		}
	}

	protected abstract bool Validate(decimal? value);
}
