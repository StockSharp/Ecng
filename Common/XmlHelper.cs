namespace Ecng.Common
{
	using System;
	using System.Xml;
	using System.Xml.Linq;

	public static class XmlHelper
	{
		public static T GetElementValue<T>(this XElement elem, string name, T defaultValue = default(T))
		{
			var value =  elem.GetElementValue(name, null, defaultValue.IsNull(true));
			return value == null ? defaultValue : value.To<T>();
		}

		public static string GetElementValue(this XElement elem, string name, string defaultValue = null, bool throwIfNotExist = true)
		{
			if (elem == null)
				throw new ArgumentNullException("elem");

			var attr = elem.Element(name);

			if (attr != null)
				return attr.Value;

			if (!throwIfNotExist)
				return null;

			throw new ArgumentException("Element '{0}' doesn't exist.".Put(name), "name");
		}

		public static T GetAttributeValue<T>(this XElement elem, string name, T defaultValue = default(T))
		{
			var value = elem.GetAttributeValue(name, null, defaultValue.IsNull(true));
			return value == null ? defaultValue : value.To<T>();
		}

		public static string GetAttributeValue(this XElement elem, string name, string defaultValue = null, bool throwIfNotExist = true)
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

			writer.WriteAttributeString(name, value != null ? value.To<string>() : string.Empty);
		}

		public static bool Compare(this XmlNode first, XmlNode second)
		{
			if (first == null)
				throw new ArgumentNullException("first");

			if (second == null)
				throw new ArgumentNullException("second");

			return first.OuterXml == second.OuterXml;
		}
	}
}