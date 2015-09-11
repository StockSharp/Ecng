namespace Ecng.Common
{
	using System;
	using System.Xml;
	using System.Xml.Linq;

	public static class XmlHelper
	{
		public static T GetElementValue<T>(this XElement parent, string name, T defaultValue = default(T))
		{
			if (parent == null)
				throw new ArgumentNullException("parent");

			var elem = parent.Element(name);
			return elem == null ? defaultValue : elem.Value.To<T>();
		}

		public static T GetAttributeValue<T>(this XElement elem, string name, T defaultValue = default(T))
		{
			if (elem == null)
				throw new ArgumentNullException("elem");

			var attr = elem.Attribute(name);
			return attr == null ? defaultValue : attr.Value.To<T>();
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