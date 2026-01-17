#if NET10_0_OR_GREATER
namespace Ecng.Tests.Data;

using System.Diagnostics;

using Ecng.Common;
using Ecng.Data;

using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;

[TestClass]
[TestCategory("Integration")]
[DoNotParallelize]
public class DatabaseTableIntegrationTests : BaseTestClass
{
	private const string _testTableName = "ecng_table_test";
	private static string _sqliteDbPath;

	[ClassInitialize]
	public static void ClassInit(TestContext context)
	{
		DatabaseProviderRegistry.Register(DatabaseProviderRegistry.SqlServer, SqlClientFactory.Instance);
		DatabaseProviderRegistry.Register(DatabaseProviderRegistry.SQLite, SqliteFactory.Instance);

		_sqliteDbPath = Path.Combine(Path.GetTempPath(), "ecng_integration_test.db");
		if (File.Exists(_sqliteDbPath))
			File.Delete(_sqliteDbPath);
	}

	[ClassCleanup]
	public static void ClassCleanup()
	{
		if (File.Exists(_sqliteDbPath))
		{
			try { File.Delete(_sqliteDbPath); }
			catch { /* ignore */ }
		}
	}

	private static DatabaseConnectionPair GetConnectionPair(string provider)
	{
		return provider switch
		{
			DatabaseProviderRegistry.SqlServer => new()
			{
				Provider = DatabaseProviderRegistry.SqlServer,
				ConnectionString = GetSecret("DB_CONNECTION_STRING"),
			},
			DatabaseProviderRegistry.SQLite => new()
			{
				Provider = DatabaseProviderRegistry.SQLite,
				ConnectionString = $"Data Source={_sqliteDbPath}",
			},
			_ => throw new ArgumentException($"Unknown provider: {provider}"),
		};
	}

	private static DatabaseConnectionPair GetSqlServerConnectionPair()
		=> GetConnectionPair(DatabaseProviderRegistry.SqlServer);

	private static IDictionary<string, object> ToDict(int id, string name)
		=> new Dictionary<string, object> { ["Id"] = id, ["Name"] = name };

	private static IDictionary<string, object> ToDict(int id, string name, decimal value)
		=> new Dictionary<string, object> { ["Id"] = id, ["Name"] = name, ["Value"] = value };

	#region DDL Tests

	[TestMethod]
	public async Task Table_CreateAndDrop_Success()
	{
		var provider = AdoDatabaseProvider.Instance;
		using var connection = provider.CreateConnection(GetSqlServerConnectionPair());
		var table = provider.GetTable(connection, _testTableName);

		// Drop if exists
		await table.DropAsync(CancellationToken);

		// Create
		var columns = new Dictionary<string, Type>
		{
			["Id"] = typeof(int),
			["Name"] = typeof(string),
			["Value"] = typeof(decimal),
			["CreatedAt"] = typeof(DateTime),
		};
		await table.CreateAsync(columns, CancellationToken);

		// Drop
		await table.DropAsync(CancellationToken);
	}

	#endregion

	#region DML Tests

