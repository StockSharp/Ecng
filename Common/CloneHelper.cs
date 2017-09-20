namespace Ecng.Common
{
	using System;

	[Obsolete]
	public static class CloneHelper
	{
		public static T CloneNullable<T>(this T obj)
			where T : class, ICloneable
		{
			if (obj == null)
				return null;

			return (T)obj.Clone();
		}
	}
}