namespace Ecng.Serialization
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

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
				{ typeof(System.Security.SecureString), typeof(SecureStringFieldFactory) },
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
				throw new ArgumentException($"Entity type cannot be a collection. The type is '{entityType}'.", nameof(entityType));

			_schemaFactories.SafeAdd(entityType, key => factory);

			var schema = _schemas.SafeAdd(entityType, key =>
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

			foreach (var field in schema.Fields.Where(f => f.Type != typeof(object) && f.IsInnerSchema()))
			{
				var innerSchema = field.Type.GetSchema();

				foreach (var innerField in innerSchema.Fields)
				{
					if (!field.InnerSchemaIgnoreFields.Contains(innerField.Name))
						field.InnerSchemaNameOverrides.TryAdd(innerField.Name, field.Name + innerField.Name);
				}
			}

			return schema;
		}

		#endregion

		#region ValidateSchema

		private static void ValidateSchema(Schema schema)
		{
			if (schema is null)
				throw new ArgumentNullException(nameof(schema));

			if (schema.EntityType is null)
				throw new ArgumentException("Entity type is null.", nameof(schema));

			if (schema.Fields.IsEmpty() && !schema.EntityType.Is<ISerializable>())
				throw new ArgumentException($"Type '{schema.EntityType}' has no one members.");

			if (!(schema.EntityType.IsClass || schema.EntityType.IsStruct() || schema.EntityType.IsInterface))
				throw new ArgumentException($"Type '{schema.EntityType}' must be class, struct or interface.");

			if (schema.EntityType.IsClass && schema.Factory is null && !(schema.EntityType.IsAbstract || schema.EntityType.IsInterface) && schema.EntityType.GetConstructor(Type.EmptyTypes) == null)
				throw new ArgumentException($"Type '{schema.EntityType}' must have factory.");

			var names = new List<string>();

			foreach (var field in schema.Fields.SerializableFields)
			{
				if (names.Contains(field.Name))
					throw new ArgumentException($"Field '{field.Member.Name}' has duplicate name. Entity type is '{schema.EntityType}'.");

				names.Add(field.Name);

				if (field.Name.IsEmpty())
					throw new ArgumentNullException($"Field '{field.Member.Name}' can't be null or empty. Entity type is '{schema.EntityType}'.");

				if (field.Factory is null)
					throw new ArgumentException($"Field '{field.Member.Name}' must have field factory. Entity type is '{schema.EntityType}'.");

				if (field.Accessor is null)
					throw new ArgumentException($"Field '{field.Member.Name}' must have accessor. Entity type is '{schema.EntityType}'.");
			}
		}

		#endregion
	}
}