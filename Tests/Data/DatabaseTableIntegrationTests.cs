namespace Ecng.Tests.Data;

using Ecng.Data;
using Ecng.Data.Sql;

/// <summary>
/// Integration tests for database table operations.
/// Tests run with SQL Server, PostgreSQL, and SQLite providers via DataRow parameterization.
/// </summary>
[TestClass]
[TestCategory("Integration")]
[TestCategory("Database")]
[DoNotParallelize]
public class DatabaseTableIntegrationTests : BaseTestClass
{
	private const string _testTableName = "Ecng_TableTest";

	[ClassInitialize]
	public static void ClassInit(TestContext context)
	{
		DbTestHelper.RegisterAll();
	}

	[ClassCleanup]
	public static void ClassCleanup()
	{
		DbTestHelper.ClearSQLitePools();
	}

	private static IDictionary<string, object> ToDict(int id, string name)
		=> new Dictionary<string, object> { ["Id"] = id, ["Name"] = name };

	private static IDictionary<string, object> ToDict(int id, string name, decimal value)
		=> new Dictionary<string, object> { ["Id"] = id, ["Name"] = name, ["Value"] = value };

	/// <summary>
	/// Creates a table with PRIMARY KEY via raw SQL (needed for upsert).
	/// </summary>
	private static void CreateTableWithPrimaryKey(string provider, string tableName, Dictionary<string, Type> columns)
	{
		var dialect = DbTestHelper.GetDialect(provider);
		var sql = Query.CreateCreateTable(tableName, columns, primaryKeyColumns: ["Id"]).Render(dialect);
		DbTestHelper.ExecuteRaw(provider, sql);
	}

	/// <summary>
	/// Drops a table by name using raw SQL.
	/// </summary>
	private static void DropTableRaw(string provider, string tableName)
	{
		var dialect = DbTestHelper.GetDialect(provider);
		var quoted = dialect.QuoteIdentifier(tableName);
		DbTestHelper.ExecuteRaw(provider, $"DROP TABLE IF EXISTS {quoted}");
	}

	#region DDL Tests

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Table_CreateAndDrop_Success(string providerName)
	{
		DbTestHelper.SkipIfUnavailable(providerName);

		var provider = AdoDatabaseProvider.Instance;
		using var connection = provider.CreateConnection(DbTestHelper.TryGetConnectionPair(providerName));
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

		// Verify table was created by inserting and selecting
		await table.InsertAsync(ToDict(1, "Test", 10.5m), CancellationToken);
		var results = await table.SelectAsync(null, null, null, null, CancellationToken);
		results.Count().AssertEqual(1);

		// Drop
		await table.DropAsync(CancellationToken);

		// Drop again - should not throw (idempotent)
		await table.DropAsync(CancellationToken);
	}

	#endregion

