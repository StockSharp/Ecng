namespace Ecng.Serialization;

/// <summary>
/// Interface for entities that can be persisted to a database.
/// </summary>
public interface IDbPersistable
{
	/// <summary>
	/// Gets the entity's primary key identity value.
	/// </summary>
	object GetIdentity();

	/// <summary>
	/// Sets the entity's primary key identity value.
	/// </summary>
	void SetIdentity(object id);

	/// <summary>
	/// Saves the entity's state to the specified settings storage.
	/// </summary>
	void Save(SettingsStorage storage);

	/// <summary>
	/// Asynchronously loads the entity's state from the specified settings storage.
	/// </summary>
	ValueTask LoadAsync(SettingsStorage storage, IStorage db, CancellationToken cancellationToken);

	/// <summary>
	/// Initializes relation-many lists for the entity.
	/// </summary>
	void InitLists(IStorage db) { }

	/// <summary>
	/// Gets the database schema for this entity type.
	/// </summary>
	Schema Schema => SchemaRegistry.Get(GetType());
}
