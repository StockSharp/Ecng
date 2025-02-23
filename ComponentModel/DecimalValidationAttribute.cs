namespace Ecng.ComponentModel;

/// <summary>
/// Represents a validation attribute that ensures a decimal value is greater than zero when provided.
/// </summary>
public class DecimalGreaterThanZeroAttribute : ComparableGreaterThanZeroAttribute<decimal>
{
}

/// <summary>
/// Represents a validation attribute that ensures a decimal value is either null or greater than zero.
/// </summary>
public class DecimalNullOrMoreZeroAttribute : ComparableNullOrMoreZeroAttribute<decimal>
{
}

/// <summary>
/// Represents a validation attribute that ensures a decimal value is either null or not negative.
/// </summary>
public class DecimalNullOrNotNegativeAttribute : ComparableNullOrNotNegativeAttribute<decimal>
{
}

/// <summary>
/// Represents a validation attribute that ensures a decimal value is not negative when provided.
/// </summary>
public class DecimalNotNegativeAttribute : ComparableNotNegativeAttribute<decimal>
{
}