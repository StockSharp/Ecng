namespace Ecng.Serialization;

using System.Collections.Concurrent;
using System.Reflection;

using Ecng.Common;

/// <summary>
/// Global registry for entity <see cref="Schema"/> metadata.
/// </summary>
public static class SchemaRegistry
{
	private static readonly ConcurrentDictionary<Type, Schema> _cache = [];

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
		=> _cache.GetOrAdd(entityType, CreateFromReflection);

	private static Schema CreateFromReflection(Type entityType)
	{
		var entityAttr = entityType.GetCustomAttribute<EntityAttribute>();
		var columns = new List<SchemaColumn>();
		SchemaColumn identity = null;
		var loadProps = new List<(PropertyInfo prop, bool isClass)>();

		foreach (var prop in entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
		{
			if (prop.GetMethod is null || prop.SetMethod is null)
				continue;

			if (prop.GetCustomAttribute<IgnoreAttribute>() is not null)
				continue;

			if (prop.GetCustomAttribute<RelationManyAttribute>() is not null)
				continue;

			if (prop.Name == "Id" || prop.GetCustomAttribute<IdentityAttribute>() is not null)
			{
				identity = new() { Name = prop.Name, ClrType = prop.PropertyType, IsReadOnly = true };
				loadProps.Add((prop, false));
				continue;
			}

			var clrType = prop.PropertyType;
			var isRelationSingle = prop.GetCustomAttribute<RelationSingleAttribute>() is not null;

			if (isRelationSingle)
				clrType = typeof(long);

			var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
			var isClass = targetType.IsClass && targetType != typeof(string) && targetType != typeof(byte[]);

			columns.Add(new()
			{
				Name = prop.Name,
				ClrType = clrType,
			});

			loadProps.Add((prop, isClass));
		}

		// build Load delegate (captures PropertyInfo[] once, no per-call reflection lookup)
		var propsSnapshot = loadProps.ToArray();

		return new()
		{
			TableName = entityAttr?.Name.IsEmpty() == false ? entityAttr.Name : entityType.Name,
			NoCache = entityAttr?.NoCache ?? false,
			EntityType = entityType,
			Identity = identity,
			Columns = columns,
			Factory = () => Activator.CreateInstance(entityType),
			Load = (entity, input) =>
			{
				foreach (var (prop, isClass) in propsSnapshot)
				{
					if (!input.TryGetItem(prop.Name, out var item) || item.Value is null or DBNull)
						continue;

					// skip FK/entity reference types (stored as long in DB)
					if (isClass)
						continue;

					var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
					prop.SetValue(entity, item.Value.To(targetType));
				}
			},
		};
	}
}
