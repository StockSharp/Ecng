namespace Ecng.Serialization;

/// <summary>
/// Represents a single column in a database schema.
/// </summary>
public record SchemaColumn
{
	/// <summary>
	/// Gets the column name.
	/// </summary>
	public required string Name { get; init; }

	/// <summary>
	/// Gets the CLR type of the column value.
	/// </summary>
	public required Type ClrType { get; init; }

	/// <summary>
	/// Gets whether the column is read-only (e.g. computed or identity).
	/// </summary>
	public bool IsReadOnly { get; init; }

	/// <summary>
	/// Gets whether the column has a unique constraint.
	/// </summary>
	public bool IsUnique { get; init; }

	/// <summary>
	/// Gets whether the column is indexed.
	/// </summary>
	public bool IsIndex { get; init; }
}

/// <summary>
/// Describes the database schema for an entity type.
/// </summary>
public class Schema
{
	/// <summary>
	/// Gets the database table name.
	/// </summary>
	public required string TableName { get; init; }

	/// <summary>
	/// Gets the CLR entity type this schema describes.
	/// </summary>
	public required Type EntityType { get; init; }

	/// <summary>
	/// Gets whether this schema maps to a database view.
	/// </summary>
	public bool IsView { get; init; }

	/// <summary>
	/// Gets whether caching is disabled for this schema.
	/// </summary>
	public bool NoCache { get; init; }

	/// <summary>
	/// Gets the identity (primary key) column, or null if none.
	/// </summary>
	public required SchemaColumn Identity { get; init; }

	/// <summary>
	/// Gets the non-identity columns of the schema.
	/// </summary>
	public required IReadOnlyList<SchemaColumn> Columns { get; init; }

	/// <summary>
	/// Gets the factory delegate used to create new entity instances.
	/// </summary>
	public Func<object> Factory { get; init; }

	/// <summary>
	/// Fallback load for non-IDbPersistable types (projection DTOs).
	/// Set by SchemaRegistry.CreateFromReflection.
	/// </summary>
	public Action<object, SerializationItemCollection> Load { get; set; }

	/// <summary>
	/// Gets the schema name (alias for <see cref="TableName"/>).
	/// </summary>
	public string Name => TableName;

	/// <summary>
	/// Gets whether the schema is effectively read-only (no writable columns).
	/// </summary>
	public bool ReadOnly => NonReadOnlyColumns.Count == 0;

	private Lazy<IReadOnlyList<SchemaColumn>> _allColumns;

	/// <summary>
	/// Gets all columns including the identity column.
	/// </summary>
	public IReadOnlyList<SchemaColumn> AllColumns => (_allColumns ??= new(() =>
	{
		var list = new List<SchemaColumn>(1 + Columns.Count);
		if (Identity is not null)
			list.Add(Identity);
		list.AddRange(Columns);
		return list;
	})).Value;

	private Lazy<IReadOnlyList<SchemaColumn>> _nonReadOnlyColumns;

	/// <summary>
	/// Gets columns that are writable (not read-only).
	/// </summary>
	public IReadOnlyList<SchemaColumn> NonReadOnlyColumns => (_nonReadOnlyColumns ??= new(() =>
		AllColumns.Where(c => !c.IsReadOnly).ToList())).Value;

	private Lazy<IReadOnlyList<SchemaColumn>> _readOnlyColumns;

	/// <summary>
	/// Gets columns that are read-only.
	/// </summary>
	public IReadOnlyList<SchemaColumn> ReadOnlyColumns => (_readOnlyColumns ??= new(() =>
		AllColumns.Where(c => c.IsReadOnly).ToList())).Value;

	private Lazy<IReadOnlyList<SchemaColumn>> _uniqueColumns;

	/// <summary>
	/// Gets columns that have a unique constraint.
	/// </summary>
	public IReadOnlyList<SchemaColumn> UniqueColumns => (_uniqueColumns ??= new(() =>
		AllColumns.Where(c => c.IsUnique).ToList())).Value;

	private Lazy<IReadOnlyList<SchemaColumn>> _indexColumns;

	/// <summary>
	/// Gets columns that are indexed.
	/// </summary>
	public IReadOnlyList<SchemaColumn> IndexColumns => (_indexColumns ??= new(() =>
		AllColumns.Where(c => c.IsIndex).ToList())).Value;

	private Lazy<Dictionary<string, SchemaColumn>> _columnsByName;

	/// <summary>
	/// Tries to find a column by name (case-insensitive), returning null if not found.
	/// </summary>
	public SchemaColumn TryGetColumn(string name)
	{
		_columnsByName ??= new(() =>
		{
			var dict = new Dictionary<string, SchemaColumn>(StringComparer.OrdinalIgnoreCase);
			foreach (var c in AllColumns)
				dict[c.Name] = c;
			return dict;
		});

		return _columnsByName.Value.TryGetValue(name, out var col) ? col : null;
	}

	/// <summary>
	/// Creates a new entity instance using the configured factory.
	/// </summary>
	public object CreateEntity() => Factory();
}
