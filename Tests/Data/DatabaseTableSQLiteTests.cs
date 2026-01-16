#if NET10_0_OR_GREATER
namespace Ecng.Tests.Data;

using System.Diagnostics;

using Ecng.Common;
using Ecng.Data;

using Microsoft.Data.Sqlite;

/// <summary>
/// SQLite-specific tests to verify cross-database compatibility of the ADO provider.
/// </summary>
[TestClass]
[TestCategory("Integration")]
[TestCategory("SQLite")]
[DoNotParallelize]
public class DatabaseTableSQLiteTests : BaseTestClass
{
	private const string _testTableName = "ecng_sqlite_test";
	private static string _testDbPath;

	[ClassInitialize]
	public static void ClassInit(TestContext context)
	{
		// Register SQLite provider
		DatabaseProviderRegistry.Register(DatabaseProviderRegistry.SQLite, SqliteFactory.Instance);

		// Create test database file in temp folder
		_testDbPath = Path.Combine(Path.GetTempPath(), "ecng_test.db");

		// Clean up any existing test database
		if (File.Exists(_testDbPath))
			File.Delete(_testDbPath);
	}

	[ClassCleanup]
	public static void ClassCleanup()
	{
		// Clean up test database
		if (File.Exists(_testDbPath))
		{
			try { File.Delete(_testDbPath); }
			catch { /* ignore cleanup errors */ }
		}
	}

	private static DatabaseConnectionPair GetSQLiteConnectionPair(bool inMemory = false)
	{
		var connStr = inMemory
			? "Data Source=:memory:"
			: $"Data Source={_testDbPath}";

		return new()
		{
			Provider = DatabaseProviderRegistry.SQLite,
			ConnectionString = connStr,
		};
	}

	private static IDictionary<string, object> ToDict(int id, string name)
		=> new Dictionary<string, object> { ["Id"] = id, ["Name"] = name };

	private static IDictionary<string, object> ToDict(int id, string name, decimal value)
		=> new Dictionary<string, object> { ["Id"] = id, ["Name"] = name, ["Value"] = value };

	/// <summary>
	/// Creates a table with PRIMARY KEY on Id column.
	/// Required for SQLite UPSERT (ON CONFLICT) to work.
	/// </summary>
	private static async Task CreateTableWithPrimaryKeyAsync(string tableName)
	{
		using var conn = new SqliteConnection($"Data Source={_testDbPath}");
		await conn.OpenAsync();
		using var cmd = conn.CreateCommand();
		cmd.CommandText = $"CREATE TABLE IF NOT EXISTS \"{tableName}\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT)";
		await cmd.ExecuteNonQueryAsync();
	}

	/// <summary>
	/// Drops a table by name.
	/// </summary>
	private static async Task DropTableAsync(string tableName)
	{
		using var conn = new SqliteConnection($"Data Source={_testDbPath}");
		await conn.OpenAsync();
		using var cmd = conn.CreateCommand();
		cmd.CommandText = $"DROP TABLE IF EXISTS \"{tableName}\"";
		await cmd.ExecuteNonQueryAsync();
	}

	#region DDL Tests - SQL Server specific issues

	/// <summary>
	/// Tests CREATE TABLE with SQLite.
	/// </summary>
	[TestMethod]
	public async Task SQLite_CreateTable_Success()
	{
		var provider = AdoDatabaseProvider.Instance;
		using var connection = provider.CreateConnection(GetSQLiteConnectionPair());
		var table = provider.GetTable(connection, _testTableName);

		// Drop if exists
		await table.DropAsync(CancellationToken);

		// Create table - this will fail for ADO/Dapper with SQL Server syntax
		var columns = new Dictionary<string, Type>
		{
			["Id"] = typeof(int),
			["Name"] = typeof(string),
			["Value"] = typeof(decimal),
			["CreatedAt"] = typeof(DateTime),
		};
		await table.CreateAsync(columns, CancellationToken);

		// Verify table was created by inserting and selecting
		await table.InsertAsync(ToDict(1, "Test", 10.5m), CancellationToken);
		var results = await table.SelectAsync(null, null, null, null, CancellationToken);
		results.Count().AssertEqual(1);

		// Cleanup
		await table.DropAsync(CancellationToken);
	}

