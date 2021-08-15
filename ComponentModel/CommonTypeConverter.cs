namespace Ecng.ComponentModel
{
	#region Using Directives

	using System;
	using System.ComponentModel;
	using System.Linq;
	using System.Reflection;
	using System.Collections.Generic;
	using System.Globalization;
	using System.ComponentModel.Design.Serialization;

	using Ecng.Common;
	using Ecng.Reflection;

	#endregion

	/// <summary>
	/// Provides a unified way of converting types of values to other types, as well as for accessing specified members.
	/// </summary>
	public class CommonTypeConverter : TypeConverter
	{
		#region Private Fields

		private readonly Type _type;
		private readonly FastInvoker[] _memberInvokers;

		#endregion

		#region CommonTypeConverter.ctor()

		/// <summary>
		/// Initializes a new instance of the <see cref="CommonTypeConverter"/> class.
		/// </summary>
		/// <param name="type">The type.</param>
		public CommonTypeConverter(Type type)
		{
			if (type is null)
				throw new ArgumentNullException(nameof(type));

			var attr = type.GetAttribute<CommonTypeConverterNamesAttribute>();

			if (attr is null)
				throw new ArgumentException(nameof(type));

			_type = type;

			_memberInvokers = new FastInvoker[attr.Members.Length];

			int index = 0;
			foreach (string member in attr.Members)
			{
				var info = type.GetMember<MemberInfo>(member);

				if (info is PropertyInfo pi)
					_memberInvokers[index] = FastInvoker.Create(pi, true);
				else if (info is FieldInfo fi)
					_memberInvokers[index] = FastInvoker.Create(fi, true);
				else
					throw new ArgumentOutOfRangeException(nameof(member), member.To<string>());

				index++;
			}
		}

		#endregion

		#region TypeConverter Members

		/// <summary>
		/// Returns whether this converter can convert an object of the given type to the type of this converter, using the specified context.
		/// </summary>
		/// <param name="context">An <see cref="T:System.ComponentModel.ITypeDescriptorContext"></see> that provides a format context.</param>
		/// <param name="sourceType">A <see cref="T:System.Type"></see> that represents the type you want to convert from.</param>
		/// <returns>
		/// true if this converter can perform the conversion; otherwise, false.
		/// </returns>
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		{
			return ((sourceType == typeof(string)) || base.CanConvertFrom(context, sourceType));
		}

		/// <summary>
		/// Returns whether this converter can convert the object to the specified type, using the specified context.
		/// </summary>
		/// <param name="context">An <see cref="T:System.ComponentModel.ITypeDescriptorContext"></see> that provides a format context.</param>
		/// <param name="destinationType">A <see cref="T:System.Type"></see> that represents the type you want to convert to.</param>
		/// <returns>
		/// true if this converter can perform the conversion; otherwise, false.
		/// </returns>
		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
		{
			return ((destinationType == typeof(InstanceDescriptor)) || base.CanConvertTo(context, destinationType));
		}

		/// <summary>
		/// Returns whether changing a value on this object requires a call to <see cref="M:System.ComponentModel.TypeConverter.CreateInstance(System.Collections.IDictionary)"></see> to create a new value, using the specified context.
		/// </summary>
		/// <param name="context">An <see cref="T:System.ComponentModel.ITypeDescriptorContext"></see> that provides a format context.</param>
		/// <returns>
		/// true if changing a property on this object requires a call to <see cref="M:System.ComponentModel.TypeConverter.CreateInstance(System.Collections.IDictionary)"></see> to create a new value; otherwise, false.
		/// </returns>
		public override bool GetCreateInstanceSupported(ITypeDescriptorContext context)
		{
			return true;
		}

		/// <summary>
		/// Returns a collection of properties for the type of array specified by the value parameter, using the specified context and attributes.
		/// </summary>
		/// <param name="context">An <see cref="T:System.ComponentModel.ITypeDescriptorContext"></see> that provides a format context.</param>
		/// <param name="value">An <see cref="T:System.Object"></see> that specifies the type of array for which to get properties.</param>
		/// <param name="attributes">An array of type <see cref="T:System.Attribute"></see> that is used as a filter.</param>
		/// <returns>
		/// A <see cref="T:System.ComponentModel.PropertyDescriptorCollection"></see> with the properties that are exposed for this data type, or null if there are no properties.
		/// </returns>
		public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
		{
			var properties = TypeDescriptor.GetProperties(_type, attributes);
			return properties.Sort(_memberInvokers.Select(invoker => invoker.Member.Name).ToArray());
		}

		/// <summary>
		/// Returns whether this object supports properties, using the specified context.
		/// </summary>
		/// <param name="context">An <see cref="T:System.ComponentModel.ITypeDescriptorContext"></see> that provides a format context.</param>
		/// <returns>
		/// true if <see cref="M:System.ComponentModel.TypeConverter.GetProperties(System.Object)"></see> should be called to find the properties of this object; otherwise, false.
		/// </returns>
		public override bool GetPropertiesSupported(ITypeDescriptorContext context)
		{
			return true;
		}

		/// <summary>
		/// Converts the given object to the type of this converter, using the specified context and culture information.
		/// </summary>
		/// <param name="context">An <see cref="T:System.ComponentModel.ITypeDescriptorContext"></see> that provides a format context.</param>
		/// <param name="culture">The <see cref="T:System.Globalization.CultureInfo"></see> to use as the current culture.</param>
		/// <param name="value">The <see cref="T:System.Object"></see> to convert.</param>
		/// <returns>
		/// An <see cref="T:System.Object"></see> that represents the converted value.
		/// </returns>
		/// <exception cref="T:System.NotSupportedException">The conversion cannot be performed. </exception>
		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			if (value is not string text)
				return base.ConvertFrom(context, culture, value);

			text = text.Trim();

			if (text.IsEmpty())
				return null;

			//if (culture is null)
			//	culture = CultureInfo.CurrentCulture;
			//culture.NumberFormat.
			//throw new NotSupportedException();

			var fieldTexts = text.Split(new [] { ',', ';' });

			if (fieldTexts.Length != _memberInvokers.Length)
				throw new ArgumentException(nameof(value));

			var args = new List<object>();

			int index = 0;
			foreach (var invoker in _memberInvokers)
			{
				var converter = TypeDescriptor.GetConverter(invoker.Member.GetMemberType());
				args.Add(converter.ConvertFromString(fieldTexts[index++].Trim()));
			}

			return _type.CreateInstance(args.ToArray());
		}

		/// <summary>
		/// Converts the given value object to the specified type, using the specified context and culture information.
		/// </summary>
		/// <param name="context">An <see cref="T:System.ComponentModel.ITypeDescriptorContext"></see> that provides a format context.</param>
		/// <param name="culture">A <see cref="T:System.Globalization.CultureInfo"></see>. If null is passed, the current culture is assumed.</param>
		/// <param name="value">The <see cref="T:System.Object"></see> to convert.</param>
		/// <param name="destinationType">The <see cref="T:System.Type"></see> to convert the value parameter to.</param>
		/// <returns>
		/// An <see cref="T:System.Object"></see> that represents the converted value.
		/// </returns>
		/// <exception cref="T:System.NotSupportedException">The conversion cannot be performed. </exception>
		/// <exception cref="T:System.ArgumentNullException">The destinationType parameter is null. </exception>
		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			if (destinationType is null)
				throw new ArgumentNullException(nameof(destinationType));

			if (_type.IsInstanceOfType(value))
			{
				if (destinationType == typeof(string))
				{
					if (culture is null)
						culture = CultureInfo.CurrentCulture;

					var fieldTexts = new List<string>();

					foreach (var invoker in _memberInvokers)
					{
						var converter = TypeDescriptor.GetConverter(invoker.Member.GetMemberType());
						fieldTexts.Add(converter.ConvertToString(invoker.GetValue(value)));
					}

					return fieldTexts.Join(culture.TextInfo.ListSeparator + " ");
				}

				if (destinationType == typeof(InstanceDescriptor))
				{
					var member = _type.GetConstructor(_memberInvokers.Select(invoker => invoker.Member.GetMemberType()).ToArray());
					if (member != null)
						return new InstanceDescriptor(member, _memberInvokers.Select(invoker => invoker.GetValue(value)).ToArray());
				}

			}
			
			return base.ConvertTo(context, culture, value, destinationType);
		}

		#endregion
	}
}