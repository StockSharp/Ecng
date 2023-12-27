namespace Ecng.Common
{
	using System;
	using System.Linq;
	using System.Xml;
	using System.Xml.Linq;

	public static class XmlHelper
	{
		public static T GetElementValue<T>(this XElement parent, XName name, T defaultValue = default)
		{
			if (parent is null)
				throw new ArgumentNullException(nameof(parent));

			var elem = parent.Element(name);
			return elem is null ? defaultValue : elem.Value.To<T>();
		}

		public static T GetAttributeValue<T>(this XElement elem, XName name, T defaultValue = default)
		{
			if (elem is null)
				throw new ArgumentNullException(nameof(elem));

			var attr = elem.Attribute(name);
			return attr is null ? defaultValue : attr.Value.To<T>();
		}

		public static XmlWriter WriteAttribute(this XmlWriter writer, string name, object value)
		{
			if (writer is null)
				throw new ArgumentNullException(nameof(writer));

			writer.WriteAttributeString(name, value != null ? value.To<string>() : string.Empty);

			return writer;
		}

		public static bool Compare(this XmlNode first, XmlNode second)
		{
			if (first is null)
				throw new ArgumentNullException(nameof(first));

			if (second is null)
				throw new ArgumentNullException(nameof(second));

			return first.OuterXml == second.OuterXml;
		}

		public static bool IsXmlString(this string value)
			=> value.All(IsXmlChar);

		public static bool IsXmlChar(this char c)
			=> XmlConvert.IsXmlChar(c);
	}
}