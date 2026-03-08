namespace Ecng.Serialization;

/// <summary>
/// Extension methods for database persistence operations.
/// </summary>
public static class DbPersistableHelper
{
	/// <summary>
	/// Sets a foreign key value in the settings storage.
	/// </summary>
	public static SettingsStorage SetFk(this SettingsStorage storage, string name, long? id)
	{
		storage.Set(name, id);
		return storage;
	}

	/// <summary>
	/// Asynchronously loads a foreign key entity from the database.
	/// </summary>
	public static async ValueTask<T> LoadFkAsync<T>(this SettingsStorage storage, string name, IStorage db, CancellationToken ct)
	{
		var id = storage.GetValue<long?>(name);

		if (id is null)
			return default;

		return await db.GetByIdAsync<long, T>(id.Value, ct);
	}

	/// <summary>
	/// Converts a settings storage to a serialization item collection using the specified schema columns.
	/// </summary>
	public static SerializationItemCollection ToItems(this SettingsStorage storage, IReadOnlyList<SchemaColumn> columns)
	{
		var items = new SerializationItemCollection();

		foreach (var col in columns)
		{
			if (storage.TryGetValue(col.Name, out var value))
				items.Add(new(col.Name, col.ClrType, value));
		}

		return items;
	}

	/// <summary>
	/// Converts a serialization item collection to a settings storage.
	/// </summary>
	public static SettingsStorage ToStorage(this SerializationItemCollection items)
	{
		var storage = new SettingsStorage();

		foreach (var item in items)
			storage[item.Name] = item.Value;

		return storage;
	}
}
