namespace Ecng.Serialization
{
	using System;
	using System.IO;
	using System.Text;
	using System.Xml;
	using System.Linq;
	using System.Xml.Linq;

	using Ecng.Common;
	using Ecng.Reflection;

	public interface IXmlSerializer : ISerializer
	{
		Encoding Encoding { get; set; }
		bool Indent { get; set; }

		void Serialize(SerializationItemCollection source, XElement element);
		void Serialize(object graph, XElement element);

		object Deserialize(XElement element);
	}

	public class XmlSerializer<T> : Serializer<T>, IXmlSerializer
	{
		private const string _typeAttr = "type";
		private const string _isNullAttr = "isNull";

		private XElement _element;

		public XmlSerializer()
		{
		}

		public bool Indent { get; set; } = true;

		private Encoding _encoding = new UTF8Encoding(false);

		public Encoding Encoding
		{
			get => _encoding;
			set => _encoding = value ?? throw new ArgumentNullException(nameof(value));
		}

		private static string FormatTypeName(Type type)
		{
			if (type is null)
				throw new ArgumentNullException(nameof(type));

			if (type.IsArray)
				return FormatTypeName(type.GetElementType()) + "Array";
			else
			{
				if (type.IsGenericType)
				{
					var args = type.GetGenericArguments();

					var argsStr = args
						.Select((t, index) => FormatTypeName(t) + ((index < args.Length - 1) ? "And" : string.Empty))
						.Aggregate(string.Empty, (current, s) => current + s);

					return type.Name.Substring(0, type.Name.IndexOf('`')) + "Begin" + argsStr + "End";
				}
				else
					return type.Name;
			}
		}

		public void Serialize(SerializationItemCollection source, XElement element)
		{
			_element = element ?? throw new ArgumentNullException(nameof(element));

			Serialize(source, new MemoryStream());
		}

		void IXmlSerializer.Serialize(object graph, XElement element)
		{
			Serialize((T)graph, element);
		}

		object IXmlSerializer.Deserialize(XElement element)
		{
			return Deserialize(element);
		}

		public void Serialize(T graph, XElement element)
		{
			_element = element ?? throw new ArgumentNullException(nameof(element));

			Serialize(graph, new MemoryStream());
		}

		public T Deserialize(XElement element)
		{
			_element = element ?? throw new ArgumentNullException(nameof(element));

			return Deserialize(new MemoryStream());
		}

		#region Serializer<T> Members

		public override string FileExtension => "xml";

		public override void Serialize(FieldList fields, SerializationItemCollection source, Stream stream)
		{
			if (source is null)
				throw new ArgumentNullException(nameof(source));

			if (stream is null)
				throw new ArgumentNullException(nameof(stream));

			XDocument doc = null;
			XElement rootElem;

			if (_element is null)
			{
				rootElem = new XElement(FormatTypeName(Schema.EntityType));
				doc = new XDocument(rootElem);
			}
			else
				rootElem = _element;

			var isSinglePrimitive = typeof(T).IsSerializablePrimitive() && _element is null;

			foreach (var item in source)
			{
				var itemElem = isSinglePrimitive ? rootElem : new XElement(IsCollection ? FormatTypeName(item.Field.Type) : item.Field.Name);

				if (item.Value is null)
					itemElem.Add(new XAttribute(_isNullAttr, true));

				if (item.Value != null)
				{
					if (item.Value is SerializationItemCollection items)
					{
						var serializer = (IXmlSerializer)GetSerializer(item.Field.Type);
						serializer.Serialize(items, itemElem);
					}
					else
					{
						itemElem.Value = item.Value is byte[] v ? v.Base64() : item.Value.To<string>();
					}
				}

				if (!IsCollection && !fields.Contains(item.Field.Name))
					itemElem.Add(new XAttribute(_typeAttr, item.Field.Type.GetTypeAsString(false)));

				if (!isSinglePrimitive)
					rootElem.Add(itemElem);
			}

			if (doc is null)
				return;

			using (var writer = XmlWriter.Create(stream, new XmlWriterSettings { Indent = Indent, OmitXmlDeclaration = true, Encoding = Encoding }))
				doc.Save(writer);
		}

