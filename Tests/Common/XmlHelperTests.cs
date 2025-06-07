namespace Ecng.Tests.Common;

using System.Xml;
using System.Xml.Linq;

[TestClass]
public class XmlHelperTests
{
	[TestMethod]
	public void ElementAttribute()
	{
		var doc = new XElement("root", new XAttribute("attr", 5), new XElement("child", "10"));
		doc.GetElementValue<int>("child").AssertEqual(10);
		doc.GetAttributeValue<int>("attr").AssertEqual(5);
		doc.GetElementValue("missing", 7).AssertEqual(7);
		doc.GetAttributeValue("miss", 3).AssertEqual(3);
	}

	[TestMethod]
	public void WriterAndCompare()
	{
		var sb = new System.Text.StringBuilder();
		using var writer = XmlWriter.Create(sb);
		writer.WriteStartElement("root");
		writer.WriteAttribute("attr", 1);
		writer.WriteEndElement();
		writer.Flush();
		var xml1 = new XmlDocument();
		xml1.LoadXml(sb.ToString());
		var xml2 = new XmlDocument();
		xml2.LoadXml(sb.ToString());
		xml1.Compare(xml2).AssertTrue();
	}

	[TestMethod]
	public void XmlString()
	{
		"abc".IsXmlString().AssertTrue();
		"\u0001".IsXmlString().AssertFalse();
	}

	[TestMethod]
	public void XmlChar()
	{
		XmlHelper.IsXmlChar('a').AssertTrue();
		XmlHelper.IsXmlChar('\u0001').AssertFalse();
	}
}