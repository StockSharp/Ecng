namespace Ecng.Serialization
{
	using System;
	using System.Collections.Generic;
	using System.Reflection;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Reflection;

	public class TypeSchemaFactory : SchemaFactory
	{
		private readonly MemberTypes _memberTypes;
		private readonly BindingFlags _flags = BindingFlags.Instance;

		#region TypeSchemaFactory.ctor()

		public TypeSchemaFactory(SearchBy searchBy, VisibleScopes scope)
		{
			SearchBy = searchBy;
			Scope = scope;

			switch (searchBy)
			{
				case SearchBy.Fields:
					_memberTypes = MemberTypes.Field;
					break;
				case SearchBy.Properties:
					_memberTypes = MemberTypes.Property;
					break;
				case SearchBy.Both:
					_memberTypes = MemberTypes.Field | MemberTypes.Property;
					break;
				default:
					throw new ArgumentException("searchBy");
			}

			switch (scope)
			{
				case VisibleScopes.Public:
					_flags = BindingFlags.Instance | BindingFlags.Public;
					break;
				case VisibleScopes.NonPublic:
					_flags = BindingFlags.Instance | BindingFlags.NonPublic;
					break;
				case VisibleScopes.Both:
					_flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
					break;
				default:
					throw new ArgumentException("scope");
			}
		}

		#endregion

		public SearchBy SearchBy { get; private set; }
		public VisibleScopes Scope { get; private set; }

		#region GetMembers

		protected virtual IEnumerable<MemberInfo> GetMembers(Type entityType)
		{
			var ignoredMemberNames = new List<string>(entityType.GetAttributes<IgnoreAttribute>().Select(attr => attr.FieldName));

			return entityType.GetMembers<MemberInfo>(_flags).Where(
			member =>
					_memberTypes.Contains(member.MemberType) &&
					(member.GetAttribute<IgnoreAttribute>() == null || !member.GetAttributes<IgnoreAttribute>().Any(a => a.FieldName.IsEmpty())) &&
					!member.IsIndexer() &&
					!member.ReflectedType.IsInterface &&
					(member is FieldInfo || (member is PropertyInfo && ((PropertyInfo)member).GetAccessors(true).Length == 2)) &&
					!ignoredMemberNames.Contains(member.Name)
			).OrderBy(member => member.Name);
		}

		#endregion

		#region SchemaFactory Members

		protected internal override Schema CreateSchema(Type entityType)
		{
			var schema = new Schema { EntityType = entityType };

			var entityAttr = entityType.GetAttribute<EntityAttribute>();
			if (entityAttr != null)
				schema.Name = entityAttr.Name;

			var factoryAttr = entityType.GetAttribute<EntityFactoryAttribute>();
			schema.Factory = factoryAttr != null ? factoryAttr.FactoryType.CreateInstance<EntityFactory>() : typeof(FastInvokerEntityFactory<>).Make(entityType).CreateInstance<EntityFactory>();

			if (schema.IsSerializable)
				return schema;

			schema.Fields.AddRange(GetMembers(entityType).Select(member =>
			{
				var field = member.GetAttribute<IdentityAttribute>() == null
					? new Field(schema, member) { IsIndex = (member.GetAttribute<IndexAttribute>() != null) }
					: new IdentityField(schema, member);

				var fieldAttr = member.GetAttribute<FieldAttribute>();
				if (fieldAttr != null)
				{
					field.Name = fieldAttr.Name;
					field.IsReadOnly = fieldAttr.ReadOnly;
					field.OrderedIndex = fieldAttr.Order;
				}

				var accessorAttr = member.GetAttribute<FieldAccessorAttribute>();
				field.Accessor = ((accessorAttr != null) ? accessorAttr.FactoryType : typeof(FastInvokerFieldAccessor<,>).Make(entityType, field.Type)).CreateInstance<FieldAccessor>(field);

				field.IsUnderlying = member.GetAttribute<UnderlyingAttribute>() != null;

				return field;
			}).OrderBy(f => f.OrderedIndex));

			foreach (var attribute in entityType.GetAttributes<IndexAttribute>())
			{
				schema.Fields[attribute.FieldName].IsIndex = true;
			}

			foreach (var f in schema.Fields)
			{
				var field = f;

				var fieldFactoryAttrs = field.Member.GetAttributes<FieldFactoryAttribute>();
				if (fieldFactoryAttrs.IsEmpty())
					fieldFactoryAttrs = field.Type.GetAttributes<FieldFactoryAttribute>();

				var fieldFactories = fieldFactoryAttrs.Select(a => a.CreateFactory(field)).ToList();

				if (fieldFactories.Count > 1)
					field.Factory = new FieldFactoryChain(fieldFactories, field);
				else if (fieldFactories.Count == 1)
					field.Factory = fieldFactories[0];

				if (field.Factory == null)
				{
					Type factoryType;

					if (!SchemaManager.CustomFieldFactories.TryGetValue(new Tuple<Type, string>(entityType, field.Name), out factoryType))
					{
						if (field.Type == typeof(object))
							factoryType = typeof(DynamicFieldFactory);
						else
						{
							factoryType = SchemaManager.GlobalFieldFactories.TryGetValue(field.Type);

							if (factoryType == null)
							{
								if (field.Type.IsPrimitive())
								{
									factoryType = field.Type.IsEnum()
												? typeof(EnumFieldFactory<,>).Make(field.Type, field.Type.GetEnumBaseType())
												: typeof(PrimitiveFieldFactory<,>).Make(field.Type, field.Type);
								}
								else
								{
									//factoryType = typeof(IPersistable).IsAssignableFrom(field.Type)
									//	? typeof(PersistableFieldactory<>)
									//	: (field.Type.IsCollection()
									//		? typeof(CollectionFieldFactory<>)
									//		: typeof(InnerSchemaFieldFactory<>));

									factoryType = field.Type.IsCollection()
										? typeof(CollectionFieldFactory<>)
										: typeof(InnerSchemaFieldFactory<>);

									factoryType = factoryType.Make(field.Type);
								}
							}
						}
					}

					field.Factory = factoryType.CreateInstance<FieldFactory>(field, 0);
				}

				if (field.IsInnerSchema())
				{
					foreach (var attr in field.Member.GetAttributes<NameOverrideAttribute>())
						field.InnerSchemaNameOverrides.Add(attr.OldName, attr.NewName);

					foreach (var attr in field.Member.GetAttributes<IgnoreAttribute>())
						field.InnerSchemaIgnoreFields.Add(attr.FieldName);
				}
			}

			return schema;
		}

		#endregion
	}

	[Serializable]
	public enum SearchBy
	{
		Fields,
		Properties,
		Both,
	}

	[Serializable]
	public enum VisibleScopes
	{
		Public,
		NonPublic,
		Both,
	}

	public class TypeSchemaFactoryAttribute : SchemaFactoryAttribute
	{
		#region TypeSchemaFactoryAttribute.ctor()

		public TypeSchemaFactoryAttribute(SearchBy searchBy, VisibleScopes scope)
		{
			SearchBy = searchBy;
			Scope = scope;
		}

		#endregion

		public SearchBy SearchBy { get; }
		public VisibleScopes Scope { get; }

		#region SchemaFactoryAttribute Members

		protected internal override SchemaFactory CreateFactory()
		{
			return new TypeSchemaFactory(SearchBy, Scope);
		}

		#endregion
	}
}