	/// <summary>
	/// Tests DROP TABLE with SQLite.
	/// </summary>
	[TestMethod]
	public async Task SQLite_DropTable_Success()
	{
		var provider = AdoDatabaseProvider.Instance;
		using var connection = provider.CreateConnection(GetSQLiteConnectionPair());
		var table = provider.GetTable(connection, _testTableName);

		// Create table first
		var columns = new Dictionary<string, Type>
		{
			["Id"] = typeof(int),
			["Name"] = typeof(string),
		};
		await table.CreateAsync(columns, CancellationToken);

		// Drop table - should work
		await table.DropAsync(CancellationToken);

		// Drop again - should not throw (idempotent)
		await table.DropAsync(CancellationToken);
	}

	#endregion

	#region Pagination Tests - OFFSET/FETCH vs LIMIT/OFFSET

	/// <summary>
	/// Tests pagination with SQLite.
	/// </summary>
	[TestMethod]
	public async Task SQLite_Pagination_Success()
	{
		var provider = AdoDatabaseProvider.Instance;
		using var connection = provider.CreateConnection(GetSQLiteConnectionPair());
		var table = provider.GetTable(connection, _testTableName);

		// Setup
		await table.DropAsync(CancellationToken);
		await table.CreateAsync(new Dictionary<string, Type>
		{
			["Id"] = typeof(int),
			["Name"] = typeof(string),
		}, CancellationToken);

		// Insert test data
		var rows = Enumerable.Range(1, 20)
			.Select(i => ToDict(i, $"Item {i}"))
			.ToList();
		await table.BulkInsertAsync(rows, CancellationToken);

		// Select with pagination (skip 5, take 10)
		var orderBy = new[] { new OrderByCondition("Id") };
		var results = await table.SelectAsync(null, orderBy, skip: 5, take: 10, CancellationToken);
		var list = results.ToList();

		list.Count.AssertEqual(10);
		Convert.ToInt32(list[0]["Id"]).AssertEqual(6);
		Convert.ToInt32(list[9]["Id"]).AssertEqual(15);

		// Cleanup
		await table.DropAsync(CancellationToken);
	}

	#endregion

	#region Upsert Tests - MERGE vs INSERT ON CONFLICT

	/// <summary>
	/// Tests UPSERT with SQLite.
	/// </summary>
	[TestMethod]
	public async Task SQLite_Upsert_Success()
	{
		var upsertTableName = "upsert_test";
		var provider = AdoDatabaseProvider.Instance;
		using var connection = provider.CreateConnection(GetSQLiteConnectionPair());
		var table = provider.GetTable(connection, upsertTableName);

		// Setup - create table with PRIMARY KEY using raw SQL (required for ON CONFLICT)
		await DropTableAsync(upsertTableName);
		await CreateTableWithPrimaryKeyAsync(upsertTableName);

		// Upsert - should insert
		await table.UpsertAsync(ToDict(1, "First"), ["Id"], CancellationToken);

		var results = await table.SelectAsync(null, null, null, null, CancellationToken);
		var list = results.ToList();
		list.Count.AssertEqual(1);
		list[0]["Name"].ToString().AssertEqual("First");

		// Upsert - should update
		await table.UpsertAsync(ToDict(1, "Updated"), ["Id"], CancellationToken);

		results = await table.SelectAsync(null, null, null, null, CancellationToken);
		list = results.ToList();
		list.Count.AssertEqual(1);
		list[0]["Name"].ToString().AssertEqual("Updated");

		// Cleanup
		await table.DropAsync(CancellationToken);
	}

	#endregion

	#region Basic DML Tests

