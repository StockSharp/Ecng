namespace Ecng.Common
{
	using System;

	public static class CloneHelper
	{
		public static T TypedClone<T>(this T value)
			where T : ICloneable
		{
			return (T)value.Clone();
		}

		[Obsolete]
		public static T CloneNullable<T>(this T obj)
			where T : class, ICloneable
		{
			if (obj == null)
				return null;

			return obj.TypedClone();
		}
	}
}