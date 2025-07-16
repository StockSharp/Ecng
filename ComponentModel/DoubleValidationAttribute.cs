namespace Ecng.ComponentModel;

/// <summary>
/// Represents a validation attribute that ensures a double value is greater than zero when provided.
/// </summary>
public class DoubleGreaterThanZeroAttribute : ComparableGreaterThanZeroAttribute<double>
{
}

/// <summary>
/// Represents a validation attribute that ensures a double value is either null or greater than zero.
/// </summary>
public class DoubleNullOrMoreZeroAttribute : ComparableNullOrMoreZeroAttribute<double>
{
}

/// <summary>
/// Represents a validation attribute that ensures a double value is either null or not negative.
/// </summary>
public class DoubleNullOrNotNegativeAttribute : ComparableNullOrNotNegativeAttribute<double>
{
}

/// <summary>
/// Represents a validation attribute that ensures a double value is not negative when provided.
/// </summary>
public class DoubleNotNegativeAttribute : ComparableNotNegativeAttribute<double>
{
}
