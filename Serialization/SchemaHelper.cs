namespace Ecng.Serialization
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Reflection;

	public static class SchemaHelper
	{
		public static EntityFactory<TEntity> GetFactory<TEntity>(this Schema schema)
		{
			if (schema == null)
				throw new ArgumentNullException("schema");

			return (EntityFactory<TEntity>)schema.Factory;
		}

		public static FieldAccessor<TEntity> GetAccessor<TEntity>(this Field field)
		{
			if (field == null)
				throw new ArgumentNullException("field");

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

		#region MatchFactory

		private static readonly Dictionary<Tuple<Field, Type>, bool> _matchFactoryCache = new Dictionary<Tuple<Field, Type>, bool>();

		public static bool MatchFactory(this Field field, Type baseType)
		{
			if (field == null)
				throw new ArgumentNullException("field");

			if (baseType == null)
				throw new ArgumentNullException("baseType");

			return _matchFactoryCache.SafeAdd(new Tuple<Field, Type>(field, baseType), key => key.Item1.Factory != null && MatchFactory(key.Item1.Factory, key.Item2));
		}

		private static bool MatchFactory(this FieldFactory factory, Type baseType)
		{
			if (factory == null)
				throw new ArgumentNullException("factory");

			if (baseType == null)
				throw new ArgumentNullException("baseType");

			var chain = factory as FieldFactoryChain;
			if (chain != null)
				return chain.AscFactories.Any(innerFactory => MatchFactory(innerFactory, baseType));
			else
				return factory.GetType().GetGenericType(baseType) != null;
		}

		#endregion
	}
}