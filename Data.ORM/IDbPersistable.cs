namespace Ecng.Serialization;

public interface IDbPersistable
{
	object GetIdentity();
	void SetIdentity(object id);
	void Save(SettingsStorage storage);
	ValueTask LoadAsync(SettingsStorage storage, IStorage db, CancellationToken cancellationToken);
	void InitLists(IStorage db) { }
}
