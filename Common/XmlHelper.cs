namespace Ecng.Common;

using System;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

/// <summary>
/// Provides helper extension methods for working with XML elements, attributes, and XML strings.
/// </summary>
public static class XmlHelper
{
	/// <summary>
	/// Retrieves the value of the child element with the specified name and converts it to the specified type.
	/// </summary>
	/// <typeparam name="T">The type to which the element's value should be converted.</typeparam>
	/// <param name="parent">The parent XML element.</param>
	/// <param name="name">The name of the child element.</param>
	/// <param name="defaultValue">The default value to return if the element is not found.</param>
	/// <returns>The converted value of the child element, or the default value if the element does not exist.</returns>
	/// <exception cref="ArgumentNullException">Thrown when the parent element is null.</exception>
	public static T GetElementValue<T>(this XElement parent, XName name, T defaultValue = default)
	{
		if (parent is null)
			throw new ArgumentNullException(nameof(parent));

		var elem = parent.Element(name);
		return elem is null ? defaultValue : elem.Value.To<T>();
	}

	/// <summary>
	/// Retrieves the value of the attribute with the specified name and converts it to the specified type.
	/// </summary>
	/// <typeparam name="T">The type to which the attribute's value should be converted.</typeparam>
	/// <param name="elem">The XML element containing the attribute.</param>
	/// <param name="name">The name of the attribute.</param>
	/// <param name="defaultValue">The default value to return if the attribute is not found.</param>
	/// <returns>The converted value of the attribute, or the default value if the attribute does not exist.</returns>
	/// <exception cref="ArgumentNullException">Thrown when the XML element is null.</exception>
	public static T GetAttributeValue<T>(this XElement elem, XName name, T defaultValue = default)
	{
		if (elem is null)
			throw new ArgumentNullException(nameof(elem));

		var attr = elem.Attribute(name);
		return attr is null ? defaultValue : attr.Value.To<T>();
	}

	/// <summary>
	/// Writes an attribute with the specified name and value to the XmlWriter.
	/// </summary>
	/// <param name="writer">The XML writer.</param>
	/// <param name="name">The name of the attribute to write.</param>
	/// <param name="value">The value of the attribute. If null, an empty string is written.</param>
	/// <returns>The same instance of the XmlWriter to allow for method chaining.</returns>
	/// <exception cref="ArgumentNullException">Thrown when the XmlWriter is null.</exception>
	public static XmlWriter WriteAttribute(this XmlWriter writer, string name, object value)
	{
		if (writer is null)
			throw new ArgumentNullException(nameof(writer));

		writer.WriteAttributeString(name, value != null ? value.To<string>() : string.Empty);

		return writer;
	}

	/// <summary>
	/// Compares two XmlNode objects based on their OuterXml representation.
	/// </summary>
	/// <param name="first">The first XmlNode to compare.</param>
	/// <param name="second">The second XmlNode to compare.</param>
	/// <returns>True if both XmlNodes have identical OuterXml representations; otherwise, false.</returns>
	/// <exception cref="ArgumentNullException">
	/// Thrown when the first or second XmlNode is null.
	/// </exception>
	public static bool Compare(this XmlNode first, XmlNode second)
	{
		if (first is null)
			throw new ArgumentNullException(nameof(first));

		if (second is null)
			throw new ArgumentNullException(nameof(second));

		return first.OuterXml == second.OuterXml;
	}

	/// <summary>
	/// Determines whether the specified string contains only valid XML characters.
	/// </summary>
	/// <param name="value">The string to check.</param>
	/// <returns>True if all characters in the string are valid XML characters; otherwise, false.</returns>
	public static bool IsXmlString(this string value)
		=> value.All(IsXmlChar);

	/// <summary>
	/// Determines whether the specified character is a valid XML character.
	/// </summary>
	/// <param name="c">The character to check.</param>
	/// <returns>True if the character is a valid XML character; otherwise, false.</returns>
	public static bool IsXmlChar(this char c)
		=> XmlConvert.IsXmlChar(c);
}