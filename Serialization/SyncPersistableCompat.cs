namespace Ecng.Serialization;

#pragma warning disable CS0618 // The obsolete contract is still honoured for types that have not moved to IAsyncPersistable.

/// <summary>
/// The single place that reads and writes objects through the obsolete <see cref="IPersistable"/>.
/// </summary>
internal static class SyncPersistableCompat
{
	/// <summary>
	/// The type implements the obsolete <see cref="IPersistable"/>.
	/// </summary>
	public static bool IsSyncPersistable(this Type type)
		=> type.Is<IPersistable>();

	/// <summary>
	/// Loads <paramref name="obj"/> through <see cref="IPersistable"/>, if it implements it.
	/// </summary>
	public static bool TryLoadSync(object obj, SettingsStorage storage)
	{
		if (obj is not IPersistable persistable)
			return false;

		persistable.Load(storage);
		return true;
	}

	/// <summary>
	/// Saves <paramref name="obj"/> through <see cref="IPersistable"/>, if it implements it.
	/// </summary>
	public static bool TrySaveSync(object obj, out SettingsStorage storage)
	{
		if (obj is not IPersistable persistable)
		{
			storage = null;
			return false;
		}

		storage = new SettingsStorage();
		persistable.Save(storage);
		return true;
	}
}
