namespace Ecng.Serialization
{
	using System;
	using System.IO;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;

	using Ecng.Common;
	using Ecng.Reflection;

	[Obsolete("Use JsonSerializer<T> insted.")]
	public class BinarySerializer<T> : LegacySerializer<T>
	{
		public override string FileExtension => "bin";

		public override async ValueTask Serialize(FieldList fields, SerializationItemCollection source, Stream stream, CancellationToken cancellationToken)
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
						var serializer = this.GetLegacySerializer(item.Field.Type);
						await serializer.Serialize(col, stream, cancellationToken);
					}
					else
						stream.WriteEx(item.Value);
				}
			}
		}

		public override async ValueTask Deserialize(Stream stream, FieldList fields, SerializationItemCollection source, CancellationToken cancellationToken)
		{
			if (IsCollection)
			{
				var length = stream.Read<int>();
				var serializer = this.GetLegacySerializer(typeof(T).GetItemType());

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
							await serializer.Deserialize(stream, innerSource, cancellationToken);
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
							await this.GetLegacySerializer(field.Type).Deserialize(stream, innerSource, cancellationToken);
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