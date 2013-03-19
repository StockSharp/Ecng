namespace Ecng.Common
{
	using System;
	using System.Xml;
	using System.Xml.Linq;

	public static class XmlHelper
	{
		public static T GetElementValue<T>(this XElement elem, string name)
		{
			return elem.GetElementValue(name).To<T>();
		}

		public static string GetElementValue(this XElement elem, string name)
		{
			if (elem == null)
				throw new ArgumentNullException("elem");

			var attr = elem.Element(name);

			if (attr == null)
				throw new ArgumentException("Element '{0}' doesn't exist.".Put(name), "name");

			return attr.Value;
		}

		public static T GetAttributeValue<T>(this XElement elem, string name, T defaultValue = default(T))
		{
			var value = elem.GetAttributeValue(name);
			return value == null ? defaultValue : value.To<T>();
		}

		public static string GetAttributeValue(this XElement elem, string name, string defaultValue = null)
		{
			if (elem == null)
				throw new ArgumentNullException("elem");

			var attr = elem.Attribute(name);
			return attr == null ? defaultValue : attr.Value;
			//throw new ArgumentException("Attribute '{0}' doesn't exist.".Put(name), "name");
		}

		public static void WriteAttribute(this XmlWriter writer, string name, object value)
		{
			if (writer == null)
				throw new ArgumentNullException("writer");

			writer.WriteAttributeString(name, value != null ? value.ToString() : String.Empty);
		}

		/// <summary>
		/// from StockSharp.Transaq
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>

		public static string GetValue(this XAttribute obj)
		{
			if (obj == null)
				return String.Empty;

			return obj.Value;
		}

		public static T GetValue<T>(this XAttribute obj, T? defaultValue = null) where T:struct
		{
			if (obj == null && defaultValue.HasValue)
				return defaultValue.Value;

			return obj.Value.To<T>();
		}

		public static T GetValue<T>(this XElement obj, T? defaultValue = null) where T : struct
		{
			if (obj == null && defaultValue.HasValue)
				return defaultValue.Value;

			return obj.Value.To<T>();
		}

		public static bool ToBool(this XAttribute obj, bool? defaultValue = null)
		{
			return obj.GetValue<bool>(defaultValue);
		}

		public static int ToInt(this XAttribute obj, int? defaultValue = null)
		{
			return obj.GetValue<int>(defaultValue);
		}

		public static long ToLong(this XAttribute obj, long? defaultValue = null)
		{
			return obj.GetValue<long>(defaultValue);
		}

		public static long ToLong(this XElement obj, long? defaultValue = null)
		{
			return obj.GetValue<long>(defaultValue);
		}

		public static int ToInt(this XElement obj, int? defaultValue = null)
		{
			return obj.GetValue<int>(defaultValue);
		}
	}
}