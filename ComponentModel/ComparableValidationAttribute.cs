namespace Ecng.ComponentModel;

using System;
using System.ComponentModel.DataAnnotations;

using Ecng.Common;

public abstract class ComparableValidationAttribute<T> : ValidationAttribute
	where T : struct, IComparable<T>
{
	public override bool IsValid(object value)
	{
		try
		{
			return Validate(value.To<T?>());
		}
		catch (Exception)
		{
			return false;
		}
	}

	protected abstract bool Validate(T? value);
}

public abstract class ComparableGreaterThanZeroAttribute<T> : ComparableValidationAttribute<T>
	where T : struct, IComparable<T>
{
	protected override bool Validate(T? value)
		=> value is not null && value.Value.CompareTo(default) > 0;
}

public abstract class ComparableNullOrMoreZeroAttribute<T> : ComparableValidationAttribute<T>
	where T : struct, IComparable<T>
{
	protected override bool Validate(T? value)
		=> value is null || value.Value.CompareTo(default) > 0;
}

public abstract class ComparableNullOrNotNegativeAttribute<T> : ComparableValidationAttribute<T>
	where T : struct, IComparable<T>
{
	protected override bool Validate(T? value)
		=> value is null || value.Value.CompareTo(default) >= 0;
}