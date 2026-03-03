namespace Ecng.Tests.Data;

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;
using Ecng.Data;
using Ecng.Data.Sql;
using Ecng.UnitTesting;

/// <summary>
/// Base class for ADO-level integration tests.
/// Tests CRUD operations through <see cref="AdoDatabaseProvider"/> and <see cref="IDatabaseTable"/>.
/// </summary>
public abstract class AdoIntegrationTestsBase : BaseTestClass
{
	private IDatabaseConnection _connection;
	private IDatabaseTable _table;

	protected abstract string ProviderName { get; }
	protected abstract ISqlDialect Dialect { get; }
	protected abstract DbProviderFactory Factory { get; }
	protected abstract string GetConnectionString();

	private const string TableName = "AdoTestItems";

	private static readonly Dictionary<string, Type> _columns = new()
	{
		["Id"] = typeof(int),
		["Name"] = typeof(string),
		["Value"] = typeof(decimal),
		["Created"] = typeof(DateTime),
	};

	protected void SetUp()
	{
		var connStr = GetConnectionString();
		if (connStr.IsEmpty())
		{
			Inconclusive($"Connection string not configured for {ProviderName}.");
			return;
		}

		DatabaseProviderRegistry.Register(ProviderName, Factory);
		DatabaseProviderRegistry.RegisterDialect(ProviderName, Dialect);

		var pair = new DatabaseConnectionPair
		{
			Provider = ProviderName,
			ConnectionString = connStr,
		};

		_connection = AdoDatabaseProvider.Instance.CreateConnection(pair);
		_table = AdoDatabaseProvider.Instance.GetTable(_connection, TableName);
	}

	protected async Task TearDown()
	{
		if (_table is not null)
		{
			try { await _table.DropAsync(CancellationToken.None); }
			catch { /* ignore if table doesn't exist */ }
		}

		(_connection as IDisposable)?.Dispose();
	}

	protected async Task CreateTable_Test()
	{
		EnsureSetUp();
		await _table.CreateAsync(_columns, CancellationToken.None);

		// Verify table exists by selecting (should return empty)
		var rows = await _table.SelectAsync(null, null, null, null, CancellationToken.None);
		rows.Any().AssertFalse("Table should be empty after creation");
	}

	protected async Task InsertAndSelect_Test()
	{
		EnsureSetUp();
		await _table.CreateAsync(_columns, CancellationToken.None);

		var values = new Dictionary<string, object>
		{
			["Id"] = 1,
			["Name"] = "Test Item",
			["Value"] = 42.5m,
			["Created"] = new DateTime(2025, 1, 15, 10, 30, 0, DateTimeKind.Unspecified),
		};

		await _table.InsertAsync(values, CancellationToken.None);

		var rows = (await _table.SelectAsync(null, null, null, null, CancellationToken.None)).ToList();
		rows.Count.AssertEqual(1);

		var row = rows[0];
		Convert.ToInt32(row["Id"]).AssertEqual(1);
		row["Name"].To<string>().AssertEqual("Test Item");
		Convert.ToDecimal(row["Value"]).AssertEqual(42.5m);
	}

	protected async Task SelectWithFilter_Test()
	{
		EnsureSetUp();
		await _table.CreateAsync(_columns, CancellationToken.None);

		for (var i = 1; i <= 5; i++)
		{
			await _table.InsertAsync(new Dictionary<string, object>
			{
				["Id"] = i,
				["Name"] = $"Item {i}",
				["Value"] = i * 10m,
				["Created"] = new DateTime(2025, 1, i, 0, 0, 0, DateTimeKind.Unspecified),
			}, CancellationToken.None);
		}

		var filters = new[] { new FilterCondition("Id", ComparisonOperator.Greater, 3) };
		var rows = (await _table.SelectAsync(filters, null, null, null, CancellationToken.None)).ToList();
		rows.Count.AssertEqual(2);
	}

	protected async Task SelectWithOrderBy_Test()
	{
		EnsureSetUp();
		await _table.CreateAsync(_columns, CancellationToken.None);

		for (var i = 1; i <= 3; i++)
		{
			await _table.InsertAsync(new Dictionary<string, object>
			{
				["Id"] = i,
				["Name"] = $"Item {i}",
				["Value"] = (4 - i) * 10m,
				["Created"] = new DateTime(2025, 1, i, 0, 0, 0, DateTimeKind.Unspecified),
			}, CancellationToken.None);
		}

		var orderBy = new[] { new OrderByCondition("Value") }; // ASC
		var rows = (await _table.SelectAsync(null, orderBy, null, null, CancellationToken.None)).ToList();
		rows.Count.AssertEqual(3);
		Convert.ToDecimal(rows[0]["Value"]).AssertEqual(10m);
		Convert.ToDecimal(rows[2]["Value"]).AssertEqual(30m);
	}