	[TestMethod]
	public async Task SQLite_InsertAndSelect_Success()
	{
		var provider = AdoDatabaseProvider.Instance;
		using var connection = provider.CreateConnection(GetSQLiteConnectionPair());
		var table = provider.GetTable(connection, _testTableName);

		// Setup
		await table.DropAsync(CancellationToken);
		await table.CreateAsync(new Dictionary<string, Type>
		{
			["Id"] = typeof(int),
			["Name"] = typeof(string),
		}, CancellationToken);

		// Insert
		await table.InsertAsync(ToDict(1, "Test"), CancellationToken);

		// Select
		var results = await table.SelectAsync(null, null, null, null, CancellationToken);
		var list = results.ToList();

		list.Count.AssertEqual(1);
		Convert.ToInt32(list[0]["Id"]).AssertEqual(1);
		list[0]["Name"].ToString().AssertEqual("Test");

		// Cleanup
		await table.DropAsync(CancellationToken);
	}

	[TestMethod]
	public async Task SQLite_BulkInsert_Success()
	{
		var provider = AdoDatabaseProvider.Instance;
		using var connection = provider.CreateConnection(GetSQLiteConnectionPair());
		var table = provider.GetTable(connection, _testTableName);

		// Setup
		await table.DropAsync(CancellationToken);
		await table.CreateAsync(new Dictionary<string, Type>
		{
			["Id"] = typeof(int),
			["Name"] = typeof(string),
		}, CancellationToken);

		// Bulk insert
		var rows = Enumerable.Range(1, 100)
			.Select(i => ToDict(i, $"Item {i}"))
			.ToList();
		await table.BulkInsertAsync(rows, CancellationToken);

		// Verify
		var results = await table.SelectAsync(null, null, null, null, CancellationToken);
		results.Count().AssertEqual(100);

		// Cleanup
		await table.DropAsync(CancellationToken);
	}

	[TestMethod]
	public async Task SQLite_SelectWithFilter_Success()
	{
		var provider = AdoDatabaseProvider.Instance;
		using var connection = provider.CreateConnection(GetSQLiteConnectionPair());
		var table = provider.GetTable(connection, _testTableName);

		// Setup
		await table.DropAsync(CancellationToken);
		await table.CreateAsync(new Dictionary<string, Type>
		{
			["Id"] = typeof(int),
			["Name"] = typeof(string),
		}, CancellationToken);

		// Insert test data
		var rows = Enumerable.Range(1, 10)
			.Select(i => ToDict(i, $"Item {i}"))
			.ToList();
		await table.BulkInsertAsync(rows, CancellationToken);

		// Select with filter
		var filters = new[] { new FilterCondition("Id", ComparisonOperator.Greater, 5) };
		var results = await table.SelectAsync(filters, null, null, null, CancellationToken);
		var list = results.ToList();

		list.Count.AssertEqual(5);
		list.All(row => Convert.ToInt32(row["Id"]) > 5).AssertEqual(true);

		// Cleanup
		await table.DropAsync(CancellationToken);
	}

	[TestMethod]
	public async Task SQLite_Update_Success()
	{
		var provider = AdoDatabaseProvider.Instance;
		using var connection = provider.CreateConnection(GetSQLiteConnectionPair());
		var table = provider.GetTable(connection, _testTableName);

		// Setup
		await table.DropAsync(CancellationToken);
		await table.CreateAsync(new Dictionary<string, Type>
		{
			["Id"] = typeof(int),
			["Name"] = typeof(string),
		}, CancellationToken);

		// Insert
		await table.InsertAsync(ToDict(1, "Original"), CancellationToken);

		// Update
		var filters = new[] { new FilterCondition("Id", ComparisonOperator.Equal, 1) };
		await table.UpdateAsync(new Dictionary<string, object> { ["Name"] = "Updated" }, filters, CancellationToken);

		// Verify
		var results = await table.SelectAsync(filters, null, null, null, CancellationToken);
		var list = results.ToList();

		list.Count.AssertEqual(1);
		list[0]["Name"].ToString().AssertEqual("Updated");

		// Cleanup
		await table.DropAsync(CancellationToken);
	}

