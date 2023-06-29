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
			return obj?.TypedClone();
		}

		/// <summary>
		/// Helper to clone object without UI related parts. Use with <see cref="Scope{T}"/> and clone.
		/// </summary>
		public class CloneWithoutUI {}
	}
}
