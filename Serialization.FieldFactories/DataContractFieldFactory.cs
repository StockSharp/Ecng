namespace Ecng.Serialization
{
	using System;
	using System.IO;
	using System.Runtime.Serialization;
	using System.Runtime.Serialization.Json;

	using Ecng.Common;

	[Serializable]
	public class DataContractFieldFactory<I, TSerializer> : FieldFactory<I, Stream>
		where TSerializer : XmlObjectSerializer
	{
		public DataContractFieldFactory(Field field, int order, TSerializer serializer)
			: base(field, order)
		{
			Serializer = serializer;
		}

		public TSerializer Serializer { get; }

		protected override I OnCreateInstance(ISerializer serializer, Stream source)
		{
			return (I)Serializer.ReadObject(source);
		}

		protected override Stream OnCreateSource(ISerializer serializer, I instance)
		{
			var stream = new MemoryStream();
			Serializer.WriteObject(stream, instance);
			return stream;
		}
	}

	public abstract class DataContractAttribute : ReflectionFieldFactoryAttribute
	{
		protected override object[] GetArgs(Field field)
		{
			return new object[] { GetSerializer(field) };
		}

		protected abstract XmlObjectSerializer GetSerializer(Field field);
	}

	public class XmlContractAttribute : DataContractAttribute
	{
		protected override Type GetFactoryType(Field field)
		{
			return typeof(DataContractFieldFactory<,>).Make(field.Type, typeof(DataContractSerializer));
		}

		protected override XmlObjectSerializer GetSerializer(Field field)
		{
			return new DataContractSerializer(field.Type);
		}
	}

	public class JsonContractAttribute : DataContractAttribute
	{
		protected override Type GetFactoryType(Field field)
		{
			return typeof(DataContractFieldFactory<,>).Make(field.Type, typeof(DataContractJsonSerializer));
		}

		protected override XmlObjectSerializer GetSerializer(Field field)
		{
			return new DataContractJsonSerializer(field.Type);
		}
	}

#if !NETCOREAPP && !NETSTANDARD
	public class NetContractAttribute : DataContractAttribute
	{
		protected override Type GetFactoryType(Field field)
		{
			return typeof(DataContractFieldFactory<,>).Make(field.Type, typeof(NetDataContractSerializer));
		}

		protected override XmlObjectSerializer GetSerializer(Field field)
		{
			return new NetDataContractSerializer();
		}
	}
#endif
}