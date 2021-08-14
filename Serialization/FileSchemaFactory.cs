namespace Ecng.Serialization
{
	#region Using Directives

	using System;
	using System.IO;

	#endregion

	public class FileSchemaFactory : SchemaFactory
	{
		#region Private Fields

		private readonly Serializer<Schema> _formatter;

		#endregion

		#region FileSchemaFactory.ctor()

		public FileSchemaFactory(Serializer<Schema> formatter)
		{
			_formatter = formatter;
		}

		#endregion

		#region SchemaFactory Members

		protected internal override Schema CreateSchema(Type entityType)
		{
			using (var stream = new FileStream(entityType.Name + ".schema", FileMode.Open))
				return _formatter.Deserialize(stream);
		}

		#endregion
	}

	public abstract class FileSchemaFactoryAttribute : SchemaFactoryAttribute
	{
		//#region FormatterType

		//private Type _formatterType = typeof(BinarySerializer<Schema>);

		//public Type FormatterType
		//{
		//    get { return _formatterType; }
		//    set
		//    {
		//        if (value == null)
		//            throw new ArgumentNullException(nameof(value));

		//        if (value.GetGenericType(typeof(Serializer<Schema>)) == null)
		//            throw new ArgumentException(nameof(value));

		//        _formatterType = value;
		//    }
		//}

		//#endregion

		#region SchemaFactoryAttribute Members

		protected internal override SchemaFactory CreateFactory()
		{
			return new FileSchemaFactory(CreateSerializer());
		}

		#endregion

		protected abstract Serializer<Schema> CreateSerializer();
	}

	public class BinarySchemaFactoryAttribute : FileSchemaFactoryAttribute
	{
		protected override Serializer<Schema> CreateSerializer()
		{
			return new BinarySerializer<Schema>();
		}
	}

	public class XmlSchemaFactoryAttribute : FileSchemaFactoryAttribute
	{
		protected override Serializer<Schema> CreateSerializer()
		{
			return new XmlSerializer<Schema>();
		}
	}
}