	[TestMethod]
	public async Task SQLite_Delete_Success()
	{
		var provider = AdoDatabaseProvider.Instance;
		using var connection = provider.CreateConnection(GetSQLiteConnectionPair());
		var table = provider.GetTable(connection, _testTableName);

		// Setup
		await table.DropAsync(CancellationToken);
		await table.CreateAsync(new Dictionary<string, Type>
		{
			["Id"] = typeof(int),
			["Name"] = typeof(string),
		}, CancellationToken);

		// Insert test data
		var rows = Enumerable.Range(1, 10)
			.Select(i => ToDict(i, $"Item {i}"))
			.ToList();
		await table.BulkInsertAsync(rows, CancellationToken);

		// Delete items with Id > 5
		var filters = new[] { new FilterCondition("Id", ComparisonOperator.Greater, 5) };
		var deletedCount = await table.DeleteAsync(filters, CancellationToken);

		deletedCount.AssertEqual(5);

		// Verify
		var results = await table.SelectAsync(null, null, null, null, CancellationToken);
		results.Count().AssertEqual(5);

		// Cleanup
		await table.DropAsync(CancellationToken);
	}

	[TestMethod]
	public async Task SQLite_SelectWithLike_Success()
	{
		var provider = AdoDatabaseProvider.Instance;
		using var connection = provider.CreateConnection(GetSQLiteConnectionPair());
		var table = provider.GetTable(connection, _testTableName);

		// Setup
		await table.DropAsync(CancellationToken);
		await table.CreateAsync(new Dictionary<string, Type>
		{
			["Id"] = typeof(int),
			["Name"] = typeof(string),
		}, CancellationToken);

		// Insert test data
		await table.InsertAsync(ToDict(1, "Apple"), CancellationToken);
		await table.InsertAsync(ToDict(2, "Banana"), CancellationToken);
		await table.InsertAsync(ToDict(3, "Apricot"), CancellationToken);

		// Select with LIKE
		var filters = new[] { new FilterCondition("Name", ComparisonOperator.Like, "A%") };
		var results = await table.SelectAsync(filters, null, null, null, CancellationToken);
		var list = results.ToList();

		list.Count.AssertEqual(2);
		list.All(row => row["Name"].ToString().StartsWith("A")).AssertEqual(true);

		// Cleanup
		await table.DropAsync(CancellationToken);
	}

	[TestMethod]
	public async Task SQLite_SelectWithIn_Success()
	{
		var provider = AdoDatabaseProvider.Instance;
		using var connection = provider.CreateConnection(GetSQLiteConnectionPair());
		var table = provider.GetTable(connection, _testTableName);

		// Setup
		await table.DropAsync(CancellationToken);
		await table.CreateAsync(new Dictionary<string, Type>
		{
			["Id"] = typeof(int),
			["Name"] = typeof(string),
		}, CancellationToken);

		// Insert test data
		var rows = Enumerable.Range(1, 10)
			.Select(i => ToDict(i, $"Item {i}"))
			.ToList();
		await table.BulkInsertAsync(rows, CancellationToken);

		// Select with IN
		var filters = new[] { new FilterCondition("Id", ComparisonOperator.In, new[] { 2, 4, 6 }) };
		var results = await table.SelectAsync(filters, null, null, null, CancellationToken);
		var list = results.ToList();

		list.Count.AssertEqual(3);
		list.Select(row => Convert.ToInt32(row["Id"])).OrderBy(x => x).ToArray().AssertEqual([2, 4, 6]);

		// Cleanup
		await table.DropAsync(CancellationToken);
	}

	#endregion

	#region Edge Cases from task.txt