	protected async Task Update_Test()
	{
		EnsureSetUp();
		await _table.CreateAsync(_columns, CancellationToken.None);

		await _table.InsertAsync(new Dictionary<string, object>
		{
			["Id"] = 1,
			["Name"] = "Original",
			["Value"] = 10m,
			["Created"] = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Unspecified),
		}, CancellationToken.None);

		var updateValues = new Dictionary<string, object>
		{
			["Name"] = "Updated",
			["Value"] = 99m,
		};
		var filters = new[] { new FilterCondition("Id", ComparisonOperator.Equal, 1) };

		await _table.UpdateAsync(updateValues, filters, CancellationToken.None);

		var rows = (await _table.SelectAsync(null, null, null, null, CancellationToken.None)).ToList();
		rows.Count.AssertEqual(1);
		rows[0]["Name"].To<string>().AssertEqual("Updated");
		Convert.ToDecimal(rows[0]["Value"]).AssertEqual(99m);
	}

	protected async Task Delete_Test()
	{
		EnsureSetUp();
		await _table.CreateAsync(_columns, CancellationToken.None);

		for (var i = 1; i <= 3; i++)
		{
			await _table.InsertAsync(new Dictionary<string, object>
			{
				["Id"] = i,
				["Name"] = $"Item {i}",
				["Value"] = i * 10m,
				["Created"] = new DateTime(2025, 1, i, 0, 0, 0, DateTimeKind.Unspecified),
			}, CancellationToken.None);
		}

		var filters = new[] { new FilterCondition("Id", ComparisonOperator.Equal, 2) };
		var deleted = await _table.DeleteAsync(filters, CancellationToken.None);
		deleted.AssertEqual(1);

		var rows = (await _table.SelectAsync(null, null, null, null, CancellationToken.None)).ToList();
		rows.Count.AssertEqual(2);
	}

	protected async Task Upsert_Test()
	{
		EnsureSetUp();
		// Upsert requires PRIMARY KEY constraint on the key column
		await CreateTableWithPrimaryKey();

		var values = new Dictionary<string, object>
		{
			["Id"] = 1,
			["Name"] = "Original",
			["Value"] = 10m,
			["Created"] = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Unspecified),
		};

		// Insert via upsert
		await _table.UpsertAsync(values, ["Id"], CancellationToken.None);

		var rows = (await _table.SelectAsync(null, null, null, null, CancellationToken.None)).ToList();
		rows.Count.AssertEqual(1);
		rows[0]["Name"].To<string>().AssertEqual("Original");

		// Update via upsert (same key)
		values["Name"] = "Upserted";
		values["Value"] = 99m;
		await _table.UpsertAsync(values, ["Id"], CancellationToken.None);

		rows = (await _table.SelectAsync(null, null, null, null, CancellationToken.None)).ToList();
		rows.Count.AssertEqual(1);
		rows[0]["Name"].To<string>().AssertEqual("Upserted");
		Convert.ToDecimal(rows[0]["Value"]).AssertEqual(99m);
	}

	protected async Task BulkInsert_Test()
	{
		EnsureSetUp();
		await _table.CreateAsync(_columns, CancellationToken.None);

		var batch = Enumerable.Range(1, 50).Select(i => (IDictionary<string, object>)new Dictionary<string, object>
		{
			["Id"] = i,
			["Name"] = $"Bulk {i}",
			["Value"] = i * 1.1m,
			["Created"] = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Unspecified),
		}).ToList();

		await _table.BulkInsertAsync(batch, CancellationToken.None);

		var rows = (await _table.SelectAsync(null, null, null, null, CancellationToken.None)).ToList();
		rows.Count.AssertEqual(50);
	}

	protected async Task SelectWithPagination_Test()
	{
		EnsureSetUp();
		await _table.CreateAsync(_columns, CancellationToken.None);

		for (var i = 1; i <= 10; i++)
		{
			await _table.InsertAsync(new Dictionary<string, object>
			{
				["Id"] = i,
				["Name"] = $"Item {i}",
				["Value"] = i * 10m,
				["Created"] = new DateTime(2025, 1, i, 0, 0, 0, DateTimeKind.Unspecified),
			}, CancellationToken.None);
		}

		var orderBy = new[] { new OrderByCondition("Id") };
		var rows = (await _table.SelectAsync(null, orderBy, 2, 3, CancellationToken.None)).ToList();
		rows.Count.AssertEqual(3);
		Convert.ToInt32(rows[0]["Id"]).AssertEqual(3); // skip 2, take 3 → items 3,4,5
	}

	private async Task CreateTableWithPrimaryKey()
	{
		// Drop first in case it exists from a prior run
		try { await _table.DropAsync(CancellationToken.None); } catch { }

		// Create table with PRIMARY KEY via raw SQL (needed for upsert)
		var sql = Query.CreateCreateTable(TableName, _columns, primaryKeyColumns: ["Id"]).Render(Dialect);

		using var conn = Factory.CreateConnection();
		conn.ConnectionString = GetConnectionString();
		await conn.OpenAsync();
		using var cmd = conn.CreateCommand();
		cmd.CommandText = sql;
		await cmd.ExecuteNonQueryAsync();
	}

	private void EnsureSetUp()
	{
		if (_table is null)
			Inconclusive($"Connection string not configured for {ProviderName}.");
	}
}

