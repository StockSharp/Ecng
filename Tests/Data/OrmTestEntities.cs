namespace Ecng.Tests.Data;

using System.ComponentModel;

using Ecng.Data.Sql;
using Ecng.Serialization;

[Entity(Name = "Ecng_TestItem")]
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

[Entity(Name = "Ecng_TestCategory")]
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

[Entity(Name = "Ecng_TestItemCategory")]
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

[Entity(Name = "Ecng_TestPerson")]
public class TestPerson : IDbPersistable
{
	public long Id { get; set; }
	public string Name { get; set; }

	[AllColumnsField]
	public object AllColumns;

	[RelationMany(typeof(TestPersonTaskList))]
	public TestPersonTaskList Tasks { get; set; }

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

	public void InitLists(IStorage db)
	{
		Tasks = new TestPersonTaskList(db, this);
	}
}

[Entity(Name = "Ecng_TestTask")]
public class TestTask : IDbPersistable
{
	public long Id { get; set; }
	public string Title { get; set; }
	public int Priority { get; set; }

	[RelationSingle]
	public TestPerson Person { get; set; }

	object IDbPersistable.GetIdentity() => Id;
	void IDbPersistable.SetIdentity(object id) => Id = id.To<long>();

	public void Save(SettingsStorage storage)
	{
		storage
			.Set(nameof(Title), Title)
			.Set(nameof(Priority), Priority)
			.SetFk(nameof(Person), Person?.Id);
	}

	public async ValueTask LoadAsync(SettingsStorage storage, IStorage db, CancellationToken cancellationToken)
	{
		Title = storage.GetValue<string>(nameof(Title));
		Priority = storage.GetValue<int>(nameof(Priority));
		Person = await storage.LoadFkAsync<TestPerson>(nameof(Person), db, cancellationToken);
	}
}

/// <summary>
/// Sub-task of a <see cref="TestTask"/>. Used to exercise two-level navigation
/// in LINQ expressions, e.g. <c>subTasks.Where(s =&gt; s.Task.Person.Id == X)</c>.
/// </summary>
[Entity(Name = "Ecng_TestSubTask")]
public class TestSubTask : IDbPersistable
{
	public long Id { get; set; }
	public string Description { get; set; }

	[RelationSingle]
	public TestTask Task { get; set; }

	object IDbPersistable.GetIdentity() => Id;
	void IDbPersistable.SetIdentity(object id) => Id = id.To<long>();

	public void Save(SettingsStorage storage)
	{
		storage
			.Set(nameof(Description), Description)
			.SetFk(nameof(Task), Task?.Id);
	}

	public async ValueTask LoadAsync(SettingsStorage storage, IStorage db, CancellationToken cancellationToken)
	{
		Description = storage.GetValue<string>(nameof(Description));
		Task = await storage.LoadFkAsync<TestTask>(nameof(Task), db, cancellationToken);
	}
}

public class TestPersonTaskList(IStorage storage, TestPerson person) : RelationManyList<TestTask, long>(storage)
{
	private readonly TestPerson _person = person ?? throw new ArgumentNullException(nameof(person));

	public override IQueryable<TestTask> ToQueryable()
		=> base.ToQueryable().Where(t => t.Person.Id == _person.Id);

	protected override ValueTask<long> OnGetCount(bool deleted, CancellationToken cancellationToken)
		=> ToQueryable().CountAsyncEx(cancellationToken);

	protected override async ValueTask<TestTask[]> OnGetGroup(long startIndex, long count, bool deleted, string orderBy, ListSortDirection direction, CancellationToken cancellationToken)
	{
		var q = ToQueryable();

		if (startIndex > 0)
			q = q.Skip((int)startIndex);

		if (count < long.MaxValue)
			q = q.Take((int)count);

		return await q.ToArrayAsyncEx(cancellationToken);
	}

	protected override ValueTask<bool> IsSaved(TestTask item, CancellationToken cancellationToken)
		=> new(item.Id > 0);

	public override ValueTask<bool> ContainsAsync(TestTask item, CancellationToken cancellationToken)
		=> ToQueryable().Where(t => t.Id == item.Id).AnyAsyncEx(cancellationToken);
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

/// <summary>
/// View entity for testing Join with MemberInit in result selector.
/// Simulates a view where computed columns come from joined tables.
/// </summary>
public class VTestItemWithCategory : IDbPersistable
{
	public long Id { get; set; }
	public string Name { get; set; }
	public string JoinedCategoryName { get; set; }

	object IDbPersistable.GetIdentity() => Id;
	void IDbPersistable.SetIdentity(object id) => Id = id.To<long>();
	public void Save(SettingsStorage storage) { }
	public ValueTask LoadAsync(SettingsStorage storage, IStorage db, CancellationToken cancellationToken) => default;
}

/// <summary>
/// View entity for testing SelectMany (left join) with MemberInit in result selector.
/// </summary>
public class VTestItemWithOptionalCategory : IDbPersistable
{
	public long Id { get; set; }
	public string Name { get; set; }
	public string LeftJoinedDescription { get; set; }

	object IDbPersistable.GetIdentity() => Id;
	void IDbPersistable.SetIdentity(object id) => Id = id.To<long>();
	public void Save(SettingsStorage storage) { }
	public ValueTask LoadAsync(SettingsStorage storage, IStorage db, CancellationToken cancellationToken) => default;
}

/// <summary>
/// Self-referencing tree node for testing multi-level RelationMany nesting.
/// Mirrors the Client → ClientGroup → Client pattern in the web app.
/// </summary>
[Entity(Name = "Ecng_TestNode")]
public class TestNode : IDbPersistable
{
	public long Id { get; set; }
	public string Name { get; set; }

