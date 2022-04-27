namespace Ecng.Serialization
{
	#region Using Directives

	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Security;
	using System.Threading;
	using System.Threading.Tasks;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Reflection;

	#endregion

	public abstract class LegacySerializer<T> : Serializer<T>, ILegacySerializer
	{
		protected LegacySerializer()
		{
			IgnoreFields = new List<string>();
		}

		protected bool IsCollection => !typeof(T).IsPrimitive() && typeof(T).IsCollection();

		public IList<string> IgnoreFields { get; }

		#region Schema

		private static readonly SyncObject _schemaLock = new();

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

					if (
						type.IsCollection() ||
						type.IsSerializablePrimitive() ||
						type == typeof(object) ||
						type == typeof(Type) ||
						type == typeof(SecureString) ||
						(type.IsNullable() && type.GetUnderlyingType().IsSerializablePrimitive()))
					{
						_schema = new Schema { EntityType = type };

						var field = new VoidField<T>((type.IsNullable() ? type.GetUnderlyingType() : type).Name);

						field.Accessor = typeof(PrimitiveFieldAccessor<T>).CreateInstance<FieldAccessor>(field);

						// NOTE:
						if (type.IsSerializablePrimitive() || type == typeof(object) || (type.IsNullable() && type.GetUnderlyingType().IsSerializablePrimitive()))
						{
							field.Factory = new PrimitiveFieldFactory<T, T>(field, 0);
							_schema.Factory = (EntityFactory)typeof(PrimitiveEntityFactory<>).Make(type).CreateInstance(field.Name);
						}
						else if (type == typeof(Type))
						{
							field.Factory = new MemberFieldFactory<Type>(field, 0, false);
							_schema.Factory = (EntityFactory)typeof(PrimitiveEntityFactory<Type>).CreateInstance(field.Name);
						}
						else if (type == typeof(SecureString))
						{
							field.Factory = SchemaManager.GlobalFieldFactories[type].CreateInstance<FieldFactory>(field, 0);
							_schema.Factory = new SecureStringEntityFactory(field.Name);
						}
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

			return new FieldList(fields);
		}

		#region Serialize

		public override ValueTask SerializeAsync(T graph, Stream stream, CancellationToken cancellationToken)
			=> Serialize(graph, GetFields(), stream, cancellationToken);

		public async ValueTask Serialize(T graph, FieldList fields, Stream stream, CancellationToken cancellationToken)
		{
			var source = new SerializationItemCollection();
			await Serialize(graph, fields, source, cancellationToken);
			await Serialize(fields, source, stream, cancellationToken);
		}

		public ValueTask Serialize(T graph, SerializationItemCollection source, CancellationToken cancellationToken)
			=> Serialize(graph, GetFields(), source, cancellationToken);

		public async ValueTask Serialize(T graph, FieldList fields, SerializationItemCollection source, CancellationToken cancellationToken)
		{
			using (new SerializationContext(graph).ToScope())
			{
				// nullable primitive can be null
				//if (graph.IsNull())
				//	throw new ArgumentNullException(nameof(graph), "Graph for type '{0}' isn't initialized.".Put(typeof(T)));

				if (fields is null)
					throw new ArgumentNullException(nameof(fields));

				if (source is null)
					throw new ArgumentNullException(nameof(source));

				var tracking = graph as ISerializationTracking;

				if (tracking != null)
					tracking.BeforeSerialize();

				if (graph is ISerializable serializable)
				{
					await serializable.Serialize(this, fields, source, cancellationToken);
					var orderedSource = source.OrderBy(item => item.Field.Name).ToArray();
					source.Clear();
					source.AddRange(orderedSource);
				}
				else
				{
					if (IsCollection)
					{
						var field = fields.First();
						source.AddRange((SerializationItemCollection)(await field.Factory.CreateSource(this, field.GetAccessor<T>().GetValue(graph), cancellationToken)).Value);
					}
					else
					{
						foreach (var field in fields)
							source.Add(await field.Factory.CreateSource(this, field.GetAccessor<T>().GetValue(graph), cancellationToken));
					}
				}

				if (tracking != null)
					tracking.AfterSerialize();
			}
		}

		public ValueTask Serialize(SerializationItemCollection source, Stream stream, CancellationToken cancellationToken)
			=> Serialize(GetFields(), source, stream, cancellationToken);

		public abstract ValueTask Serialize(FieldList fields, SerializationItemCollection source, Stream stream, CancellationToken cancellationToken);

		#endregion

		public ValueTask<object> CreateObject(SerializationItemCollection source, CancellationToken cancellationToken)
			=> Schema.Factory.CreateObject(this, source, cancellationToken);

		#region Deserialize

		public override ValueTask<T> DeserializeAsync(Stream stream, CancellationToken cancellationToken)
			=> Deserialize(stream, GetFields(), cancellationToken);

		public ValueTask Deserialize(Stream stream, SerializationItemCollection source, CancellationToken cancellationToken)
			=> Deserialize(stream, GetFields(), source, cancellationToken);

		public async ValueTask<T> Deserialize(Stream stream, FieldList fields, CancellationToken cancellationToken)
		{
			var source = new SerializationItemCollection();
			await Deserialize(stream, fields, source, cancellationToken);
			return await Deserialize(source, fields, cancellationToken);
		}

		public ValueTask<T> Deserialize(SerializationItemCollection source, CancellationToken cancellationToken)
			=> Deserialize(source, GetFields(), cancellationToken);

		public async ValueTask<T> Deserialize(SerializationItemCollection source, FieldList fields, CancellationToken cancellationToken)
		{
			var graph = (T)await CreateObject(source, cancellationToken);

			if (!Schema.Factory.FullInitialize)
				graph = await Deserialize(source, fields, graph, cancellationToken);

			return graph;
		}

		public async ValueTask<T> Deserialize(SerializationItemCollection source, FieldList fields, T graph, CancellationToken cancellationToken)
		{
			using (new SerializationContext(graph).ToScope())
			{
				if (source is null)
					throw new ArgumentNullException(nameof(source), $"Source for type '{typeof(T)}' doesn't initialized.");

				if (fields is null)
					throw new ArgumentNullException(nameof(fields));

				if (graph.IsNull())
					throw new ArgumentNullException(nameof(graph), $"Graph for type '{typeof(T)}' doesn't initialized.");

				var tracking = graph as ISerializationTracking;

				if (tracking != null)
					tracking.BeforeDeserialize();


				if (graph is ISerializable serializable)
					await serializable.Deserialize(this, fields, source, cancellationToken);
				else
				{
					foreach (var field in fields)
					{
						var item = source.TryGetItem(field.Name);

						if (item is null)
							continue;

						graph = field.GetAccessor<T>().SetValue(graph, await field.Factory.CreateInstance(this, item, default));
					}
				}

				if (tracking != null)
					tracking.AfterDeserialize();

				return graph;
			}
		}

		public abstract ValueTask Deserialize(Stream stream, FieldList fields, SerializationItemCollection source, CancellationToken cancellationToken);

		#endregion

		#region GetId

		public object GetId(T graph)
		{
			if (graph.IsNull())
				throw new ArgumentNullException(nameof(graph), $"Graph for type '{typeof(T)}' isn't initialized.");

			var identity = Schema.Identity;

			if (identity is null)
				throw new InvalidOperationException($"Schema '{typeof(T)}' doesn't provide identity.");

			return identity.GetAccessor<T>().GetValue(graph);
		}

		#endregion

		#region SetId

		public T SetId(T graph, object id)
		{
			if (graph.IsNull())
				throw new ArgumentNullException(nameof(graph), $"Graph for type '{typeof(T)}' isn't initialized.");

			if (id is null)
				throw new ArgumentNullException(nameof(id), $"Identifier for type '{typeof(T)}' isn't initialized.");

			return Schema.Identity.GetAccessor<T>().SetValue(graph, id);
		}

		#endregion
		
		#region ISerializer Members

		Schema ILegacySerializer.Schema => Schema;

		object ILegacySerializer.GetId(object graph) => GetId((T)graph);

		ValueTask ILegacySerializer.Serialize(object graph, FieldList fields, Stream stream, CancellationToken cancellationToken)
			=> Serialize((T)graph, fields, stream, cancellationToken);

		ValueTask ILegacySerializer.Serialize(object graph, SerializationItemCollection source, CancellationToken cancellationToken)
			=> Serialize((T)graph, source, cancellationToken);

		async ValueTask<object> ILegacySerializer.Deserialize(SerializationItemCollection source, CancellationToken cancellationToken)
			=> await Deserialize(source, cancellationToken);

		ValueTask ILegacySerializer.Serialize(object graph, FieldList fields, SerializationItemCollection source, CancellationToken cancellationToken)
			=> Serialize((T)graph, fields, source, cancellationToken);

		async ValueTask<object> ILegacySerializer.Deserialize(SerializationItemCollection source, FieldList fields, object graph, CancellationToken cancellationToken)
			=> await Deserialize(source, fields, (T)graph, cancellationToken);
		
		#endregion
	}
}