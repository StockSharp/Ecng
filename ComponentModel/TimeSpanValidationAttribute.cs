namespace Ecng.ComponentModel;

using System;
using System.ComponentModel.DataAnnotations;

using Ecng.Common;

/// <summary>
/// Indicates that a TimeSpan value must be greater than zero when provided.
/// </summary>
public class TimeSpanGreaterThanZeroAttribute : ComparableGreaterThanZeroAttribute<TimeSpan>
{
}

/// <summary>
/// Indicates that a TimeSpan value must be either null or greater than zero.
/// </summary>
public class TimeSpanNullOrMoreZeroAttribute : ComparableNullOrMoreZeroAttribute<TimeSpan>
{
}

/// <summary>
/// Indicates that a TimeSpan value must be either null or not negative.
/// </summary>
public class TimeSpanNullOrNotNegativeAttribute : ComparableNullOrNotNegativeAttribute<TimeSpan>
{
}

/// <summary>
/// Indicates that a TimeSpan value must not be negative when provided.
/// </summary>
public class TimeSpanNotNegativeAttribute : ComparableNotNegativeAttribute<TimeSpan>
{
}

/// <summary>
/// Validates that a <see cref="TimeSpan"/> value lies on a discrete grid defined by Base + n * Step (in ticks).
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class TimeSpanStepAttribute : ValidationAttribute, IValidator
{
	/// <summary>
	/// Creates a new instance using TimeSpan values.
	/// </summary>
	/// <param name="step">Positive step.</param>
	/// <param name="baseValue">Base offset (default TimeSpan.Zero).</param>
	public TimeSpanStepAttribute(TimeSpan step, TimeSpan baseValue = default)
	{
		if (step <= TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(step), "Step must be > 0.");

		StepTicks = step.Ticks;
		BaseTicks = baseValue.Ticks;
	}

	/// <summary>
	/// Creates a new instance using millisecond step/base.
	/// </summary>
	/// <param name="stepMilliseconds">Positive step in milliseconds.</param>
	/// <param name="baseMilliseconds">Base offset in milliseconds (default 0).</param>
	public TimeSpanStepAttribute(long stepMilliseconds, long baseMilliseconds = 0)
		: this(TimeSpan.FromMilliseconds(stepMilliseconds), TimeSpan.FromMilliseconds(baseMilliseconds))
	{
	}

	/// <summary>
	/// Creates a new instance using TimeSpan strings (standard TimeSpan.Parse formats).
	/// </summary>
	/// <param name="step">Positive step (e.g. "00:00:05" or "0:0:5").</param>
	/// <param name="baseValue">Base offset (default 00:00:00).</param>
	public TimeSpanStepAttribute(string step, string baseValue = "00:00:00")
		: this((step.IsEmptyOrWhiteSpace() ? throw new ArgumentNullException(nameof(step)) : step.To<TimeSpan>()), baseValue.To<TimeSpan>())
	{
	}

	/// <summary>Step in ticks (always > 0).</summary>
	public long StepTicks { get; }

	/// <summary>Base offset in ticks.</summary>
	public long BaseTicks { get; }

	/// <inheritdoc />
	public bool DisableNullCheck { get; set; }

	/// <inheritdoc />
	public override bool IsValid(object value)
	{
		if (value is null)
			return DisableNullCheck;

		if (value is not TimeSpan ts)
			return false;

		checked
		{
			var diff = ts.Ticks - BaseTicks;
			return diff % StepTicks == 0;
		}
	}
}