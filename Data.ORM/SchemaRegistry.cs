namespace Ecng.Serialization;

using System.Reflection;

using Ecng.Collections;
using Ecng.Common;
using Ecng.ComponentModel;
using Ecng.Data.Sql;

/// <summary>
/// Global registry for entity <see cref="Schema"/> metadata.
/// </summary>
public static class SchemaRegistry
{
	private static readonly SynchronizedDictionary<Type, Schema> _cache = [];
	private static readonly SynchronizedDictionary<Type, Type> _typeMappings = [];

	static SchemaRegistry()
	{
		RegisterTypeMapping(typeof(Price), typeof(string));
	}

	/// <summary>
	/// Registers a CLR type mapping for DB column storage.
	/// </summary>
	public static void RegisterTypeMapping(Type sourceType, Type dbColumnType)
	{
		ArgumentNullException.ThrowIfNull(sourceType);
		ArgumentNullException.ThrowIfNull(dbColumnType);
		_typeMappings[sourceType] = dbColumnType;
	}

	/// <summary>
	/// Registers a schema for the given entity type.
	/// </summary>
	public static void Register(Schema meta)
	{
		ArgumentNullException.ThrowIfNull(meta);
		_cache[meta.EntityType] = meta;
	}

	/// <summary>
	/// Tries to get a registered schema for the specified entity type.
	/// </summary>
	public static bool TryGet(Type entityType, out Schema meta)
		=> _cache.TryGetValue(entityType, out meta);

	/// <summary>
	/// Gets the schema for the specified entity type, creating one via reflection if not registered.
	/// </summary>
	public static Schema Get(Type entityType)
	{
		using (_cache.EnterScope())
		{
			if (_cache.TryGetValue(entityType, out var schema))
				return schema;

			return CreateFromReflection(entityType);
		}
	}

	private static Type GetMappedType(Type type)
	{
		type = type.GetUnderlyingType() ?? type;
		return _typeMappings.TryGetValue(type, out var mapped) ? mapped : null;
	}

	private static bool IsSimpleType(Type type)
	{
		type = type.GetUnderlyingType() ?? type;

		if (type == typeof(string) || type == typeof(byte[]))
			return true;

		if (type.IsEnum)
			return true;

		if (type.IsPrimitive || type == typeof(decimal) || type == typeof(DateTime)
			|| type == typeof(DateTimeOffset) || type == typeof(TimeSpan)
			|| type == typeof(Guid) || type == typeof(DateOnly) || type == typeof(TimeOnly))
			return true;

		if (_typeMappings.ContainsKey(type))
			return true;

		return false;
	}

	private static bool IsInnerSchemaType(Type type, HashSet<Type> visiting)
	{
		type = type.GetUnderlyingType() ?? type;

		if (IsSimpleType(type))
			return false;

		if (!type.IsClass && !type.IsValueType)
			return false;

		// prevent circular references within InnerSchema nesting
		if (!visiting.Add(type))
			return false;

		try
		{
			var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
			var hasProps = false;

			foreach (var prop in props)
			{
				if (prop.GetMethod is null || prop.SetMethod is null)
					continue;

				hasProps = true;

				if (IsSimpleType(prop.PropertyType))
					continue;

				if (prop.GetAttribute<RelationSingleAttribute>() is not null)
					continue;

				var innerType = prop.PropertyType.GetUnderlyingType() ?? prop.PropertyType;
				if (innerType.IsClass && IsInnerSchemaType(innerType, visiting))
					continue;

				return false;
			}

			return hasProps;
		}
		finally
		{
			visiting.Remove(type);
		}
	}

	private static Dictionary<string, string> GetNameOverrides(PropertyInfo prop)
	{
		var result = new Dictionary<string, string>();

		foreach (var attr in prop.GetAttributes<NameOverrideAttribute>())
			result[attr.OldName] = attr.NewName;

		return result;
	}

	private static string GetColumnName(string outerPropName, string innerPropName, Dictionary<string, string> nameOverrides)
	{
		if (nameOverrides.TryGetValue(innerPropName, out var colName))
			return colName;

		return outerPropName + innerPropName;
	}

