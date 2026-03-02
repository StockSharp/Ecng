namespace Ecng.Serialization;

public static class DbPersistableHelper
{
	public static SettingsStorage SetFk(this SettingsStorage storage, string name, long? id)
	{
		storage.Set(name, id);
		return storage;
	}

	public static async ValueTask<T> LoadFkAsync<T>(this SettingsStorage storage, string name, IStorage db, CancellationToken ct)
	{
		var id = storage.GetValue<long?>(name);

		if (id is null or 0)
			return default;

		return await db.GetByIdAsync<long, T>(id.Value, ct);
	}

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

	public static SettingsStorage ToStorage(this SerializationItemCollection items)
	{
		var storage = new SettingsStorage();

		foreach (var item in items)
			storage[item.Name] = item.Value;

		return storage;
	}
}
