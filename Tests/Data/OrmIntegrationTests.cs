#if NET10_0_OR_GREATER

namespace Ecng.Tests.Data;

using System.ComponentModel;
using System.Data.Common;
using System.Linq.Expressions;

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
				DbTestHelper.EnsureTable(provider, SchemaRegistry.Get(typeof(TestSubTask)), autoIncrement: true);
				DbTestHelper.EnsureTable(provider, SchemaRegistry.Get(typeof(TestNode)), autoIncrement: true);
				DbTestHelper.EnsureTable(provider, SchemaRegistry.Get(typeof(TestNodeChild)), autoIncrement: true);
				DbTestHelper.EnsureTable(provider, SchemaRegistry.Get(typeof(TestItemTag)), autoIncrement: false);
			}
		}

		_db = DbTestHelper.CreateDatabase(provider);

		lock (_initLock)
			_databases.Add(_db);

		DbTestHelper.DeleteAll(provider, "Ecng_TestItemCategory");
		DbTestHelper.DeleteAll(provider, "Ecng_TestNodeChild");
		DbTestHelper.DeleteAll(provider, "Ecng_TestSubTask");
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

	private async Task<TestItem> InsertItem(string name = "Test", int priority = 1, decimal price = 9.99m, bool isActive = true, int? nullableValue = null, DateTime? createdAt = null, DateTime? modifiedAt = null)
	{
		var item = new TestItem
		{
			Name = name,
			Priority = priority,
			Price = price,
			CreatedAt = createdAt ?? DateTime.UtcNow,
			ModifiedAt = modifiedAt,
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

	#region Select Projection Tests

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Select_Primitive_Empty(string provider)
	{
		SetUp(provider);

		var ids = await Query<TestItem>()
			.Where(x => x.Priority > 1000)
			.Select(x => x.Id)
			.ToArrayAsyncEx(CancellationToken);

		ids.Length.AssertEqual(0);
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Select_Primitive_NonEmpty(string provider)
	{
		SetUp(provider);
		var a = await InsertItem("Alpha", priority: 1);
		var b = await InsertItem("Beta", priority: 2);

		var ids = await Query<TestItem>()
			.Select(x => x.Id)
			.ToArrayAsyncEx(CancellationToken);

		ids.Length.AssertEqual(2);
		ids.OrderBy(i => i).ToArray().SequenceEqual([a.Id, b.Id]).AssertTrue();
	}

	/// <summary>
	/// Regression: projecting <c>(long?)t.Nav.Id</c> over rows that include a
	/// NULL FK must materialize into <c>long?[]</c> with the matching null
	/// slots. Before the fix this threw
	/// <c>NotSupportedException: No constructor of System.Nullable\`1[Int64]
	/// matches the projected columns</c> out of <c>SelectAsyncEnumerator.
	/// MaterializeByCtor</c> — the dispatch took the ctor-search branch
	/// because <c>Nullable&lt;long&gt;</c> isn't an <c>IsSerializablePrimitive</c>,
	/// and no ctor parameter names match the single-column projection.
	/// </summary>
	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Select_NullableFkId_WithNullRow_MaterializesNulls(string provider)
	{
		SetUp(provider);

		var alice = await InsertPerson("Alice");
		await InsertTask("WithPerson", alice);
		await InsertTask("Orphan", null);    // FK column is NULL on this row

		await ClearCache();

		var ids = await Query<TestTask>()
			.Select(t => (long?)t.Person.Id)
			.ToArrayAsyncEx(CancellationToken);

		ids.Length.AssertEqual(2);
		ids.Count(x => x is null).AssertEqual(1);
		ids.Count(x => x == alice.Id).AssertEqual(1);
	}

	/// <summary>
	/// Companion to <see cref="Select_NullableFkId_WithNullRow_MaterializesNulls"/>:
	/// the same single-column <c>(long?)Nav.Id</c> projection through
	/// <c>FirstOrDefaultAsyncEx</c> instead of <c>ToArrayAsyncEx</c>. Pre-fix
	/// this threw <c>NullReferenceException</c> out of
	/// <c>Database.GetOrAddCache</c> because the result-side dispatch in
	/// <c>ExecuteResultAsync</c> took the entity-materialization branch on
	/// <c>Nullable&lt;long&gt;</c> and called <c>SchemaRegistry.Get(typeof(long?))</c>,
	/// which has no schema → null meta → NRE.
	/// </summary>
	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task FirstOrDefault_NullableFkId_NoMatch_ReturnsDefault(string provider)
	{
		SetUp(provider);
		var alice = await InsertPerson("Alice");
		await InsertTask("WithPerson", alice);

		await ClearCache();

		// No row passes the predicate -> FirstOrDefault must return default(long?).
		var missing = await Query<TestTask>()
			.Where(t => t.Title == "no-such-row")
			.Select(t => (long?)t.Person.Id)
			.FirstOrDefaultAsyncEx(CancellationToken);

		missing.AssertNull();

		// Matching row carries the FK -> the same call should return the id.
		var found = await Query<TestTask>()
			.Where(t => t.Title == "WithPerson")
			.Select(t => (long?)t.Person.Id)
			.FirstOrDefaultAsyncEx(CancellationToken);

		found.AssertEqual(alice.Id);
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Select_String_NonEmpty(string provider)
	{
		SetUp(provider);
		await InsertItem("Alpha");
		await InsertItem("Beta");

		var names = await Query<TestItem>()
			.Select(x => x.Name)
			.ToArrayAsyncEx(CancellationToken);

		names.OrderBy(n => n).ToArray().SequenceEqual(["Alpha", "Beta"]).AssertTrue();
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Select_AnonymousType_TwoMembers(string provider)
	{
		SetUp(provider);
		var a = await InsertItem("Alpha", priority: 1);
		var b = await InsertItem("Beta", priority: 2);

		var rows = await Query<TestItem>()
			.Select(x => new { x.Id, x.Name })
			.ToArrayAsyncEx(CancellationToken);

		rows.Length.AssertEqual(2);
		rows.OrderBy(r => r.Id).Select(r => (r.Id, r.Name)).ToArray()
			.SequenceEqual([(a.Id, "Alpha"), (b.Id, "Beta")]).AssertTrue();
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Select_AnonymousType_WithAlias(string provider)
	{
		SetUp(provider);
		var a = await InsertItem("Alpha", priority: 7);

		var rows = await Query<TestItem>()
			.Select(x => new { ItemId = x.Id, Label = x.Name, Prio = x.Priority })
			.ToArrayAsyncEx(CancellationToken);

		rows.Length.AssertEqual(1);
		rows[0].ItemId.AssertEqual(a.Id);
		rows[0].Label.AssertEqual("Alpha");
		rows[0].Prio.AssertEqual(7);
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Select_AnonymousType_WithNavigationId(string provider)
	{
		SetUp(provider);
		var person = await InsertPerson("Alice");
		await InsertTask("T1", person, priority: 3);
		await InsertTask("T2", person, priority: 7);

		var rows = await Query<TestTask>()
			.Select(t => new { t.Id, PersonId = t.Person.Id })
			.ToArrayAsyncEx(CancellationToken);

		rows.Length.AssertEqual(2);
		rows.All(r => r.PersonId == person.Id).AssertTrue();
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Select_NullableNavigationMember_KeepsRowsWithNullFk(string provider)
	{
		SetUp(provider);

		var alice = await InsertPerson("Alice");
		await InsertTask("with-person", alice, priority: 1);
		// Orphan row: the nullable Person FK is left NULL.
		await Storage.AddAsync(new TestTask { Title = "orphan", Priority = 2, Person = null }, CancellationToken);

		// Projecting a non-Id member through the NULLABLE Person navigation must keep
		// the orphan row (with PersonName == null), i.e. the nav hop has to resolve to
		// a LEFT JOIN. (Was: every relation hop was an INNER JOIN, which silently dropped
		// the null-FK row; the translator now emits a LEFT OUTER join for nav hops.)
		var rows = await Query<TestTask>()
			.Select(t => new { t.Id, t.Title, PersonName = t.Person.Name })
			.ToArrayAsyncEx(CancellationToken);

		rows.Length.AssertEqual(2);
		rows.Any(r => r.Title == "orphan").AssertTrue();
		(rows.Single(r => r.Title == "orphan").PersonName is null).AssertTrue();
		(rows.Single(r => r.Title == "with-person").PersonName == "Alice").AssertTrue();
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Select_TwoHopNavigationMember_IntoAnonymous(string provider)
	{
		SetUp(provider);

		var alice = await InsertPerson("Alice");
		var task = await InsertTask("T1", alice, priority: 1);
		await Storage.AddAsync(new TestSubTask { Description = "with-task", Task = task }, CancellationToken);
		// Orphan: a sub-task whose Task FK is NULL. The two-hop projection must keep
		// it (PersonName == null) rather than INNER-JOIN dropping it.
		await Storage.AddAsync(new TestSubTask { Description = "orphan", Task = null }, CancellationToken);

		// Two-hop navigation (SubTask -> Task -> Person) projected into an anonymous
		// type. Confirms the SELECT path resolves a chain longer than one hop and that
		// every hop is a LEFT OUTER join, so a null FK anywhere along the chain keeps
		// the parent row. This is the projection guarantee the Broker trading/admin
		// repositories need to drop their post-query display-painting passes.
		var rows = await Query<TestSubTask>()
			.Select(s => new { s.Id, s.Description, PersonName = s.Task.Person.Name })
			.ToArrayAsyncEx(CancellationToken);

		rows.Length.AssertEqual(2);
		(rows.Single(r => r.Description == "with-task").PersonName == "Alice").AssertTrue();
		(rows.Single(r => r.Description == "orphan").PersonName is null).AssertTrue();
	}

	/// <summary>
	/// Server-side AVG over a date-difference, grouped by a real column. Isolates the
	/// dialect-aware DATEDIFF rendering from the grand-total path: a non-constant
	/// GROUP BY key is valid on every provider, so the only thing under test is that
	/// <c>(end - start).TotalHours</c> renders to portable SQL — SQL Server DATEDIFF,
	/// PostgreSQL EXTRACT(EPOCH ...) and SQLite julianday(). Latencies are whole hours
	/// so integer (SqlServer) and real (PostgreSQL/SQLite) AVG converge to the same value.
	/// </summary>
	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task GroupByColumn_AverageOfDateDiffHours_TranslatesServerSide(string provider)
	{
		SetUp(provider);

		var t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		// priority 1 -> latencies 10h and 20h (avg 15h); priority 2 -> 30h (avg 30h).
		await InsertItem("a", priority: 1, createdAt: t0, modifiedAt: t0.AddHours(10));
		await InsertItem("b", priority: 1, createdAt: t0, modifiedAt: t0.AddHours(20));
		await InsertItem("c", priority: 2, createdAt: t0, modifiedAt: t0.AddHours(30));

		var rows = await Query<TestItem>()
			.Where(x => x.ModifiedAt != null)
			.GroupBy(x => x.Priority)
			.Select(g => new { Priority = g.Key, Avg = g.Average(x => ((x.ModifiedAt ?? x.CreatedAt) - x.CreatedAt).TotalHours) })
			.ToArrayAsyncEx(CancellationToken);

		rows.Length.AssertEqual(2);
		(Math.Abs(rows.Single(r => r.Priority == 1).Avg - 15d) < 1.0).AssertTrue();
		(Math.Abs(rows.Single(r => r.Priority == 2).Avg - 30d) < 1.0).AssertTrue();
	}

	/// <summary>
	/// Constant-key GroupBy denotes a grand total over the whole filtered set, so it
	/// must emit NO GROUP BY clause: a bare <c>SELECT avg(...) FROM ... WHERE ...</c>
	/// collapses to a single row on every dialect, whereas <c>GROUP BY 1</c> is
	/// rejected by SQL Server. Uses a plain integer column (no DATEDIFF) so the only
	/// thing under test is the constant-key grand-total path.
	/// </summary>
	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task GroupBy_ConstantKey_GrandTotalAverage(string provider)
	{
		SetUp(provider);

		await InsertItem("a", priority: 10);
		await InsertItem("b", priority: 20);
		await InsertItem("c", priority: 30);

		var rows = await Query<TestItem>()
			.GroupBy(x => 1)
			.Select(g => new { Avg = g.Average(x => x.Priority) })
			.ToArrayAsyncEx(CancellationToken);

		rows.Length.AssertEqual(1);
		(Math.Abs(rows[0].Avg - 20d) < 0.0001).AssertTrue();
	}

	/// <summary>
	/// Server-side AVG over a date-difference, aggregated as a single grand total —
	/// the KycRepository average-decision-latency KPI: average of
	/// <c>(ModifiedAt ?? CreatedAt) - CreatedAt</c> in hours across the whole filtered
	/// set. Exercises the two features it once depended on together — the constant-key
	/// grand total (<c>GroupBy(x =&gt; 1)</c> emits no GROUP BY) and dialect-aware
	/// DATEDIFF rendering — on SqlServer, PostgreSQL and SQLite. Latencies are whole
	/// hours, so integer (SqlServer) and real (PostgreSQL/SQLite) AVG converge to 15.
	/// </summary>
	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task GroupBy_AverageOfDateDiffHours_TranslatesServerSide(string provider)
	{
		SetUp(provider);

		var t0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		// Two "decided" items with latencies 10h and 20h -> average 15h. Integer
		// DATEDIFF(hour) over {10, 20} averages to exactly 15, so the assertion does
		// not depend on whether AVG returns an integer or a real.
		await InsertItem("a", createdAt: t0, modifiedAt: t0.AddHours(10));
		await InsertItem("b", createdAt: t0, modifiedAt: t0.AddHours(20));
		// An "undecided" item (ModifiedAt null) that the filter excludes.
		await InsertItem("c", createdAt: t0, modifiedAt: null);

		var rows = await Query<TestItem>()
			.Where(x => x.ModifiedAt != null)
			.GroupBy(x => 1)
			.Select(g => new { Avg = g.Average(x => ((x.ModifiedAt ?? x.CreatedAt) - x.CreatedAt).TotalHours) })
			.ToArrayAsyncEx(CancellationToken);

		rows.Length.AssertEqual(1);
		(Math.Abs(rows[0].Avg - 15d) < 1.0).AssertTrue();
	}

	public record SelectItemDto(long Id, string Name);

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Select_PositionalRecord(string provider)
	{
		SetUp(provider);
		var a = await InsertItem("Alpha", priority: 1);

		var rows = await Query<TestItem>()
			.Select(x => new SelectItemDto(x.Id, x.Name))
			.ToArrayAsyncEx(CancellationToken);

		rows.Length.AssertEqual(1);
		rows[0].Id.AssertEqual(a.Id);
		rows[0].Name.AssertEqual("Alpha");
	}

	public class SelectNamedRow
	{
		public long Id { get; set; }
		public string Name { get; set; }
		public int Priority { get; set; }
		public decimal Price { get; set; }
		public bool IsActive { get; set; }
	}

	/// <summary>
	/// Select projection into a named class via member-init must populate
	/// every assigned property from the row, mirroring the anonymous-type
	/// path. Without the fix every property comes back as default(T).
	/// </summary>
	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Select_NamedClass_MemberInit_PopulatesProperties(string provider)
	{
		SetUp(provider);

		var inserted = await InsertItem(
			name: "Alpha",
			priority: 42,
			price: 123.45m,
			isActive: true);

		var rows = await Query<TestItem>()
			.Select(x => new SelectNamedRow
			{
				Id = x.Id,
				Name = x.Name,
				Priority = x.Priority,
				Price = x.Price,
				IsActive = x.IsActive,
			})
			.ToArrayAsyncEx(CancellationToken);

		rows.Length.AssertEqual(1);
		rows[0].Id.AssertEqual(inserted.Id);
		rows[0].Name.AssertEqual("Alpha");
		rows[0].Priority.AssertEqual(42);
		rows[0].Price.AssertEqual(123.45m);
		rows[0].IsActive.AssertEqual(true);
	}

	// Anonymous-type control for the named-class projection above.
	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Select_AnonymousType_PopulatesProperties_Control(string provider)
	{
		SetUp(provider);

		var inserted = await InsertItem(
			name: "Alpha",
			priority: 42,
			price: 123.45m,
			isActive: true);

		var rows = await Query<TestItem>()
			.Select(x => new
			{
				Id = x.Id,
				Name = x.Name,
				Priority = x.Priority,
				Price = x.Price,
				IsActive = x.IsActive,
			})
			.ToArrayAsyncEx(CancellationToken);

		rows.Length.AssertEqual(1);
		rows[0].Id.AssertEqual(inserted.Id);
		rows[0].Name.AssertEqual("Alpha");
		rows[0].Priority.AssertEqual(42);
		rows[0].Price.AssertEqual(123.45m);
		rows[0].IsActive.AssertEqual(true);
	}

	// Named-class target for the navigation member-init projection below. PersonName
	// is fed from a joined column (TestTask.Person.Name), not a self column of the
	// queried entity, so it can only land via the leftover-column property setter path.
	public class SelectTaskNamedRow
	{
		public long Id { get; set; }
		public string Title { get; set; }
		public string PersonName { get; set; }
	}

	/// <summary>
	/// Multi-row Select projection into a named class via member-init, where one of
	/// the assigned members is a NAVIGATION column (<c>t.Person.Name</c>) rather than a
	/// self column of the queried entity. This is the exact shape the Broker trading/admin
	/// repositories used to avoid — they ran the query first and then "painted" the
	/// joined display values onto each row in a separate <c>Populate*Display</c> pass,
	/// because the named-class object-initializer projection used to come back with every
	/// property at <c>default(T)</c>.
	///
	/// <para>Two regression surfaces are exercised together here:</para>
	/// <list type="bullet">
	/// <item><b>Per-row materialization.</b> Three rows are seeded with distinct values;
	/// each result row must carry its OWN column values. A regression in the leftover-column
	/// setter loop (which indexes the column buffer by row <c>[r]</c>) would paint every row
	/// with row 0's values or leave them at <c>default</c> — the precise corruption the
	/// post-query painting workaround existed to dodge.</item>
	/// <item><b>Navigation column into a named class.</b> <c>PersonName = t.Person.Name</c>
	/// is sourced from a LEFT OUTER join, not from the root entity, so it can only be
	/// materialized through the by-name property-setter path for columns not consumed by
	/// the (parameterless) constructor.</item>
	/// </list>
	///
	/// <para>Background: anonymous-type projections (<c>Select(x =&gt; new { ... })</c>) and
	/// positional-record projections (<c>Select(x =&gt; new Dto(...))</c>) have always
	/// materialized correctly via the constructor path. The named-class object-initializer
	/// shape (<c>Select(x =&gt; new Named { A = x.A, B = x.Nav.B })</c>) was the remaining
	/// Ecng ORM gap: leftover columns went unread, so every assigned property returned its
	/// default. That gap was closed in Data.ORM/Database.cs by routing each column not
	/// consumed by the constructor to a case-insensitively matched writable property after
	/// the instance is created. This test is the per-row + navigation guard for that fix and,
	/// passing, demonstrates the Broker <c>Populate*Display</c> workaround is now fully
	/// removable.</para>
	/// </summary>
	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Select_NamedClass_MemberInit_NavigationColumn_MultiRow(string provider)
	{
		SetUp(provider);

		var alice = await InsertPerson("Alice");
		var bob = await InsertPerson("Bob");
		await InsertTask("T1", alice, priority: 1);
		await InsertTask("T2", bob, priority: 2);
		await InsertTask("T3", alice, priority: 3);

		var rows = await Query<TestTask>()
			.Select(t => new SelectTaskNamedRow
			{
				Id = t.Id,
				Title = t.Title,
				PersonName = t.Person.Name,
			})
			.ToArrayAsyncEx(CancellationToken);

		rows.Length.AssertEqual(3);

		// Each row must carry its own column values, including the joined PersonName.
		var t1 = rows.Single(r => r.Title == "T1");
		(t1.Id > 0).AssertTrue();
		t1.PersonName.AssertEqual("Alice");

		var t2 = rows.Single(r => r.Title == "T2");
		(t2.Id > 0).AssertTrue();
		t2.PersonName.AssertEqual("Bob");

		var t3 = rows.Single(r => r.Title == "T3");
		(t3.Id > 0).AssertTrue();
		t3.PersonName.AssertEqual("Alice");

		// No row may collapse to defaults (the pre-fix failure: blank Title / null PersonName).
		rows.All(r => !r.Title.IsEmpty()).AssertTrue();
		rows.All(r => !r.PersonName.IsEmpty()).AssertTrue();
		rows.All(r => r.Id > 0).AssertTrue();
	}

	/// <summary>
	/// The same named-class member-init projection (including a navigation column)
	/// but through the single-result terminal <c>FirstOrDefaultAsyncEx</c>. Unlike
	/// <c>ToArrayAsyncEx</c> (which enumerates via the async SelectAsyncEnumerable /
	/// MaterializeByCtor path), FirstOrDefaultAsyncEx falls through to the synchronous
	/// <c>FirstOrDefault()</c> result-dispatch. That dispatch must treat a named,
	/// non-entity projection class as a constructor/property projection, not as an
	/// entity — otherwise it casts the projected row to <c>IDbPersistable</c> and throws
	/// <c>InvalidCastException</c>.
	/// </summary>
	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Select_NamedClass_MemberInit_FirstOrDefaultAsyncEx(string provider)
	{
		SetUp(provider);

		var alice = await InsertPerson("Alice");
		var task = await InsertTask("Build", alice, priority: 5);

		await ClearCache();

		var row = await Query<TestTask>()
			.Where(t => t.Id == task.Id)
			.Select(t => new SelectTaskNamedRow
			{
				Id = t.Id,
				Title = t.Title,
				PersonName = t.Person.Name,
			})
			.FirstOrDefaultAsyncEx(CancellationToken);

		row.AssertNotNull();
		row.Id.AssertEqual(task.Id);
		row.Title.AssertEqual("Build");
		row.PersonName.AssertEqual("Alice");
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

	#region No-Identity Entity Tests

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task NoIdentity_FirstOrDefault(string provider)
	{
		SetUp(provider);

		var item = await InsertItem("TagTarget");

		var tag = new TestItemTag { Item = item, Tag = "important" };
		await Storage.AddAsync(tag, CancellationToken);

		await ClearCache();

		// FirstOrDefaultAsyncEx sets Take(1) which triggers ORDER BY — must not use non-existent Id column
		var result = await Query<TestItemTag>()
			.Where(t => t.Tag == "important")
			.FirstOrDefaultAsyncEx(CancellationToken);

		result.AssertNotNull();
		result.Tag.AssertEqual("important");
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task NoIdentity_WhereQuery(string provider)
	{
		SetUp(provider);

		var item = await InsertItem("TagOwner");

		await Storage.AddAsync(new TestItemTag { Item = item, Tag = "alpha" }, CancellationToken);
		await Storage.AddAsync(new TestItemTag { Item = item, Tag = "beta" }, CancellationToken);

		await ClearCache();

		var results = await Query<TestItemTag>()
			.Where(t => t.Item.Id == item.Id)
			.ToArrayAsyncEx(CancellationToken);

		results.Length.AssertEqual(2);
	}

	/// <summary>
	/// Verifies that Contains with a navigation property's Id works end-to-end.
	/// Filters TestTask by an array of Person Ids using Contains.
	/// </summary>
	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Where_Contains_NavigationPropertyId(string provider)
	{
		SetUp(provider);

		var alice = await InsertPerson("Alice");
		var bob = await InsertPerson("Bob");
		var charlie = await InsertPerson("Charlie");

		await InsertTask("A1", alice);
		await InsertTask("A2", alice);
		await InsertTask("B1", bob);
		await InsertTask("C1", charlie);

		await ClearCache();

		var personIds = new[] { alice.Id, bob.Id };
		var results = await Query<TestTask>()
			.Where(t => personIds.Contains(t.Person.Id))
			.ToArrayAsyncEx(CancellationToken);

		results.Length.AssertEqual(3);
	}

	/// <summary>
	/// Verifies that Contains with navigation property Id works on entities
	/// without an identity column (BaseJoinEntity pattern like TestItemTag).
	/// </summary>
	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Where_Contains_NavigationPropertyId_NoIdentity(string provider)
	{
		SetUp(provider);

		var item1 = await InsertItem("Owner1");
		var item2 = await InsertItem("Owner2");
		var item3 = await InsertItem("Owner3");

		await Storage.AddAsync(new TestItemTag { Item = item1, Tag = "alpha" }, CancellationToken);
		await Storage.AddAsync(new TestItemTag { Item = item1, Tag = "beta" }, CancellationToken);
		await Storage.AddAsync(new TestItemTag { Item = item2, Tag = "gamma" }, CancellationToken);
		await Storage.AddAsync(new TestItemTag { Item = item3, Tag = "delta" }, CancellationToken);

		await ClearCache();

		var itemIds = new[] { item1.Id, item2.Id };
		var results = await Query<TestItemTag>()
			.Where(t => itemIds.Contains(t.Item.Id))
			.ToArrayAsyncEx(CancellationToken);

		results.Length.AssertEqual(3);
	}

	/// <summary>
	/// Verifies that Contains works with multiple different FK navigation properties
	/// in the same Where clause.
	/// </summary>
	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Where_Contains_MultipleNavigationPropertyIds(string provider)
	{
		SetUp(provider);

		var item1 = await InsertItem("Item1");
		var item2 = await InsertItem("Item2");
		var cat1 = await InsertCategory("Cat1");
		var cat2 = await InsertCategory("Cat2");

		await InsertItemCategory(item1, cat1);
		await InsertItemCategory(item1, cat2);
		await InsertItemCategory(item2, cat1);
		await InsertItemCategory(item2, cat2);

		await ClearCache();

		var itemIds = new[] { item1.Id };
		var catIds = new[] { cat1.Id };
		var results = await Query<TestItemCategory>()
			.Where(ic => itemIds.Contains(ic.Item.Id) && catIds.Contains(ic.Category.Id))
			.ToArrayAsyncEx(CancellationToken);

		results.Length.AssertEqual(1);
		results[0].Item.Id.AssertEqual(item1.Id);
		results[0].Category.Id.AssertEqual(cat1.Id);
	}

	/// <summary>
	/// Verifies that Contains with navigation property Id works combined with
	/// other filter conditions.
	/// </summary>
	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Where_Contains_NavigationPropertyId_WithOtherFilters(string provider)
	{
		SetUp(provider);

		var alice = await InsertPerson("Alice");
		var bob = await InsertPerson("Bob");

		await InsertTask("Low", alice, priority: 1);
		await InsertTask("High", alice, priority: 10);
		await InsertTask("BobHigh", bob, priority: 20);

		await ClearCache();

		var personIds = new[] { alice.Id, bob.Id };
		var results = await Query<TestTask>()
			.Where(t => personIds.Contains(t.Person.Id) && t.Priority > 5)
			.ToArrayAsyncEx(CancellationToken);

		results.Length.AssertEqual(2);
	}

	/// <summary>
	/// Verifies that OrderBy with navigation property Id works end-to-end.
	/// Tasks should be ordered by the FK column (Person Id), not the entity's own Id.
	/// </summary>
	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task OrderBy_NavigationPropertyId(string provider)
	{
		SetUp(provider);

		var alice = await InsertPerson("Alice");
		var bob = await InsertPerson("Bob");

		// Insert in reverse order: Bob's task first, Alice's task second
		await InsertTask("BobTask", bob, priority: 1);
		await InsertTask("AliceTask", alice, priority: 1);

		await ClearCache();

		var results = await Query<TestTask>()
			.OrderBy(t => t.Person.Id)
			.ToArrayAsyncEx(CancellationToken);

		results.Length.AssertEqual(2);
		// Alice was created first → has lower Person Id → should come first
		results[0].Person.Id.AssertEqual(alice.Id);
		results[1].Person.Id.AssertEqual(bob.Id);
	}

	/// <summary>
	/// Verifies that ThenBy with navigation property Id works end-to-end.
	/// Tasks with same priority should be sub-sorted by Person Id (FK column).
	/// </summary>
	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task ThenBy_NavigationPropertyId(string provider)
	{
		SetUp(provider);

		var alice = await InsertPerson("Alice");
		var bob = await InsertPerson("Bob");

		// Both tasks have same priority, but Bob's task is inserted first
		await InsertTask("BobTask", bob, priority: 5);
		await InsertTask("AliceTask", alice, priority: 5);

		await ClearCache();

		var results = await Query<TestTask>()
			.OrderBy(t => t.Priority)
			.ThenBy(t => t.Person.Id)
			.ToArrayAsyncEx(CancellationToken);

		results.Length.AssertEqual(2);
		// Same priority → ThenBy Person.Id → Alice (lower Id) first
		results[0].Person.Id.AssertEqual(alice.Id);
		results[1].Person.Id.AssertEqual(bob.Id);
	}

	/// <summary>
	/// Verifies that OrderByDescending with navigation property Id works end-to-end.
	/// Tasks should be sorted by Person Id descending.
	/// </summary>
	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task OrderByDescending_NavigationPropertyId(string provider)
	{
		SetUp(provider);

		var alice = await InsertPerson("Alice");
		var bob = await InsertPerson("Bob");

		await InsertTask("AliceTask", alice, priority: 1);
		await InsertTask("BobTask", bob, priority: 1);

		await ClearCache();

		var results = await Query<TestTask>()
			.OrderByDescending(t => t.Person.Id)
			.ToArrayAsyncEx(CancellationToken);

		results.Length.AssertEqual(2);
		// Bob has higher Person Id → should come first in DESC
		results[0].Person.Id.AssertEqual(bob.Id);
		results[1].Person.Id.AssertEqual(alice.Id);
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

	#region Translator regressions

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task LeftJoin_WithIdsContainsAndEntityNullCheck_TranslatesCorrectly(string provider)
	{
		// Regression: GroupJoin+SelectMany(DefaultIfEmpty) followed by a
		// Where that combines `ids.Contains(p.Id)` with `t2 == null` used
		// to emit "[t2].* IS NULL" plus a stray "[p].[Id]" — invalid SQL.
		// The fix lifts entity-null comparisons to the identity column and
		// resolves transparent-id chains to the right alias.
		SetUp(provider);

		var alice = await Storage.AddAsync(new TestPerson { Name = "Alice" }, CancellationToken);
		var bob = await Storage.AddAsync(new TestPerson { Name = "Bob" }, CancellationToken);
		await Storage.AddAsync(new TestTask { Title = "T1", Person = alice, Priority = 5 }, CancellationToken);

		var ids = new[] { alice.Id, bob.Id };

		var query = from p in Query<TestPerson>()
					join t in Query<TestTask>() on p.Id equals t.Person.Id into tg
					from t2 in tg.DefaultIfEmpty()
					where ids.Contains(p.Id) && (t2 == null || t2.Priority > 0)
					select p;

		var result = await query.ToArrayAsyncEx(CancellationToken);
		(result.Length >= 2).AssertTrue();
	}

	/// <summary>
	/// View-processor shape: GroupJoin + DefaultIfEmpty followed by a
	/// projection that contains a sub-query referencing the outer source
	/// (`i.Id`). Ensures the sub-query's reference to `i.Id` resolves to the
	/// original outer FROM alias, emitting `... ([b1].[File] = [e].[Id]) ...`.
	/// (Was: the translator leaked the compiler-generated transparent identifier,
	/// emitting `... ([b1].[File] = [&lt;&gt;h__TransparentIdentifier2].[Id]) ...`, so
	/// SQL Server reported "The multi-part identifier
	/// &lt;&gt;h__TransparentIdentifier2.Id could not be bound".)
	/// </summary>
	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task ViewProcessorShape_GroupJoin_ProjectionWithSubqueryReferencingOuter(string provider)
	{
		SetUp(provider);
		var i1 = await InsertItem("WithCat");
		var i2 = await InsertItem("NoCat");
		var cat = await InsertCategory("Cat1");
		await InsertItemCategory(i1, cat);

		await ClearCache();

		// Sub-query sources are captured as locals before the main
		// from-block — the translator cannot rewrite Query<T>() method
		// calls inside an expression tree.
		var itemCategories = Query<TestItemCategory>();

		var view =
			from i in Query<TestItem>()
			join ic in itemCategories on i.Id equals ic.Item.Id into ics
			from ic1 in ics.DefaultIfEmpty()
			select new TestItem
			{
				Id = i.Id,
				Name = i.Name,
				// Sub-query references the outer `i.Id` — count-of-related
				// pattern that view processors commonly emit.
				Priority = (
					from x in itemCategories
					where x.Item.Id == i.Id
					select x
				).Count(),
			};

		var ids = new[] { i1.Id, i2.Id };
		var results = await view.Where(e => ids.Contains(e.Id)).ToArrayAsyncEx(CancellationToken);

		results.Length.AssertEqual(2);
	}

	/// <summary>
	/// Mirrors VTopicViewProcessor: projection wraps single-`from` correlated
	/// sub-queries (.Any() / .Count() / .Max()) referencing the outer source
	/// via `x.Y.Id == outer.Id`. Ensures the translator resolves `outer.Id` to
	/// the outer FROM alias [e], emitting `[x].[Item] = [e].[Id]`. (Was: the
	/// sub-query's own alias leaked, emitting `[x].[Item] = [x].[Id]` and
	/// producing wrong per-row results — or, on PostgreSQL, a bool→bit cast
	/// failure downstream of the broken SQL.)
	/// </summary>
	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task ViewProcessorShape_Projection_CorrelatedSubqueryWithAny(string provider)
	{
		SetUp(provider);
		var i1 = await InsertItem("WithCat");
		var i2 = await InsertItem("NoCat");
		var cat = await InsertCategory("Cat1");
		await InsertItemCategory(i1, cat);

		await ClearCache();

		var itemCategories = Query<TestItemCategory>();

		var view =
			from i in Query<TestItem>()
			select new TestItem
			{
				Id = i.Id,
				Name = i.Name,
				IsActive = (from x in itemCategories where x.Item.Id == i.Id select x).Any(),
			};

		var results = (await view.ToArrayAsyncEx(CancellationToken))
			.OrderBy(r => r.Id)
			.ToArray();

		results.Length.AssertEqual(2);
		results[0].IsActive.AssertTrue("i1 (with category) should be IsActive=true");
		results[1].IsActive.AssertFalse("i2 (no category) should be IsActive=false");
	}

	/// <summary>
	/// Mirrors StockSharp.Web's DbTests.CountWithPaging:
	///   view.ToQueryable().SkipLong(N).Take(M).CountAsyncEx()
	/// where the view is itself a GROUP BY query that projects to a non-table
	/// shape via `g.Key.X` / `g.Count()`. Ensures the paged-then-counted grouped
	/// view binds correctly. (Was: the translator emitted a stray
	/// `[<>h__TransparentIdentifier0].[Id]` reference that SQL Server failed to bind.)
	/// </summary>
	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task GroupByProjection_PagingThenCount(string provider)
	{
		SetUp(provider);
		await InsertItem("A", priority: 1);
		await InsertItem("B", priority: 1);
		await InsertItem("C", priority: 2);

		await ClearCache();

		// View shape mirrored on VProductBugReportCount:
		//   group by composite Key, project Key + Count() into a new TestItem
		var view =
			from i in Query<TestItem>()
			group i by new { i.Priority, i.IsActive } into g
			select new TestItem
			{
				Id = 0,
				Name = "grouped",
				Priority = g.Key.Priority,
				IsActive = g.Key.IsActive,
				NullableValue = g.Count(),
			};

		// Skip(0).Take(N).Count() — the same shape DbTests.CountWithPaging hits.
		var count = await view.Skip(0).Take(10).CountAsyncEx(CancellationToken);

		(count > 0).AssertTrue($"Expected count > 0 from grouped view, got {count}");
	}

	/// <summary>
	/// Mirrors StockSharp.Web's GetPaged path: the LINQ tree applies
	/// <c>.OrderBy(x =&gt; x.Id).Skip().Take()</c> on top of a GROUP BY view.
	/// The translator wraps the grouped query in a CTE for paging; ensures the
	/// outer ORDER BY references the CTE alias [p]. (Was: the inner table alias
	/// leaked into the outer ORDER BY, producing <c>order by [e].[Id]</c> against
	/// <c>from [cteresults] [p]</c> — SQL Server: "The multi-part identifier
	/// '[e].[Id]' could not be bound".)
	/// </summary>
	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task GroupByProjection_OrderByThenPaging_RebindsAliasToCte(string provider)
	{
		SetUp(provider);
		await InsertItem("A", priority: 1);
		await InsertItem("B", priority: 1);
		await InsertItem("C", priority: 2);

		await ClearCache();

		var view =
			from i in Query<TestItem>()
			group i by new { i.Priority, i.IsActive } into g
			select new TestItem
			{
				Id = 0,
				Name = "grouped",
				Priority = g.Key.Priority,
				IsActive = g.Key.IsActive,
				NullableValue = g.Count(),
			};

		// .OrderBy(x => x.Id).Skip(0).Take(N) — the same shape Apply() builds.
		var rows = await view.OrderBy(x => x.Id).Skip(0).Take(10).ToArrayAsyncEx(CancellationToken);

		(rows.Length > 0).AssertTrue($"Expected rows > 0 from grouped+ordered+paged view, got {rows.Length}");
	}

	/// <summary>
	/// Mirrors VProductBugReportCount: group by composite key and project a
	/// MEMBER whose value is a CASE expression that references both
	/// <c>g.Key.X</c> AND <c>g.Count()</c>. Ensures the conditional's
	/// <c>g.Key.Priority</c> resolves to <c>[e].[Priority]</c>. (Was: the
	/// conditional sub-query Context had TableAlias=null — <c>AnalyseSubqueryShape</c>
	/// only handled MethodCallExpression — so <c>GetAlias("Priority")</c> returned the
	/// member name verbatim and emitted invalid <c>[Priority].*</c>; the Context now
	/// falls back to the outer TableAlias for bare ConditionalExpression sub-queries.)
	/// </summary>
	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task GroupByProjection_ConditionalUsingKeyAndAggregate_ResolvesAlias(string provider)
	{
		SetUp(provider);
		await InsertItem("A", priority: 0);
		await InsertItem("B", priority: 0);
		await InsertItem("C", priority: 1);

		await ClearCache();

		// Mirrors the failing VProductBugReportCount projection shape:
		//   Priority = (g.Key.Priority == default ? -g.Count() : g.Key.Priority)
		var view =
			from i in Query<TestItem>()
			group i by new { i.Priority, i.IsActive } into g
			select new TestItem
			{
				Id = 0,
				Name = "grouped",
				IsActive = g.Key.IsActive,
				Priority = g.Key.Priority == default ? -g.Count() : g.Key.Priority,
				NullableValue = g.Count(),
			};

		var rows = await view.OrderBy(x => x.Id).Skip(0).Take(10).ToArrayAsyncEx(CancellationToken);

		(rows.Length > 0).AssertTrue($"Expected rows > 0 from conditional projection, got {rows.Length}");
	}

	/// <summary>
	/// Mirrors VProductBugReportCount more precisely:
	/// <c>from e in T1 join j in T2 on e.FK equals j.Id group e by new { j.Id, j.Field } into g</c>
	/// then projecting <c>g.Key.Field</c> inside a conditional. The group key
	/// references members of the JOINED table — they must resolve to the join
	/// alias (e.g. <c>[i].[Priority]</c>), not the main FROM alias
	/// (<c>[e].[Priority]</c>) which doesn't have those columns at all.
	/// </summary>
	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task GroupByProjection_KeyOnJoinedTable_ResolvesToJoinAlias(string provider)
	{
		SetUp(provider);
		var item1 = await InsertItem("A", priority: 0);
		var item2 = await InsertItem("B", priority: 1);
		var cat = await InsertCategory("CatA");
		await InsertItemCategory(item1, cat);
		await InsertItemCategory(item2, cat);

		await ClearCache();

		var view =
			from ic in Query<TestItemCategory>()
			join i in Query<TestItem>() on ic.Item.Id equals i.Id
			group ic by new { i.Id, i.Priority, i.IsActive } into g
			select new TestItem
			{
				Id = 0,
				Name = "grouped",
				IsActive = g.Key.IsActive,
				// Key reference must resolve to JOIN alias, not the main alias
				Priority = g.Key.Priority == default ? -g.Count() : g.Key.Priority,
				NullableValue = g.Count(),
			};

		var rows = await view.OrderBy(x => x.Id).Skip(0).Take(10).ToArrayAsyncEx(CancellationToken);

		(rows.Length > 0).AssertTrue($"Expected rows > 0 from join+groupby+conditional, got {rows.Length}");
	}

	/// <summary>
	/// Mirrors StockSharp.Web's TopicTagService.FindAsync chain:
	/// <c>(from a in T1 join b in T2 ... select new { A=a, B=b }).Where(p =&gt; p.B.Field == X)</c>
	/// — a Where layered on top of an anonymous projection that includes
	/// a joined entity. Ensures the Where lambda's `p.B.Field` resolves to the
	/// JOIN alias `[b]`, not the main FROM alias `[a]`. (Was: the translator
	/// emitted <c>[a].[Field]</c> and SQL Server failed with "Invalid column
	/// name 'Field'".)
	/// </summary>
	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task WhereAfterJoinAnonymousProjection_ResolvesJoinAlias(string provider)
	{
		SetUp(provider);
		var i1 = await InsertItem("Match", priority: 1);
		var i2 = await InsertItem("NoMatch", priority: 2);
		var cat = await InsertCategory("CatA");
		await InsertItemCategory(i1, cat);
		await InsertItemCategory(i2, cat);

		await ClearCache();

		var view =
			from ic in Query<TestItemCategory>()
			join i in Query<TestItem>() on ic.Item.Id equals i.Id
			select new { Mapping = ic, Detail = i };

		// Where on the projected anonymous type — `p.Detail.Name` must
		// resolve to TestItem's alias [i], not TestItemCategory's [e].
		// Then unwrap to a registered entity to keep the materialiser happy.
		var rows = await view
			.Where(p => p.Detail.Name == "Match")
			.Select(p => p.Mapping)
			.ToArrayAsyncEx(CancellationToken);

		rows.Length.AssertEqual(1);
	}

	/// <summary>
	/// Mirrors VNugetSpecificationViewProcessor: a Join projects an FK
	/// short-circuit through the JOINED parameter, e.g.
	/// <c>(from e in T1 join b in T2 on e.X equals b.Id select new T { Ref = new() { Id = b.Cat.Id } })</c>.
	/// The resolver must use <c>b</c>'s registered alias, not the main FROM
	/// alias, so the FK column is emitted as <c>[b].[Cat]</c> rather than
	/// <c>[e].[Cat]</c> (NugetSpecification has no Cat column).
	/// </summary>
	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task ProjectionFkShortCircuitOnJoinParameter_UsesJoinAlias(string provider)
	{
		SetUp(provider);
		var item = await InsertItem("OnlyItem", priority: 1);
		var cat = await InsertCategory("CatOnly");
		await InsertItemCategory(item, cat);

		await ClearCache();

		// `tic.Category.Id` where tic is the JOIN parameter — the FK
		// short-circuit must qualify with [tic]'s alias, not [e]'s.
		var view =
			from e in Query<TestItem>()
			join tic in Query<TestItemCategory>() on e.Id equals tic.Item.Id
			select new TestItemCategory
			{
				Id = 0,
				Item = new() { Id = e.Id },
				Category = new() { Id = tic.Category.Id },
			};

		var rows = await view.ToArrayAsyncEx(CancellationToken);

		rows.Length.AssertEqual(1);
		(rows[0].Category?.Id ?? 0).AssertEqual(cat.Id);
	}

	/// <summary>
	/// Mirrors StockSharp.Web's DbTests.EnumerateTopics:
	/// <c>view.OrderBy(t =&gt; t.ProjectedComputedField).Take(N)</c>. Ensures
	/// ORDER BY over a computed SELECT-list output references the bare alias.
	/// (Was: the translator qualified the order-by with the source-table alias
	/// — <c>[e].[ProjectedField]</c> — but the column is a SELECT-list output,
	/// not a physical column on the source.)
	/// </summary>
	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task OrderByProjectedAlias_FromViewSelect_EmitsUnqualified(string provider)
	{
		SetUp(provider);
		await InsertItem("A", priority: 1);
		await InsertItem("B", priority: 2);
		await InsertItem("C", priority: 3);

		await ClearCache();

		// The projection introduces a SELECT-only field `NullableValue`
		// computed via a correlated sub-query — it has no underlying column.
		// Ordering by it must NOT qualify with the source alias — it must
		// reference the SELECT-list alias.
		var items = Query<TestItem>();

		var view =
			from i in items
			select new TestItem
			{
				Id = i.Id,
				Name = i.Name,
				NullableValue = (from j in items where j.Priority <= i.Priority select j).Count(),
			};

		var rows = await view.OrderBy(t => t.NullableValue).Take(10).ToArrayAsyncEx(CancellationToken);

		rows.Length.AssertEqual(3);
		// First row must have the smallest NullableValue (running rank 1).
		(rows[0].NullableValue >= 1).AssertTrue($"Expected first NullableValue >=1, got {rows[0].NullableValue}");
	}

	/// <summary>
	/// Mirrors StockSharp.Web's ByGroups extension:
	/// <c>source.Where(e =&gt; (from inner in inners where ... group ... select g.Key.Id).Contains(e.Id))</c>.
	/// The translator must set the sub-query's <c>TableAlias</c> BEFORE
	/// visiting the inner expression so references like <c>inner.X</c>
	/// resolve to <c>[inner].[X]</c>, not the outer alias <c>[e].[X]</c>.
	/// </summary>
	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task ContainsSubqueryWithGroupBy_ResolvesInnerAlias(string provider)
	{
		SetUp(provider);
		var i1 = await InsertItem("Match1", priority: 1);
		var i2 = await InsertItem("Match2", priority: 2);
		var i3 = await InsertItem("NoMatch", priority: 3);
		var cat = await InsertCategory("CatA");
		await InsertItemCategory(i1, cat);
		await InsertItemCategory(i2, cat);

		await ClearCache();

		var items = Query<TestItem>();
		var itemCategories = Query<TestItemCategory>();
		var catIds = new[] { cat.Id };

		// Sub-query with Where + GroupBy must resolve `pgp.X` to `[pgp]`
		// alias, not the outer `[e]`.
		var query = items.Where(e =>
		(
			from pgp in itemCategories
			where catIds.Contains(pgp.Category.Id)
			group pgp by pgp.Item into g
			where g.Count() > 0
			select g.Key.Id
		).Contains(e.Id));

		var rows = await query.OrderBy(t => t.Id).ToArrayAsyncEx(CancellationToken);

		rows.Length.AssertEqual(2);
	}

	#endregion

	#region Audit regression: translator helpers

	private static IQueryable<T> CreateAuditQueryable<T>()
		=> new DefaultQueryable<T>(new DefaultQueryProvider<T>(new ThrowingQueryContext()), null);

	/// <summary>
	/// Minimal <see cref="IQueryContext"/> that throws on every terminal — the
	/// translation tests only inspect generated SQL/parameters, never execute.
	/// </summary>
	private sealed class ThrowingQueryContext : IQueryContext
	{
		IEnumerable<TResult> IQueryContext.ExecuteEnum<TSource, TResult>(Expression expression)
			=> throw new NotSupportedException();

		IAsyncEnumerable<TResult> IQueryContext.ExecuteEnumAsync<TSource, TResult>(Expression expression)
			=> throw new NotSupportedException();

		ValueTask IQueryContext.ExecuteAsync<TSource>(Expression expression)
			=> throw new NotSupportedException();

		TResult IQueryContext.ExecuteResult<TSource, TResult>(Expression expression)
			=> throw new NotSupportedException();

		ValueTask<TResult> IQueryContext.ExecuteResultAsync<TSource, TResult>(Expression expression)
			=> throw new NotSupportedException();
	}

	private static (string sql, IDictionary<string, (Type, object)> parameters) TranslateAudit<TSource>(IQueryable queryable)
	{
		var meta = SchemaRegistry.Get(typeof(TSource));
		var translator = new ExpressionQueryTranslator(meta);
		var query = translator.GenerateSql(queryable.Expression);
		var parameters = translator.Parameters;
		return (query.Render(SqlServerDialect.Instance), parameters);
	}

	#endregion

	#region Audit regression: LINQ translation

	/// <summary>
	/// Regression test for <c>SqlFunctions.IfNull</c> translation: ensures
	/// <c>.IfNull(x, def)</c> emits the dialect's coalesce function (SqlServer
	/// <c>isnull</c>), like the neighbouring <c>IsNull</c> function, so the default is
	/// substituted for NULL. (Was: <c>IfNullVisitor</c> emitted <c>nullif(...)</c> — the
	/// semantic opposite of COALESCE; Data.ORM\Sql\Expression\MethodVisitors.cs.)
	/// </summary>
	[TestMethod]
	public void IfNull_TranslatesToCoalesce_NotNullIf()
	{
		var items = CreateAuditQueryable<TestItem>();

		var query = items.Where(i => i.Name.IfNull("default") == "x");

		var (sql, _) = TranslateAudit<TestItem>(query);

		// SqlServerDialect.IsNullFunction == "isnull" (its COALESCE form).
		sql.ContainsIgnoreCase("isnull(").AssertTrue($"IfNull must emit the dialect coalesce function, got: {sql}");
		sql.ContainsIgnoreCase("nullif").AssertFalse($"IfNull must NOT emit NULLIF (inverted semantics), got: {sql}");
	}

	/// <summary>
	/// Regression test for sub-query parameter merging: ensures every <c>@param</c>
	/// referenced by the generated SQL has a matching key in the parameter dictionary
	/// (and vice versa) when a parameterised GROUP BY key routes through
	/// <c>AddParamsFromSubquery</c>. (Was: the merge renamed sub-context parameters to
	/// <c>{key}_{n}</c> after the SQL had already been rendered against the original
	/// names, leaving orphan keys the SQL never bound; Data.ORM\Sql\Expression\Context.cs.)
	/// </summary>
	[TestMethod]
	public void GroupByParameterisedKey_SqlReferencesMatchEmittedParameters()
	{
		const string captured = "vip";

		var items = CreateAuditQueryable<TestItem>();

		// A new{...} GROUP BY key that captures a closure constant routes through
		// VisitConstant -> parameterisation -> AddParamsFromSubquery(change:true).
		var query = items
			.GroupBy(i => new { Flag = i.Name == captured })
			.Select(g => new { g.Key, Count = g.Count() });

		var (sql, parameters) = TranslateAudit<TestItem>(query);

		var referenced = ExtractParamRefs(sql);
		IsTrue(referenced.Count > 0, $"Expected at least one @param reference in SQL, got: {sql}");

		// Every emitted parameter must be referenced by the generated SQL. The earlier
		// defect renamed the sub-context parameter to `{key}_{n}` AFTER the SQL had been
		// rendered against the original name, leaving an orphan `p0_0` that no `@p0_0`
		// reference ever bound; the correct merge keeps the names in sync.
		foreach (var key in parameters.Keys)
		{
			referenced.Contains(key).AssertTrue(
				$"Parameter '{key}' is emitted but never referenced by the SQL " +
				$"(orphan from key rename); referenced: {string.Join(",", referenced)}; sql: {sql}");
		}

		// And every SQL reference must resolve to an emitted parameter.
		foreach (var name in referenced)
		{
			parameters.ContainsKey(name).AssertTrue(
				$"SQL references @{name} but the parameter dictionary has no such key " +
				$"(keys: {string.Join(",", parameters.Keys)}); sql: {sql}");
		}
	}

	/// <summary>
	/// Collects the bare parameter names referenced in <paramref name="sql"/>
	/// (SqlServer prefix <c>@</c>), e.g. <c>@p0</c> -&gt; <c>p0</c>.
	/// </summary>
	private static List<string> ExtractParamRefs(string sql)
	{
		var names = new List<string>();
		for (var i = 0; i < sql.Length; i++)
		{
			if (sql[i] != '@')
				continue;

			var j = i + 1;
			while (j < sql.Length && (char.IsLetterOrDigit(sql[j]) || sql[j] == '_'))
				j++;

			if (j > i + 1)
				names.Add(sql.Substring(i + 1, j - i - 1));

			i = j - 1;
		}
		return names;
	}

	#endregion

	#region Audit regression: query provider routing

	/// <summary>
	/// Regression test for <c>DefaultQueryProvider.ResolveExecuteMethod</c> routing:
	/// ensures a synchronous scalar <c>string</c> terminal dispatches to the scalar
	/// <c>ExecuteResult&lt;TSource,string&gt;</c> path. (Was: any result type that merely
	/// implemented <c>IEnumerable&lt;&gt;</c> — including <c>string</c>, an
	/// <c>IEnumerable&lt;char&gt;</c> — was routed into the enumerable branch and threw
	/// InvalidOperationException; Data.ORM\DefaultQueryProvider.cs.)
	/// </summary>
	[TestMethod]
	public void Execute_ScalarString_RoutesToExecuteResult_NotEnumerableBranch()
	{
		var ctx = new RecordingScalarContext { ResultValue = "the-name" };
		IQueryProvider provider = new DefaultQueryProvider<TestItem>(ctx);

		// A constant string expression stands in for a scalar terminal whose result
		// type is string; the routing decision is purely type-driven.
		var result = provider.Execute<string>(Expression.Constant("the-name"));

		AreEqual("the-name", result);
		ctx.ExecuteResultCalled.AssertTrue("Scalar string terminal must route to ExecuteResult, not the IEnumerable branch.");
	}

	/// <summary>
	/// Records whether the scalar <c>ExecuteResult</c> path was taken and supplies a
	/// canned string result for it.
	/// </summary>
	private sealed class RecordingScalarContext : IQueryContext
	{
		public string ResultValue { get; set; }
		public bool ExecuteResultCalled { get; private set; }

		TResult IQueryContext.ExecuteResult<TSource, TResult>(Expression expression)
		{
			ExecuteResultCalled = true;
			return (TResult)(object)ResultValue;
		}

		IEnumerable<TResult> IQueryContext.ExecuteEnum<TSource, TResult>(Expression expression)
			=> throw new NotSupportedException();

		IAsyncEnumerable<TResult> IQueryContext.ExecuteEnumAsync<TSource, TResult>(Expression expression)
			=> throw new NotSupportedException();

		ValueTask IQueryContext.ExecuteAsync<TSource>(Expression expression)
			=> throw new NotSupportedException();

		ValueTask<TResult> IQueryContext.ExecuteResultAsync<TSource, TResult>(Expression expression)
			=> throw new NotSupportedException();
	}

	/// <summary>
	/// Regression test for <c>AnyAsyncEx</c>: ensures an empty value-type sequence (e.g.
	/// <c>Select(x =&gt; x.Id)</c>) yields <see langword="false"/>. (Was: implemented as
	/// <c>FirstOrDefaultAsyncEx(...) is not null</c>, which for a value type returned
	/// <c>default(long)</c> == 0 — boxed and never null — so an empty sequence wrongly
	/// reported <see langword="true"/>; Data.ORM\QueryableAsyncExtensions.cs.)
	/// </summary>
	[TestMethod]
	public async Task AnyAsyncEx_EmptyValueTypeSequence_ReturnsFalse()
	{
		// A queryable whose underlying provider yields an empty long sequence.
		var source = new EmptyAsyncQueryable<long>();

		var any = await source.AnyAsyncEx(CancellationToken);

		any.AssertFalse("AnyAsyncEx over an empty value-type sequence must be false, not true.");
	}

	/// <summary>
	/// Minimal queryable whose terminal evaluation returns an empty sequence both
	/// synchronously and asynchronously — drives <c>FirstOrDefaultAsync</c>/<c>FirstOrDefault</c>.
	/// </summary>
	private sealed class EmptyAsyncQueryable<T> : IOrderedQueryable<T>, IQueryProvider, IAsyncEnumerable<T>
	{
		public EmptyAsyncQueryable()
		{
			Expression = Expression.Constant(this);
		}

		private EmptyAsyncQueryable(Expression expression)
		{
			Expression = expression;
		}

		public Type ElementType => typeof(T);
		public Expression Expression { get; }
		public IQueryProvider Provider => this;

		public IEnumerator<T> GetEnumerator() => Enumerable.Empty<T>().GetEnumerator();
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

		public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
		{
			await Task.Yield();
			yield break;
		}

		IQueryable IQueryProvider.CreateQuery(Expression expression)
			=> new EmptyAsyncQueryable<T>(expression);

		IQueryable<TElement> IQueryProvider.CreateQuery<TElement>(Expression expression)
			=> new EmptyAsyncQueryable<TElement>(expression);

		object IQueryProvider.Execute(Expression expression)
			=> default(T);

		TResult IQueryProvider.Execute<TResult>(Expression expression)
		{
			// First/FirstOrDefault terminal over an empty sequence.
			return default;
		}
	}

	#endregion

	#region Audit regression: RelationManyList cache

	/// <summary>
	/// Regression test for <c>CopyToAsync(array, index)</c>: ensures all source entities
	/// are copied into the destination starting at <c>array[index]</c>, none skipped.
	/// (Was: <c>index</c> — a destination offset per the <c>ICollection.CopyTo</c>
	/// contract — was passed as the SOURCE <c>startIndex</c> to <c>GetRangeAsync</c>, so
	/// the first <c>index</c> source entities were dropped; Data.ORM\RelationManyList.cs.)
	/// </summary>
	[TestMethod]
	public async Task CopyToAsync_NonZeroIndex_CopiesAllEntitiesWithoutSkipping()
	{
		var list = new TestRelationManyList(new NullStorage())
		{
			GroupItems =
			[
				new TestItem { Id = 1, Name = "A" },
				new TestItem { Id = 2, Name = "B" },
				new TestItem { Id = 3, Name = "C" },
			],
		};

		var array = new TestItem[4];

		await list.CopyToAsync(array, 1, CancellationToken);

		// Destination offset 1 must hold the FULL source set [1,2,3]; slot 0 stays null.
		array[0].AssertNull();
		array[1].AssertNotNull("First source entity must not be skipped.");
		array[1].Id.AssertEqual(1L);
		array[2].AssertNotNull();
		array[2].Id.AssertEqual(2L);
		array[3].AssertNotNull();
		array[3].Id.AssertEqual(3L);
	}

	/// <summary>
	/// Regression test for <c>AddAsync</c> on a bulk-loaded list: ensures adding an entity
	/// whose id is already cached does not throw — the cache entry is replaced/kept and
	/// the add succeeds. (Was: <c>AddAsync</c> called <c>dict.Add(id, item)</c>, which
	/// threw ArgumentException when a concurrent or pre-populated cache already held the
	/// id, despite the row having been saved; Data.ORM\RelationManyList.cs.)
	/// </summary>
	[TestMethod]
	public async Task AddAsync_BulkLoad_IdAlreadyCached_DoesNotThrow()
	{
		var list = new TestRelationManyList(new NullStorage())
		{
			BulkLoad = true,
			// Bulk cache is initialised with id 7 already present.
			GroupItems = [new TestItem { Id = 7, Name = "existing" }],
		};

		// Force bulk init so the cache dictionary already contains id 7.
		await list.GetRangeAsync(0, long.MaxValue, false, null, ListSortDirection.Ascending, CancellationToken);

		// OnAdd echoes the same id back; the production code then re-adds it to the cache.
		var added = await list.AddAsync(new TestItem { Id = 7, Name = "again" }, CancellationToken);

		added.AssertNotNull();
		added.Id.AssertEqual(7L);
	}

	/// <summary>
	/// Regression test for <c>CountAsync(deleted:true)</c> on a bulk-load list: ensures
	/// the deleted count comes from storage (here, the configured storage count) rather
	/// than the non-deleted bulk cache. (Was: it warmed the cache via
	/// <c>GetRangeAsync(..., deleted:true, ...)</c>, which never populates the dictionary,
	/// then returned the empty <c>dict.Count</c> of 0; Data.ORM\RelationManyList.cs.)
	/// </summary>
	[TestMethod]
	public async Task CountAsync_BulkLoad_Deleted_ReturnsStorageCount_NotEmptyCache()
	{
		var list = new TestRelationManyList(new NullStorage())
		{
			BulkLoad = true,
			GetCountResult = 5,
			// Non-deleted view is empty so the bulk cache, if (wrongly) consulted, is 0.
			GroupItems = [],
		};

		var deletedCount = await list.CountAsync(true, CancellationToken);

		deletedCount.AssertEqual(5);
	}

	/// <summary>
	/// Regression test for <c>RemoveAsync</c> count maintenance: ensures the cached
	/// <c>_count</c> is decremented only when the storage delete actually succeeded.
	/// (Was: it decremented unconditionally even when <c>OnRemove</c> returned
	/// <see langword="false"/>, so with <c>CacheCount</c> on a no-op delete skewed the
	/// count downward and could drive it negative; Data.ORM\RelationManyList.cs.)
	/// </summary>
	[TestMethod]
	public async Task RemoveAsync_StorageDeleteFailed_DoesNotDecrementCachedCount()
	{
		var list = new RemoveFailsList(new NullStorage())
		{
			CacheCount = true,
			GetCountResult = 3,
		};

		// Prime the cached count to 3.
		(await list.CountAsync(CancellationToken)).AssertEqual(3);

		// OnRemove returns false (no rows deleted) — count must stay at 3.
		var removed = await list.RemoveAsync(new TestItem { Id = 999 }, CancellationToken);
		removed.AssertFalse();

		(await list.CountAsync(CancellationToken)).AssertEqual(3);
	}

	/// <summary>
	/// <see cref="TestRelationManyList"/> variant whose <c>OnRemove</c> reports a failed
	/// storage delete (no rows affected).
	/// </summary>
	private sealed class RemoveFailsList(IStorage storage) : RelationManyList<TestItem, long>(storage)
	{
		protected override ValueTask<TestItem> OnAdd(TestItem entity, CancellationToken cancellationToken)
			=> new(entity);

		protected override ValueTask<TestItem> OnUpdate(TestItem entity, CancellationToken cancellationToken)
			=> new(entity);

		protected override ValueTask<bool> OnRemove(TestItem entity, CancellationToken cancellationToken)
			=> new(false);

		protected override ValueTask OnClear(CancellationToken cancellationToken)
			=> default;

		protected override ValueTask<long> OnGetCount(bool deleted, CancellationToken cancellationToken)
			=> new(GetCountResult);

		protected override ValueTask<TestItem[]> OnGetGroup(long startIndex, long count, bool deleted, string orderBy, ListSortDirection direction, CancellationToken cancellationToken)
			=> new(Array.Empty<TestItem>());

		public override ValueTask<bool> ContainsAsync(TestItem item, CancellationToken cancellationToken)
			=> new(false);

		protected override ValueTask<bool> IsSaved(TestItem item, CancellationToken cancellationToken)
			=> new(item.Id > 0);

		public long GetCountResult { get; set; }
	}

	#endregion

	#region Audit regression: EntityCacheStore (internal, reached via reflection)

	private static EntityCacheStore CreateCacheStore()
		=> new();

	private static (Type, string, object) CacheKey(string name)
		=> (typeof(TestItem), name, (object)name);

	private static void StoreAdd(EntityCacheStore store, (Type, string, object) key, object entity, bool complete)
		=> store.Entries[key] = (entity, complete);

	private static bool StoreContains(EntityCacheStore store, (Type, string, object) key)
		=> store.Entries.ContainsKey(key);

	private static void StoreTouch(EntityCacheStore store, (Type, string, object) key)
		=> store.Touch(key);

	private static void StoreSetTimeout(EntityCacheStore store, TimeSpan value)
		=> store.Timeout = value;

	private static void StoreSetMaxEntries(EntityCacheStore store, int value)
		=> store.MaxEntries = value;

	private static Task StoreTrimExpired(EntityCacheStore store, CancellationToken token)
		=> store.TrimExpiredAsync(token).AsTask();

	/// <summary>
	/// Regression test for <c>EntityCacheStore.Touch</c> size-bound eviction: ensures
	/// incomplete (complete == false) entries are never chosen as the LRU victim. (Was:
	/// the LRU eviction over <c>MaxEntries</c> ignored the <c>complete</c> flag and could
	/// drop an incomplete entry that an active BulkScope still depended on, so the
	/// subsequent hydration read threw KeyNotFoundException; Data.ORM\EntityCacheStore.cs.)
	/// </summary>
	[TestMethod]
	public void Touch_DoesNotEvictIncompleteEntries()
	{
		var store = CreateCacheStore();
		StoreSetMaxEntries(store, 1);

		var incomplete = CacheKey("pending");
		var complete = CacheKey("loaded");

		// The incomplete entry is touched first (becomes the LRU tail).
		StoreAdd(store, incomplete, new TestItem { Id = 1 }, complete: false);
		StoreTouch(store, incomplete);

		// A second, complete entry pushes the count over MaxEntries == 1.
		StoreAdd(store, complete, new TestItem { Id = 2 }, complete: true);
		StoreTouch(store, complete);

		StoreContains(store, incomplete).AssertTrue(
			"Incomplete cache entry (still needed by a BulkScope) must not be LRU-evicted.");
	}

	/// <summary>
	/// Regression test for <c>EntityCacheStore.Touch</c> timestamping: ensures an entry
	/// cached before the timeout was lowered at runtime is still TTL-evictable by a
	/// subsequent <c>TrimExpiredAsync</c>. (Was: <c>Touch</c> recorded a timestamp only
	/// while <c>Timeout != TimeSpan.MaxValue</c>, so earlier entries carried none and the
	/// trim — which enumerates only timestamped entries — never reached them;
	/// Data.ORM\EntityCacheStore.cs.)
	/// </summary>
	[TestMethod]
	public async Task TrimExpired_EvictsEntriesCachedBeforeTimeoutWasLowered()
	{
		var store = CreateCacheStore();

		// Cached while TTL is disabled: Touch records no timestamp.
		var early = CacheKey("early");
		StoreAdd(store, early, new TestItem { Id = 1 }, complete: true);
		StoreTouch(store, early);

		// Operator lowers the TTL at runtime.
		StoreSetTimeout(store, TimeSpan.FromMilliseconds(1));

		// Give the (already old) entry time to exceed the new TTL.
		await Task.Delay(30, CancellationToken);

		await StoreTrimExpired(store, CancellationToken);

		StoreContains(store, early).AssertFalse(
			"An entry cached before the timeout was lowered must still be TTL-evictable.");
	}

	#endregion

	#region Audit regression: pagination SQL cache

	/// <summary>
	/// Regression test for paginated SELECT rendering: ensures pagination is
	/// parameterised, so the SQL text (the command-cache key) is the same regardless of
	/// the page offset. (Was: <c>Query.CreateSelect</c> inlined the offset/limit as SQL
	/// literals, so every distinct page produced a distinct SQL string that was cached
	/// forever as its own DatabaseCommand — an unbounded leak while paging large tables;
	/// Data.ORM\Database.cs.)
	/// </summary>
	[TestMethod]
	public void Pagination_RendersOffsetIndependentSql_SoCommandCacheIsBounded()
	{
		var dialect = SqlServerDialect.Instance;
		var orderBy = $"{dialect.QuoteIdentifier("Id")} asc";

		var page0 = Ecng.Data.Sql.Query.CreateSelect("Ecng_TestItem", null, orderBy, 0, 20).Render(dialect);
		var page1 = Ecng.Data.Sql.Query.CreateSelect("Ecng_TestItem", null, orderBy, 20, 20).Render(dialect);
		var page2 = Ecng.Data.Sql.Query.CreateSelect("Ecng_TestItem", null, orderBy, 40, 20).Render(dialect);

		AreEqual(page0, page1,
			$"Paging must not inline the offset as a literal — otherwise every page is a new cache entry.\npage0: {page0}\npage1: {page1}");
		AreEqual(page1, page2,
			$"Paging must not inline the offset as a literal.\npage1: {page1}\npage2: {page2}");
	}

	#endregion

	#region Audit regression: Database.CreateConnectionAsync leak

	/// <summary>
	/// Regression test for <c>Database.CreateConnectionAsync</c>: ensures that when
	/// <c>OpenAsync</c> faults (server down, bad credentials, timeout) the freshly created
	/// <see cref="DbConnection"/> is disposed before the exception propagates. (Was: the
	/// open had no try/catch, so a faulting connection was never disposed and accumulated
	/// under retry/health-check loops; Data.ORM\Database.cs.)
	/// </summary>
	[TestMethod]
	public async Task CreateConnectionAsync_OpenFails_DisposesConnection()
	{
		var conn = new ThrowOnOpenConnection();
		var factory = new SingleConnectionFactory(conn);
		using var db = new Database("LeakTest", "fake", factory, SqlServerDialect.Instance);

		await ThrowsAsync<InvalidOperationException>(
			async () => await db.CreateConnectionAsync(CancellationToken));

		conn.WasDisposed.AssertTrue("A connection whose OpenAsync throws must be disposed, not leaked.");
	}

	private sealed class SingleConnectionFactory(DbConnection connection) : DbProviderFactory
	{
		public override DbConnection CreateConnection() => connection;
	}

	/// <summary>
	/// <see cref="DbConnection"/> whose async open always faults; records whether it was
	/// disposed.
	/// </summary>
	private sealed class ThrowOnOpenConnection : DbConnection
	{
		public bool WasDisposed { get; private set; }

		public override string ConnectionString { get; set; } = string.Empty;
		public override string Database => "fake";
		public override string DataSource => "fake";
		public override string ServerVersion => "0.0";
		public override System.Data.ConnectionState State => System.Data.ConnectionState.Closed;

		public override void ChangeDatabase(string databaseName) { }
		public override void Close() { }
		public override void Open() => throw new InvalidOperationException("open failed");

		public override Task OpenAsync(CancellationToken cancellationToken)
			=> throw new InvalidOperationException("open failed");

		protected override DbTransaction BeginDbTransaction(System.Data.IsolationLevel isolationLevel)
			=> throw new NotSupportedException();

		protected override DbCommand CreateDbCommand()
			=> throw new NotSupportedException();

		protected override void Dispose(bool disposing)
		{
			WasDisposed = true;
			base.Dispose(disposing);
		}
	}

	#endregion

	#region Audit regression: Database integration (SQLite)

	/// <summary>
	/// Regression test for <c>Database.UpdateAsync</c> with a non-<c>Id</c> identity:
	/// ensures the row is actually updated (re-read shows the new value) when the identity
	/// column is declared via <c>[Identity]</c> on a differently named column. (Was: the
	/// WHERE-key value was filled from the entity only when the key column was literally
	/// named <c>"Id"</c>, so otherwise it bound to <c>DBNull</c> and the UPDATE matched no
	/// rows while reporting success — silent data loss; Data.ORM\Database.cs.)
	/// </summary>
	[TestMethod]
	public async Task UpdateAsync_NonIdIdentity_ActuallyUpdatesRow()
	{
		const string provider = DatabaseProviderRegistry.SQLite;
		DbTestHelper.SkipIfUnavailable(provider);

		var meta = SchemaRegistry.Get(typeof(AuditNonIdEntity));
		DbTestHelper.EnsureTable(provider, meta, autoIncrement: true);
		DbTestHelper.DeleteAll(provider, meta.TableName);

		using var db = DbTestHelper.CreateDatabase(provider);
		IStorage storage = db;

		var entity = new AuditNonIdEntity { Title = "before" };
		await storage.AddAsync(entity, CancellationToken);
		entity.EntityKey.AssertGreater(0L);

		entity.Title = "after";
		await storage.UpdateAsync(entity, CancellationToken);

		await storage.ClearCacheAsync(CancellationToken);

		var loaded = await storage.GetByIdAsync<long, AuditNonIdEntity>(entity.EntityKey, CancellationToken);
		loaded.AssertNotNull();
		loaded.Title.AssertEqual("after",
			"UpdateAsync must update an entity whose identity column is not named 'Id'.");

		DbTestHelper.DropTable(provider, meta.TableName);
	}

	/// <summary>
	/// Entity whose identity is declared via <see cref="IdentityAttribute"/> on a column
	/// NOT named <c>Id</c> — the exact shape that defeats the hardcoded <c>"Id"</c>
	/// fallback in <c>UpdateAsync</c>. <c>Save</c> deliberately does not write the
	/// identity (the repository convention) so the fallback branch is exercised.
	/// </summary>
	[Entity(Name = "Ecng_AuditNonId")]
	public class AuditNonIdEntity : IDbPersistable
	{
		[Identity]
		public long EntityKey { get; set; }

		public string Title { get; set; }

		object IDbPersistable.GetIdentity() => EntityKey;
		void IDbPersistable.SetIdentity(object id) => EntityKey = id.To<long>();

		public void Save(SettingsStorage storage)
		{
			storage.Set(nameof(Title), Title);
		}

		public ValueTask LoadAsync(SettingsStorage storage, IStorage db, CancellationToken cancellationToken)
		{
			Title = storage.GetValue<string>(nameof(Title));
			return default;
		}
	}

	#endregion
}

#endif
