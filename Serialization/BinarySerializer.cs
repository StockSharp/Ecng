namespace Ecng.Serialization
{
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
				stream.WriteEx(source.Count);

			foreach (var item in source)
			{
				stream.WriteEx(item.Value != null);

				//if (IsCollection)
				//	stream.Write(item.Value != null);
				//else
				//{
				//	if (item.Value is null)
				//		throw new ArgumentException(nameof(source));
				//}

				if (item.Value != null)
				{
					if (item.Value is SerializationItemCollection col)
					{
						var serializer = GetSerializer(item.Field.Type);
						serializer.Serialize(col, stream);
					}
					else
						stream.WriteEx(item.Value);
				}
			}
		}

		public override void Deserialize(Stream stream, FieldList fields, SerializationItemCollection source)
		{
			if (IsCollection)
			{
				var length = stream.Read<int>();
				var serializer = GetSerializer(typeof(T).GetItemType());

				var isPrimitive = serializer.Type.IsSerializablePrimitive();

				for (var i = 0; i < length; i++)
				{
					object value;

					if (stream.Read<bool>())
					{
						if (isPrimitive)
							value = stream.Read(serializer.Type);
						else
						{
							var innerSource = new SerializationItemCollection();
							serializer.Deserialize(stream, innerSource);
							value = serializer.Type.IsSerializablePrimitive() ? innerSource.First().Value : innerSource;
						}
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
						var type = field.Factory.SourceType;

						if (type == typeof(SerializationItemCollection))
						{
							var innerSource = new SerializationItemCollection();
							GetSerializer(field.Type).Deserialize(stream, innerSource);
							itemValue = innerSource;
						}
						else
							itemValue = stream.Read(type.IsNullable() ? type.GetUnderlyingType() : type);
					}
					else
						itemValue = null;

					source.Add(new SerializationItem(field, itemValue));
				}
			}
		}
	}
}