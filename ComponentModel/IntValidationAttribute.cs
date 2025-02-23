namespace Ecng.ComponentModel;

/// <summary>
/// Specifies that an integer value must be greater than zero when validated.
/// </summary>
public class IntGreaterThanZeroAttribute : ComparableGreaterThanZeroAttribute<int>
{
}

/// <summary>
/// Specifies that an integer value must be either null or greater than zero.
/// </summary>
public class IntNullOrMoreZeroAttribute : ComparableNullOrMoreZeroAttribute<int>
{
}

/// <summary>
/// Specifies that an integer value must be either null or not negative.
/// </summary>
public class IntNullOrNotNegativeAttribute : ComparableNullOrNotNegativeAttribute<int>
{
}

/// <summary>
/// Specifies that an integer value must not be negative when provided.
/// </summary>
public class IntNotNegativeAttribute : ComparableNotNegativeAttribute<int>
{
}