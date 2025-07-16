namespace Ecng.ComponentModel;

/// <summary>
/// Represents a validation attribute that ensures a long value is greater than zero when provided.
/// </summary>
public class LongGreaterThanZeroAttribute : ComparableGreaterThanZeroAttribute<long>
{
}

/// <summary>
/// Represents a validation attribute that ensures a long value is either null or greater than zero.
/// </summary>
public class LongNullOrMoreZeroAttribute : ComparableNullOrMoreZeroAttribute<long>
{
}

/// <summary>
/// Represents a validation attribute that ensures a long value is either null or not negative.
/// </summary>
public class LongNullOrNotNegativeAttribute : ComparableNullOrNotNegativeAttribute<long>
{
}

/// <summary>
/// Represents a validation attribute that ensures a long value is not negative when provided.
/// </summary>
public class LongNotNegativeAttribute : ComparableNotNegativeAttribute<long>
{
}