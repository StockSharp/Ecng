namespace Ecng.ComponentModel;

using System;
using System.ComponentModel.DataAnnotations;

/// <summary>
/// Base validation attribute for <see cref="Price"/> that validates only the <see cref="Price.Value"/> component.
/// </summary>
public abstract class PriceValidationAttributeBase : ValidationAttribute, IValidator
{
	/// <summary>
	/// If true, null values are treated as valid (only for attributes where null is not unconditionally allowed).
	/// </summary>
	public bool DisableNullCheck { get; set; }

	/// <summary>
	/// Gets a value indicating whether null price values are always accepted (independently of <see cref="DisableNullCheck"/>).
	/// </summary>
	protected virtual bool AcceptNull => false;

	/// <summary>
	/// Core validation over the numeric price value (already extracted).
	/// </summary>
	/// <param name="value">Price numeric value.</param>
	protected abstract bool Validate(decimal value);

	/// <inheritdoc />
	public override bool IsValid(object value)
	{
		if (value is null)
			return AcceptNull || DisableNullCheck;

		if (value is not Price price)
			return false;

		return Validate(price.Value);
	}
}

/// <summary>
/// Ensures a <see cref="Price"/> value is greater than zero (> 0). Null is valid only if <see cref="PriceValidationAttributeBase.DisableNullCheck"/> is true.
/// </summary>
public class PriceGreaterThanZeroAttribute : PriceValidationAttributeBase
{
	/// <inheritdoc />
	protected override bool Validate(decimal value) => value > 0m;
}

/// <summary>
/// Ensures a <see cref="Price"/> value is null or greater than zero (> 0).
/// </summary>
public class PriceNullOrMoreZeroAttribute : PriceValidationAttributeBase
{
	/// <inheritdoc />
	protected override bool AcceptNull => true;
	/// <inheritdoc />
	protected override bool Validate(decimal value) => value > 0m;
}

/// <summary>
/// Ensures a <see cref="Price"/> value is null or not negative (>= 0).
/// </summary>
public class PriceNullOrNotNegativeAttribute : PriceValidationAttributeBase
{
	/// <inheritdoc />
	protected override bool AcceptNull => true;
	/// <inheritdoc />
	protected override bool Validate(decimal value) => value >= 0m;
}

/// <summary>
/// Ensures a <see cref="Price"/> value is not negative (>= 0). Null is valid only if <see cref="PriceValidationAttributeBase.DisableNullCheck"/> is true.
/// </summary>
public class PriceNotNegativeAttribute : PriceValidationAttributeBase
{
	/// <inheritdoc />
	protected override bool Validate(decimal value) => value >= 0m;
}

/// <summary>
/// Validates that a <see cref="Price"/> value lies on a discrete grid defined by Base + n * Step using <see cref="Price.Value"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
[CLSCompliant(false)]
public class PriceStepAttribute : ValidationAttribute, IValidator
{
	/// <summary>
	/// Creates a new instance of <see cref="PriceStepAttribute"/>.
	/// </summary>
	/// <param name="step">Positive step.</param>
	/// <param name="baseValue">Base value (default 0).</param>
	public PriceStepAttribute(decimal step, decimal baseValue = 0)
	{
		if (step <= 0)
			throw new ArgumentOutOfRangeException(nameof(step));

		Step = step;
		BaseValue = baseValue;
	}

	/// <summary>Step (>0).</summary>
	public decimal Step { get; }

	/// <summary>Base value.</summary>
	public decimal BaseValue { get; }

	/// <inheritdoc />
	public bool DisableNullCheck { get; set; }

	/// <inheritdoc />
	public override bool IsValid(object value)
	{
		if (value is null)
			return DisableNullCheck;

		if (value is not Price price)
			return false;

		var diff = price.Value - BaseValue;
		return diff % Step == 0m;
	}
}