	#region DML Tests

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Table_InsertAndSelect_Success(string providerName)
	{
		DbTestHelper.SkipIfUnavailable(providerName);

		var provider = AdoDatabaseProvider.Instance;
		using var connection = provider.CreateConnection(DbTestHelper.TryGetConnectionPair(providerName));
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
		list[0]["Id"].To<int>().AssertEqual(1);
		list[0]["Name"].ToString().AssertEqual("Test");

		// Cleanup
		await table.DropAsync(CancellationToken);
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Table_BulkInsert_Success(string providerName)
	{
		DbTestHelper.SkipIfUnavailable(providerName);

		var provider = AdoDatabaseProvider.Instance;
		using var connection = provider.CreateConnection(DbTestHelper.TryGetConnectionPair(providerName));
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
	[DataRow(DatabaseProviderRegistry.SqlServer, 10000)]
	[DataRow(DatabaseProviderRegistry.PostgreSql, 5000)]
	[DataRow(DatabaseProviderRegistry.SQLite, 5000)]
	public async Task Table_BulkInsert_LargeDataset_Success(string providerName, int rowCount)
	{
		DbTestHelper.SkipIfUnavailable(providerName);

		var provider = AdoDatabaseProvider.Instance;
		using var connection = provider.CreateConnection(DbTestHelper.TryGetConnectionPair(providerName));
		var table = provider.GetTable(connection, _testTableName);

		// Setup
		await table.DropAsync(CancellationToken);
		await table.CreateAsync(new Dictionary<string, Type>
		{
			["Id"] = typeof(int),
			["Name"] = typeof(string),
			["Value"] = typeof(decimal),
		}, CancellationToken);

		// Bulk insert
		var rows = Enumerable.Range(1, rowCount)
			.Select(i => ToDict(i, $"Bulk Item {i}", i * 0.01m))
			.ToList();

		var sw = Stopwatch.StartNew();
		await table.BulkInsertAsync(rows, CancellationToken);
		sw.Stop();

		Console.WriteLine($"{providerName}: Inserted {rowCount} rows in {sw.ElapsedMilliseconds}ms");

		// Verify count
		var results = await table.SelectAsync(null, null, null, null, CancellationToken);
		results.Count().AssertEqual(rowCount);

		// Cleanup
		await table.DropAsync(CancellationToken);
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Table_SelectWithFilter_Success(string providerName)
	{
		DbTestHelper.SkipIfUnavailable(providerName);

		var provider = AdoDatabaseProvider.Instance;
		using var connection = provider.CreateConnection(DbTestHelper.TryGetConnectionPair(providerName));
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
		list.All(row => row["Id"].To<int>() > 5).AssertEqual(true);

		// Cleanup
		await table.DropAsync(CancellationToken);
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Table_SelectWithOrderBy_Success(string providerName)
	{
		DbTestHelper.SkipIfUnavailable(providerName);

		var provider = AdoDatabaseProvider.Instance;
		using var connection = provider.CreateConnection(DbTestHelper.TryGetConnectionPair(providerName));
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
		var orderBy = new[] { new OrderByCondition("Id", Descending: true) };
		var results = await table.SelectAsync(null, orderBy, null, null, CancellationToken);
		var list = results.ToList();

		list[0]["Id"].To<int>().AssertEqual(5);
		list[4]["Id"].To<int>().AssertEqual(1);

		// Cleanup
		await table.DropAsync(CancellationToken);
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Table_SelectWithPagination_Success(string providerName)
	{
		DbTestHelper.SkipIfUnavailable(providerName);

		var provider = AdoDatabaseProvider.Instance;
		using var connection = provider.CreateConnection(DbTestHelper.TryGetConnectionPair(providerName));
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
		list[0]["Id"].To<int>().AssertEqual(6);
		list[9]["Id"].To<int>().AssertEqual(15);

		// Cleanup
		await table.DropAsync(CancellationToken);
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Table_Update_Success(string providerName)
	{
		DbTestHelper.SkipIfUnavailable(providerName);

		var provider = AdoDatabaseProvider.Instance;
		using var connection = provider.CreateConnection(DbTestHelper.TryGetConnectionPair(providerName));
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
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Table_Delete_Success(string providerName)
	{
		DbTestHelper.SkipIfUnavailable(providerName);

		var provider = AdoDatabaseProvider.Instance;
		using var connection = provider.CreateConnection(DbTestHelper.TryGetConnectionPair(providerName));
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
		var list = results.ToList();

		list.Count.AssertEqual(5);
		list.All(row => row["Id"].To<int>() <= 5).AssertEqual(true);

		// Cleanup
		await table.DropAsync(CancellationToken);
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Table_SelectWithInOperator_Success(string providerName)
	{
		DbTestHelper.SkipIfUnavailable(providerName);

		var provider = AdoDatabaseProvider.Instance;
		using var connection = provider.CreateConnection(DbTestHelper.TryGetConnectionPair(providerName));
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
		list.Select(row => row["Id"].To<int>()).OrderBy(x => x).ToArray().AssertEqual([2, 4, 6, 8]);

		// Cleanup
		await table.DropAsync(CancellationToken);
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Table_SelectWithLikeOperator_Success(string providerName)
	{
		DbTestHelper.SkipIfUnavailable(providerName);

		var provider = AdoDatabaseProvider.Instance;
		using var connection = provider.CreateConnection(DbTestHelper.TryGetConnectionPair(providerName));
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
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Table_Delete_ReturnsDeletedCount(string providerName)
	{
		DbTestHelper.SkipIfUnavailable(providerName);

		var provider = AdoDatabaseProvider.Instance;
		using var connection = provider.CreateConnection(DbTestHelper.TryGetConnectionPair(providerName));
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

	#region Upsert Tests

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Table_Upsert_InsertAndUpdate_Success(string providerName)
	{
		DbTestHelper.SkipIfUnavailable(providerName);

		var upsertTableName = "Ecng_UpsertTest";
		var provider = AdoDatabaseProvider.Instance;
		using var connection = provider.CreateConnection(DbTestHelper.TryGetConnectionPair(providerName));
		var table = provider.GetTable(connection, upsertTableName);

		// Create table with PRIMARY KEY (required for upsert ON CONFLICT / MERGE)
		DropTableRaw(providerName, upsertTableName);
		CreateTableWithPrimaryKey(providerName, upsertTableName, new Dictionary<string, Type>
		{
			["Id"] = typeof(int),
			["Name"] = typeof(string),
		});

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

	#endregion

	#region Validation Tests

	[TestMethod]
	public void Table_GetTable_NullConnection_Throws()
	{
		var provider = AdoDatabaseProvider.Instance;
		Throws<ArgumentNullException>(() => provider.GetTable(null, "test"));
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public void Table_GetTable_EmptyTableName_Throws(string providerName)
	{
		DbTestHelper.SkipIfUnavailable(providerName);

		var provider = AdoDatabaseProvider.Instance;
		using var connection = provider.CreateConnection(DbTestHelper.TryGetConnectionPair(providerName));
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
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Table_BulkInsert_ManyColumns_Success(string providerName)
	{
		DbTestHelper.SkipIfUnavailable(providerName);

		var provider = AdoDatabaseProvider.Instance;
		using var connection = provider.CreateConnection(DbTestHelper.TryGetConnectionPair(providerName));
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
		resultsList[0]["Id"].To<int>().AssertEqual(1);
		resultsList[0]["Col1"].AssertEqual("V1_1");
		resultsList[0]["Col99"].AssertEqual("V1_99");

		// Cleanup
		await table.DropAsync(CancellationToken);
	}

	#endregion

	#region Edge Cases

	/// <summary>
	/// Tests IN operator with empty list.
	/// Should not produce "IN ()" which is SQL syntax error.
	/// </summary>
	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Table_SelectWithEmptyIn_ReturnsEmpty(string providerName)
	{
		DbTestHelper.SkipIfUnavailable(providerName);

		var provider = AdoDatabaseProvider.Instance;
		using var connection = provider.CreateConnection(DbTestHelper.TryGetConnectionPair(providerName));
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
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Table_TimeSpanStorage_Consistent(string providerName)
	{
		DbTestHelper.SkipIfUnavailable(providerName);

		var provider = AdoDatabaseProvider.Instance;
		using var connection = provider.CreateConnection(DbTestHelper.TryGetConnectionPair(providerName));
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
			Fail($"Unexpected Duration type: {storedDuration?.GetType()?.Name ?? "null"}");
		}

		// Cleanup
		await table.DropAsync(CancellationToken);
	}

	/// <summary>
	/// Verifies that BulkInsert with empty rows list does not throw.
	/// </summary>
	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Table_BulkInsert_EmptyList_DoesNotThrow(string providerName)
	{
		DbTestHelper.SkipIfUnavailable(providerName);

		var provider = AdoDatabaseProvider.Instance;
		using var connection = provider.CreateConnection(DbTestHelper.TryGetConnectionPair(providerName));
		var table = provider.GetTable(connection, _testTableName);

		// Setup
		await table.DropAsync(CancellationToken);
		await table.CreateAsync(new Dictionary<string, Type>
		{
			["Id"] = typeof(int),
			["Name"] = typeof(string),
		}, CancellationToken);

		// BulkInsert with empty list - should not throw
		var emptyRows = new List<IDictionary<string, object>>();
		await table.BulkInsertAsync(emptyRows, CancellationToken);

		// Verify table is still empty
		var results = await table.SelectAsync(null, null, null, null, CancellationToken);
		results.Count().AssertEqual(0);

		// Cleanup
		await table.DropAsync(CancellationToken);
	}

	/// <summary>
	/// Verifies that BulkInsert with empty first row is handled gracefully.
	/// Empty first row should not cause DivideByZeroException.
	/// </summary>
	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Table_BulkInsert_EmptyFirstRow_ShouldHandleGracefully(string providerName)
	{
		DbTestHelper.SkipIfUnavailable(providerName);

		var provider = AdoDatabaseProvider.Instance;
		using var connection = provider.CreateConnection(DbTestHelper.TryGetConnectionPair(providerName));
		var table = provider.GetTable(connection, _testTableName);

		// Setup
		await table.DropAsync(CancellationToken);
		await table.CreateAsync(new Dictionary<string, Type>
		{
			["Id"] = typeof(int),
			["Name"] = typeof(string),
		}, CancellationToken);

		// BulkInsert with empty first row (no columns)
		var rows = new List<IDictionary<string, object>>
		{
			new Dictionary<string, object>(), // Empty row - columns.Count = 0
			ToDict(1, "Test")
		};

		// This should either:
		// 1. Throw a clear ArgumentException (not DivideByZeroException)
		// 2. Or skip empty rows and insert the rest
		try
		{
			await table.BulkInsertAsync(rows, CancellationToken);
			// If it succeeds, verify only non-empty row was inserted
			var results = await table.SelectAsync(null, null, null, null, CancellationToken);
			(results.Count() <= 1).AssertTrue("Should insert at most the non-empty row");
		}
		catch (DivideByZeroException)
		{
			Fail("BulkInsert should not throw DivideByZeroException for empty first row");
		}
		catch (ArgumentException)
		{
			// This is acceptable - clear error about empty row
		}

		// Cleanup
		await table.DropAsync(CancellationToken);
	}

	/// <summary>
	/// Verifies that BulkInsert with inconsistent columns across rows is handled.
	/// </summary>
	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Table_BulkInsert_InconsistentColumns_ShouldNotLoseData(string providerName)
	{
		DbTestHelper.SkipIfUnavailable(providerName);

		var provider = AdoDatabaseProvider.Instance;
		using var connection = provider.CreateConnection(DbTestHelper.TryGetConnectionPair(providerName));
		var table = provider.GetTable(connection, _testTableName);

		// Setup - create table with all columns that will be used
		await table.DropAsync(CancellationToken);
		await table.CreateAsync(new Dictionary<string, Type>
		{
			["Id"] = typeof(int),
			["Name"] = typeof(string),
			["Extra"] = typeof(string),
		}, CancellationToken);

		// First row has Id, Name
		// Second row has Id, Name, Extra
		var rows = new List<IDictionary<string, object>>
		{
			new Dictionary<string, object> { ["Id"] = 1, ["Name"] = "First" },
			new Dictionary<string, object> { ["Id"] = 2, ["Name"] = "Second", ["Extra"] = "ExtraData" }
		};

		await table.BulkInsertAsync(rows, CancellationToken);

		// Verify data - the "Extra" column in second row might be lost due to the bug
		var results = await table.SelectAsync(null, null, null, null, CancellationToken);
		var list = results.ToList();

		list.Count.AssertEqual(2);

		// Check if Extra column data was preserved in second row
		var secondRow = list.FirstOrDefault(r => r["Id"].To<int>() == 2);
		secondRow.AssertNotNull();

		// If bug exists, Extra will be null/missing even though we provided it
		var extraValue = secondRow.TryGetValue("Extra", out var val) ? val?.ToString() : null;
		extraValue.AssertEqual("ExtraData",
			"BulkInsert should not silently ignore columns that exist in subsequent rows but not in first row");

		// Cleanup
		await table.DropAsync(CancellationToken);
	}

	#endregion

	#region NULL Filter Handling

	/// <summary>
	/// Verifies that filtering with NULL value uses IS NULL syntax.
	/// </summary>
	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Table_SelectWithNullFilter_ShouldUseIsNull(string providerName)
	{
		DbTestHelper.SkipIfUnavailable(providerName);

		var provider = AdoDatabaseProvider.Instance;
		using var connection = provider.CreateConnection(DbTestHelper.TryGetConnectionPair(providerName));
		var table = provider.GetTable(connection, _testTableName);

		// Setup
		await table.DropAsync(CancellationToken);
		await table.CreateAsync(new Dictionary<string, Type>
		{
			["Id"] = typeof(int),
			["Name"] = typeof(string),
		}, CancellationToken);

		// Insert test data - one with NULL name, one with value
		await table.InsertAsync(new Dictionary<string, object> { ["Id"] = 1, ["Name"] = null }, CancellationToken);
		await table.InsertAsync(new Dictionary<string, object> { ["Id"] = 2, ["Name"] = "HasValue" }, CancellationToken);

		// Select where Name = NULL
		var filters = new[] { new FilterCondition("Name", ComparisonOperator.Equal, null) };
		var results = await table.SelectAsync(filters, null, null, null, CancellationToken);
		var list = results.ToList();

		list.Count.AssertEqual(1, "Filter with NULL should find the row with NULL value");
		list[0]["Id"].To<int>().AssertEqual(1);

		// Cleanup
		await table.DropAsync(CancellationToken);
	}

	/// <summary>
	/// Verifies that filtering with NOT NULL value uses IS NOT NULL syntax.
	/// </summary>
	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Table_SelectWithNotNullFilter_ShouldUseIsNotNull(string providerName)
	{
		DbTestHelper.SkipIfUnavailable(providerName);

		var provider = AdoDatabaseProvider.Instance;
		using var connection = provider.CreateConnection(DbTestHelper.TryGetConnectionPair(providerName));
		var table = provider.GetTable(connection, _testTableName);

		// Setup
		await table.DropAsync(CancellationToken);
		await table.CreateAsync(new Dictionary<string, Type>
		{
			["Id"] = typeof(int),
			["Name"] = typeof(string),
		}, CancellationToken);

		// Insert test data
		await table.InsertAsync(new Dictionary<string, object> { ["Id"] = 1, ["Name"] = null }, CancellationToken);
		await table.InsertAsync(new Dictionary<string, object> { ["Id"] = 2, ["Name"] = "HasValue" }, CancellationToken);

		// Select where Name <> NULL (should find rows where Name is NOT NULL)
		var filters = new[] { new FilterCondition("Name", ComparisonOperator.NotEqual, null) };
		var results = await table.SelectAsync(filters, null, null, null, CancellationToken);
		var list = results.ToList();

		list.Count.AssertEqual(1, "Filter with NotEqual NULL should find rows with non-NULL values");
		list[0]["Id"].To<int>().AssertEqual(2);

		// Cleanup
		await table.DropAsync(CancellationToken);
	}

	#endregion

	#region Parameter Limit Tests

	/// <summary>
	/// Verifies behavior when columns exceed configured MaxParameters.
	/// SQLite MaxParameters is set to 900 in code, but actual SQLite limit may be higher.
	/// </summary>
	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Table_BulkInsert_ColumnsExceedMaxParameters_ShouldValidateUpfront(string providerName)
	{
		DbTestHelper.SkipIfUnavailable(providerName);

		var provider = AdoDatabaseProvider.Instance;
		using var connection = provider.CreateConnection(DbTestHelper.TryGetConnectionPair(providerName));
		var table = provider.GetTable(connection, _testTableName + "_toomany");

		// Get the dialect's MaxParameters setting
		var dialect = DbTestHelper.GetDialect(providerName);
		var maxParams = dialect.MaxParameters; // 900

		// Create table with columns > MaxParameters
		var columnCount = maxParams + 100; // 1000 columns
		var columns = new Dictionary<string, Type> { ["Id"] = typeof(int) };
		for (var i = 1; i < columnCount; i++)
			columns[$"Col{i}"] = typeof(string);

		await table.DropAsync(CancellationToken);
		await table.CreateAsync(columns, CancellationToken);

		// Create 1 row with all columns
		var row = new Dictionary<string, object> { ["Id"] = 1 };
		for (var c = 1; c < columnCount; c++)
			row[$"Col{c}"] = $"V{c}";

		var rows = new List<IDictionary<string, object>> { row };

		// Verify: batchSize calculation allows this even though it exceeds MaxParameters
		(columnCount > maxParams).AssertTrue("Test setup: columns should exceed MaxParameters");

		// BulkInsertAsync should throw ArgumentException when columns exceed MaxParameters
		await Assert.ThrowsExactlyAsync<ArgumentException>(
			() => table.BulkInsertAsync(rows, CancellationToken));

		// Cleanup
		await table.DropAsync(CancellationToken);
	}

	#endregion
}
