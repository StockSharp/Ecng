namespace Ecng.ComponentModel;

using System;
using System.ComponentModel.DataAnnotations;

using Ecng.Common;

public abstract class ComparableValidationAttribute<T> : ValidationAttribute, IValidator
	where T : struct, IComparable<T>
{
	public bool DisableNullCheck { get; set; }

	public override bool IsValid(object value)
	{
		try
		{
			return Validate(value.To<T?>(), DisableNullCheck);
		}
		catch (Exception)
		{
			return false;
		}
	}

	protected abstract bool Validate(T? value, bool disableNullCheck);
}

public abstract class ComparableGreaterThanZeroAttribute<T> : ComparableValidationAttribute<T>
	where T : struct, IComparable<T>
{
	protected override bool Validate(T? value, bool disableNullCheck)
		=> value is null ? disableNullCheck : value.Value.CompareTo(default) > 0;
}

public abstract class ComparableNullOrMoreZeroAttribute<T> : ComparableValidationAttribute<T>
	where T : struct, IComparable<T>
{
	protected override bool Validate(T? value, bool disableNullCheck)
		=> value is null || value.Value.CompareTo(default) > 0;
}

public abstract class ComparableNullOrNotNegativeAttribute<T> : ComparableValidationAttribute<T>
	where T : struct, IComparable<T>
{
	protected override bool Validate(T? value, bool disableNullCheck)
		=> value is null || value.Value.CompareTo(default) >= 0;
}

public abstract class ComparableNotNegativeAttribute<T> : ComparableValidationAttribute<T>
	where T : struct, IComparable<T>
{
	protected override bool Validate(T? value, bool disableNullCheck)
		=> value is null ? disableNullCheck : value.Value.CompareTo(default) >= 0;
}