	[TestMethod]
	public async Task Table_InsertAndSelect_Success()
	{
		var provider = AdoDatabaseProvider.Instance;
		using var connection = provider.CreateConnection(GetSqlServerConnectionPair());
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
	public async Task Table_BulkInsert_Success()
	{
		var provider = AdoDatabaseProvider.Instance;
		using var connection = provider.CreateConnection(GetSqlServerConnectionPair());
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
	public async Task Table_BulkInsert_LargeDataset_Success()
	{
		var provider = AdoDatabaseProvider.Instance;
		using var connection = provider.CreateConnection(GetSqlServerConnectionPair());
		var table = provider.GetTable(connection, _testTableName);

		// Setup
		await table.DropAsync(CancellationToken);
		await table.CreateAsync(new Dictionary<string, Type>
		{
			["Id"] = typeof(int),
			["Name"] = typeof(string),
			["Value"] = typeof(decimal),
		}, CancellationToken);

		// Bulk insert 10000 items
		var rows = Enumerable.Range(1, 10000)
			.Select(i => ToDict(i, $"Bulk Item {i}", i * 0.01m))
			.ToList();

		var sw = Stopwatch.StartNew();
		await table.BulkInsertAsync(rows, CancellationToken);
		sw.Stop();

		Console.WriteLine($"ADO: Inserted 10000 rows in {sw.ElapsedMilliseconds}ms");

		// Verify count
		var results = await table.SelectAsync(null, null, null, null, CancellationToken);
		results.Count().AssertEqual(10000);

		// Cleanup
		await table.DropAsync(CancellationToken);
	}

	[TestMethod]
	public async Task Table_SelectWithFilter_Success()
	{
		var provider = AdoDatabaseProvider.Instance;
		using var connection = provider.CreateConnection(GetSqlServerConnectionPair());
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
	public async Task Table_SelectWithOrderBy_Success()
	{
		var provider = AdoDatabaseProvider.Instance;
		using var connection = provider.CreateConnection(GetSqlServerConnectionPair());
		var table = provider.GetTable(connection, _testTableName);

		// Setup
		await table.DropAsync(CancellationToken);
		await table.CreateAsync(new Dictionary<string, Type>
		{
			["Id"] = typeof(int),
			["Name"] = typeof(string),
		}, CancellationToken);

		// Insert test data
		var rows = Enumerable.Range(1, 5)
			.Select(i => ToDict(i, $"Item {i}"))
			.ToList();
		await table.BulkInsertAsync(rows, CancellationToken);

		// Select with order by descending
		var orderBy = new[] { new OrderByCondition("Id", descending: true) };
		var results = await table.SelectAsync(null, orderBy, null, null, CancellationToken);
		var list = results.ToList();

		Convert.ToInt32(list[0]["Id"]).AssertEqual(5);
		Convert.ToInt32(list[4]["Id"]).AssertEqual(1);

		// Cleanup
		await table.DropAsync(CancellationToken);
	}

	[TestMethod]
	public async Task Table_SelectWithPagination_Success()
	{
		var provider = AdoDatabaseProvider.Instance;
		using var connection = provider.CreateConnection(GetSqlServerConnectionPair());
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

		// Select with pagination (skip 5, take 10), ordered by Id
		var orderBy = new[] { new OrderByCondition("Id") };
		var results = await table.SelectAsync(null, orderBy, skip: 5, take: 10, CancellationToken);
		var list = results.ToList();

		list.Count.AssertEqual(10);
		Convert.ToInt32(list[0]["Id"]).AssertEqual(6);
		Convert.ToInt32(list[9]["Id"]).AssertEqual(15);

		// Cleanup
		await table.DropAsync(CancellationToken);
	}

	[TestMethod]
	public async Task Table_Update_Success()
	{
		var provider = AdoDatabaseProvider.Instance;
		using var connection = provider.CreateConnection(GetSqlServerConnectionPair());
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
	public async Task Table_Delete_Success()
	{
		var provider = AdoDatabaseProvider.Instance;
		using var connection = provider.CreateConnection(GetSqlServerConnectionPair());
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
		await table.DeleteAsync(filters, CancellationToken);

		// Verify
		var results = await table.SelectAsync(null, null, null, null, CancellationToken);
		var list = results.ToList();

		list.Count.AssertEqual(5);
		list.All(row => Convert.ToInt32(row["Id"]) <= 5).AssertEqual(true);

		// Cleanup
		await table.DropAsync(CancellationToken);
	}

	[TestMethod]
	public async Task Table_SelectWithInOperator_Success()
	{
		var provider = AdoDatabaseProvider.Instance;
		using var connection = provider.CreateConnection(GetSqlServerConnectionPair());
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

		// Select with IN operator
		var filters = new[] { new FilterCondition("Id", ComparisonOperator.In, new[] { 2, 4, 6, 8 }) };
		var results = await table.SelectAsync(filters, null, null, null, CancellationToken);
		var list = results.ToList();

		list.Count.AssertEqual(4);
		list.Select(row => Convert.ToInt32(row["Id"])).OrderBy(x => x).ToArray().AssertEqual([2, 4, 6, 8]);

		// Cleanup
		await table.DropAsync(CancellationToken);
	}

	[TestMethod]
	public async Task Table_Upsert_InsertAndUpdate_Success()
	{
		var provider = AdoDatabaseProvider.Instance;
		using var connection = provider.CreateConnection(GetSqlServerConnectionPair());
		var table = provider.GetTable(connection, _testTableName);

		// Setup
		await table.DropAsync(CancellationToken);
		await table.CreateAsync(new Dictionary<string, Type>
		{
			["Id"] = typeof(int),
			["Name"] = typeof(string),
		}, CancellationToken);

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

		// Upsert - should insert second row
		await table.UpsertAsync(ToDict(2, "Second"), ["Id"], CancellationToken);

		results = await table.SelectAsync(null, null, null, null, CancellationToken);
		list = results.ToList();
		list.Count.AssertEqual(2);

		// Cleanup
		await table.DropAsync(CancellationToken);
	}

	[TestMethod]
	public async Task Table_SelectWithLikeOperator_Success()
	{
		var provider = AdoDatabaseProvider.Instance;
		using var connection = provider.CreateConnection(GetSqlServerConnectionPair());
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
		await table.InsertAsync(ToDict(4, "Cherry"), CancellationToken);
		await table.InsertAsync(ToDict(5, "Avocado"), CancellationToken);

		// Select with LIKE operator - names starting with 'A'
		var filters = new[] { new FilterCondition("Name", ComparisonOperator.Like, "A%") };
		var results = await table.SelectAsync(filters, null, null, null, CancellationToken);
		var list = results.ToList();

		list.Count.AssertEqual(3);
		list.All(row => row["Name"].ToString().StartsWith("A")).AssertEqual(true);

		// Select with LIKE operator - names containing 'an'
		filters = [new FilterCondition("Name", ComparisonOperator.Like, "%an%")];
		results = await table.SelectAsync(filters, null, null, null, CancellationToken);
		list = results.ToList();

		list.Count.AssertEqual(1);
		list[0]["Name"].ToString().AssertEqual("Banana");

		// Cleanup
		await table.DropAsync(CancellationToken);
	}

	[TestMethod]
	public async Task Table_Delete_ReturnsDeletedCount()
	{
		var provider = AdoDatabaseProvider.Instance;
		using var connection = provider.CreateConnection(GetSqlServerConnectionPair());
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

		// Delete items with Id > 7 (should delete 3 rows: 8, 9, 10)
		var filters = new[] { new FilterCondition("Id", ComparisonOperator.Greater, 7) };
		var deletedCount = await table.DeleteAsync(filters, CancellationToken);

		deletedCount.AssertEqual(3);

		// Delete non-existing items (should return 0)
		filters = [new FilterCondition("Id", ComparisonOperator.Greater, 100)];
		deletedCount = await table.DeleteAsync(filters, CancellationToken);

		deletedCount.AssertEqual(0);

		// Cleanup
		await table.DropAsync(CancellationToken);
	}

	#endregion

	#region Validation Tests

	[TestMethod]
	public void Table_GetTable_NullConnection_Throws()
	{
		var provider = AdoDatabaseProvider.Instance;
		Throws<ArgumentNullException>(() => provider.GetTable(null, "test"));
	}

	[TestMethod]
	public void Table_GetTable_EmptyTableName_Throws()
	{
		var provider = AdoDatabaseProvider.Instance;
		using var connection = provider.CreateConnection(GetSqlServerConnectionPair());
		Throws<ArgumentNullException>(() => provider.GetTable(connection, ""));
	}

	#endregion

	#region Batch Size Tests

	/// <summary>
	/// Tests bulk insert with many columns to verify batch size calculation respects parameter limits.
	/// SQL Server: 2100 params, SQLite: 999 params.
	/// With 100 columns: SQL Server = 20 rows/batch, SQLite = 9 rows/batch.
	/// </summary>
	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Table_BulkInsert_ManyColumns_Success(string providerName)
	{
		var provider = AdoDatabaseProvider.Instance;
		using var connection = provider.CreateConnection(GetConnectionPair(providerName));
		var table = provider.GetTable(connection, _testTableName + "_manycols");

		// Setup - create table with 100 columns
		const int columnCount = 100;
		var columns = new Dictionary<string, Type> { ["Id"] = typeof(int) };
		for (var i = 1; i < columnCount; i++)
			columns[$"Col{i}"] = typeof(string);

		await table.DropAsync(CancellationToken);
		await table.CreateAsync(columns, CancellationToken);

		// Create 100 rows with all columns filled
		// This will test multiple batches due to parameter limits
		var rows = Enumerable.Range(1, 100)
			.Select(i =>
			{
				var row = new Dictionary<string, object> { ["Id"] = i };
				for (var c = 1; c < columnCount; c++)
					row[$"Col{c}"] = $"V{i}_{c}";
				return (IDictionary<string, object>)row;
			})
			.ToList();

		var sw = Stopwatch.StartNew();
		await table.BulkInsertAsync(rows, CancellationToken);
		sw.Stop();

		Console.WriteLine($"{providerName}: Inserted 100 rows with {columnCount} columns in {sw.ElapsedMilliseconds}ms");

		// Verify count
		var results = await table.SelectAsync(null, null, null, null, CancellationToken);
		results.Count().AssertEqual(100);

		// Verify data integrity
		var resultsList = results.ToList();
		Convert.ToInt32(resultsList[0]["Id"]).AssertEqual(1);
		resultsList[0]["Col1"].AssertEqual("V1_1");
		resultsList[0]["Col99"].AssertEqual("V1_99");

		// Cleanup
		await table.DropAsync(CancellationToken);
	}

	#endregion
}
#endif
