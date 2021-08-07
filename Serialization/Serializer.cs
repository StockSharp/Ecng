namespace Ecng.Serialization
{
	#region Using Directives

	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Reflection;

	#endregion

	public abstract class Serializer<T> : ISerializer
	{
		protected Serializer()
		{
			IgnoreFields = new List<string>();
		}

		protected bool IsCollection => !typeof(T).IsPrimitive() && typeof(T).IsCollection();

		public IList<string> IgnoreFields { get; }

		#region Schema

		private static readonly object _schemaLock = new object();

		private static Schema _schema;

		public static Schema Schema
		{
			get
			{
				lock (_schemaLock)
				{
					if (_schema != null)
						return _schema;

					var type = typeof(T);

					if (type.IsCollection() || type.IsSerializablePrimitive() || type == typeof(object) || type == typeof(Type))
					{
						_schema = new Schema { EntityType = type };

						var field = new VoidField<T>(type.Name);

						field.Accessor = typeof(PrimitiveFieldAccessor<T>).CreateInstance<FieldAccessor>(field);

						// NOTE:
						if (type.IsSerializablePrimitive() || type == typeof(object))
						{
							field.Factory = new PrimitiveFieldFactory<T, T>(field, 0);
							_schema.Factory = (EntityFactory)typeof(PrimitiveEntityFactory<>).Make(type).CreateInstance(field.Name);
						}
						else if (type == typeof(Type))
						{
							field.Factory = new MemberFieldFactory<Type>(field, 0, false);
							_schema.Factory = (EntityFactory)typeof(PrimitiveEntityFactory<Type>).CreateInstance(field.Name);
						}
							//else if (type == typeof(object))
							//{
							//    field.Factory = new DynamicFieldFactory(field, 0, false);
							//    _schema.Factory = (EntityFactory)typeof(DynamicFieldFactory<>).Make(type).CreateInstance(field.Name);
							//}
						else
						{
							field.Factory = typeof(RealCollectionFieldFactory<,>)
								.Make(type, type.GetItemType())
								.CreateInstance<FieldFactory<T, SerializationItemCollection>>(field, 0);

							_schema.Factory = (EntityFactory)typeof(CollectionEntityFactory<,>).Make(type, type.GetItemType()).CreateInstance<object>();
						}

						_schema.Fields.Add(field);
					}
					else
						_schema = SchemaManager.GetSchema<T>();

					return _schema;
				}
			}
		}

		#endregion

		private FieldList GetFields()
		{
			IEnumerable<Field> fields = Schema.Fields.SerializableFields;

			fields = fields.Where(f => !IgnoreFields.Contains(f.Name));

			var cxt = Scope<SerializationContext>.Current;

			while (cxt != null)
			{
				if (cxt.Value.Filter != null)
					fields = cxt.Value.Filter(fields);

				cxt = cxt.Parent;
			}

			return new FieldList(fields);
		}

		#region Serialize

		public byte[] Serialize(T graph)
		{
			var stream = new MemoryStream();
			Serialize(graph, stream);
			return stream.To<byte[]>();
		}

		public void Serialize(T graph, string fileName)
		{
			File.WriteAllBytes(fileName, Serialize(graph));
		}

		public virtual void Serialize(T graph, Stream stream)
		{
			Serialize(graph, GetFields(), stream);
		}

		public void Serialize(T graph, FieldList fields, Stream stream)
		{
			var source = new SerializationItemCollection();
			Serialize(graph, fields, source);
			Serialize(fields, source, stream);
		}

		public void Serialize(T graph, SerializationItemCollection source)
		{
			Serialize(graph, GetFields(), source);
		}

		public void Serialize(T graph, FieldList fields, SerializationItemCollection source)
		{
			using (new SerializationContext { Entity = graph }.ToScope())
			{
				if (graph.IsNull())
					throw new ArgumentNullException(nameof(graph), "Graph for type '{0}' isn't initialized.".Put(typeof(T)));

				if (fields == null)
					throw new ArgumentNullException(nameof(fields));

				if (source == null)
					throw new ArgumentNullException(nameof(source));

				var tracking = graph as ISerializationTracking;

				if (tracking != null)
					tracking.BeforeSerialize();

				if (graph is ISerializable serializable)
				{
					serializable.Serialize(this, fields, source);
					var orderedSource = source.OrderBy(item => item.Field.Name).ToArray();
					source.Clear();
					source.AddRange(orderedSource);
				}
				else
				{
					if (IsCollection)
					{
						var field = fields.First();
						source.AddRange((SerializationItemCollection)field.Factory.CreateSource(this, field.GetAccessor<T>().GetValue(graph)).Value);
					}
					else
					{
						foreach (var field in fields)
							source.Add(field.Factory.CreateSource(this, field.GetAccessor<T>().GetValue(graph)));
					}
				}

				if (tracking != null)
					tracking.AfterSerialize();
			}
		}

		public void Serialize(SerializationItemCollection source, Stream stream)
		{
			Serialize(GetFields(), source, stream);
		}

		public abstract void Serialize(FieldList fields, SerializationItemCollection source, Stream stream);

		#endregion

		public object CreateObject(SerializationItemCollection source)
			=> Schema.Factory.CreateObject(this, source);

		#region Deserialize

		public T Deserialize(string fileName)
		{
			using (var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
				return Deserialize(stream);
		}

		public T Deserialize(byte[] data)
		{
			var stream = new MemoryStream(data);
			return Deserialize(stream);
		}

		public virtual T Deserialize(Stream stream)
		{
			return Deserialize(stream, GetFields());
		}

		public void Deserialize(Stream stream, SerializationItemCollection source)
		{
			Deserialize(stream, GetFields(), source);
		}

		public T Deserialize(Stream stream, FieldList fields)
		{
			var source = new SerializationItemCollection();
			Deserialize(stream, fields, source);

			return Deserialize(source, fields);
		}

		public T Deserialize(SerializationItemCollection source)
		{
			return Deserialize(source, GetFields());
		}

		public T Deserialize(SerializationItemCollection source, FieldList fields)
		{
			var graph = (T)CreateObject(source);

			if (!Schema.Factory.FullInitialize)
				graph = Deserialize(source, fields, graph);

			return graph;
		}

		public T Deserialize(SerializationItemCollection source, FieldList fields, T graph)
		{
			using (new SerializationContext { Entity = graph }.ToScope())
			{
				if (source == null)
					throw new ArgumentNullException(nameof(source), "Source for type '{0}' doesn't initialized.".Put(typeof(T)));

				if (fields == null)
					throw new ArgumentNullException(nameof(fields));

				if (graph.IsNull())
					throw new ArgumentNullException(nameof(graph), "Graph for type '{0}' doesn't initialized.".Put(typeof(T)));

				var tracking = graph as ISerializationTracking;

				if (tracking != null)
					tracking.BeforeDeserialize();


				if (graph is ISerializable serializable)
					serializable.Deserialize(this, fields, source);
				else
				{
					foreach (var field in fields)
					{
						var item = source.TryGetItem(field.Name);

						if (item == null)
							continue;

						graph = field.GetAccessor<T>().SetValue(graph, field.Factory.CreateInstance(this, item));
					}
				}

				if (tracking != null)
					tracking.AfterDeserialize();

				return graph;
			}
		}

		public abstract void Deserialize(Stream stream, FieldList fields, SerializationItemCollection source);

		#endregion

		public Serializer<TData> GetSerializer<TData>()
		{
			return (Serializer<TData>)GetSerializer(typeof(TData));
		}

		public virtual ISerializer GetSerializer(Type entityType)
			=> GetType().GetGenericTypeDefinition().Make(entityType).CreateInstance<ISerializer>();

		#region GetId

		public object GetId(T graph)
		{
			if (graph.IsNull())
				throw new ArgumentNullException(nameof(graph), "Graph for type '{0}' isn't initialized.".Put(typeof(T)));

			var identity = Schema.Identity;

			if (identity == null)
				throw new InvalidOperationException("Schema '{0}' doesn't provide identity.".Put(typeof(T)));

			return identity.GetAccessor<T>().GetValue(graph);
		}

		#endregion

		#region SetId

		public T SetId(T graph, object id)
		{
			if (graph.IsNull())
				throw new ArgumentNullException(nameof(graph), "Graph for type '{0}' isn't initialized.".Put(typeof(T)));

			if (id == null)
				throw new ArgumentNullException(nameof(id), "Identifier for type '{0}' isn't initialized.".Put(typeof(T)));

			return Schema.Identity.GetAccessor<T>().SetValue(graph, id);
		}

		#endregion

		public abstract string FileExtension { get; }
		
		#region ISerializer Members

		Schema ISerializer.Schema => Schema;

		Type ISerializer.Type => typeof(T);

		string ISerializer.FileExtension => FileExtension;

		object ISerializer.GetId(object graph)
		{
			return GetId((T)graph);
		}

		byte[] ISerializer.Serialize(object graph)
		{
			return Serialize((T)graph);
		}

		object ISerializer.Deserialize(byte[] data)
		{
			return Deserialize(data);
		}

		void ISerializer.Serialize(object graph, string fileName)
		{
			Serialize((T)graph, fileName);
		}

		object ISerializer.Deserialize(string fileName)
		{
			return Deserialize(fileName);
		}

		void ISerializer.Serialize(object graph, Stream stream)
		{
			Serialize((T)graph, stream);
		}

		object ISerializer.Deserialize(Stream stream)
		{
			return Deserialize(stream);
		}

		void ISerializer.Serialize(object graph, FieldList fields, Stream stream)
		{
			Serialize((T)graph, fields, stream);
		}

		void ISerializer.Serialize(object graph, SerializationItemCollection source)
		{
			Serialize((T)graph, source);
		}

		object ISerializer.Deserialize(SerializationItemCollection source)
		{
			return Deserialize(source);
		}

		void ISerializer.Serialize(object graph, FieldList fields, SerializationItemCollection source)
		{
			Serialize((T)graph, fields, source);
		}

		object ISerializer.Deserialize(SerializationItemCollection source, FieldList fields, object graph)
		{
			return Deserialize(source, fields, (T)graph);
		}
		
		#endregion
	}
}