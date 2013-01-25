namespace Ecng.Serialization
{
	using System;

	public class TimeSpanFieldFactory : PrimitiveFieldFactory<TimeSpan, long>
	{
		public TimeSpanFieldFactory(Field field, int order)
			: base(field, order)
		{
		}
	}

	public sealed class TimeSpanAttribute : ReflectionImplFieldFactoryAttribute
	{
		public TimeSpanAttribute()
			: base(typeof(TimeSpanFieldFactory))
		{
		}
	}
}