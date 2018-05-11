namespace Ecng.Serialization
{
	using System;
	using System.IO;
	using System.Linq;

	using Ecng.Common;
	using Ecng.Reflection;

	public class BinarySerializer<T> : Serializer<T>
	{
		public override string FileExtension => "bin";

		public override void Serialize(FieldList fields, SerializationItemCollection source, Stream stream)
		{
			if (IsCollection)
				stream.Write(source.Count);

			foreach (var item in source)
			{
				if (IsCollection)
					stream.Write(item.Value != null);
				else
				{
					if (item.Value == null)
						throw new ArgumentException("source");
				}

				if (item.Value != null)
				{
					if (item.Value is SerializationItemCollection)
					{
						var serializer = GetSerializer(item.Field.Type);
						serializer.Serialize((SerializationItemCollection)item.Value, stream);
					}
					else
						stream.Write(item.Value);
				}
			}
		}

		public override void Deserialize(Stream stream, FieldList fields, SerializationItemCollection source)
		{
			if (IsCollection)
			{
				var length = stream.Read<int>();
				var serializer = GetSerializer(typeof(T).GetItemType());

				for (var i = 0; i < length; i++)
				{
					object value;

					if (stream.Read<bool>())
					{
						var innerSource = new SerializationItemCollection();
						serializer.Deserialize(stream, innerSource);
						value = serializer.Type.IsSerializablePrimitive() ? innerSource.First().Value : innerSource;
					}
					else
						value = null;

					source.Add(new SerializationItem(new VoidField(i.ToString(), serializer.Type), value));
				}
			}
			else
			{
				foreach (var field in fields)
				{
					object itemValue;

					var hasValue = stream.Read<bool>();
					if (hasValue)
					{
						if (field.Factory.SourceType == typeof(SerializationItemCollection))
						{
							var innerSource = new SerializationItemCollection();
							GetSerializer(field.Type).Deserialize(stream, innerSource);
							itemValue = innerSource;
						}
						else
							itemValue = stream.Read(field.Factory.SourceType);
					}
					else
						itemValue = null;

					source.Add(new SerializationItem(field, itemValue));
				}
			}
		}

		public override ISerializer GetSerializer(Type entityType)
		{
			return typeof(BinarySerializer<>).Make(entityType).CreateInstance<ISerializer>();
		}
	}
}