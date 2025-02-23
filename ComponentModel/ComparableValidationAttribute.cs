namespace Ecng.ComponentModel;

using System;
using System.ComponentModel.DataAnnotations;

using Ecng.Common;

/// <summary>
/// Provides a base class for validation attributes that compare values of a struct type.
/// </summary>
/// <typeparam name="T">The type of value to compare. Must be a struct implementing IComparable&lt;T&gt;.</typeparam>
public abstract class ComparableValidationAttribute<T> : ValidationAttribute, IValidator
	where T : struct, IComparable<T>
{
	/// <summary>
	/// Gets or sets a value indicating whether to disable the null check.
	/// When set to true, null values bypass validation.
	/// </summary>
	public bool DisableNullCheck { get; set; }

	/// <summary>
	/// Validates the specified value by converting it and applying the custom validation.
	/// </summary>
	/// <param name="value">The value to validate.</param>
	/// <returns>
	/// True if the value is valid; otherwise, false.
	/// </returns>
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

	/// <summary>
	/// Validates the given nullable value using a custom rule.
	/// </summary>
	/// <param name="value">The nullable value to validate.</param>
	/// <param name="disableNullCheck">A flag indicating whether null check should be disabled.</param>
	/// <returns>
	/// True if the value passes the validation rule; otherwise, false.
	/// </returns>
	protected abstract bool Validate(T? value, bool disableNullCheck);
}

/// <summary>
/// Indicates that a comparable value must be greater than zero when provided.
/// </summary>
/// <typeparam name="T">The type of value to compare. Must be a struct implementing IComparable&lt;T&gt;.</typeparam>
public abstract class ComparableGreaterThanZeroAttribute<T> : ComparableValidationAttribute<T>
	where T : struct, IComparable<T>
{
	/// <summary>
	/// Validates that the value is greater than zero if it is not null.
	/// </summary>
	/// <param name="value">The nullable value to validate.</param>
	/// <param name="disableNullCheck">A flag indicating whether null value should bypass validation.</param>
	/// <returns>
	/// True if the value is null and null checks are disabled, or if the non-null value is greater than zero; otherwise, false.
	/// </returns>
	protected override bool Validate(T? value, bool disableNullCheck)
		=> value is null ? disableNullCheck : value.Value.CompareTo(default) > 0;
}

/// <summary>
/// Indicates that a comparable value must be either null or greater than zero.
/// </summary>
/// <typeparam name="T">The type of value to compare. Must be a struct implementing IComparable&lt;T&gt;.</typeparam>
public abstract class ComparableNullOrMoreZeroAttribute<T> : ComparableValidationAttribute<T>
	where T : struct, IComparable<T>
{
	/// <summary>
	/// Validates that the value is null or greater than zero.
	/// </summary>
	/// <param name="value">The nullable value to validate.</param>
	/// <param name="disableNullCheck">A flag indicating whether null check should be disabled (not used in this case).</param>
	/// <returns>
	/// True if the value is null or the non-null value is greater than zero; otherwise, false.
	/// </returns>
	protected override bool Validate(T? value, bool disableNullCheck)
		=> value is null || value.Value.CompareTo(default) > 0;
}

/// <summary>
/// Indicates that a comparable value must be either null or not negative.
/// </summary>
/// <typeparam name="T">The type of value to compare. Must be a struct implementing IComparable&lt;T&gt;.</typeparam>
public abstract class ComparableNullOrNotNegativeAttribute<T> : ComparableValidationAttribute<T>
	where T : struct, IComparable<T>
{
	/// <summary>
	/// Validates that the value is null or not negative.
	/// </summary>
	/// <param name="value">The nullable value to validate.</param>
	/// <param name="disableNullCheck">A flag indicating whether null check should be disabled (not used in this case).</param>
	/// <returns>
	/// True if the value is null or the non-null value is not negative; otherwise, false.
	/// </returns>
	protected override bool Validate(T? value, bool disableNullCheck)
		=> value is null || value.Value.CompareTo(default) >= 0;
}

/// <summary>
/// Indicates that a comparable value must not be negative when provided.
/// </summary>
/// <typeparam name="T">The type of value to compare. Must be a struct implementing IComparable&lt;T&gt;.</typeparam>
public abstract class ComparableNotNegativeAttribute<T> : ComparableValidationAttribute<T>
	where T : struct, IComparable<T>
{
	/// <summary>
	/// Validates that the value is not negative if it is provided.
	/// </summary>
	/// <param name="value">The nullable value to validate.</param>
	/// <param name="disableNullCheck">A flag indicating whether null value should bypass validation.</param>
	/// <returns>
	/// True if the value is null and null checks are disabled, or if the non-null value is not negative; otherwise, false.
	/// </returns>
	protected override bool Validate(T? value, bool disableNullCheck)
		=> value is null ? disableNullCheck : value.Value.CompareTo(default) >= 0;
}