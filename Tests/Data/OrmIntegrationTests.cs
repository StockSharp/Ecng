namespace Ecng.Tests.Data;

using System.ComponentModel;

using Ecng.Common;
using Ecng.Data;
using Ecng.Serialization;
using Ecng.UnitTesting;

using Microsoft.Data.SqlClient;

[TestClass]
[TestCategory("Integration")]
[DoNotParallelize]
public class OrmIntegrationTests : BaseTestClass
{
	private static Database _db;
	private static string _connectionString;


	[ClassInitialize]
	public static void ClassInit(TestContext context)
	{
		_connectionString = TryGetSecret("DB_CONNECTION_STRING");

		if (_connectionString.IsEmpty())
			return;

		_db = new Database("ORM Test Database", _connectionString, SqlClientFactory.Instance, SqlServerDialect.Instance);
		_db.AllowDeleteAll = true;

		// Create test tables via raw DDL
		using var conn = new SqlConnection(_connectionString);
		conn.Open();

		Execute(conn, @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TestItem')
CREATE TABLE [TestItem] (
    Id bigint identity(1,1) PRIMARY KEY,
    [Name] nvarchar(200) NULL,
    [Priority] int NOT NULL DEFAULT 0,
    Price decimal(18,4) NOT NULL DEFAULT 0,
    CreatedAt datetime NOT NULL DEFAULT getdate(),
    IsActive bit NOT NULL DEFAULT 0,
    NullableValue int NULL
)");

		Execute(conn, @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TestCategory')
CREATE TABLE [TestCategory] (
    Id bigint identity(1,1) PRIMARY KEY,
    CategoryName nvarchar(200) NULL,
    [Description] nvarchar(max) NULL
)");

		Execute(conn, @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TestItemCategory')
CREATE TABLE [TestItemCategory] (
    Id bigint identity(1,1) PRIMARY KEY,
    Item bigint NOT NULL,
    Category bigint NOT NULL
)");
	}

	private static void Execute(SqlConnection conn, string sql)
	{
		using var cmd = new SqlCommand(sql, conn);
		cmd.ExecuteNonQuery();
	}

	private void EnsureDb()
	{
		if (_db is null)
			Inconclusive("DB_CONNECTION_STRING secret not configured.");
	}

	[TestInitialize]
	public void TestInit()
	{
		if (_db is null)
			return;

		using var conn = new SqlConnection(_connectionString);
		conn.Open();

		Execute(conn, "DELETE FROM [TestItemCategory]");
		Execute(conn, "DELETE FROM [TestItem]");
		Execute(conn, "DELETE FROM [TestCategory]");

		Storage.ClearCacheAsync(CancellationToken).AsTask().Wait();
	}

	[ClassCleanup]
	public static void ClassCleanup()
	{
		_db?.Dispose();
	}

	#region Helpers

	private IStorage Storage => _db;

	private static IQueryable<T> Query<T>()
		=> new DefaultQueryable<T>(new DefaultQueryProvider<T>(_db), null);

	private async Task ClearCache()
		=> await Storage.ClearCacheAsync(CancellationToken);

	private async Task<TestItem> InsertItem(string name = "Test", int priority = 1, decimal price = 9.99m, bool isActive = true, int? nullableValue = null)
	{
		var item = new TestItem
		{
			Name = name,
			Priority = priority,
			Price = price,
			CreatedAt = DateTime.UtcNow,
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

	private async Task<TestItemCategory> InsertItemCategory(TestItem item, TestCategory category)
	{
		var ic = new TestItemCategory
		{
			Item = item,
			Category = category,
		};
		return await Storage.AddAsync(ic, CancellationToken);
	}

	#endregion

	#region CRUD Tests

	[TestMethod]
	public async Task Crud_InsertAndReadById()
	{
		EnsureDb();
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
	public async Task Crud_Update()
	{
		EnsureDb();
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
	public async Task Crud_Delete()
	{
		EnsureDb();
		var item = await InsertItem("ToDelete");
		var result = await Storage.RemoveAsync(item, CancellationToken);
		result.AssertTrue();

		await ClearCache();

		var loaded = await Storage.GetByIdAsync<long, TestItem>(item.Id, CancellationToken);
		loaded.AssertNull();
	}

	[TestMethod]
	public async Task Crud_GetCount()
	{
		EnsureDb();
		await InsertItem("A");
		await InsertItem("B");
		await InsertItem("C");

		var count = await Storage.GetCountAsync<TestItem>(CancellationToken);
		count.AssertEqual(3);
	}

	[TestMethod]
	public async Task Crud_GetGroup_Paging()
	{
		EnsureDb();
		for (var i = 0; i < 10; i++)
			await InsertItem($"Item{i}", priority: i);

		var page = await Storage.GetGroupAsync<TestItem>(2, 3, false, "Id", ListSortDirection.Ascending, CancellationToken);
		page.Length.AssertEqual(3);
	}

	#endregion

	#region Where Tests

	[TestMethod]
	public async Task Where_Equality()
	{
		EnsureDb();
		await InsertItem("Alpha");
		await InsertItem("Beta");

		var result = await Query<TestItem>().Where(x => x.Name == "Alpha").FirstOrDefaultAsyncEx(CancellationToken);
		result.AssertNotNull();
		result.Name.AssertEqual("Alpha");
	}

	[TestMethod]
	public async Task Where_GreaterThan()
	{
		EnsureDb();
		await InsertItem("Low", priority: 1);
		await InsertItem("Mid", priority: 5);
		await InsertItem("High", priority: 10);

		var results = await Query<TestItem>().Where(x => x.Priority > 3).ToArrayAsyncEx(CancellationToken);
		results.Length.AssertEqual(2);
	}

	[TestMethod]
	public async Task Where_Boolean()
	{
		EnsureDb();
		await InsertItem("Active", isActive: true);
		await InsertItem("Inactive", isActive: false);

		var results = await Query<TestItem>().Where(x => x.IsActive).ToArrayAsyncEx(CancellationToken);
		results.Length.AssertEqual(1);
		results[0].Name.AssertEqual("Active");
	}

	[TestMethod]
	public async Task Where_Nullable()
	{
		EnsureDb();
		await InsertItem("WithValue", nullableValue: 42);
		await InsertItem("NullValue");

		var results = await Query<TestItem>().Where(x => x.NullableValue != null).ToArrayAsyncEx(CancellationToken);
		results.Length.AssertEqual(1);
		results[0].Name.AssertEqual("WithValue");
	}

	[TestMethod]
	public async Task Where_Contains_IN()
	{
		EnsureDb();
		var a = await InsertItem("A");
		var b = await InsertItem("B");
		await InsertItem("C");

		var ids = new[] { a.Id, b.Id };
		var results = await Query<TestItem>().Where(x => ids.Contains(x.Id)).ToArrayAsyncEx(CancellationToken);
		results.Length.AssertEqual(2);
	}

	[TestMethod]
	public async Task Where_Like()
	{
		EnsureDb();
		await InsertItem("Apple");
		await InsertItem("Banana");
		await InsertItem("Avocado");

		var results = await Query<TestItem>().Where(x => x.Name.StartsWith("A")).ToArrayAsyncEx(CancellationToken);
		results.Length.AssertEqual(2);
	}

	#endregion

	#region OrderBy Tests

	[TestMethod]
	public async Task OrderBy_Asc()
	{
		EnsureDb();
		await InsertItem("C", priority: 3);
		await InsertItem("A", priority: 1);
		await InsertItem("B", priority: 2);

		var results = await Query<TestItem>().OrderBy(x => x.Priority).ToArrayAsyncEx(CancellationToken);
		results[0].Name.AssertEqual("A");
		results[1].Name.AssertEqual("B");
		results[2].Name.AssertEqual("C");
	}

	[TestMethod]
	public async Task OrderBy_Desc()
	{
		EnsureDb();
		await InsertItem("C", priority: 3);
		await InsertItem("A", priority: 1);
		await InsertItem("B", priority: 2);

		var results = await Query<TestItem>().OrderByDescending(x => x.Priority).ToArrayAsyncEx(CancellationToken);
		results[0].Name.AssertEqual("C");
		results[1].Name.AssertEqual("B");
		results[2].Name.AssertEqual("A");
	}

	[TestMethod]
	public async Task OrderBy_ThenBy()
	{
		EnsureDb();
		await InsertItem("B2", priority: 1, price: 20);
		await InsertItem("B1", priority: 1, price: 10);
		await InsertItem("A1", priority: 0, price: 5);

		var results = await Query<TestItem>().OrderBy(x => x.Priority).ThenBy(x => x.Price).ToArrayAsyncEx(CancellationToken);
		results[0].Name.AssertEqual("A1");
		results[1].Name.AssertEqual("B1");
		results[2].Name.AssertEqual("B2");
	}

	[TestMethod]
	public async Task OrderBy_SkipTake()
	{
		EnsureDb();
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
	public async Task String_Contains()
	{
		EnsureDb();
		await InsertItem("Hello World");
		await InsertItem("Goodbye");

		var results = await Query<TestItem>().Where(x => x.Name.Contains("World")).ToArrayAsyncEx(CancellationToken);
		results.Length.AssertEqual(1);
		results[0].Name.AssertEqual("Hello World");
	}

	[TestMethod]
	public async Task String_StartsWith()
	{
		EnsureDb();
		await InsertItem("Apple");
		await InsertItem("Banana");

		var results = await Query<TestItem>().Where(x => x.Name.StartsWith("App")).ToArrayAsyncEx(CancellationToken);
		results.Length.AssertEqual(1);
	}

	[TestMethod]
	public async Task String_EndsWith()
	{
		EnsureDb();
		await InsertItem("Apple");
		await InsertItem("Pineapple");
		await InsertItem("Banana");

		var results = await Query<TestItem>().Where(x => x.Name.EndsWith("ple")).ToArrayAsyncEx(CancellationToken);
		results.Length.AssertEqual(2);
	}

	#endregion

	#region Aggregation Tests

	[TestMethod]
	public async Task Agg_Count()
	{
		EnsureDb();
		await InsertItem("A");
		await InsertItem("B");

		var count = await Query<TestItem>().CountAsyncEx(CancellationToken);
		count.AssertEqual(2);
	}

	[TestMethod]
	public async Task Agg_Any()
	{
		EnsureDb();
		var any = await Query<TestItem>().AnyAsyncEx(CancellationToken);
		any.AssertFalse();

		await InsertItem("A");
		await ClearCache();

		var any2 = await Query<TestItem>().AnyAsyncEx(CancellationToken);
		any2.AssertTrue();
	}

	[TestMethod]
	public async Task Agg_FirstOrDefault()
	{
		EnsureDb();
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
	public async Task Join_RelationSingle()
	{
		EnsureDb();
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
	public async Task Join_InnerJoin_LinqSyntax()
	{
		EnsureDb();
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
	public async Task Join_InnerJoin_WithWhere()
	{
		EnsureDb();
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
	public async Task Join_InnerJoin_TwoJoins()
	{
		EnsureDb();
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
	public async Task Join_LeftJoin_GroupJoin()
	{
		EnsureDb();
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

	#endregion

	#region Compound Where Tests

	[TestMethod]
	public async Task Where_And()
	{
		EnsureDb();
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
	public async Task Where_Or()
	{
		EnsureDb();
		await InsertItem("High", priority: 10, isActive: false);
		await InsertItem("Active", priority: 1, isActive: true);
		await InsertItem("Neither", priority: 1, isActive: false);

		var results = await Query<TestItem>()
			.Where(x => x.Priority > 5 || x.IsActive)
			.ToArrayAsyncEx(CancellationToken);

		results.Length.AssertEqual(2);
	}

	[TestMethod]
	public async Task Where_ComplexAndOr()
	{
		EnsureDb();
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
	public async Task Where_Chained()
	{
		EnsureDb();
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
	public async Task Where_NullableEqualsNull()
	{
		EnsureDb();
		await InsertItem("HasNull");
		await InsertItem("HasValue", nullableValue: 7);

		var results = await Query<TestItem>()
			.Where(x => x.NullableValue == null)
			.ToArrayAsyncEx(CancellationToken);

		results.Length.AssertEqual(1);
		results[0].Name.AssertEqual("HasNull");
	}

	[TestMethod]
	public async Task Where_NegatedBoolean()
	{
		EnsureDb();
		await InsertItem("Active", isActive: true);
		await InsertItem("Inactive", isActive: false);

		var results = await Query<TestItem>()
			.Where(x => !x.IsActive)
			.ToArrayAsyncEx(CancellationToken);

		results.Length.AssertEqual(1);
		results[0].Name.AssertEqual("Inactive");
	}

	[TestMethod]
	public async Task Where_Arithmetic_Add()
	{
		EnsureDb();
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
	public async Task DateTime_FilterByRange()
	{
		EnsureDb();
		await InsertItem("Recent");

		var results = await Query<TestItem>()
			.Where(x => x.CreatedAt > DateTime.UtcNow.AddDays(-1))
			.ToArrayAsyncEx(CancellationToken);

		results.Length.AssertEqual(1);
		results[0].Name.AssertEqual("Recent");
	}

	[TestMethod]
	public async Task DateTime_OrderByCreatedAt()
	{
		EnsureDb();
		await InsertItem("First");
		await InsertItem("Second");
		await InsertItem("Third");

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
	public async Task String_ToUpper()
	{
		EnsureDb();
		await InsertItem("hello");
		await InsertItem("WORLD");

		var results = await Query<TestItem>()
			.Where(x => x.Name.ToUpper() == "HELLO")
			.ToArrayAsyncEx(CancellationToken);

		results.Length.AssertEqual(1);
		results[0].Name.AssertEqual("hello");
	}

	[TestMethod]
	public async Task String_ToLower()
	{
		EnsureDb();
		await InsertItem("HELLO");
		await InsertItem("world");

		var results = await Query<TestItem>()
			.Where(x => x.Name.ToLower() == "hello")
			.ToArrayAsyncEx(CancellationToken);

		results.Length.AssertEqual(1);
		results[0].Name.AssertEqual("HELLO");
	}

	[TestMethod]
	public async Task String_Trim()
	{
		EnsureDb();
		await InsertItem("  padded  ");
		await InsertItem("clean");

		var results = await Query<TestItem>()
			.Where(x => x.Name.Trim() == "padded")
			.ToArrayAsyncEx(CancellationToken);

		results.Length.AssertEqual(1);
	}

	[TestMethod]
	public async Task String_Substring()
	{
		EnsureDb();
		await InsertItem("Hello World");
		await InsertItem("Goodbye");

		var results = await Query<TestItem>()
			.Where(x => x.Name.Substring(0, 5) == "Hello")
			.ToArrayAsyncEx(CancellationToken);

		results.Length.AssertEqual(1);
		results[0].Name.AssertEqual("Hello World");
	}

	[TestMethod]
	public async Task String_Replace()
	{
		EnsureDb();
		await InsertItem("foo-bar");
		await InsertItem("baz-qux");

		var results = await Query<TestItem>()
			.Where(x => x.Name.Replace("-", "_") == "foo_bar")
			.ToArrayAsyncEx(CancellationToken);

		results.Length.AssertEqual(1);
		results[0].Name.AssertEqual("foo-bar");
	}

	[TestMethod]
	public async Task String_IsNullOrEmpty()
	{
		EnsureDb();
		await InsertItem("NonEmpty");
		await InsertItem("");

		var results = await Query<TestItem>()
			.Where(x => !string.IsNullOrEmpty(x.Name))
			.ToArrayAsyncEx(CancellationToken);

		results.Length.AssertEqual(1);
		results[0].Name.AssertEqual("NonEmpty");
	}

	#endregion

	#region Coalesce and Conditional Tests

	[TestMethod]
	public async Task Coalesce_NullableValue()
	{
		EnsureDb();
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
	public async Task Distinct_Values()
	{
		EnsureDb();
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
	public async Task Agg_CountWithFilter()
	{
		EnsureDb();
		await InsertItem("Active1", isActive: true);
		await InsertItem("Active2", isActive: true);
		await InsertItem("Inactive", isActive: false);

		var count = await Query<TestItem>()
			.Where(x => x.IsActive)
			.CountAsyncEx(CancellationToken);

		count.AssertEqual(2);
	}

	[TestMethod]
	public async Task Agg_AnyWithFilter()
	{
		EnsureDb();
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
	public async Task Agg_FirstOrDefault_WithOrderAndFilter()
	{
		EnsureDb();
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
	public async Task Complex_WhereOrderBySkipTake()
	{
		EnsureDb();
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
	public async Task Complex_JoinWithOrderByAndTake()
	{
		EnsureDb();
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
	public async Task Complex_MultipleStringFilters()
	{
		EnsureDb();
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
	public async Task Complex_ContainsIN_WithOtherFilters()
	{
		EnsureDb();
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
	public async Task Complex_JoinFilterOnBothTables()
	{
		EnsureDb();
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
	public async Task Complex_DecimalComparison()
	{
		EnsureDb();
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
	public async Task Transaction_Commit()
	{
		EnsureDb();
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
	public async Task Transaction_Rollback()
	{
		EnsureDb();
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
}
