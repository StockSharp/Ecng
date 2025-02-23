namespace Ecng.ComponentModel;

using System;

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