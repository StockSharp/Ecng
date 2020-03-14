namespace Ecng.Serialization
{
	using System;
	using System.Xml;
	using System.Xml.Linq;

	using Ecng.Common;

	[Serializable]
	public class XmlFieldFactory<I> : PrimitiveFieldFactory<I, string>
	{
		public XmlFieldFactory(Field field, int order)
			: base(field, order)
		{
		}
	}

	public class XmlAttribute : ReflectionFieldFactoryAttribute
	{
		protected override Type GetFactoryType(Field field)
		{
			if (field.Type != typeof(XmlDocument) || field.Type != typeof(XDocument))
				throw new ArgumentException("field");

			return typeof(XmlFieldFactory<>).Make(field.Type);
		}
	}
}