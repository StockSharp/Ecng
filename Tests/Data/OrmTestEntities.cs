namespace Ecng.Tests.Data;

using Ecng.Common;
using Ecng.Serialization;

public class TestItem : IDbPersistable
{
	public long Id { get; set; }
	public string Name { get; set; }
	public int Priority { get; set; }
	public decimal Price { get; set; }
	public DateTime CreatedAt { get; set; }
	public bool IsActive { get; set; }
	public int? NullableValue { get; set; }

	object IDbPersistable.GetIdentity() => Id;
	void IDbPersistable.SetIdentity(object id) => Id = id.To<long>();

	public void Save(SettingsStorage storage)
	{
		storage
			.Set(nameof(Name), Name)
			.Set(nameof(Priority), Priority)
			.Set(nameof(Price), Price)
			.Set(nameof(CreatedAt), CreatedAt)
			.Set(nameof(IsActive), IsActive)
			.Set(nameof(NullableValue), NullableValue);
	}

	public ValueTask LoadAsync(SettingsStorage storage, IStorage db, CancellationToken cancellationToken)
	{
		Name = storage.GetValue<string>(nameof(Name));
		Priority = storage.GetValue<int>(nameof(Priority));
		Price = storage.GetValue<decimal>(nameof(Price));
		CreatedAt = storage.GetValue<DateTime>(nameof(CreatedAt));
		IsActive = storage.GetValue<bool>(nameof(IsActive));
		NullableValue = storage.GetValue<int?>(nameof(NullableValue));
		return default;
	}
}

public class TestCategory : IDbPersistable
{
	public long Id { get; set; }
	public string CategoryName { get; set; }
	public string Description { get; set; }

	object IDbPersistable.GetIdentity() => Id;
	void IDbPersistable.SetIdentity(object id) => Id = id.To<long>();

	public void Save(SettingsStorage storage)
	{
		storage
			.Set(nameof(CategoryName), CategoryName)
			.Set(nameof(Description), Description);
	}

	public ValueTask LoadAsync(SettingsStorage storage, IStorage db, CancellationToken cancellationToken)
	{
		CategoryName = storage.GetValue<string>(nameof(CategoryName));
		Description = storage.GetValue<string>(nameof(Description));
		return default;
	}
}

public class TestItemCategory : IDbPersistable
{
	public long Id { get; set; }

	[RelationSingle]
	public TestItem Item { get; set; }

	[RelationSingle]
	public TestCategory Category { get; set; }

	object IDbPersistable.GetIdentity() => Id;
	void IDbPersistable.SetIdentity(object id) => Id = id.To<long>();

	public void Save(SettingsStorage storage)
	{
		storage
			.SetFk(nameof(Item), Item?.Id)
			.SetFk(nameof(Category), Category?.Id);
	}

	public async ValueTask LoadAsync(SettingsStorage storage, IStorage db, CancellationToken cancellationToken)
	{
		Item = await storage.LoadFkAsync<TestItem>(nameof(Item), db, cancellationToken);
		Category = await storage.LoadFkAsync<TestCategory>(nameof(Category), db, cancellationToken);
	}
}

public class TestItemWithIgnored : IDbPersistable
{
	public long Id { get; set; }
	public string Name { get; set; }

	[Ignore]
	public string Computed { get; set; }

	object IDbPersistable.GetIdentity() => Id;
	void IDbPersistable.SetIdentity(object id) => Id = id.To<long>();

	public void Save(SettingsStorage storage)
	{
		storage.Set(nameof(Name), Name);
	}

	public ValueTask LoadAsync(SettingsStorage storage, IStorage db, CancellationToken cancellationToken)
	{
		Name = storage.GetValue<string>(nameof(Name));
		return default;
	}
}
