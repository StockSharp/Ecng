namespace Ecng.ComponentModel;

using System;
using System.ComponentModel.DataAnnotations;

using Ecng.Common;

public abstract class ComparableValidationAttribute<T> : ValidationAttribute, IValidator
	where T : struct, IComparable<T>
{
	public override bool IsValid(object value)
		=> IsValid(value, true);

	public bool IsValid(object value, bool checkOnNull)
	{
		try
		{
			return Validate(value.To<T?>(), checkOnNull);
		}
		catch (Exception)
		{
			return false;
		}
	}

	protected abstract bool Validate(T? value, bool checkOnNull);
}

public abstract class ComparableGreaterThanZeroAttribute<T> : ComparableValidationAttribute<T>
	where T : struct, IComparable<T>
{
	protected override bool Validate(T? value, bool checkOnNull)
		=> value is null ? !checkOnNull : value.Value.CompareTo(default) > 0;
}

public abstract class ComparableNullOrMoreZeroAttribute<T> : ComparableValidationAttribute<T>
	where T : struct, IComparable<T>
{
	protected override bool Validate(T? value, bool checkOnNull)
		=> value is null || value.Value.CompareTo(default) > 0;
}

public abstract class ComparableNullOrNotNegativeAttribute<T> : ComparableValidationAttribute<T>
	where T : struct, IComparable<T>
{
	protected override bool Validate(T? value, bool checkOnNull)
		=> value is null || value.Value.CompareTo(default) >= 0;
}

public abstract class ComparableNotNegativeAttribute<T> : ComparableValidationAttribute<T>
	where T : struct, IComparable<T>
{
	protected override bool Validate(T? value, bool checkOnNull)
		=> value is null ? !checkOnNull : value.Value.CompareTo(default) >= 0;
}