	[RelationMany(typeof(TestNodeChildList))]
	public TestNodeChildList Children { get; set; }

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

	public void InitLists(IStorage db)
	{
		Children = new TestNodeChildList(db, this);
	}
}

/// <summary>
/// Junction entity linking a parent TestNode to a child TestNode.
/// Mirrors ClientGroup (Client FK + Group FK).
/// </summary>
[Entity(Name = "Ecng_TestNodeChild")]
public class TestNodeChild : IDbPersistable
{
	public long Id { get; set; }

	[RelationSingle]
	public TestNode Parent { get; set; }

	[RelationSingle]
	public TestNode Child { get; set; }

	object IDbPersistable.GetIdentity() => Id;
	void IDbPersistable.SetIdentity(object id) => Id = id.To<long>();

	public void Save(SettingsStorage storage)
	{
		storage
			.SetFk(nameof(Parent), Parent?.Id)
			.SetFk(nameof(Child), Child?.Id);
	}

	public async ValueTask LoadAsync(SettingsStorage storage, IStorage db, CancellationToken cancellationToken)
	{
		Parent = await storage.LoadFkAsync<TestNode>(nameof(Parent), db, cancellationToken);
		Child = await storage.LoadFkAsync<TestNode>(nameof(Child), db, cancellationToken);
	}
}

/// <summary>
/// Join entity without identity column — simulates BaseJoinEntity pattern.
/// </summary>
[Entity(Name = "Ecng_TestItemTag")]
public class TestItemTag : IDbPersistable
{
	[RelationSingle]
	public TestItem Item { get; set; }

	public string Tag { get; set; }

	object IDbPersistable.GetIdentity() => null;
	void IDbPersistable.SetIdentity(object id) { }

	public void Save(SettingsStorage storage)
	{
		storage
			.SetFk(nameof(Item), Item?.Id)
			.Set(nameof(Tag), Tag);
	}

	public async ValueTask LoadAsync(SettingsStorage storage, IStorage db, CancellationToken cancellationToken)
	{
		Item = await storage.LoadFkAsync<TestItem>(nameof(Item), db, cancellationToken);
		Tag = storage.GetValue<string>(nameof(Tag));
	}
}

/// <summary>
/// Abstract base with two RelationSingle audit FKs (Created/Modified) for
/// regression tests around FK-cycle resolution and inheritance-driven joins.
/// </summary>
public abstract class TestBrokerBase : IDbPersistable
{
	public long Id { get; set; }

	[RelationSingle]
	public TestBrokerUser CreatedBy { get; set; }

	[RelationSingle]
	public TestBrokerUser ModifiedBy { get; set; }

	object IDbPersistable.GetIdentity() => Id;
	void IDbPersistable.SetIdentity(object id) => Id = id.To<long>();

	public virtual void Save(SettingsStorage storage)
	{
		storage
			.SetFk(nameof(CreatedBy), CreatedBy?.Id)
			.SetFk(nameof(ModifiedBy), ModifiedBy?.Id);
	}

	public virtual async ValueTask LoadAsync(SettingsStorage storage, IStorage db, CancellationToken ct)
	{
		CreatedBy = await storage.LoadFkAsync<TestBrokerUser>(nameof(CreatedBy), db, ct);
		ModifiedBy = await storage.LoadFkAsync<TestBrokerUser>(nameof(ModifiedBy), db, ct);
	}
}

[Entity(Name = "Ecng_TestBrokerUser")]
public partial class TestBrokerUser : TestBrokerBase
{
	public string Email { get; set; } = string.Empty;
	public string FirstName { get; set; } = string.Empty;
	public string LastName { get; set; } = string.Empty;
}

[Entity(Name = "Ecng_TestBrokerPortfolio")]
public partial class TestBrokerPortfolio : TestBrokerBase
{
	[RelationSingle]
	public TestBrokerUser User { get; set; }

	public string Name { get; set; } = string.Empty;
}

public class TestNodeChildList(IStorage storage, TestNode parent) : RelationManyList<TestNodeChild, long>(storage)
{
	private readonly TestNode _parent = parent ?? throw new ArgumentNullException(nameof(parent));

	public override IQueryable<TestNodeChild> ToQueryable()
		=> base.ToQueryable().Where(c => c.Parent.Id == _parent.Id);

	protected override ValueTask<long> OnGetCount(bool deleted, CancellationToken cancellationToken)
		=> ToQueryable().CountAsyncEx(cancellationToken);

	protected override async ValueTask<TestNodeChild[]> OnGetGroup(long startIndex, long count, bool deleted, string orderBy, ListSortDirection direction, CancellationToken cancellationToken)
	{
		var q = ToQueryable();

		if (startIndex > 0)
			q = q.Skip((int)startIndex);

		if (count < long.MaxValue)
			q = q.Take((int)count);

		return await q.ToArrayAsyncEx(cancellationToken);
	}

	protected override ValueTask<bool> IsSaved(TestNodeChild item, CancellationToken cancellationToken)
		=> new(item.Id > 0);

	public override ValueTask<bool> ContainsAsync(TestNodeChild item, CancellationToken cancellationToken)
		=> ToQueryable().Where(c => c.Id == item.Id).AnyAsyncEx(cancellationToken);
}
