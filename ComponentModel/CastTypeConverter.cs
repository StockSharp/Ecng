namespace Ecng.ComponentModel
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Globalization;
	using System.Linq;

	using Ecng.Common;

	/// <summary>
	/// Converts from <see cref="IEnumerable{Object}"/> to <see cref="IEnumerable{T}"/>.
	/// </summary>
	public class CastTypeConverter : TypeConverter
	{
		/// <inheritdoc />
		public override bool CanConvertFrom(ITypeDescriptorContext ctx, Type sourceType)
		{
			if (!typeof(IEnumerable).IsAssignableFrom(sourceType) || ctx.PropertyDescriptor == null)
				return base.CanConvertFrom(ctx, sourceType);

			var argType = TryGetIEnumerableArg(ctx.PropertyDescriptor.PropertyType);

			return argType != null || base.CanConvertFrom(ctx, sourceType);
		}

		/// <inheritdoc />
		public override object ConvertFrom(ITypeDescriptorContext ctx, CultureInfo culture, object value)
		{
			var argType = TryGetIEnumerableArg(ctx.PropertyDescriptor?.PropertyType);

			if (value == null || argType == null)
				return null;

			var objects = ((IEnumerable)value).Cast<object>().ToHashSet().ToArray();
			var arr = argType.CreateArray(objects.Length);

			var isConvertible = typeof(IConvertible).IsAssignableFrom(argType);

			for (var i = 0; i < arr.Length; ++i)
				arr.SetValue(isConvertible ? objects[i].To(argType) : objects[i], i);

			return arr;
		}

		static Type TryGetIEnumerableArg(Type type)
		{
			if (type == null || !type.IsGenericType || type.GetGenericTypeDefinition() != typeof(IEnumerable<>))
				return null;

			return type.GetGenericArguments()[0];
		}
	}
}