namespace Ecng.Tests.Data;

using System.Data.Common;

using Ecng.Data;
using Ecng.Data.Sql;

/// <summary>
/// ADO-level integration tests for CRUD operations through <see cref="AdoDatabaseProvider"/> and <see cref="IDatabaseTable"/>.
/// Parameterized to run against all supported database providers.
/// </summary>
[TestClass]
[TestCategory("Integration")]
[TestCategory("Database")]
[DoNotParallelize]
public class AdoIntegrationTests : BaseTestClass
{
	private IDatabaseConnection _connection;
	private IDatabaseTable _table;
	private string _provider;

	private const string TableName = "Ecng_AdoTestItems";

	private static readonly Dictionary<string, Type> _columns = new()
	{
		["Id"] = typeof(int),
		["Name"] = typeof(string),
		["Value"] = typeof(decimal),
		["Created"] = typeof(DateTime),
	};

	private void SetUp(string provider)
	{
		_provider = provider;

		DbTestHelper.RegisterAll();
		DbTestHelper.SkipIfUnavailable(provider);

		var connStr = DbTestHelper.TryGetConnectionString(provider);
		var factory = DbTestHelper.GetFactory(provider);
		var dialect = DbTestHelper.GetDialect(provider);

		DatabaseProviderRegistry.Register(provider, factory);
		DatabaseProviderRegistry.RegisterDialect(provider, dialect);

		var pair = new DatabaseConnectionPair
		{
			Provider = provider,
			ConnectionString = connStr,
		};

		_connection = AdoDatabaseProvider.Instance.CreateConnection(pair);
		_table = AdoDatabaseProvider.Instance.GetTable(_connection, TableName);
	}

