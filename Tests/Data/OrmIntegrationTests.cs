#if NET10_0_OR_GREATER

namespace Ecng.Tests.Data;

using System.ComponentModel;

using Ecng.Data;
using Ecng.Data.Sql;
using Ecng.Serialization;

[TestClass]
[TestCategory("Integration")]
[TestCategory("Database")]
[DoNotParallelize]
public class OrmIntegrationTests : BaseTestClass
{
	private Database _db;

	private static readonly Lock _initLock = new();
	private static readonly HashSet<string> _initializedProviders = [];
	private static readonly List<Database> _databases = [];

	[ClassInitialize]
	public static void ClassInit(TestContext context)
	{
		DbTestHelper.RegisterAll();
	}

	[ClassCleanup]
	public static void ClassCleanup()
	{
		lock (_initLock)
		{
			foreach (var db in _databases)
				db.Dispose();

			_databases.Clear();
		}

		DbTestHelper.ClearSQLitePools();
	}

	private void SetUp(string provider)
	{
		DbTestHelper.SkipIfUnavailable(provider);

		lock (_initLock)
		{
			if (_initializedProviders.Add(provider))
			{
				DbTestHelper.EnsureTable(provider, SchemaRegistry.Get(typeof(TestItem)), autoIncrement: true);
				DbTestHelper.EnsureTable(provider, SchemaRegistry.Get(typeof(TestCategory)), autoIncrement: true);
				DbTestHelper.EnsureTable(provider, SchemaRegistry.Get(typeof(TestItemCategory)), autoIncrement: true);
				DbTestHelper.EnsureTable(provider, SchemaRegistry.Get(typeof(TestPerson)), autoIncrement: true);
				DbTestHelper.EnsureTable(provider, SchemaRegistry.Get(typeof(TestTask)), autoIncrement: true);
				DbTestHelper.EnsureTable(provider, SchemaRegistry.Get(typeof(TestNode)), autoIncrement: true);
				DbTestHelper.EnsureTable(provider, SchemaRegistry.Get(typeof(TestNodeChild)), autoIncrement: true);
			}
		}

		_db = DbTestHelper.CreateDatabase(provider);

		lock (_initLock)
			_databases.Add(_db);

		DbTestHelper.DeleteAll(provider, "Ecng_TestItemCategory");
		DbTestHelper.DeleteAll(provider, "Ecng_TestNodeChild");
		DbTestHelper.DeleteAll(provider, "Ecng_TestTask");
		DbTestHelper.DeleteAll(provider, "Ecng_TestItem");
		DbTestHelper.DeleteAll(provider, "Ecng_TestCategory");
		DbTestHelper.DeleteAll(provider, "Ecng_TestPerson");
		DbTestHelper.DeleteAll(provider, "Ecng_TestNode");

		Storage.ClearCacheAsync(CancellationToken).AsTask().Wait();
	}

	[TestCleanup]
	public void TestCleanup()
	{
		_db?.Dispose();
	}

	#region Helpers

	private IStorage Storage => _db;

	private IQueryable<T> Query<T>()
		=> new DefaultQueryable<T>(new DefaultQueryProvider<T>(_db), null);

	private async Task ClearCache()
		=> await Storage.ClearCacheAsync(CancellationToken);

	private async Task<TestItem> InsertItem(string name = "Test", int priority = 1, decimal price = 9.99m, bool isActive = true, int? nullableValue = null, DateTime? createdAt = null)
	{
		var item = new TestItem
		{
			Name = name,
			Priority = priority,
			Price = price,
			CreatedAt = createdAt ?? DateTime.UtcNow,
			IsActive = isActive,
			NullableValue = nullableValue,
		};
		return await Storage.AddAsync(item, CancellationToken);
	}

	private async Task<TestCategory> InsertCategory(string name = "Cat", string desc = "Desc")
	{
		var cat = new TestCategory
		{
			CategoryName = name,
			Description = desc,
		};
		return await Storage.AddAsync(cat, CancellationToken);
	}

	private async Task<TestPerson> InsertPerson(string name)
	{
		var person = new TestPerson { Name = name };
		return await Storage.AddAsync(person, CancellationToken);
	}

	private async Task<TestTask> InsertTask(string title, TestPerson person, int priority = 0)
	{
		var task = new TestTask
		{
			Title = title,
			Priority = priority,
			Person = person,
		};
		return await Storage.AddAsync(task, CancellationToken);
	}

	private async Task<TestItemCategory> InsertItemCategory(TestItem item, TestCategory category)
	{
		var ic = new TestItemCategory
		{
			Item = item,
			Category = category,
		};
		return await Storage.AddAsync(ic, CancellationToken);
	}

	private async Task<TestNode> InsertNode(string name)
	{
		var node = new TestNode { Name = name };
		return await Storage.AddAsync(node, CancellationToken);
	}

	private async Task<TestNodeChild> InsertNodeChild(TestNode parent, TestNode child)
	{
		var nc = new TestNodeChild { Parent = parent, Child = child };
		return await Storage.AddAsync(nc, CancellationToken);
	}

	#endregion

