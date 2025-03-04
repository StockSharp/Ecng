namespace Ecng.ComponentModel;

#region Using Directives

using System.ComponentModel;
using System.Globalization;

#endregion

/// <summary>
/// Auxilary class for accessing typed data from type converters.
/// </summary>
public static class TypeConverterHelper
{
	/// <summary>
	/// Gets the converter.
	/// </summary>
	/// <returns></returns>
	public static TypeConverter GetConverter<T>()
	{
		return TypeDescriptor.GetConverter(typeof(T));
	}

	/// <summary>
	/// Converts the specified text to an object.
	/// </summary>
	/// <param name="value">The value.</param>
	/// <returns></returns>
	public static T FromString<T>(string value)
	{
		return (T)GetConverter<T>().ConvertFromString(value);
	}

	/// <summary>
	/// Converts the specified text to an object.
	/// </summary>
	/// <param name="context">The context.</param>
	/// <param name="culture">The culture.</param>
	/// <param name="value">The value.</param>
	/// <returns></returns>
	public static T FromString<T>(ITypeDescriptorContext context, CultureInfo culture, string value)
	{
		return (T)GetConverter<T>().ConvertFromString(context, culture, value);
	}

	/// <summary>
	/// Converts the specified value to a string representation.
	/// </summary>
	/// <param name="value">The value.</param>
	/// <returns></returns>
	public static string ToString<T>(T value)
	{
		return GetConverter<T>().ConvertToString(value);
	}

	/// <summary>
	/// Converts the specified value to a string representation.
	/// </summary>
	/// <param name="context">The context.</param>
	/// <param name="culture">The culture.</param>
	/// <param name="value">The value.</param>
	/// <returns></returns>
	public static string ToString<T>(ITypeDescriptorContext context, CultureInfo culture, T value)
	{
		return GetConverter<T>().ConvertToString(context, culture, value);
	}
}