	private static void FlattenInnerSchema(
		Type innerType,
		string prefix,
		Dictionary<string, string> nameOverrides,
		List<SchemaColumn> columns,
		HashSet<Type> visiting,
		bool outerNullable = false)
	{
		foreach (var prop in innerType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
		{
			if (prop.GetMethod is null || prop.SetMethod is null)
				continue;

			if (prop.GetAttribute<IgnoreAttribute>() is not null)
				continue;

			var colName = GetColumnName(prefix, prop.Name, nameOverrides);
			var colAttr = prop.GetAttribute<ColumnAttribute>();
			var isNullable = outerNullable || ResolveNullable(colAttr, prop.PropertyType);

			if (prop.GetAttribute<RelationSingleAttribute>() is not null)
			{
				columns.Add(new()
				{
					Name = colName,
					ClrType = typeof(long),
					IsNullable = isNullable,
				});
				continue;
			}

			var propType = prop.PropertyType.GetUnderlyingType() ?? prop.PropertyType;

			var mappedType = GetMappedType(prop.PropertyType);
			if (mappedType is not null)
			{
				columns.Add(new()
				{
					Name = colName,
					ClrType = mappedType,
					IsNullable = isNullable,
				});
			}
			else if (IsSimpleType(prop.PropertyType))
			{
				var clrType = propType.IsEnum
					? Enum.GetUnderlyingType(propType)
					: prop.PropertyType;

				columns.Add(new()
				{
					Name = colName,
					ClrType = clrType,
					IsNullable = isNullable,
					MaxLength = colAttr?.MaxLength ?? 0,
				});
			}
			else if ((propType.IsClass || propType.IsValueType) && IsInnerSchemaType(propType, visiting))
			{
				var innerOverrides = GetNameOverrides(prop);
				FlattenInnerSchema(propType, colName, innerOverrides, columns, visiting, isNullable);
			}
		}
	}

	private static bool ResolveNullable(ColumnAttribute colAttr, Type propType)
		=> colAttr is { IsNullableSet: true } ? colAttr.IsNullable : propType.IsNullable();

	private static Schema CreateFromReflection(Type entityType)
	{
		var entityAttr = entityType.GetAttribute<EntityAttribute>();

		// phase 1: create schema with known metadata, put into cache immediately
		// so circular dependencies just get the partially initialized schema
		var schema = new Schema
		{
			TableName = entityAttr?.Name.IsEmpty() == false ? entityAttr.Name : entityType.Name,
			NoCache = entityAttr?.NoCache ?? false,
			EntityType = entityType,
			Factory = () => entityType.CreateInstance(),
		};

		_cache[entityType] = schema;

		// phase 2: discover columns via reflection
		var columns = new List<SchemaColumn>();
		SchemaColumn identity = null;
		var visiting = new HashSet<Type> { entityType };

			foreach (var prop in entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
			{
				if (prop.GetMethod is null || prop.SetMethod is null)
					continue;

				if (prop.GetAttribute<IgnoreAttribute>() is not null)
					continue;

				if (prop.GetAttribute<RelationManyAttribute>() is not null)
					continue;

				if (prop.GetAttribute<AllColumnsFieldAttribute>() is not null)
					continue;

				if (prop.Name == "Id" || prop.GetAttribute<IdentityAttribute>() is not null)
				{
					identity = new()
					{
						Name = prop.Name,
						ClrType = prop.PropertyType,
						IsReadOnly = true,
						IsUnique = true,
						IsIndex = true,
					};
					continue;
				}

				var isRelationSingle = prop.GetAttribute<RelationSingleAttribute>() is not null;
				var colAttr = prop.GetAttribute<ColumnAttribute>();

				if (isRelationSingle)
				{
					var uniqueAttr = prop.GetAttribute<UniqueAttribute>();
					var indexAttr = prop.GetAttribute<IndexAttribute>();

					columns.Add(new()
					{
						Name = prop.Name,
						ClrType = typeof(long),
						IsUnique = uniqueAttr is not null,
						IsIndex = indexAttr is not null || uniqueAttr is not null,
						IsNullable = ResolveNullable(colAttr, prop.PropertyType),
					});
					continue;
				}

				var propType = prop.PropertyType.GetUnderlyingType() ?? prop.PropertyType;

				// type with registered DB column mapping
				var mappedType = GetMappedType(prop.PropertyType);
				if (mappedType is not null)
				{
					var uniqueAttr = prop.GetAttribute<UniqueAttribute>();
					var indexAttr = prop.GetAttribute<IndexAttribute>();

					columns.Add(new()
					{
						Name = prop.Name,
						ClrType = mappedType,
						IsUnique = uniqueAttr is not null,
						IsIndex = indexAttr is not null || uniqueAttr is not null,
						IsNullable = ResolveNullable(colAttr, prop.PropertyType),
					});
					continue;
				}

				// InnerSchema detection: class or value type without relation attributes
				if (propType != typeof(string) && propType != typeof(byte[])
					&& (propType.IsClass || propType.IsValueType)
					&& IsInnerSchemaType(propType, visiting))
				{
					var nameOverrides = GetNameOverrides(prop);
					var outerNullable = ResolveNullable(colAttr, prop.PropertyType);
					FlattenInnerSchema(propType, prop.Name, nameOverrides, columns, visiting, outerNullable);
					continue;
				}

				// simple property — skip unsupported complex types (e.g. circular references)
				if (!IsSimpleType(prop.PropertyType))
					continue;

				var clrType = propType.IsEnum
					? Enum.GetUnderlyingType(propType)
					: prop.PropertyType;

				var unique = prop.GetAttribute<UniqueAttribute>();
				var index = prop.GetAttribute<IndexAttribute>();

				columns.Add(new()
				{
					Name = prop.Name,
					ClrType = clrType,
					IsUnique = unique is not null,
					IsIndex = index is not null || unique is not null,
					IsNullable = ResolveNullable(colAttr, prop.PropertyType),
					MaxLength = colAttr?.MaxLength ?? 0,
				});
			}

			schema.SetColumnsAndIdentity(identity, columns);
		return schema;
	}
}
