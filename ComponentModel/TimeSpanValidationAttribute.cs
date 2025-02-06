namespace Ecng.ComponentModel;

using System;

public class TimeSpanGreaterThanZeroAttribute : ComparableGreaterThanZeroAttribute<TimeSpan>
{
}

public class TimeSpanNullOrMoreZeroAttribute : ComparableNullOrMoreZeroAttribute<TimeSpan>
{
}

public class TimeSpanNullOrNotNegativeAttribute : ComparableNullOrNotNegativeAttribute<TimeSpan>
{
}