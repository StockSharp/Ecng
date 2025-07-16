namespace Ecng.ComponentModel;

/// <summary>
/// Represents a validation attribute that ensures a float value is greater than zero when provided.
/// </summary>
public class FloatGreaterThanZeroAttribute : ComparableGreaterThanZeroAttribute<float>
{
}

/// <summary>
/// Represents a validation attribute that ensures a float value is either null or greater than zero.
/// </summary>
public class FloatNullOrMoreZeroAttribute : ComparableNullOrMoreZeroAttribute<float>
{
}

/// <summary>
/// Represents a validation attribute that ensures a float value is either null or not negative.
/// </summary>
public class FloatNullOrNotNegativeAttribute : ComparableNullOrNotNegativeAttribute<float>
{
}

/// <summary>
/// Represents a validation attribute that ensures a float value is not negative when provided.
/// </summary>
public class FloatNotNegativeAttribute : ComparableNotNegativeAttribute<float>
{
}