	/// <summary>
	/// Tests IN operator with empty list.
	/// Should not produce "IN ()" which is SQL syntax error.
	/// </summary>
	[TestMethod]
	public async Task SQLite_SelectWithEmptyIn_ReturnsEmpty()
	{
		var provider = AdoDatabaseProvider.Instance;
		using var connection = provider.CreateConnection(GetSQLiteConnectionPair());
		var table = provider.GetTable(connection, _testTableName);

		// Setup
		await table.DropAsync(CancellationToken);
		await table.CreateAsync(new Dictionary<string, Type>
		{
			["Id"] = typeof(int),
			["Name"] = typeof(string),
		}, CancellationToken);

		// Insert test data
		await table.InsertAsync(ToDict(1, "Test"), CancellationToken);

		// Select with empty IN - should return empty, not throw
		var filters = new[] { new FilterCondition("Id", ComparisonOperator.In, Array.Empty<int>()) };
		var results = await table.SelectAsync(filters, null, null, null, CancellationToken);
		var list = results.ToList();

		list.Count.AssertEqual(0);

		// Cleanup
		await table.DropAsync(CancellationToken);
	}

	/// <summary>
	/// Tests TimeSpan storage and retrieval.
	/// Verifies consistent serialization policy (should use Ticks).
	/// </summary>
	[TestMethod]
	public async Task SQLite_TimeSpanStorage_Consistent()
	{
		var provider = AdoDatabaseProvider.Instance;
		using var connection = provider.CreateConnection(GetSQLiteConnectionPair());
		var table = provider.GetTable(connection, _testTableName);

		// Setup
		await table.DropAsync(CancellationToken);
		await table.CreateAsync(new Dictionary<string, Type>
		{
			["Id"] = typeof(int),
			["Duration"] = typeof(TimeSpan),
		}, CancellationToken);

		// Insert with TimeSpan
		var duration = TimeSpan.FromHours(2) + TimeSpan.FromMinutes(30) + TimeSpan.FromSeconds(15);
		await table.InsertAsync(new Dictionary<string, object>
		{
			["Id"] = 1,
			["Duration"] = duration,
		}, CancellationToken);

		// Select and verify
		var results = await table.SelectAsync(null, null, null, null, CancellationToken);
		var list = results.ToList();

		list.Count.AssertEqual(1);

		// Duration should be stored as ticks (BIGINT) and retrievable
		var storedDuration = list[0]["Duration"];
		if (storedDuration is long ticks)
		{
			new TimeSpan(ticks).AssertEqual(duration);
		}
		else if (storedDuration is TimeSpan ts)
		{
			ts.AssertEqual(duration);
		}
		else
		{
			Assert.Fail($"Unexpected Duration type: {storedDuration?.GetType()?.Name ?? "null"}");
		}

		// Cleanup
		await table.DropAsync(CancellationToken);
	}

	/// <summary>
	/// Tests large bulk insert performance with SQLite.
	/// </summary>
	[TestMethod]
	public async Task SQLite_BulkInsert_LargeDataset_Success()
	{
		var provider = AdoDatabaseProvider.Instance;
		using var connection = provider.CreateConnection(GetSQLiteConnectionPair());
		var table = provider.GetTable(connection, _testTableName);

		// Setup
		await table.DropAsync(CancellationToken);
		await table.CreateAsync(new Dictionary<string, Type>
		{
			["Id"] = typeof(int),
			["Name"] = typeof(string),
			["Value"] = typeof(decimal),
		}, CancellationToken);

		// Bulk insert 5000 items
		var rows = Enumerable.Range(1, 5000)
			.Select(i => ToDict(i, $"Bulk Item {i}", i * 0.01m))
			.ToList();

		var sw = Stopwatch.StartNew();
		await table.BulkInsertAsync(rows, CancellationToken);
		sw.Stop();

		Console.WriteLine($"SQLite ADO: Inserted 5000 rows in {sw.ElapsedMilliseconds}ms");

		// Verify count
		var results = await table.SelectAsync(null, null, null, null, CancellationToken);
		results.Count().AssertEqual(5000);

		// Cleanup
		await table.DropAsync(CancellationToken);
	}

	#endregion
}
#endif
