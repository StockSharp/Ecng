namespace Ecng.Serialization;

public record SchemaColumn
{
	public required string Name { get; init; }
	public required Type ClrType { get; init; }
	public bool IsReadOnly { get; init; }
	public bool IsUnique { get; init; }
	public bool IsIndex { get; init; }
}

public class Schema
{
	public required string TableName { get; init; }
	public required Type EntityType { get; init; }
	public bool IsView { get; init; }
	public bool NoCache { get; init; }
	public required SchemaColumn Identity { get; init; }
	public required IReadOnlyList<SchemaColumn> Columns { get; init; }
	public Func<object> Factory { get; init; }

	/// <summary>
	/// Fallback load for non-IDbPersistable types (projection DTOs).
	/// Set by SchemaRegistry.CreateFromReflection.
	/// </summary>
	public Action<object, SerializationItemCollection> Load { get; set; }

	public string Name => TableName;

	public bool ReadOnly => NonReadOnlyColumns.Count == 0;

	private Lazy<IReadOnlyList<SchemaColumn>> _allColumns;
	public IReadOnlyList<SchemaColumn> AllColumns => (_allColumns ??= new(() =>
	{
		var list = new List<SchemaColumn>(1 + Columns.Count);
		if (Identity is not null)
			list.Add(Identity);
		list.AddRange(Columns);
		return list;
	})).Value;

	private Lazy<IReadOnlyList<SchemaColumn>> _nonReadOnlyColumns;
	public IReadOnlyList<SchemaColumn> NonReadOnlyColumns => (_nonReadOnlyColumns ??= new(() =>
		AllColumns.Where(c => !c.IsReadOnly).ToList())).Value;

	private Lazy<IReadOnlyList<SchemaColumn>> _readOnlyColumns;
	public IReadOnlyList<SchemaColumn> ReadOnlyColumns => (_readOnlyColumns ??= new(() =>
		AllColumns.Where(c => c.IsReadOnly).ToList())).Value;

	private Lazy<IReadOnlyList<SchemaColumn>> _uniqueColumns;
	public IReadOnlyList<SchemaColumn> UniqueColumns => (_uniqueColumns ??= new(() =>
		AllColumns.Where(c => c.IsUnique).ToList())).Value;

	private Lazy<IReadOnlyList<SchemaColumn>> _indexColumns;
	public IReadOnlyList<SchemaColumn> IndexColumns => (_indexColumns ??= new(() =>
		AllColumns.Where(c => c.IsIndex).ToList())).Value;

	private Lazy<Dictionary<string, SchemaColumn>> _columnsByName;
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

	public object CreateEntity() => Factory();
}
