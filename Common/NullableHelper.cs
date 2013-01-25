namespace Ecng.Common
{
	using System;

	public static class NullableHelper
	{
		public static Type GetUnderlyingType(this Type nullableType)
		{
			return Nullable.GetUnderlyingType(nullableType);
		}

		public static bool IsNullable(this Type type)
		{
			if (type == null)
				throw new ArgumentNullException("type");

			return type.GetUnderlyingType() != null;
		}

		public static bool IsNull<T>(this T value)
		{
			return value.IsNull(false);
		}

		public static bool IsNull<T>(this T value, bool checkValueTypeOnDefault)
		{
			if (value is ValueType)
				return checkValueTypeOnDefault && value.Equals(default(T));
			else
				return object.ReferenceEquals(value, null);
		}

		public static TResult Convert<T, TResult>(this T value, Func<T, TResult> notNullFunc, Func<TResult> nullFunc)
			where T : class
		{
			if (notNullFunc == null)
				throw new ArgumentNullException("notNullFunc");

			if (nullFunc == null)
				throw new ArgumentNullException("nullFunc");

			return value == null ? nullFunc() : notNullFunc(value);
		}
	}
}