	#region CRUD Tests

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Crud_InsertAndReadById(string provider)
	{
		SetUp(provider);
		var item = await InsertItem("Alpha", 5, 12.50m, true, 42);
		item.Id.AssertGreater(0);

		var loaded = await Storage.GetByIdAsync<long, TestItem>(item.Id, CancellationToken);
		loaded.AssertNotNull();
		loaded.Name.AssertEqual("Alpha");
		loaded.Priority.AssertEqual(5);
		loaded.Price.AssertEqual(12.50m);
		loaded.IsActive.AssertEqual(true);
		loaded.NullableValue.AssertEqual(42);
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Crud_Update(string provider)
	{
		SetUp(provider);
		var item = await InsertItem("Original");
		item.Name = "Updated";
		item.Priority = 99;
		await Storage.UpdateAsync(item, CancellationToken);

		await ClearCache();

		var loaded = await Storage.GetByIdAsync<long, TestItem>(item.Id, CancellationToken);
		loaded.Name.AssertEqual("Updated");
		loaded.Priority.AssertEqual(99);
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Crud_Delete(string provider)
	{
		SetUp(provider);
		var item = await InsertItem("ToDelete");
		var result = await Storage.RemoveAsync(item, CancellationToken);
		result.AssertTrue();

		await ClearCache();

		var loaded = await Storage.GetByIdAsync<long, TestItem>(item.Id, CancellationToken);
		loaded.AssertNull();
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Crud_GetCount(string provider)
	{
		SetUp(provider);
		await InsertItem("A");
		await InsertItem("B");
		await InsertItem("C");

		var count = await Storage.GetCountAsync<TestItem>(CancellationToken);
		count.AssertEqual(3);
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Crud_GetGroup_Paging(string provider)
	{
		SetUp(provider);
		for (var i = 0; i < 10; i++)
			await InsertItem($"Item{i}", priority: i);

		var page = await Storage.GetGroupAsync<TestItem>(2, 3, false, "Id", ListSortDirection.Ascending, CancellationToken);
		page.Length.AssertEqual(3);
	}

	#endregion

	#region Where Tests

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Where_Equality(string provider)
	{
		SetUp(provider);
		await InsertItem("Alpha");
		await InsertItem("Beta");

		var result = await Query<TestItem>().Where(x => x.Name == "Alpha").FirstOrDefaultAsyncEx(CancellationToken);
		result.AssertNotNull();
		result.Name.AssertEqual("Alpha");
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Where_GreaterThan(string provider)
	{
		SetUp(provider);
		await InsertItem("Low", priority: 1);
		await InsertItem("Mid", priority: 5);
		await InsertItem("High", priority: 10);

		var results = await Query<TestItem>().Where(x => x.Priority > 3).ToArrayAsyncEx(CancellationToken);
		results.Length.AssertEqual(2);
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Where_Boolean(string provider)
	{
		SetUp(provider);
		await InsertItem("Active", isActive: true);
		await InsertItem("Inactive", isActive: false);

		var results = await Query<TestItem>().Where(x => x.IsActive).ToArrayAsyncEx(CancellationToken);
		results.Length.AssertEqual(1);
		results[0].Name.AssertEqual("Active");
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Where_Nullable(string provider)
	{
		SetUp(provider);
		await InsertItem("WithValue", nullableValue: 42);
		await InsertItem("NullValue");

		var results = await Query<TestItem>().Where(x => x.NullableValue != null).ToArrayAsyncEx(CancellationToken);
		results.Length.AssertEqual(1);
		results[0].Name.AssertEqual("WithValue");
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Where_Contains_IN(string provider)
	{
		SetUp(provider);
		var a = await InsertItem("A");
		var b = await InsertItem("B");
		await InsertItem("C");

		var ids = new[] { a.Id, b.Id };
		var results = await Query<TestItem>().Where(x => ids.Contains(x.Id)).ToArrayAsyncEx(CancellationToken);
		results.Length.AssertEqual(2);
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Where_Like(string provider)
	{
		SetUp(provider);
		await InsertItem("Apple");
		await InsertItem("Banana");
		await InsertItem("Avocado");

		var results = await Query<TestItem>().Where(x => x.Name.StartsWith("A")).ToArrayAsyncEx(CancellationToken);
		results.Length.AssertEqual(2);
	}

	#endregion

	#region OrderBy Tests

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task OrderBy_Asc(string provider)
	{
		SetUp(provider);
		await InsertItem("C", priority: 3);
		await InsertItem("A", priority: 1);
		await InsertItem("B", priority: 2);

		var results = await Query<TestItem>().OrderBy(x => x.Priority).ToArrayAsyncEx(CancellationToken);
		results[0].Name.AssertEqual("A");
		results[1].Name.AssertEqual("B");
		results[2].Name.AssertEqual("C");
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task OrderBy_Desc(string provider)
	{
		SetUp(provider);
		await InsertItem("C", priority: 3);
		await InsertItem("A", priority: 1);
		await InsertItem("B", priority: 2);

		var results = await Query<TestItem>().OrderByDescending(x => x.Priority).ToArrayAsyncEx(CancellationToken);
		results[0].Name.AssertEqual("C");
		results[1].Name.AssertEqual("B");
		results[2].Name.AssertEqual("A");
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task OrderBy_ThenBy(string provider)
	{
		SetUp(provider);
		await InsertItem("B2", priority: 1, price: 20);
		await InsertItem("B1", priority: 1, price: 10);
		await InsertItem("A1", priority: 0, price: 5);

		var results = await Query<TestItem>().OrderBy(x => x.Priority).ThenBy(x => x.Price).ToArrayAsyncEx(CancellationToken);
		results[0].Name.AssertEqual("A1");
		results[1].Name.AssertEqual("B1");
		results[2].Name.AssertEqual("B2");
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task OrderBy_ByName(string provider)
	{
		SetUp(provider);
		await InsertItem("Charlie", priority: 3);
		await InsertItem("Alice", priority: 1);
		await InsertItem("Bob", priority: 2);

		var results = await Query<TestItem>().OrderBy(x => x.Name).ToArrayAsyncEx(CancellationToken);
		results[0].Name.AssertEqual("Alice");
		results[1].Name.AssertEqual("Bob");
		results[2].Name.AssertEqual("Charlie");
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task OrderBy_SkipTake(string provider)
	{
		SetUp(provider);
		for (var i = 0; i < 10; i++)
			await InsertItem($"Item{i}", priority: i);

		var results = await Query<TestItem>().OrderBy(x => x.Priority).Skip(3).Take(4).ToArrayAsyncEx(CancellationToken);
		results.Length.AssertEqual(4);
		results[0].Priority.AssertEqual(3);
		results[3].Priority.AssertEqual(6);
	}

	#endregion

	#region String Tests

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task String_Contains(string provider)
	{
		SetUp(provider);
		await InsertItem("Hello World");
		await InsertItem("Goodbye");

		var results = await Query<TestItem>().Where(x => x.Name.Contains("World")).ToArrayAsyncEx(CancellationToken);
		results.Length.AssertEqual(1);
		results[0].Name.AssertEqual("Hello World");
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task String_StartsWith(string provider)
	{
		SetUp(provider);
		await InsertItem("Apple");
		await InsertItem("Banana");

		var results = await Query<TestItem>().Where(x => x.Name.StartsWith("App")).ToArrayAsyncEx(CancellationToken);
		results.Length.AssertEqual(1);
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task String_EndsWith(string provider)
	{
		SetUp(provider);
		await InsertItem("Apple");
		await InsertItem("Pineapple");
		await InsertItem("Banana");

		var results = await Query<TestItem>().Where(x => x.Name.EndsWith("ple")).ToArrayAsyncEx(CancellationToken);
		results.Length.AssertEqual(2);
	}

	#endregion

	#region Aggregation Tests

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Agg_Count(string provider)
	{
		SetUp(provider);
		await InsertItem("A");
		await InsertItem("B");

		var count = await Query<TestItem>().CountAsyncEx(CancellationToken);
		count.AssertEqual(2);
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Agg_Any(string provider)
	{
		SetUp(provider);
		var any = await Query<TestItem>().AnyAsyncEx(CancellationToken);
		any.AssertFalse();

		await InsertItem("A");
		await ClearCache();

		var any2 = await Query<TestItem>().AnyAsyncEx(CancellationToken);
		any2.AssertTrue();
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Agg_FirstOrDefault(string provider)
	{
		SetUp(provider);
		var result = await Query<TestItem>().FirstOrDefaultAsyncEx(CancellationToken);
		result.AssertNull();

		await InsertItem("A");
		await ClearCache();

		var result2 = await Query<TestItem>().FirstOrDefaultAsyncEx(CancellationToken);
		result2.AssertNotNull();
		result2.Name.AssertEqual("A");
	}

	#endregion

	#region Join Tests

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Join_RelationSingle(string provider)
	{
		SetUp(provider);
		var item = await InsertItem("Item1");
		var cat = await InsertCategory("Cat1");
		var ic = await InsertItemCategory(item, cat);

		await ClearCache();

		var loaded = await Storage.GetByIdAsync<long, TestItemCategory>(ic.Id, CancellationToken);
		loaded.AssertNotNull();
		loaded.Item.AssertNotNull();
		loaded.Item.Name.AssertEqual("Item1");
		loaded.Category.AssertNotNull();
		loaded.Category.CategoryName.AssertEqual("Cat1");
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Join_InnerJoin_LinqSyntax(string provider)
	{
		SetUp(provider);
		var item1 = await InsertItem("Joined1", priority: 10);
		var item2 = await InsertItem("Orphan", priority: 20);
		var cat = await InsertCategory("CatA");
		await InsertItemCategory(item1, cat);

		await ClearCache();

		var results = await (
			from ic in Query<TestItemCategory>()
			join i in Query<TestItem>() on ic.Item.Id equals i.Id
			select i
		).ToArrayAsyncEx(CancellationToken);

		results.Length.AssertEqual(1);
		results[0].Name.AssertEqual("Joined1");
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Join_InnerJoin_SelectFromTable(string provider)
	{
		SetUp(provider);
		var item1 = await InsertItem("Joined1", priority: 10);
		var item2 = await InsertItem("Orphan", priority: 20);
		var cat = await InsertCategory("CatA");
		await InsertItemCategory(item1, cat);

		await ClearCache();

		// SELECT [e].* always returns the FROM table entity (TestItemCategory here)
		// The join filters — only TestItemCategory rows with matching TestItem are returned
		var results = await (
			from ic in Query<TestItemCategory>()
			join i in Query<TestItem>() on ic.Item.Id equals i.Id
			select ic
		).ToArrayAsyncEx(CancellationToken);

		results.Length.AssertEqual(1);
		results[0].Item.Id.AssertEqual(item1.Id);
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Join_InnerJoin_WithWhere(string provider)
	{
		SetUp(provider);
		var item1 = await InsertItem("Active1", isActive: true);
		var item2 = await InsertItem("Inactive1", isActive: false);
		var cat = await InsertCategory("CatX");
		await InsertItemCategory(item1, cat);
		await InsertItemCategory(item2, cat);

		await ClearCache();

		var results = await (
			from ic in Query<TestItemCategory>()
			join i in Query<TestItem>() on ic.Item.Id equals i.Id
			where i.IsActive
			select i
		).ToArrayAsyncEx(CancellationToken);

		results.Length.AssertEqual(1);
		results[0].Name.AssertEqual("Active1");
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Join_InnerJoin_WithMultipleMatches(string provider)
	{
		SetUp(provider);
		var item1 = await InsertItem("Multi1");
		var item2 = await InsertItem("Multi2");
		var cat1 = await InsertCategory("CatM1");
		var cat2 = await InsertCategory("CatM2");
		await InsertItemCategory(item1, cat1);
		await InsertItemCategory(item1, cat2);
		await InsertItemCategory(item2, cat1);

		await ClearCache();

		// Join returns TestItemCategory rows matching TestItem rows — verifies 3 links exist
		var results = await (
			from ic in Query<TestItemCategory>()
			join i in Query<TestItem>() on ic.Item.Id equals i.Id
			select ic
		).ToArrayAsyncEx(CancellationToken);

		results.Length.AssertEqual(3);
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Join_InnerJoin_TwoJoins(string provider)
	{
		SetUp(provider);
		var item = await InsertItem("ItemForDoubleJoin");
		var cat = await InsertCategory("CatForDoubleJoin");
		await InsertItemCategory(item, cat);

		await ClearCache();

		var results = await (
			from ic in Query<TestItemCategory>()
			join i in Query<TestItem>() on ic.Item.Id equals i.Id
			join c in Query<TestCategory>() on ic.Category.Id equals c.Id
			where i.Name == "ItemForDoubleJoin"
			select c
		).ToArrayAsyncEx(CancellationToken);

		results.Length.AssertEqual(1);
		results[0].CategoryName.AssertEqual("CatForDoubleJoin");
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Join_LeftJoin_GroupJoin(string provider)
	{
		SetUp(provider);
		var item1 = await InsertItem("WithCat");
		var item2 = await InsertItem("NoCat");
		var cat = await InsertCategory("Cat1");
		await InsertItemCategory(item1, cat);

		await ClearCache();

		var results = await (
			from i in Query<TestItem>()
			join ic in Query<TestItemCategory>() on i.Id equals ic.Item.Id into ics
			from ic1 in ics.DefaultIfEmpty()
			select i
		).ToArrayAsyncEx(CancellationToken);

		results.Length.AssertEqual(2);
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Join_InnerJoin_ChainedJoins(string provider)
	{
		SetUp(provider);
		var item = await InsertItem("ItemForDoubleJoin");
		var cat = await InsertCategory("CatForDoubleJoin");
		await InsertItemCategory(item, cat);

		await ClearCache();

		// Chained joins using method syntax to avoid anonymous type intermediates
		var results = await Query<TestItemCategory>()
			.Join(Query<TestItem>(), ic => ic.Item.Id, i => i.Id, (ic, i) => ic)
			.Join(Query<TestCategory>(), ic => ic.Category.Id, c => c.Id, (ic, c) => ic)
			.ToArrayAsyncEx(CancellationToken);

		results.Length.AssertEqual(1);
		results[0].Item.Id.AssertEqual(item.Id);
		results[0].Category.Id.AssertEqual(cat.Id);
	}

	#endregion

	#region Compound Where Tests

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Where_And(string provider)
	{
		SetUp(provider);
		await InsertItem("Match", priority: 5, isActive: true);
		await InsertItem("NoMatch1", priority: 5, isActive: false);
		await InsertItem("NoMatch2", priority: 1, isActive: true);

		var results = await Query<TestItem>()
			.Where(x => x.Priority > 3 && x.IsActive)
			.ToArrayAsyncEx(CancellationToken);

		results.Length.AssertEqual(1);
		results[0].Name.AssertEqual("Match");
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Where_Or(string provider)
	{
		SetUp(provider);
		await InsertItem("High", priority: 10, isActive: false);
		await InsertItem("Active", priority: 1, isActive: true);
		await InsertItem("Neither", priority: 1, isActive: false);

		var results = await Query<TestItem>()
			.Where(x => x.Priority > 5 || x.IsActive)
			.ToArrayAsyncEx(CancellationToken);

		results.Length.AssertEqual(2);
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Where_ComplexAndOr(string provider)
	{
		SetUp(provider);
		await InsertItem("A", priority: 10, price: 100, isActive: true);
		await InsertItem("B", priority: 1, price: 200, isActive: false);
		await InsertItem("C", priority: 10, price: 50, isActive: false);
		await InsertItem("D", priority: 1, price: 50, isActive: false);

		// (priority > 5 AND isActive) OR (price > 150)
		var results = await Query<TestItem>()
			.Where(x => (x.Priority > 5 && x.IsActive) || x.Price > 150)
			.ToArrayAsyncEx(CancellationToken);

		results.Length.AssertEqual(2);
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Where_Chained(string provider)
	{
		SetUp(provider);
		await InsertItem("A", priority: 5, isActive: true);
		await InsertItem("B", priority: 3, isActive: true);
		await InsertItem("C", priority: 5, isActive: false);

		var results = await Query<TestItem>()
			.Where(x => x.Priority > 4)
			.Where(x => x.IsActive)
			.ToArrayAsyncEx(CancellationToken);

		results.Length.AssertEqual(1);
		results[0].Name.AssertEqual("A");
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Where_NullableEqualsNull(string provider)
	{
		SetUp(provider);
		await InsertItem("HasNull");
		await InsertItem("HasValue", nullableValue: 7);

		var results = await Query<TestItem>()
			.Where(x => x.NullableValue == null)
			.ToArrayAsyncEx(CancellationToken);

		results.Length.AssertEqual(1);
		results[0].Name.AssertEqual("HasNull");
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Where_NegatedBoolean(string provider)
	{
		SetUp(provider);
		await InsertItem("Active", isActive: true);
		await InsertItem("Inactive", isActive: false);

		var results = await Query<TestItem>()
			.Where(x => !x.IsActive)
			.ToArrayAsyncEx(CancellationToken);

		results.Length.AssertEqual(1);
		results[0].Name.AssertEqual("Inactive");
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Where_Arithmetic_Add(string provider)
	{
		SetUp(provider);
		await InsertItem("Low", priority: 3);
		await InsertItem("High", priority: 8);

		var results = await Query<TestItem>()
			.Where(x => x.Priority + 2 > 9)
			.ToArrayAsyncEx(CancellationToken);

		results.Length.AssertEqual(1);
		results[0].Name.AssertEqual("High");
	}

	#endregion

	#region DateTime Tests

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task DateTime_FilterByRange(string provider)
	{
		SetUp(provider);
		await InsertItem("Recent");

		var results = await Query<TestItem>()
			.Where(x => x.CreatedAt > DateTime.UtcNow.AddDays(-1))
			.ToArrayAsyncEx(CancellationToken);

		results.Length.AssertEqual(1);
		results[0].Name.AssertEqual("Recent");
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task DateTime_OrderByCreatedAt(string provider)
	{
		SetUp(provider);
		var baseTime = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);
		await InsertItem("First", createdAt: baseTime);
		await InsertItem("Second", createdAt: baseTime.AddHours(1));
		await InsertItem("Third", createdAt: baseTime.AddHours(2));

		var results = await Query<TestItem>()
			.OrderByDescending(x => x.CreatedAt)
			.ToArrayAsyncEx(CancellationToken);

		results.Length.AssertEqual(3);
		results[0].Name.AssertEqual("Third");
		results[2].Name.AssertEqual("First");
	}

	#endregion

	#region Advanced String Tests

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task String_ToUpper(string provider)
	{
		SetUp(provider);
		await InsertItem("hello");
		await InsertItem("WORLD");

		var results = await Query<TestItem>()
			.Where(x => x.Name.ToUpper() == "HELLO")
			.ToArrayAsyncEx(CancellationToken);

		results.Length.AssertEqual(1);
		results[0].Name.AssertEqual("hello");
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task String_ToLower(string provider)
	{
		SetUp(provider);
		await InsertItem("HELLO");
		await InsertItem("world");

		var results = await Query<TestItem>()
			.Where(x => x.Name.ToLower() == "hello")
			.ToArrayAsyncEx(CancellationToken);

		results.Length.AssertEqual(1);
		results[0].Name.AssertEqual("HELLO");
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task String_Trim(string provider)
	{
		SetUp(provider);
		await InsertItem("  padded  ");
		await InsertItem("clean");

		var results = await Query<TestItem>()
			.Where(x => x.Name.Trim() == "padded")
			.ToArrayAsyncEx(CancellationToken);

		results.Length.AssertEqual(1);
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task String_Substring(string provider)
	{
		SetUp(provider);
		await InsertItem("Hello World");
		await InsertItem("Goodbye");

		var results = await Query<TestItem>()
			.Where(x => x.Name.Substring(0, 5) == "Hello")
			.ToArrayAsyncEx(CancellationToken);

		results.Length.AssertEqual(1);
		results[0].Name.AssertEqual("Hello World");
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task String_Replace(string provider)
	{
		SetUp(provider);
		await InsertItem("foo-bar");
		await InsertItem("baz-qux");

		var results = await Query<TestItem>()
			.Where(x => x.Name.Replace("-", "_") == "foo_bar")
			.ToArrayAsyncEx(CancellationToken);

		results.Length.AssertEqual(1);
		results[0].Name.AssertEqual("foo-bar");
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task String_IsNullOrEmpty(string provider)
	{
		SetUp(provider);
		await InsertItem("NonEmpty");
		await InsertItem("");

		var results = await Query<TestItem>()
			.Where(x => !string.IsNullOrEmpty(x.Name))
			.ToArrayAsyncEx(CancellationToken);

		results.Length.AssertEqual(1);
		results[0].Name.AssertEqual("NonEmpty");
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task String_IsEmpty(string provider)
	{
		SetUp(provider);
		await InsertItem("NonEmpty");
		await InsertItem("");

		var results = await Query<TestItem>()
			.Where(x => !x.Name.IsEmpty())
			.ToArrayAsyncEx(CancellationToken);

		results.Length.AssertEqual(1);
		results[0].Name.AssertEqual("NonEmpty");
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task String_IsEmptyOrWhiteSpace(string provider)
	{
		SetUp(provider);
		await InsertItem("NonEmpty");
		await InsertItem("");

		var results = await Query<TestItem>()
			.Where(x => !x.Name.IsEmptyOrWhiteSpace())
			.ToArrayAsyncEx(CancellationToken);

		results.Length.AssertEqual(1);
		results[0].Name.AssertEqual("NonEmpty");
	}

	#endregion

	#region Coalesce and Conditional Tests

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Coalesce_NullableValue(string provider)
	{
		SetUp(provider);
		await InsertItem("WithNull");
		await InsertItem("WithValue", nullableValue: 42);

		var results = await Query<TestItem>()
			.Where(x => (x.NullableValue ?? 0) > 10)
			.ToArrayAsyncEx(CancellationToken);

		results.Length.AssertEqual(1);
		results[0].Name.AssertEqual("WithValue");
	}

	#endregion

	#region Distinct Tests

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Distinct_Values(string provider)
	{
		SetUp(provider);
		await InsertItem("A", priority: 1);
		await InsertItem("B", priority: 1);
		await InsertItem("C", priority: 2);

		var results = await Query<TestItem>()
			.Distinct()
			.ToArrayAsyncEx(CancellationToken);

		results.Length.AssertEqual(3);
	}

	#endregion

	#region Aggregation with Filter Tests

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Agg_CountWithFilter(string provider)
	{
		SetUp(provider);
		await InsertItem("Active1", isActive: true);
		await InsertItem("Active2", isActive: true);
		await InsertItem("Inactive", isActive: false);

		var count = await Query<TestItem>()
			.Where(x => x.IsActive)
			.CountAsyncEx(CancellationToken);

		count.AssertEqual(2);
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Agg_AnyWithFilter(string provider)
	{
		SetUp(provider);
		await InsertItem("Low", priority: 1);

		var anyHigh = await Query<TestItem>()
			.Where(x => x.Priority > 100)
			.AnyAsyncEx(CancellationToken);
		anyHigh.AssertFalse();

		var anyLow = await Query<TestItem>()
			.Where(x => x.Priority == 1)
			.AnyAsyncEx(CancellationToken);
		anyLow.AssertTrue();
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Agg_FirstOrDefault_WithOrderAndFilter(string provider)
	{
		SetUp(provider);
		await InsertItem("C", priority: 3);
		await InsertItem("A", priority: 1);
		await InsertItem("B", priority: 2);

		var result = await Query<TestItem>()
			.Where(x => x.Priority > 1)
			.OrderBy(x => x.Priority)
			.FirstOrDefaultAsyncEx(CancellationToken);

		result.AssertNotNull();
		result.Name.AssertEqual("B");
	}

	#endregion

	#region Complex Query Tests

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Complex_WhereOrderBySkipTake(string provider)
	{
		SetUp(provider);
		for (var i = 0; i < 20; i++)
			await InsertItem($"Item{i:D2}", priority: i, isActive: i % 2 == 0);

		var results = await Query<TestItem>()
			.Where(x => x.IsActive)
			.OrderBy(x => x.Priority)
			.Skip(2)
			.Take(3)
			.ToArrayAsyncEx(CancellationToken);

		results.Length.AssertEqual(3);
		results[0].Priority.AssertEqual(4);
		results[1].Priority.AssertEqual(6);
		results[2].Priority.AssertEqual(8);
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Complex_JoinWithOrderByAndTake(string provider)
	{
		SetUp(provider);
		var cat = await InsertCategory("Electronics");
		for (var i = 0; i < 5; i++)
		{
			var item = await InsertItem($"Product{i}", priority: i, price: (i + 1) * 10m);
			await InsertItemCategory(item, cat);
		}

		await ClearCache();

		var results = await (
			from ic in Query<TestItemCategory>()
			join i in Query<TestItem>() on ic.Item.Id equals i.Id
			where i.Price > 20
			orderby i.Price descending
			select i
		).Take(2).ToArrayAsyncEx(CancellationToken);

		results.Length.AssertEqual(2);
		results[0].Price.AssertEqual(50m);
		results[1].Price.AssertEqual(40m);
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Complex_JoinWithTake(string provider)
	{
		SetUp(provider);
		var cat = await InsertCategory("Electronics");
		for (var i = 0; i < 5; i++)
		{
			var item = await InsertItem($"Product{i}", priority: i);
			await InsertItemCategory(item, cat);
		}

		await ClearCache();

		// Join with Take — returns first 2 TestItemCategory rows that have matching TestItem
		var results = await (
			from ic in Query<TestItemCategory>()
			join i in Query<TestItem>() on ic.Item.Id equals i.Id
			select ic
		).Take(2).ToArrayAsyncEx(CancellationToken);

		results.Length.AssertEqual(2);
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Complex_MultipleStringFilters(string provider)
	{
		SetUp(provider);
		await InsertItem("Alpha Beta");
		await InsertItem("Alpha Gamma");
		await InsertItem("Delta Beta");
		await InsertItem("Delta Gamma");

		var results = await Query<TestItem>()
			.Where(x => x.Name.StartsWith("Alpha") && x.Name.Contains("Beta"))
			.ToArrayAsyncEx(CancellationToken);

		results.Length.AssertEqual(1);
		results[0].Name.AssertEqual("Alpha Beta");
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Complex_ContainsIN_WithOtherFilters(string provider)
	{
		SetUp(provider);
		var a = await InsertItem("A", priority: 10, isActive: true);
		var b = await InsertItem("B", priority: 20, isActive: false);
		var c = await InsertItem("C", priority: 30, isActive: true);

		var ids = new[] { a.Id, b.Id, c.Id };

		var results = await Query<TestItem>()
			.Where(x => ids.Contains(x.Id) && x.IsActive && x.Priority > 5)
			.OrderByDescending(x => x.Priority)
			.ToArrayAsyncEx(CancellationToken);

		results.Length.AssertEqual(2);
		results[0].Name.AssertEqual("C");
		results[1].Name.AssertEqual("A");
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Complex_JoinFilterOnBothTables(string provider)
	{
		SetUp(provider);
		var cat1 = await InsertCategory("Active Cat", "Description1");
		var cat2 = await InsertCategory("Old Cat", "Description2");
		var item1 = await InsertItem("Item1", isActive: true);
		var item2 = await InsertItem("Item2", isActive: false);
		await InsertItemCategory(item1, cat1);
		await InsertItemCategory(item2, cat2);
		await InsertItemCategory(item1, cat2);

		await ClearCache();

		var results = await (
			from ic in Query<TestItemCategory>()
			join i in Query<TestItem>() on ic.Item.Id equals i.Id
			join c in Query<TestCategory>() on ic.Category.Id equals c.Id
			where i.IsActive && c.CategoryName.StartsWith("Active")
			select ic
		).ToArrayAsyncEx(CancellationToken);

		results.Length.AssertEqual(1);
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Complex_JoinDoubleWithCounting(string provider)
	{
		SetUp(provider);
		var cat1 = await InsertCategory("Cat A");
		var cat2 = await InsertCategory("Cat B");
		var item1 = await InsertItem("Item1");
		var item2 = await InsertItem("Item2");
		await InsertItemCategory(item1, cat1);
		await InsertItemCategory(item1, cat2);
		await InsertItemCategory(item2, cat1);

		await ClearCache();

		// Chained double join — all 3 links exist with matching items and categories
		var results = await Query<TestItemCategory>()
			.Join(Query<TestItem>(), ic => ic.Item.Id, i => i.Id, (ic, i) => ic)
			.Join(Query<TestCategory>(), ic => ic.Category.Id, c => c.Id, (ic, c) => ic)
			.ToArrayAsyncEx(CancellationToken);

		results.Length.AssertEqual(3);
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Complex_DecimalComparison(string provider)
	{
		SetUp(provider);
		await InsertItem("Cheap", price: 9.99m);
		await InsertItem("Mid", price: 49.99m);
		await InsertItem("Expensive", price: 199.99m);

		var results = await Query<TestItem>()
			.Where(x => x.Price >= 10m && x.Price <= 100m)
			.ToArrayAsyncEx(CancellationToken);

		results.Length.AssertEqual(1);
		results[0].Name.AssertEqual("Mid");
	}

	#endregion

	#region Transaction Tests

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Transaction_Commit(string provider)
	{
		SetUp(provider);
		using (var tx = Storage.CreateTransaction())
		{
			await InsertItem("InTransaction");
			tx.Commit();
		}

		await ClearCache();

		var count = await Storage.GetCountAsync<TestItem>(CancellationToken);
		count.AssertEqual(1);
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	// SQLite: Microsoft.Data.Sqlite does not support System.Transactions (TransactionScope)
	public async Task Transaction_Rollback(string provider)
	{
		SetUp(provider);
		using (Storage.CreateTransaction())
		{
			await InsertItem("WillRollback");
			// No commit — TransactionScope disposes without Complete
		}

		await ClearCache();

		var count = await Storage.GetCountAsync<TestItem>(CancellationToken);
		count.AssertEqual(0);
	}

	#endregion

	#region Schema Tests

	[TestMethod]
	public void Schema_AutoCreate()
	{
		var meta = SchemaRegistry.Get(typeof(TestItem));
		meta.AssertNotNull();
		meta.EntityType.AssertEqual(typeof(TestItem));
		meta.Identity.AssertNotNull();
		meta.Identity.Name.AssertEqual("Id");
	}

	[TestMethod]
	public void Schema_Identity()
	{
		var meta = SchemaRegistry.Get(typeof(TestItem));
		meta.Identity.ClrType.AssertEqual(typeof(long));
		meta.Identity.IsReadOnly.AssertTrue();
	}

	[TestMethod]
	public void Schema_Columns()
	{
		var meta = SchemaRegistry.Get(typeof(TestItem));
		var colNames = meta.Columns.Select(c => c.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

		colNames.Contains("Name").AssertTrue();
		colNames.Contains("Priority").AssertTrue();
		colNames.Contains("Price").AssertTrue();
		colNames.Contains("CreatedAt").AssertTrue();
		colNames.Contains("IsActive").AssertTrue();
		colNames.Contains("NullableValue").AssertTrue();
	}

	[TestMethod]
	public void Schema_IgnoreAttribute()
	{
		var meta = SchemaRegistry.Get(typeof(TestItemWithIgnored));
		var colNames = meta.Columns.Select(c => c.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

		colNames.Contains("Name").AssertTrue();
		colNames.Contains("Computed").AssertFalse();
	}

	#endregion

	#region RelationMany Tests

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task RelationMany_TasksFilteredByPerson(string provider)
	{
		SetUp(provider);
		var alice = await InsertPerson("Alice");
		var bob = await InsertPerson("Bob");

		await InsertTask("Alice Task 1", alice, priority: 1);
		await InsertTask("Alice Task 2", alice, priority: 2);
		await InsertTask("Alice Task 3", alice, priority: 3);
		await InsertTask("Bob Task 1", bob, priority: 10);
		await InsertTask("Bob Task 2", bob, priority: 20);

		await ClearCache();

		var loadedAlice = await Storage.GetByIdAsync<long, TestPerson>(alice.Id, CancellationToken);
		loadedAlice.AssertNotNull();
		loadedAlice.Tasks.AssertNotNull();

		var aliceTasks = await loadedAlice.Tasks.ToQueryable().ToArrayAsyncEx(CancellationToken);
		aliceTasks.Length.AssertEqual(3);

		foreach (var t in aliceTasks)
			t.Person.Id.AssertEqual(alice.Id);

		var loadedBob = await Storage.GetByIdAsync<long, TestPerson>(bob.Id, CancellationToken);
		var bobTasks = await loadedBob.Tasks.ToQueryable().ToArrayAsyncEx(CancellationToken);
		bobTasks.Length.AssertEqual(2);

		foreach (var t in bobTasks)
			t.Person.Id.AssertEqual(bob.Id);
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task RelationMany_CountFiltered(string provider)
	{
		SetUp(provider);
		var alice = await InsertPerson("Alice");
		var bob = await InsertPerson("Bob");

		await InsertTask("A1", alice);
		await InsertTask("A2", alice);
		await InsertTask("A3", alice);
		await InsertTask("B1", bob);

		await ClearCache();

		var loadedAlice = await Storage.GetByIdAsync<long, TestPerson>(alice.Id, CancellationToken);
		var count = await loadedAlice.Tasks.CountAsync(CancellationToken);
		count.AssertEqual(3);

		var loadedBob = await Storage.GetByIdAsync<long, TestPerson>(bob.Id, CancellationToken);
		var bobCount = await loadedBob.Tasks.CountAsync(CancellationToken);
		bobCount.AssertEqual(1);
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task RelationMany_EmptyCollection(string provider)
	{
		SetUp(provider);
		var charlie = await InsertPerson("Charlie");

		await ClearCache();

		var loaded = await Storage.GetByIdAsync<long, TestPerson>(charlie.Id, CancellationToken);
		var tasks = await loaded.Tasks.ToQueryable().ToArrayAsyncEx(CancellationToken);
		tasks.Length.AssertEqual(0);

		var count = await loaded.Tasks.CountAsync(CancellationToken);
		count.AssertEqual(0);
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task RelationMany_QueryableWithFilter(string provider)
	{
		SetUp(provider);
		var alice = await InsertPerson("Alice");

		await InsertTask("Low", alice, priority: 1);
		await InsertTask("Mid", alice, priority: 5);
		await InsertTask("High", alice, priority: 10);
		await InsertTask("VeryHigh", alice, priority: 20);

		await ClearCache();

		var loaded = await Storage.GetByIdAsync<long, TestPerson>(alice.Id, CancellationToken);
		var highPriority = await loaded.Tasks.ToQueryable()
			.Where(t => t.Priority > 5)
			.ToArrayAsyncEx(CancellationToken);

		highPriority.Length.AssertEqual(2);
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task RelationMany_QueryableWithOrderBy(string provider)
	{
		SetUp(provider);
		var alice = await InsertPerson("Alice");

		await InsertTask("C", alice, priority: 30);
		await InsertTask("A", alice, priority: 10);
		await InsertTask("B", alice, priority: 20);

		await ClearCache();

		var loaded = await Storage.GetByIdAsync<long, TestPerson>(alice.Id, CancellationToken);
		var sorted = await loaded.Tasks.ToQueryable()
			.OrderBy(t => t.Priority)
			.ToArrayAsyncEx(CancellationToken);

		sorted.Length.AssertEqual(3);
		sorted[0].Title.AssertEqual("A");
		sorted[1].Title.AssertEqual("B");
		sorted[2].Title.AssertEqual("C");
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task RelationMany_AddViaList(string provider)
	{
		SetUp(provider);
		var alice = await InsertPerson("Alice");
		await InsertTask("Existing", alice);

		await ClearCache();

		var loaded = await Storage.GetByIdAsync<long, TestPerson>(alice.Id, CancellationToken);

		var newTask = new TestTask { Title = "Added", Priority = 99, Person = loaded };
		await loaded.Tasks.AddAsync(newTask, CancellationToken);

		await ClearCache();

		var reloaded = await Storage.GetByIdAsync<long, TestPerson>(alice.Id, CancellationToken);
		var tasks = await reloaded.Tasks.ToQueryable().ToArrayAsyncEx(CancellationToken);
		tasks.Length.AssertEqual(2);
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task RelationMany_RemoveViaList(string provider)
	{
		SetUp(provider);
		var alice = await InsertPerson("Alice");
		var t1 = await InsertTask("Keep", alice);
		var t2 = await InsertTask("Remove", alice);

		await ClearCache();

		var loaded = await Storage.GetByIdAsync<long, TestPerson>(alice.Id, CancellationToken);

		var taskToRemove = await Storage.GetByIdAsync<long, TestTask>(t2.Id, CancellationToken);
		var removed = await loaded.Tasks.RemoveAsync(taskToRemove, CancellationToken);
		removed.AssertTrue();

		await ClearCache();

		var reloaded = await Storage.GetByIdAsync<long, TestPerson>(alice.Id, CancellationToken);
		var tasks = await reloaded.Tasks.ToQueryable().ToArrayAsyncEx(CancellationToken);
		tasks.Length.AssertEqual(1);
		tasks[0].Title.AssertEqual("Keep");
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task RelationMany_AsyncEnumeration(string provider)
	{
		SetUp(provider);
		var alice = await InsertPerson("Alice");

		for (var i = 0; i < 5; i++)
			await InsertTask($"Task{i}", alice, priority: i);

		await ClearCache();

		var loaded = await Storage.GetByIdAsync<long, TestPerson>(alice.Id, CancellationToken);

		var titles = new List<string>();
		await foreach (var task in loaded.Tasks)
			titles.Add(task.Title);

		titles.Count.AssertEqual(5);
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task RelationMany_IsolationBetweenPersons(string provider)
	{
		SetUp(provider);
		var persons = new List<TestPerson>();

		for (var i = 0; i < 5; i++)
		{
			var p = await InsertPerson($"Person{i}");
			persons.Add(p);

			for (var j = 0; j <= i; j++)
				await InsertTask($"P{i}_T{j}", p, priority: j);
		}

		await ClearCache();

		for (var i = 0; i < 5; i++)
		{
			var loaded = await Storage.GetByIdAsync<long, TestPerson>(persons[i].Id, CancellationToken);
			var tasks = await loaded.Tasks.ToQueryable().ToArrayAsyncEx(CancellationToken);
			tasks.Length.AssertEqual(i + 1);

			foreach (var t in tasks)
				t.Person.Id.AssertEqual(persons[i].Id);
		}
	}

	#endregion

	#region Identity Map Tests

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task IdentityMap_SameIdSameReference(string provider)
	{
		SetUp(provider);
		var item = await InsertItem("Singleton");

		var load1 = await Storage.GetByIdAsync<long, TestItem>(item.Id, CancellationToken);
		var load2 = await Storage.GetByIdAsync<long, TestItem>(item.Id, CancellationToken);

		ReferenceEquals(load1, load2).AssertTrue();
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task IdentityMap_FkReturnsSameReference(string provider)
	{
		SetUp(provider);
		var person = await InsertPerson("Alice");
		var task = await InsertTask("Task1", person);

		await ClearCache();

		var loadedPerson = await Storage.GetByIdAsync<long, TestPerson>(person.Id, CancellationToken);
		var loadedTask = await Storage.GetByIdAsync<long, TestTask>(task.Id, CancellationToken);

		ReferenceEquals(loadedPerson, loadedTask.Person).AssertTrue();
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task IdentityMap_MultipleFksSameObject(string provider)
	{
		SetUp(provider);
		var person = await InsertPerson("Shared");
		await InsertTask("T1", person);
		await InsertTask("T2", person);
		await InsertTask("T3", person);

		await ClearCache();

		var tasks = await Query<TestTask>().ToArrayAsyncEx(CancellationToken);

		tasks.Length.AssertEqual(3);

		// all three tasks point to the same Person object in memory
		ReferenceEquals(tasks[0].Person, tasks[1].Person).AssertTrue();
		ReferenceEquals(tasks[1].Person, tasks[2].Person).AssertTrue();
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task IdentityMap_RelationManyReturnsCachedPerson(string provider)
	{
		SetUp(provider);
		var alice = await InsertPerson("Alice");
		await InsertTask("T1", alice);

		await ClearCache();

		var loaded = await Storage.GetByIdAsync<long, TestPerson>(alice.Id, CancellationToken);
		var tasks = await loaded.Tasks.ToQueryable().ToArrayAsyncEx(CancellationToken);

		// the Person FK on the task should be the same object we loaded
		ReferenceEquals(loaded, tasks[0].Person).AssertTrue();
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task IdentityMap_JoinSharesReferences(string provider)
	{
		SetUp(provider);
		var item = await InsertItem("Shared Item");
		var cat = await InsertCategory("Shared Cat");
		await InsertItemCategory(item, cat);
		await InsertItemCategory(item, cat);

		await ClearCache();

		var results = await Query<TestItemCategory>().ToArrayAsyncEx(CancellationToken);

		results.Length.AssertEqual(2);

		// both TestItemCategory rows reference the same TestItem object
		ReferenceEquals(results[0].Item, results[1].Item).AssertTrue();

		// both reference the same TestCategory object
		ReferenceEquals(results[0].Category, results[1].Category).AssertTrue();
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task IdentityMap_ClearCacheCreatesNewInstance(string provider)
	{
		SetUp(provider);
		var item = await InsertItem("Before");

		var load1 = await Storage.GetByIdAsync<long, TestItem>(item.Id, CancellationToken);

		await ClearCache();

		var load2 = await Storage.GetByIdAsync<long, TestItem>(item.Id, CancellationToken);

		// after cache clear, a different object is returned
		ReferenceEquals(load1, load2).AssertFalse();

		// but data is the same
		load1.Id.AssertEqual(load2.Id);
		load1.Name.AssertEqual(load2.Name);
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task IdentityMap_MutationVisibleWithoutReload(string provider)
	{
		SetUp(provider);
		var item = await InsertItem("Original");

		var load1 = await Storage.GetByIdAsync<long, TestItem>(item.Id, CancellationToken);
		load1.Name = "Mutated";

		// second load without cache clear — same object, sees mutation
		var load2 = await Storage.GetByIdAsync<long, TestItem>(item.Id, CancellationToken);
		load2.Name.AssertEqual("Mutated");
		ReferenceEquals(load1, load2).AssertTrue();
	}

	#endregion

	#region Nested RelationMany Tests

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task RelationMany_NestedTreeTraversal(string provider)
	{
		// Creates a 5-level deep tree: Node1 -> Node2 -> Node3 -> Node4 -> Node5
		// Then traverses it level by level via RelationMany Children + Child FK

		SetUp(provider);

		var node1 = await InsertNode("Level1");
		var node2 = await InsertNode("Level2");
		var node3 = await InsertNode("Level3");
		var node4 = await InsertNode("Level4");
		var node5 = await InsertNode("Level5");

		// Create links: each node points to the next as a child
		await InsertNodeChild(node1, node2);
		await InsertNodeChild(node2, node3);
		await InsertNodeChild(node3, node4);
		await InsertNodeChild(node4, node5);

		await ClearCache();

		// Load root and traverse the tree through RelationMany + FK
		var root = await Storage.GetByIdAsync<long, TestNode>(node1.Id, CancellationToken);
		root.AssertNotNull();
		root.Name.AssertEqual("Level1");
		root.Children.AssertNotNull();

		var currentNode = root;
		var expectedNames = new[] { "Level2", "Level3", "Level4", "Level5" };

		for (var level = 0; level < expectedNames.Length; level++)
		{
			var children = await currentNode.Children.ToQueryable().ToArrayAsyncEx(CancellationToken);
			children.Length.AssertEqual(1, $"Level {level + 1} should have exactly 1 child");

			var link = children[0];
			link.Child.AssertNotNull($"Child FK is null at level {level + 2} ({expectedNames[level]})");
			link.Child.Name.AssertNotNull($"Child.Name is null at level {level + 2}");
			link.Child.Name.AssertEqual(expectedNames[level]);

			// Check that Children list is initialized on the child node
			link.Child.Children.AssertNotNull($"Child.Children (RelationManyList) is null at level {level + 2}");

			currentNode = link.Child;
		}

		// Leaf node should have no children
		var leafChildren = await currentNode.Children.ToQueryable().ToArrayAsyncEx(CancellationToken);
		leafChildren.Length.AssertEqual(0);
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task RelationMany_NestedTreeWithMultipleChildren(string provider)
	{
		// Tests wider tree: Node1 has 3 children, each has 2 children
		SetUp(provider);

		var root = await InsertNode("Root");
		var a = await InsertNode("A");
		var b = await InsertNode("B");
		var c = await InsertNode("C");
		var a1 = await InsertNode("A1");
		var a2 = await InsertNode("A2");
		var b1 = await InsertNode("B1");
		var b2 = await InsertNode("B2");
		var c1 = await InsertNode("C1");
		var c2 = await InsertNode("C2");

		await InsertNodeChild(root, a);
		await InsertNodeChild(root, b);
		await InsertNodeChild(root, c);
		await InsertNodeChild(a, a1);
		await InsertNodeChild(a, a2);
		await InsertNodeChild(b, b1);
		await InsertNodeChild(b, b2);
		await InsertNodeChild(c, c1);
		await InsertNodeChild(c, c2);

		await ClearCache();

		var loaded = await Storage.GetByIdAsync<long, TestNode>(root.Id, CancellationToken);
		var rootChildren = await loaded.Children.ToQueryable().ToArrayAsyncEx(CancellationToken);
		rootChildren.Length.AssertEqual(3);

		var totalGrandchildren = 0;

		foreach (var link in rootChildren)
		{
			link.Child.AssertNotNull($"Child FK null for {link.Id}");
			link.Child.Children.AssertNotNull($"Child.Children null for {link.Child.Name}");

			var grandchildren = await link.Child.Children.ToQueryable().ToArrayAsyncEx(CancellationToken);
			grandchildren.Length.AssertEqual(2, $"{link.Child.Name} should have 2 children");

			foreach (var gc in grandchildren)
			{
				gc.Child.AssertNotNull($"Grandchild FK null for {gc.Id}");
				gc.Child.Name.AssertNotNull($"Grandchild name null for {gc.Id}");
				totalGrandchildren++;
			}
		}

		totalGrandchildren.AssertEqual(6);
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task RelationMany_NestedAsyncForeach(string provider)
	{
		// Uses await foreach (like web code's HasAccess pattern) instead of ToQueryable
		SetUp(provider);

		var node1 = await InsertNode("N1");
		var node2 = await InsertNode("N2");
		var node3 = await InsertNode("N3");
		var node4 = await InsertNode("N4");

		await InsertNodeChild(node1, node2);
		await InsertNodeChild(node2, node3);
		await InsertNodeChild(node3, node4);

		await ClearCache();

		var root = await Storage.GetByIdAsync<long, TestNode>(node1.Id, CancellationToken);
		root.AssertNotNull();

		// Traverse using await foreach — exactly like the web code
		var depth = 0;
		var currentNode = root;

		while (true)
		{
			var foundChild = (TestNode)null;

			await foreach (var link in currentNode.Children.WithCancellation(CancellationToken))
			{
				link.Child.AssertNotNull($"Child FK is null at depth {depth}");
				link.Child.Name.AssertNotNull($"Child.Name is null at depth {depth}");
				link.Child.Children.AssertNotNull($"Child.Children list is null at depth {depth}");
				foundChild = link.Child;
			}

			if (foundChild is null)
				break;

			currentNode = foundChild;
			depth++;
		}

		depth.AssertEqual(3); // N1->N2->N3->N4 (3 hops)
		currentNode.Name.AssertEqual("N4");
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task RelationMany_NestedRecursiveHasAccess(string provider)
	{
		// Recursive traversal mimicking HasAccess pattern from web code
		// Node1 is "root org", Node2-4 are "child orgs"
		// Check if Node1 "has access" to Node4 via nested groups
		SetUp(provider);

		var org1 = await InsertNode("Org1");
		var org2 = await InsertNode("Org2");
		var org3 = await InsertNode("Org3");
		var org4 = await InsertNode("Org4");
		var org5 = await InsertNode("Org5");

		await InsertNodeChild(org1, org2);
		await InsertNodeChild(org1, org3);
		await InsertNodeChild(org2, org4);
		await InsertNodeChild(org3, org5);

		await ClearCache();

		var root = await Storage.GetByIdAsync<long, TestNode>(org1.Id, CancellationToken);

		// Recursive HasAccess-like check
		var found = await HasAccessRecursive(root, org4.Id, 0, CancellationToken);
		found.AssertTrue("Should find Org4 in the tree");

		var found5 = await HasAccessRecursive(root, org5.Id, 0, CancellationToken);
		found5.AssertTrue("Should find Org5 in the tree");

		var notFound = await HasAccessRecursive(root, 99999, 0, CancellationToken);
		notFound.AssertFalse("Should not find non-existent node");
	}

	private static async Task<bool> HasAccessRecursive(TestNode node, long targetId, int depth, CancellationToken cancellationToken)
	{
		if (node.Id == targetId)
			return true;

		if (depth > 10)
			return false;

		await foreach (var link in node.Children.WithCancellation(cancellationToken))
		{
			link.Child.AssertNotNull($"Child FK is null at depth {depth}, node {node.Name}");

			if (await HasAccessRecursive(link.Child, targetId, depth + 1, cancellationToken))
				return true;
		}

		return false;
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task RelationSingle_FkWithZeroId(string provider)
	{
		// Verifies that LoadFkAsync does not treat id=0 as "no FK".
		SetUp(provider);

		// Insert a person with Id=0 via raw SQL
		var dialect = DbTestHelper.GetDialect(provider);
		var tableName = dialect.QuoteIdentifier("Ecng_TestPerson");

		if (provider == DatabaseProviderRegistry.SqlServer)
		{
			DbTestHelper.ExecuteRaw(provider,
				$"SET IDENTITY_INSERT {tableName} ON; " +
				$"INSERT INTO {tableName} (Id, Name) VALUES (0, 'ZeroRoot'); " +
				$"SET IDENTITY_INSERT {tableName} OFF;");
		}
		else if (provider == DatabaseProviderRegistry.PostgreSql)
		{
			DbTestHelper.ExecuteRaw(provider, $"INSERT INTO {tableName} ({dialect.QuoteIdentifier("Id")}, {dialect.QuoteIdentifier("Name")}) OVERRIDING SYSTEM VALUE VALUES (0, 'ZeroRoot')");
		}
		else
		{
			DbTestHelper.ExecuteRaw(provider, $"INSERT INTO {tableName} ({dialect.QuoteIdentifier("Id")}, {dialect.QuoteIdentifier("Name")}) VALUES (0, 'ZeroRoot')");
		}

		// Insert a task referencing Person Id=0
		var task = new TestTask { Title = "TaskForZero", Priority = 1, Person = new TestPerson { Id = 0 } };
		task = await Storage.AddAsync(task, CancellationToken);

		await ClearCache();

		var loaded = await Storage.GetByIdAsync<long, TestTask>(task.Id, CancellationToken);
		loaded.AssertNotNull();
		loaded.Person.AssertNotNull("FK with id=0 must not be null");
		loaded.Person.Name.AssertEqual("ZeroRoot");
	}

	#endregion

	#region Finding #3: BulkLoad count cap

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task BulkLoad_GetCount_ReturnsActualCount_NotCappedByMaxBulkLoadRows(string provider)
	{
		// Finding #3: When MaxBulkLoadRows is smaller than the actual row count,
		// GetCountAsync returns cache size instead of real DB count.
		SetUp(provider);

		// Set small cap to reproduce without 100K rows
		_db.MaxBulkLoadRows = 3;

		try
		{
			// Insert more rows than the cap
			for (var i = 0; i < 7; i++)
				await InsertItem($"BulkItem{i}");

			// Enable bulk load
			Storage.AddBulkLoad<TestItem>();

			// GetCountAsync should return 7 (actual DB count), not 3 (cache cap)
			var count = await Storage.GetCountAsync<TestItem>(CancellationToken);
			count.AssertEqual(7);
		}
		finally
		{
			_db.ClearBulkLoad();
			_db.MaxBulkLoadRows = 100000;
		}
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task BulkLoad_GetById_FallsBackToDb_WhenNotInCache(string provider)
	{
		// ReadAsync returns null for ids beyond bulk cache cap instead of querying DB.
		SetUp(provider);

		_db.MaxBulkLoadRows = 3;

		try
		{
			// Insert 7 rows — bulk cache will hold only 3 (lowest ids)
			var items = new List<TestItem>();
			for (var i = 0; i < 7; i++)
				items.Add(await InsertItem($"BulkById{i}"));

			Storage.AddBulkLoad<TestItem>();

			// Trigger bulk cache init
			await Storage.GetCountAsync<TestItem>(CancellationToken);

			// Last item has highest id — NOT in bulk cache
			var lastItem = items[^1];
			var loaded = await Storage.GetByIdAsync<long, TestItem>(lastItem.Id, CancellationToken);

			loaded.AssertNotNull($"GetById should fall back to DB when id {lastItem.Id} is not in bulk cache");
			loaded.Name.AssertEqual(lastItem.Name);
		}
		finally
		{
			_db.ClearBulkLoad();
			_db.MaxBulkLoadRows = 100000;
		}
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task BulkLoad_GetById_CachesDbMiss_ReturnsNullFast(string provider)
	{
		// After DB confirms id doesn't exist, subsequent calls should return null
		// without hitting DB again.
		SetUp(provider);

		_db.MaxBulkLoadRows = 3;

		try
		{
			await InsertItem("OnlyOne");
			Storage.AddBulkLoad<TestItem>();

			// Non-existent id — should return null
			var missing = await Storage.GetByIdAsync<long, TestItem>(999999, CancellationToken);
			missing.AssertNull("Non-existent id should return null");

			// Second call — should also return null (cached miss)
			var missing2 = await Storage.GetByIdAsync<long, TestItem>(999999, CancellationToken);
			missing2.AssertNull("Repeated call for non-existent id should still return null");
		}
		finally
		{
			_db.ClearBulkLoad();
			_db.MaxBulkLoadRows = 100000;
		}
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task BulkLoad_LinqWhere_QueriesFullDb_NotTruncatedCache(string provider)
	{
		// LINQ queries over bulk-loaded entities execute against truncated in-memory set
		// instead of querying the full database.
		SetUp(provider);

		_db.MaxBulkLoadRows = 3;

		try
		{
			for (var i = 0; i < 7; i++)
				await InsertItem($"LinqItem{i}", priority: i + 1);

			Storage.AddBulkLoad<TestItem>();

			// LINQ Where — should find all 7, not just the 3 in cache
			var results = await Query<TestItem>()
				.Where(x => x.Priority > 0)
				.ToArrayAsyncEx(CancellationToken);

			results.Length.AssertEqual(7,
				$"LINQ query should return all 7 rows from DB, not {results.Length} from truncated cache");
		}
		finally
		{
			_db.ClearBulkLoad();
			_db.MaxBulkLoadRows = 100000;
		}
	}

	#endregion

	#region GetByIdsAsync

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task GetByIdsAsync_ReturnsAllMatchingEntities(string provider)
	{
		SetUp(provider);

		var item1 = await InsertItem("Alpha", priority: 1);
		var item2 = await InsertItem("Beta", priority: 2);
		var item3 = await InsertItem("Gamma", priority: 3);

		await ClearCache();

		var result = await Storage.GetByIdsAsync<long, TestItem>([item1.Id, item2.Id, item3.Id], CancellationToken);

		result.Length.AssertEqual(3);
		result.Select(r => r.Name).OrderBy(n => n).ToArray()
			.AssertEqual(new[] { "Alpha", "Beta", "Gamma" });
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task GetByIdsAsync_EmptyIds_ReturnsEmpty(string provider)
	{
		SetUp(provider);

		var result = await Storage.GetByIdsAsync<long, TestItem>([], CancellationToken);

		result.Length.AssertEqual(0);
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task GetByIdsAsync_NonExistentIds_ReturnsOnlyFound(string provider)
	{
		SetUp(provider);

		var item = await InsertItem("Existing");

		await ClearCache();

		var result = await Storage.GetByIdsAsync<long, TestItem>([item.Id, 999999, 888888], CancellationToken);

		result.Length.AssertEqual(1);
		result[0].Name.AssertEqual("Existing");
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task GetByIdsAsync_UsesBulkCache_WhenAvailable(string provider)
	{
		SetUp(provider);

		_db.MaxBulkLoadRows = 100;

		try
		{
			var item1 = await InsertItem("Cached1", priority: 1);
			var item2 = await InsertItem("Cached2", priority: 2);

			Storage.AddBulkLoad<TestItem>();

			// first call populates bulk cache, second should hit it
			var result1 = await Storage.GetByIdsAsync<long, TestItem>([item1.Id, item2.Id], CancellationToken);
			result1.Length.AssertEqual(2);

			var result2 = await Storage.GetByIdsAsync<long, TestItem>([item1.Id, item2.Id], CancellationToken);
			result2.Length.AssertEqual(2);
		}
		finally
		{
			_db.ClearBulkLoad();
			_db.MaxBulkLoadRows = 100000;
		}
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task GetByIdsAsync_CachesMisses_InBulkDict(string provider)
	{
		SetUp(provider);

		_db.MaxBulkLoadRows = 3;

		try
		{
			await InsertItem("Only");

			Storage.AddBulkLoad<TestItem>();

			// query with non-existent ids — should cache the miss
			var result = await Storage.GetByIdsAsync<long, TestItem>([999999, 888888], CancellationToken);
			result.Length.AssertEqual(0);

			// second call should return immediately from cached misses
			var result2 = await Storage.GetByIdsAsync<long, TestItem>([999999, 888888], CancellationToken);
			result2.Length.AssertEqual(0);
		}
		finally
		{
			_db.ClearBulkLoad();
			_db.MaxBulkLoadRows = 100000;
		}
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task GetByIdsAsync_SingleQuery_InClause(string provider)
	{
		SetUp(provider);

		var items = new List<TestItem>();
		for (var i = 0; i < 10; i++)
			items.Add(await InsertItem($"Item{i}", priority: i));

		await ClearCache();

		// request 5 of them in one call
		var requestIds = items.Where((_, idx) => idx % 2 == 0).Select(i => i.Id).ToArray();

		var result = await Storage.GetByIdsAsync<long, TestItem>(requestIds, CancellationToken);

		result.Length.AssertEqual(5);
		result.Select(r => r.Name).OrderBy(n => n).ToArray()
			.AssertEqual(new[] { "Item0", "Item2", "Item4", "Item6", "Item8" });
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task GetByIdsAsync_PreservesInputOrder(string provider)
	{
		SetUp(provider);

		var item1 = await InsertItem("First", priority: 1);
		var item2 = await InsertItem("Second", priority: 2);
		var item3 = await InsertItem("Third", priority: 3);

		await ClearCache();

		// request in reverse order
		var result = await Storage.GetByIdsAsync<long, TestItem>([item3.Id, item1.Id, item2.Id], CancellationToken);

		result.Length.AssertEqual(3);
		result[0].Name.AssertEqual("Third");
		result[1].Name.AssertEqual("First");
		result[2].Name.AssertEqual("Second");
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task GetByIdsAsync_DuplicateIds_ReturnsDuplicateEntries(string provider)
	{
		SetUp(provider);

		var item = await InsertItem("OnlyOne");

		await ClearCache();

		var result = await Storage.GetByIdsAsync<long, TestItem>([item.Id, item.Id, item.Id], CancellationToken);

		result.Length.AssertEqual(3);
		result[0].Name.AssertEqual("OnlyOne");
		result[1].Name.AssertEqual("OnlyOne");
		result[2].Name.AssertEqual("OnlyOne");
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task GetByIdsAsync_DuplicateIds_MixedWithNonExistent(string provider)
	{
		SetUp(provider);

		var item1 = await InsertItem("A");
		var item2 = await InsertItem("B");

		await ClearCache();

		// duplicates + non-existent
		var result = await Storage.GetByIdsAsync<long, TestItem>([item2.Id, 999999, item1.Id, item2.Id, 888888], CancellationToken);

		// non-existent skipped, duplicates preserved in order
		result.Length.AssertEqual(3);
		result[0].Name.AssertEqual("B");
		result[1].Name.AssertEqual("A");
		result[2].Name.AssertEqual("B");
	}

	#endregion
}

#endif