	[TestCleanup]
	public async Task Cleanup()
	{
		if (_table is not null)
		{
			try { await _table.DropAsync(CancellationToken); }
			catch { /* ignore if table doesn't exist */ }
		}

		(_connection as IDisposable)?.Dispose();
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task CreateTable(string provider)
	{
		SetUp(provider);
		await _table.CreateAsync(_columns, CancellationToken);

		var rows = await _table.SelectAsync(null, null, null, null, CancellationToken);
		rows.Any().AssertFalse("Table should be empty after creation");
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task InsertAndSelect(string provider)
	{
		SetUp(provider);
		await _table.CreateAsync(_columns, CancellationToken);

		var values = new Dictionary<string, object>
		{
			["Id"] = 1,
			["Name"] = "Test Item",
			["Value"] = 42.5m,
			["Created"] = new DateTime(2025, 1, 15, 10, 30, 0, DateTimeKind.Unspecified),
		};

		await _table.InsertAsync(values, CancellationToken);

		var rows = (await _table.SelectAsync(null, null, null, null, CancellationToken)).ToList();
		rows.Count.AssertEqual(1);

		var row = rows[0];
		row["Id"].To<int>().AssertEqual(1);
		row["Name"].To<string>().AssertEqual("Test Item");
		row["Value"].To<decimal>().AssertEqual(42.5m);
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task SelectWithFilter(string provider)
	{
		SetUp(provider);
		await _table.CreateAsync(_columns, CancellationToken);

		for (var i = 1; i <= 5; i++)
		{
			await _table.InsertAsync(new Dictionary<string, object>
			{
				["Id"] = i,
				["Name"] = $"Item {i}",
				["Value"] = i * 10m,
				["Created"] = new DateTime(2025, 1, i, 0, 0, 0, DateTimeKind.Unspecified),
			}, CancellationToken);
		}

		var filters = new[] { new FilterCondition("Id", ComparisonOperator.Greater, 3) };
		var rows = (await _table.SelectAsync(filters, null, null, null, CancellationToken)).ToList();
		rows.Count.AssertEqual(2);
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task SelectWithOrderBy(string provider)
	{
		SetUp(provider);
		await _table.CreateAsync(_columns, CancellationToken);

		for (var i = 1; i <= 3; i++)
		{
			await _table.InsertAsync(new Dictionary<string, object>
			{
				["Id"] = i,
				["Name"] = $"Item {i}",
				["Value"] = (4 - i) * 10m,
				["Created"] = new DateTime(2025, 1, i, 0, 0, 0, DateTimeKind.Unspecified),
			}, CancellationToken);
		}

		var orderBy = new[] { new OrderByCondition("Value") }; // ASC
		var rows = (await _table.SelectAsync(null, orderBy, null, null, CancellationToken)).ToList();
		rows.Count.AssertEqual(3);
		rows[0]["Value"].To<decimal>().AssertEqual(10m);
		rows[2]["Value"].To<decimal>().AssertEqual(30m);
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Update(string provider)
	{
		SetUp(provider);
		await _table.CreateAsync(_columns, CancellationToken);

		await _table.InsertAsync(new Dictionary<string, object>
		{
			["Id"] = 1,
			["Name"] = "Original",
			["Value"] = 10m,
			["Created"] = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Unspecified),
		}, CancellationToken);

		var updateValues = new Dictionary<string, object>
		{
			["Name"] = "Updated",
			["Value"] = 99m,
		};
		var filters = new[] { new FilterCondition("Id", ComparisonOperator.Equal, 1) };

		await _table.UpdateAsync(updateValues, filters, CancellationToken);

		var rows = (await _table.SelectAsync(null, null, null, null, CancellationToken)).ToList();
		rows.Count.AssertEqual(1);
		rows[0]["Name"].To<string>().AssertEqual("Updated");
		rows[0]["Value"].To<decimal>().AssertEqual(99m);
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Delete(string provider)
	{
		SetUp(provider);
		await _table.CreateAsync(_columns, CancellationToken);

		for (var i = 1; i <= 3; i++)
		{
			await _table.InsertAsync(new Dictionary<string, object>
			{
				["Id"] = i,
				["Name"] = $"Item {i}",
				["Value"] = i * 10m,
				["Created"] = new DateTime(2025, 1, i, 0, 0, 0, DateTimeKind.Unspecified),
			}, CancellationToken);
		}

		var filters = new[] { new FilterCondition("Id", ComparisonOperator.Equal, 2) };
		var deleted = await _table.DeleteAsync(filters, CancellationToken);
		deleted.AssertEqual(1);

		var rows = (await _table.SelectAsync(null, null, null, null, CancellationToken)).ToList();
		rows.Count.AssertEqual(2);
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Upsert(string provider)
	{
		SetUp(provider);

		// Upsert requires PRIMARY KEY constraint on the key column
		await CreateTableWithPrimaryKey(provider);

		var values = new Dictionary<string, object>
		{
			["Id"] = 1,
			["Name"] = "Original",
			["Value"] = 10m,
			["Created"] = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Unspecified),
		};

		// Insert via upsert
		await _table.UpsertAsync(values, ["Id"], CancellationToken);

		var rows = (await _table.SelectAsync(null, null, null, null, CancellationToken)).ToList();
		rows.Count.AssertEqual(1);
		rows[0]["Name"].To<string>().AssertEqual("Original");

		// Update via upsert (same key)
		values["Name"] = "Upserted";
		values["Value"] = 99m;
		await _table.UpsertAsync(values, ["Id"], CancellationToken);

		rows = (await _table.SelectAsync(null, null, null, null, CancellationToken)).ToList();
		rows.Count.AssertEqual(1);
		rows[0]["Name"].To<string>().AssertEqual("Upserted");
		rows[0]["Value"].To<decimal>().AssertEqual(99m);
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task BulkInsert(string provider)
	{
		SetUp(provider);
		await _table.CreateAsync(_columns, CancellationToken);

		var batch = Enumerable.Range(1, 50).Select(i => (IDictionary<string, object>)new Dictionary<string, object>
		{
			["Id"] = i,
			["Name"] = $"Bulk {i}",
			["Value"] = i * 1.1m,
			["Created"] = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Unspecified),
		}).ToList();

		await _table.BulkInsertAsync(batch, CancellationToken);

		var rows = (await _table.SelectAsync(null, null, null, null, CancellationToken)).ToList();
		rows.Count.AssertEqual(50);
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task SelectWithPagination(string provider)
	{
		SetUp(provider);
		await _table.CreateAsync(_columns, CancellationToken);

		for (var i = 1; i <= 10; i++)
		{
			await _table.InsertAsync(new Dictionary<string, object>
			{
				["Id"] = i,
				["Name"] = $"Item {i}",
				["Value"] = i * 10m,
				["Created"] = new DateTime(2025, 1, i, 0, 0, 0, DateTimeKind.Unspecified),
			}, CancellationToken);
		}

		var orderBy = new[] { new OrderByCondition("Id") };
		var rows = (await _table.SelectAsync(null, orderBy, 2, 3, CancellationToken)).ToList();
		rows.Count.AssertEqual(3);
		rows[0]["Id"].To<int>().AssertEqual(3); // skip 2, take 3 → items 3,4,5
	}

	private async Task CreateTableWithPrimaryKey(string provider)
	{
		// Drop first in case it exists from a prior run
		try { await _table.DropAsync(CancellationToken); } catch { }

		// Create table with PRIMARY KEY via raw SQL (needed for upsert)
		var dialect = DbTestHelper.GetDialect(provider);
		var sql = Query.CreateCreateTable(TableName, _columns, primaryKeyColumns: ["Id"]).Render(dialect);

		var factory = DbTestHelper.GetFactory(provider);
		var connStr = DbTestHelper.TryGetConnectionString(provider);

		using var conn = factory.CreateConnection();
		conn.ConnectionString = connStr;
		await conn.OpenAsync();
		using var cmd = conn.CreateCommand();
		cmd.CommandText = sql;
		await cmd.ExecuteNonQueryAsync();
	}
}
