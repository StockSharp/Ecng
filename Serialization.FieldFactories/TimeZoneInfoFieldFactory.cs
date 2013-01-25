namespace Ecng.Serialization
{
	using System;

	public class TimeZoneInfoFieldFactory : PrimitiveFieldFactory<TimeZoneInfo, string>
	{
		public TimeZoneInfoFieldFactory(Field field, int order)
			: base(field, order)
		{
		}
	}

	public sealed class TimeZoneInfoAttribute : ReflectionImplFieldFactoryAttribute
	{
		public TimeZoneInfoAttribute()
			: base(typeof(TimeZoneInfoFieldFactory))
		{
		}
	}
}