#region SQL Server

[TestClass]
[TestCategory("Integration")]
[TestCategory("SqlServer")]
[DoNotParallelize]
public class AdoSqlServerIntegrationTests : AdoIntegrationTestsBase
{
	protected override string ProviderName => DatabaseProviderRegistry.SqlServer;
	protected override ISqlDialect Dialect => SqlServerDialect.Instance;
	protected override DbProviderFactory Factory => Microsoft.Data.SqlClient.SqlClientFactory.Instance;

	protected override string GetConnectionString()
		=> TryGetSecret("SQLSERVER_CONNECTION_STRING");

	[TestInitialize]
	public void Init() => SetUp();

	[TestCleanup]
	public async Task Cleanup() => await TearDown();

	[TestMethod] public async Task CreateTable() => await CreateTable_Test();
	[TestMethod] public async Task InsertAndSelect() => await InsertAndSelect_Test();
	[TestMethod] public async Task SelectWithFilter() => await SelectWithFilter_Test();
	[TestMethod] public async Task SelectWithOrderBy() => await SelectWithOrderBy_Test();
	[TestMethod] public async Task Update() => await Update_Test();
	[TestMethod] public async Task Delete() => await Delete_Test();
	[TestMethod] public async Task Upsert() => await Upsert_Test();
	[TestMethod] public async Task BulkInsert() => await BulkInsert_Test();
	[TestMethod] public async Task SelectWithPagination() => await SelectWithPagination_Test();
}

#endregion

#region PostgreSQL

#if !NET6_0

[TestClass]
[TestCategory("Integration")]
[TestCategory("PostgreSql")]
[DoNotParallelize]
public class AdoPostgreSqlIntegrationTests : AdoIntegrationTestsBase
{
	protected override string ProviderName => DatabaseProviderRegistry.PostgreSql;
	protected override ISqlDialect Dialect => PostgreSqlDialect.Instance;
	protected override DbProviderFactory Factory => Npgsql.NpgsqlFactory.Instance;

	protected override string GetConnectionString()
		=> TryGetSecret("PG_CONNECTION_STRING");

	[TestInitialize]
	public void Init() => SetUp();

	[TestCleanup]
	public async Task Cleanup() => await TearDown();

	[TestMethod] public async Task CreateTable() => await CreateTable_Test();
	[TestMethod] public async Task InsertAndSelect() => await InsertAndSelect_Test();
	[TestMethod] public async Task SelectWithFilter() => await SelectWithFilter_Test();
	[TestMethod] public async Task SelectWithOrderBy() => await SelectWithOrderBy_Test();
	[TestMethod] public async Task Update() => await Update_Test();
	[TestMethod] public async Task Delete() => await Delete_Test();
	[TestMethod] public async Task Upsert() => await Upsert_Test();
	[TestMethod] public async Task BulkInsert() => await BulkInsert_Test();
	[TestMethod] public async Task SelectWithPagination() => await SelectWithPagination_Test();
}

#endif

#endregion

#region SQLite

[TestClass]
[TestCategory("Integration")]
[TestCategory("SQLite")]
[DoNotParallelize]
public class AdoSQLiteIntegrationTests : AdoIntegrationTestsBase
{
	protected override string ProviderName => DatabaseProviderRegistry.SQLite;
	protected override ISqlDialect Dialect => SQLiteDialect.Instance;
	protected override DbProviderFactory Factory => Microsoft.Data.Sqlite.SqliteFactory.Instance;

	protected override string GetConnectionString()
		=> "Data Source=AdoTest;Mode=Memory;Cache=Shared";

	[TestInitialize]
	public void Init() => SetUp();

	[TestCleanup]
	public async Task Cleanup() => await TearDown();

	[TestMethod] public async Task CreateTable() => await CreateTable_Test();
	[TestMethod] public async Task InsertAndSelect() => await InsertAndSelect_Test();
	[TestMethod] public async Task SelectWithFilter() => await SelectWithFilter_Test();
	[TestMethod] public async Task SelectWithOrderBy() => await SelectWithOrderBy_Test();
	[TestMethod] public async Task Update() => await Update_Test();
	[TestMethod] public async Task Delete() => await Delete_Test();
	[TestMethod] public async Task Upsert() => await Upsert_Test();
	[TestMethod] public async Task BulkInsert() => await BulkInsert_Test();
	[TestMethod] public async Task SelectWithPagination() => await SelectWithPagination_Test();
}

#endregion
