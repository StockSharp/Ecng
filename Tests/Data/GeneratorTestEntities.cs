#if NET10_0_OR_GREATER

namespace Ecng.Tests.Data;

using System.ComponentModel;

using Ecng.Serialization;

public abstract partial class GenTestBaseEntity : IDbPersistable
{
	[Identity]
	[ReadOnly(true)]
	public long Id { get; set; }

	object IDbPersistable.GetIdentity() => Id;
	void IDbPersistable.SetIdentity(object id) => Id = id.To<long>();
	public virtual void Save(SettingsStorage storage) { }
	public virtual ValueTask LoadAsync(SettingsStorage storage, IStorage db, CancellationToken ct) => default;
	public virtual void InitLists(IStorage db) { }
}

[Entity(Name = "Ecng_Orders", NoCache = true)]
public partial class GenTestOrderEntity : GenTestBaseEntity
{
	public string Symbol { get; set; }
	public decimal Price { get; set; }
}

[Entity(Name = "Ecng_Products")]
public partial class GenTestProductEntity : GenTestBaseEntity
{
	public string Title { get; set; }
}

public partial class GenTestPlainEntity : GenTestBaseEntity
{
	public string Value { get; set; }
}

#endif
