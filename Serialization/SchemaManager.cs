namespace Ecng.Serialization
{
	using System;
	using System.Collections.Generic;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Reflection;

	public static class SchemaManager
	{
		private static readonly Dictionary<Type, Schema> _schemas = new();
		private static readonly Dictionary<Type, SchemaFactory> _schemaFactories = new();

		static SchemaManager()
		{
			CustomSchemaFactories = new Dictionary<Type, SchemaFactory>();
			CustomFieldFactories = new Dictionary<Tuple<Type, string>, Type>();

			GlobalFieldFactories = new Dictionary<Type, Type>
			{
				{ typeof(System.Drawing.Color), typeof(ColorFieldFactory<System.Drawing.Color>) },
			};
		}

		public static IEnumerable<Schema> Schemas => _schemas.Values;

		public static IDictionary<Type, SchemaFactory> CustomSchemaFactories { get; }
		public static IDictionary<Tuple<Type, string>, Type> CustomFieldFactories { get; }
		public static IDictionary<Type, Type> GlobalFieldFactories { get; }

		#region GetSchema

		public static Schema GetSchema<TEntity>()
		{
			return GetSchema(typeof(TEntity));
		}

		public static Schema GetSchema(this Type entityType)
		{
			return GetSchema(entityType, _schemaFactories.SafeAdd(entityType, key =>
			{
				if (!CustomSchemaFactories.TryGetValue(entityType, out var factory))
				{
					SchemaFactoryAttribute schemaAttr;

					if (entityType.IsGenericType && entityType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
					{
						schemaAttr = new TypeSchemaFactoryAttribute(SearchBy.Fields, VisibleScopes.NonPublic);
					}
					else
					{
						schemaAttr = entityType.GetAttribute<SchemaFactoryAttribute>() ??
						             (entityType.IsAbstract
						              	? new TypeSchemaFactoryAttribute(SearchBy.Fields, VisibleScopes.NonPublic)
						              	: new TypeSchemaFactoryAttribute(SearchBy.Properties, VisibleScopes.Public));
					}

					factory = schemaAttr.CreateFactory();
				}

				return factory;
			}));
		}

		public static Schema GetSchema(this Type entityType, SchemaFactory factory)
		{
			if (entityType is null)
				throw new ArgumentNullException(nameof(entityType));

			if (factory is null)
				throw new ArgumentNullException(nameof(factory));

			if (entityType.IsNullable())
				entityType = entityType.GetUnderlyingType();

			if (entityType.IsCollection())
				throw new ArgumentException("Entity type cannot be a collection. The type is '{0}'.".Put(entityType), nameof(entityType));

			_schemaFactories.SafeAdd(entityType, key => factory);

			return _schemas.SafeAdd(entityType, key =>
			{
				var schema = factory.CreateSchema(entityType);

				_schemas.Add(entityType, schema);

				try
				{
					ValidateSchema(schema);
					return schema;
				}
				finally
				{
					_schemas.Remove(entityType);
				}
			});
		}

		#endregion

		#region ValidateSchema

		private static void ValidateSchema(Schema schema)
		{
			if (schema is null)
				throw new ArgumentNullException(nameof(schema));

			if (schema.EntityType is null)
				throw new ArgumentException("Entity type is null.", nameof(schema));

			if (schema.Fields.IsEmpty() && !typeof(ISerializable).IsAssignableFrom(schema.EntityType))
				throw new ArgumentException("Type '{0}' has no one members.".Put(schema.EntityType));

			if (!(schema.EntityType.IsClass || schema.EntityType.IsStruct() || schema.EntityType.IsInterface))
				throw new ArgumentException("Type '{0}' must be class, struct or interface.".Put(schema.EntityType));

			if (schema.EntityType.IsClass && schema.Factory is null && !(schema.EntityType.IsAbstract || schema.EntityType.IsInterface) && schema.EntityType.GetConstructor(Type.EmptyTypes) == null)
				throw new ArgumentException("Type '{0}' must have factory.".Put(schema.EntityType));

			var names = new List<string>();

			foreach (var field in schema.Fields.SerializableFields)
			{
				if (names.Contains(field.Name))
					throw new ArgumentException("Field '{0}' has duplicate name. Entity type is '{1}'.".Put(field.Member.Name, schema.EntityType));

				names.Add(field.Name);

				if (field.Name.IsEmpty())
					throw new ArgumentNullException("Field '{0}' can't be null or empty. Entity type is '{1}'.".Put(field.Member.Name, schema.EntityType));

				if (field.Factory is null)
					throw new ArgumentException("Field '{0}' must have field factory. Entity type is '{1}'.".Put(field.Member.Name, schema.EntityType));

				if (field.Accessor is null)
					throw new ArgumentException("Field '{0}' must have accessor. Entity type is '{1}'.".Put(field.Member.Name, schema.EntityType));
			}
		}

		#endregion
	}
}