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
			if (type is null)
				throw new ArgumentNullException(nameof(type));

			return type.GetUnderlyingType() != null;
		}

		public static bool IsNull<T>(this T value)
		{
			return value.IsNull(false);
		}

		public static bool IsNull<T>(this T value, bool checkValueTypeOnDefault)
		{
			if (value is not ValueType)
				return value is null;

			if (!checkValueTypeOnDefault)
				return false;

			var defValue = default(T);

			// typeof(T) == typeof(object)
			defValue ??= (T)Activator.CreateInstance(value.GetType());

			return value.Equals(defValue);
		}

		public static TResult Convert<T, TResult>(this T value, Func<T, TResult> notNullFunc, Func<TResult> nullFunc)
			where T : class
		{
			if (notNullFunc is null)
				throw new ArgumentNullException(nameof(notNullFunc));

			if (nullFunc is null)
				throw new ArgumentNullException(nameof(nullFunc));

			return value is null ? nullFunc() : notNullFunc(value);
		}

		public static T? DefaultAsNull<T>(this T value)
			where T : struct
		{
			return value.IsDefault() ? null : value;
		}
	}
}