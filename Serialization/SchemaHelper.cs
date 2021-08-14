namespace Ecng.Serialization
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Common;
	using Ecng.Collections;
	using Ecng.Reflection;

	public static class SchemaHelper
	{
		public static EntityFactory<TEntity> GetFactory<TEntity>(this Schema schema)
		{
			if (schema is null)
				throw new ArgumentNullException(nameof(schema));

			return (EntityFactory<TEntity>)schema.Factory;
		}

		public static FieldAccessor<TEntity> GetAccessor<TEntity>(this Field field)
		{
			if (field is null)
				throw new ArgumentNullException(nameof(field));

			return (FieldAccessor<TEntity>)field.Accessor;
		}

		public static bool IsInnerSchema(this Field field)
		{
			return field.MatchFactory(typeof(InnerSchemaFieldFactory<>)) || field.MatchFactory(typeof(DynamicInnerSchemaFieldFactory<>));
		}

		public static bool IsCollection(this Field field)
		{
			return field.MatchFactory(typeof(CollectionFieldFactory<>));
		}

		public static bool IsRelationMany(this Field field)
		{
			return field.MatchFactory(typeof(RelationManyFieldFactory<,>));
		}

		public static bool IsRelationSingle(this Field field)
		{
			return field.MatchFactory(typeof(RelationSingleFieldFactory<,>));
		}

		public static bool IsPersistable(this Field field)
		{
			return field.MatchFactory(typeof(PersistableFieldactory<>));
		}

		#region MatchFactory

		private static readonly Dictionary<Tuple<Field, Type>, bool> _matchFactoryCache = new();

		public static bool MatchFactory(this Field field, Type baseType)
		{
			if (field is null)
				throw new ArgumentNullException(nameof(field));

			if (baseType is null)
				throw new ArgumentNullException(nameof(baseType));

			return _matchFactoryCache.SafeAdd(new Tuple<Field, Type>(field, baseType), key => key.Item1.Factory != null && MatchFactory(key.Item1.Factory, key.Item2));
		}

		private static bool MatchFactory(this FieldFactory factory, Type baseType)
		{
			if (factory is null)
				throw new ArgumentNullException(nameof(factory));

			if (baseType is null)
				throw new ArgumentNullException(nameof(baseType));

			if (factory is FieldFactoryChain chain)
				return chain.AscFactories.Any(innerFactory => MatchFactory(innerFactory, baseType));
			else
				return factory.GetType().GetGenericType(baseType) != null;
		}

		#endregion

		public static bool IsSerializablePrimitive(this Type type)
		{
			return type.IsPrimitive() || type == typeof(Uri);
		}
	}
}