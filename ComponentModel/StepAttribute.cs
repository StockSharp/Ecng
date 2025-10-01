namespace Ecng.ComponentModel;

using System;
using System.ComponentModel.DataAnnotations;

using Ecng.Common;

/// <summary>
/// Validates that a numeric value lies on a discrete grid defined by Base + n * Step.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
[CLSCompliant(false)]
public class StepAttribute : ValidationAttribute, IValidator
{
	/// <summary>
	/// Creates new <see cref="StepAttribute"/>.
	/// </summary>
	/// <param name="step">Positive step.</param>
	/// <param name="baseValue">Grid base (default 0).</param>
	public StepAttribute(decimal step, decimal baseValue = 0)
	{
		if (step <= 0)
			throw new ArgumentOutOfRangeException(nameof(step));

		Step = step;
		BaseValue = baseValue;
	}

	/// <summary>Grid step.</summary>
	public decimal Step { get; }

	/// <summary>Grid base.</summary>
	public decimal BaseValue { get; }

	/// <summary>Allow null values.</summary>
	public bool DisableNullCheck { get; set; }

	/// <inheritdoc />
	public override bool IsValid(object value)
	{
		if (value is null)
			return DisableNullCheck;

		decimal val;

		try
		{
			val = value.To<decimal>();
		}
		catch
		{
			return false;
		}

		var diff = val - BaseValue;
		var remainder = diff % Step;
		return remainder == 0m;
	}
}
