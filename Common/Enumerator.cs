namespace Ecng.Common
{
	#region Using Directives

	using System;
	using System.Collections.Generic;
	using System.Linq;
#if SILVERLIGHT
	using System.Reflection;
#endif

	#endregion

	public static class Enumerator
	{
		public static Type GetEnumBaseType<T>()
		{
			return typeof(T).GetEnumBaseType();
		}

		public static Type GetEnumBaseType(this Type enumType)
		{
			return Enum.GetUnderlyingType(enumType);
		}

		/// <summary>
		/// Gets the name.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns></returns>
		public static string GetName<T>(T value)
		{
			return value.To<Enum>().GetName();
		}

		public static string GetName(this Enum value)
		{
			return Enum.GetName(value.GetType(), value);
		}

		public static IEnumerable<T> GetValues<T>()
		{
			return typeof(T).GetValues().Cast<T>();
		}

		public static IEnumerable<T> ExcludeObsolete<T>(this IEnumerable<T> values)
		{
			return values.Where(v => v.GetAttributeOfType<ObsoleteAttribute>() == null);
		}

		public static IEnumerable<object> GetValues(this Type enumType)
		{
#if !SILVERLIGHT
			return Enum.GetValues(enumType).Cast<object>();
#else
			var enumObj = enumType.CreateInstance<Enum>();
			return enumType.GetEnumFields().Convert(field => field.GetValue(enumObj));
#endif
		}

		public static IEnumerable<string> GetNames<T>()
		{
			return typeof(T).GetNames();
		}

		public static IEnumerable<string> GetNames(this Type enumType)
		{
#if !SILVERLIGHT
			return Enum.GetNames(enumType);
#else
			return enumType.GetEnumFields().Convert(field => field.Name);
#endif	
		}

#if SILVERLIGHT
		private static IEnumerable<FieldInfo> GetEnumFields(this Type enumType)
		{
			if (enumType == null)
				throw new ArgumentNullException("enumType");

			return enumType.GetFields().Where(field => field.Name != "value__");
		}

		private static IEnumerable<TDest> Convert<TSource, TDest>(this IEnumerable<TSource> source, Converter<TSource, TDest> converter)
		{
			return source.Select(item => converter(item)).ToList();
		}
#endif
		public static bool IsDefined<T>(this T enumValue)
		{
			return Enum.IsDefined(typeof(T), enumValue);
		}

		public static IEnumerable<T> SplitMask<T>(this T maskedValue)
		{
			return GetValues<T>().Where(v => Contains(maskedValue, v));
		}

		public static T JoinMask<T>()
		{
			return GetValues<T>().JoinMask();
		}

		public static T JoinMask<T>(this IEnumerable<T> values)
		{
			if (values == null)
				throw new ArgumentNullException(nameof(values));

			return values.Aggregate(default(T), (current, t) => (current.To<long>() | t.To<long>()).To<T>());
		}

		public static T Remove<T>(T enumSource, T enumPart)
		{
			return enumSource.To<Enum>().Remove(enumPart);
		}

		public static T Remove<T>(this Enum enumSource, T enumPart)
		{
			if (enumSource.GetType() != typeof(T))
				throw new ArgumentException("enumPart");

			return (enumSource.To<long>() & ~enumPart.To<long>()).To<T>();
		}

		public static bool Contains<T>(T enumSource, T enumPart)
		{
			return enumSource.To<Enum>().Contains(enumPart.To<Enum>());
		}

		public static bool Contains(this Enum enumSource, Enum enumPart)
		{
			return (enumSource.To<long>() & enumPart.To<long>()) == enumPart.To<long>();
		}

		public static bool TryParse<T>(this string str, out T value, bool ignoreCase = true)
			where T : struct
		{
			return Enum.TryParse(str, ignoreCase, out value);
		}

		//
		// https://stackoverflow.com/a/9276348
		//

		/// <summary>
		/// Gets an attribute on an enum field value
		/// </summary>
		/// <typeparam name="TAttribute">The type of the attribute you want to retrieve</typeparam>
		/// <param name="enumVal">The enum value</param>
		/// <returns>The attribute of type <typeparam name="TAttribute" /> that exists on the enum value</returns>
		public static TAttribute GetAttributeOfType<TAttribute>(this object enumVal)
			where TAttribute : Attribute
		{
			if (enumVal == null)
				throw new ArgumentNullException(nameof(enumVal));

			var memInfo = enumVal.GetType().GetMember(enumVal.ToString());
			return memInfo[0].GetAttribute<TAttribute>(false);
		}
	}
}