		public override void Deserialize(Stream stream, FieldList fields, SerializationItemCollection source)
		{
			if (stream is null)
				throw new ArgumentNullException(nameof(stream));

			if (fields is null)
				throw new ArgumentNullException(nameof(fields));

			if (source is null)
				throw new ArgumentNullException(nameof(source));

			var root = _element;

			if (root is null)
			{
				using (var reader = XmlReader.Create(stream))
				{
					var doc = XDocument.Load(reader);

					if (doc.Root is null)
						throw new ArgumentException("Root element is null.", nameof(stream));

					root = doc.Root;
				}
			}

			if (typeof(T).IsSerializablePrimitive())
			{
				var field = fields.First();

				if (IsNull(root))
					source.Add(new SerializationItem(field, null));
				else
				{
					var itemValue = field.Factory.SourceType == typeof(byte[]) ? root.Value.Base64() : root.Value.To(field.Factory.SourceType);
					source.Add(new SerializationItem(field, itemValue));
				}
			}
			else
			{
				var elements = root.Elements().ToList();

				if (IsCollection)
				{
					var serializer = GetSerializer(typeof(T).GetItemType());

					for (var i = 0; i < elements.Count; i++)
					{
						object value;

						if (IsNull(elements[i]))
							value = null;
						else
						{
							var innerSource = new SerializationItemCollection();
							serializer.Deserialize(Encoding.GetBytes(elements[i].To<string>()).To<Stream>(), innerSource);
							value = serializer.Type.IsSerializablePrimitive() ? innerSource.First().Value : innerSource;
						}

						source.Add(new SerializationItem(new VoidField(i.To<string>(), serializer.Type), value));
					}
				}
				else
				{
					foreach (var element in elements)
					{
						var elemName = element.Name.ToString();

						if (fields.Contains(elemName))
						{
							var field = fields[elemName];
							//var elem = elements.First(e => e.Name == field.Name);

							object itemValue;

							if (IsNull(element))
								itemValue = null;
							else
							{
								if (field.Factory.SourceType == typeof(SerializationItemCollection))
								{
									var innerSource = new SerializationItemCollection();
									GetSerializer(field.Type).Deserialize(Encoding.GetBytes(element.To<string>()).To<Stream>(), innerSource);
									itemValue = innerSource;
								}
								else
								{
									itemValue = field.Factory.SourceType == typeof(byte[]) ? element.Value.Base64() : element.Value.To(field.Factory.SourceType);
								}
							}	

							source.Add(new SerializationItem(field, itemValue));
						}
						else
						{
							var fieldType = element.Attribute(_typeAttr) is null ? typeof(string) : element.GetAttributeValue<Type>(_typeAttr);

							object value;

							if (fieldType == typeof(byte[]))
								value = element.Value.Base64();
							else if (fieldType.IsSerializablePrimitive())
								value = element.Value.To(fieldType);
							else if (fieldType.IsRuntimeType())
								value = element.Value.To<Type>();
							else
							{
								var xmlSerializer = (IXmlSerializer)GetSerializer(fieldType);
								value = xmlSerializer.Deserialize(element);
							}

							source.Add(new SerializationItem(new VoidField(elemName, fieldType), value));
						}
					}
				}
			}
		}

		#endregion

		private static bool IsNull(XElement element)
		{
			return element.GetAttributeValue(_isNullAttr, false)
				|| element.GetAttributeValue<bool?>("hasValue") == false; // для обратной совместимости
		}

		public override ISerializer GetSerializer(Type entityType)
		{
			var serializer = (IXmlSerializer)base.GetSerializer(entityType);
			serializer.Encoding = Encoding;
			serializer.Indent = Indent;
			return serializer;
		}
	}
}