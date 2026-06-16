namespace Ecng.Tests.Data;

using System.Data;
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

	#region Fake-DB regression tests (no live database required)

	private const string _fakeProvider = "AdoAuditGapFake";

	/// <summary>
	/// Registers <paramref name="dialect"/> under the test provider name so that
	/// <see cref="AdoDatabaseProvider.CreateConnection"/> can resolve it, and returns a
	/// provider whose connections are produced by <paramref name="connection"/>.
	/// </summary>
	private static AdoDatabaseProvider CreateFakeProvider(ISqlDialect dialect, DbConnection connection)
	{
		DatabaseProviderRegistry.RegisterDialect(_fakeProvider, dialect);

		return new AdoDatabaseProvider(_ => connection);
	}

	private static DatabaseConnectionPair FakePair()
		=> new() { Provider = _fakeProvider, ConnectionString = "fake" };

	private static IDictionary<string, object> FakeRow(params (string key, object value)[] cells)
	{
		var d = new Dictionary<string, object>();
		foreach (var (key, value) in cells)
			d[key] = value;
		return d;
	}

	/// <summary>
	/// Regression test for the retry policy: ensures a non-idempotent INSERT is executed at
	/// most once - the retry covers only the connection-open phase, never the command
	/// execution, so a transient fault that may have reached the server is not silently
	/// replayed. (Was: the whole write lambda was re-run on any <see cref="DbException"/>,
	/// executing the statement 1 + retryCount times; Data.Ado\AdoDatabaseProvider.cs:334-359.)
	/// </summary>
	[TestMethod]
	public async Task Retry_DoesNotReRunNonIdempotentInsert()
	{
		var conn = new FakeDbConnection { ThrowOnEveryExecute = true };
		var provider = CreateFakeProvider(SQLiteDialect.Instance, conn);

		using var connection = provider.CreateConnection(FakePair());
		var table = provider.GetTable(connection, "AuditItems");

		var values = FakeRow(("Id", 1), ("Name", "x"));

		// The fake always throws a DbException from ExecuteNonQueryAsync, simulating a
		// transient fault raised after the statement may have reached the server.
		await ThrowsAsync<DbException>(() => table.InsertAsync(values, CancellationToken));

		// A non-idempotent write must not be blindly retried after it may have executed.
		AreEqual(1, conn.ExecuteCount, "INSERT must be executed exactly once, not retried after it may have hit the server.");
	}

	/// <summary>
	/// Regression test for the Ado parameter-building path: ensures <c>Dialect.PrepareParameter</c>
	/// is invoked once per parameter added for a statement, so dialect-specific parameter massaging
	/// (e.g. PostgreSQL DateTime/timestamptz binding) is applied, mirroring the ORM path. (Was:
	/// <c>AddParameters</c> set <c>param.Value</c> but never called <c>PrepareParameter</c>;
	/// Data.Ado\AdoDatabaseProvider.cs:473-489.)
	/// </summary>
	[TestMethod]
	public async Task AddParameters_InvokesDialectPrepareParameter()
	{
		var dialect = new PrepareCountingDialect(SQLiteDialect.Instance);
		var conn = new FakeDbConnection();
		var provider = CreateFakeProvider(dialect, conn);

		using var connection = provider.CreateConnection(FakePair());
		var table = provider.GetTable(connection, "AuditItems");

		await table.InsertAsync(FakeRow(("Id", 1), ("Name", "x")), CancellationToken);

		AreEqual(2, dialect.PrepareCount, "PrepareParameter must be called once per parameter (2 columns).");
	}

	/// <summary>
	/// Regression test for <c>BulkInsertAsync</c> batching: ensures no single INSERT ... VALUES
	/// statement carries more than 1000 value tuples, respecting SQL Server's hard
	/// table-value-constructor limit (error 10738). (Was: the batch size came only from the
	/// parameter limit <c>MaxParameters / columns.Count</c>, so a single-column table on SQL
	/// Server emitted statements of up to 2000 tuples; Data.Ado\AdoDatabaseProvider.cs:180.)
	/// </summary>
	[TestMethod]
	public async Task BulkInsert_RespectsSqlServerThousandRowValuesLimit()
	{
		var conn = new FakeDbConnection();
		var provider = CreateFakeProvider(SqlServerDialect.Instance, conn);

		using var connection = provider.CreateConnection(FakePair());
		var table = provider.GetTable(connection, "AuditItems");

		// Single-column rows: tuples per statement == parameters per statement.
		var rows = new List<IDictionary<string, object>>();
		for (var i = 0; i < 1500; i++)
			rows.Add(FakeRow(("Id", i)));

		await table.BulkInsertAsync(rows, CancellationToken);

		IsTrue(conn.MaxParametersPerCommand > 0, "Expected at least one executed bulk-insert command.");
		IsLessOrEqual(conn.MaxParametersPerCommand, 1000,
			"A single INSERT ... VALUES must not exceed 1000 row tuples on SQL Server.");
	}

	/// <summary>
	/// Regression test for <c>UpdateAsync</c> parameter naming: ensures a SET value for a column
	/// literally named <c>p0</c> survives even when a filter generates a parameter that would
	/// otherwise collide, because <c>BuildWhereClause</c> skips parameter names already present.
	/// (Was: filter parameters p0/p1/... overwrote SET values keyed by the same raw column name,
	/// silently corrupting data; Data.Ado\AdoDatabaseProvider.cs:399-409.)
	/// </summary>
	[TestMethod]
	public async Task Update_FilterParamsDoNotOverwriteSetColumnNamedP0()
	{
		const string setValue = "set-value-for-p0";
		const string filterValue = "filter-value";

		var conn = new FakeDbConnection();
		var provider = CreateFakeProvider(SQLiteDialect.Instance, conn);

		using var connection = provider.CreateConnection(FakePair());
		var table = provider.GetTable(connection, "AuditItems");

		// SET column literally named "p0"; a filter on another column generates param "p0".
		var values = FakeRow(("p0", setValue));
		var filters = new[] { new FilterCondition("Name", ComparisonOperator.Equal, filterValue) };

		await table.UpdateAsync(values, filters, CancellationToken);

		IsTrue(conn.CapturedParameterValues.Contains(setValue),
			"The SET value for the column named 'p0' must be preserved, not overwritten by the WHERE param.");
	}

	/// <summary>
	/// Regression test for <c>BulkInsertAsync</c> error handling: ensures the original
	/// <see cref="DbException"/> propagates even when the best-effort <c>transaction.Rollback()</c>
	/// itself throws (e.g. the connection is already dead). (Was: a failing Rollback replaced the
	/// original fault with an InvalidOperationException, hiding the root cause;
	/// Data.Ado\AdoDatabaseProvider.cs:234-247.)
	/// </summary>
	[TestMethod]
	public async Task BulkInsert_RollbackFailureDoesNotMaskOriginalException()
	{
		var conn = new FakeDbConnection
		{
			ThrowOnEveryExecute = true,
			RollbackThrows = true,
		};
		var provider = CreateFakeProvider(SQLiteDialect.Instance, conn);

		using var connection = provider.CreateConnection(FakePair());
		var table = provider.GetTable(connection, "AuditItems");

		var rows = new List<IDictionary<string, object>> { FakeRow(("Id", 1)) };

		// The execute fault (root cause) is a DbException; a masking rollback would surface
		// as InvalidOperationException instead.
		await ThrowsAsync<DbException>(() => table.BulkInsertAsync(rows, CancellationToken));
	}

	/// <summary>
	/// Regression test for <c>BulkInsertAsync</c> parameter naming: ensures generated parameter
	/// names are positional and carry no whitespace from the raw column text, so a legally quoted
	/// column like <c>"order date"</c> does not yield an invalid ADO.NET parameter name. (Was:
	/// names were built as <c>{column}_{rowIdx}</c> from the raw column name, producing
	/// <c>@order date_0</c>; Data.Ado\AdoDatabaseProvider.cs:210.)
	/// </summary>
	[TestMethod]
	public async Task BulkInsert_ParameterNamesAreNotBuiltFromRawColumnNames()
	{
		var conn = new FakeDbConnection();
		var provider = CreateFakeProvider(SQLiteDialect.Instance, conn);

		using var connection = provider.CreateConnection(FakePair());
		var table = provider.GetTable(connection, "AuditItems");

		var rows = new List<IDictionary<string, object>> { FakeRow(("order date", "2025-01-15")) };

		await table.BulkInsertAsync(rows, CancellationToken);

		foreach (var name in conn.CapturedParameterNames)
			IsFalse(name.Contains(' '), $"Parameter name '{name}' must not embed whitespace from the raw column name.");
	}

	#endregion

	#region Fake ADO.NET harness

	private sealed class FakeDbException : DbException
	{
		public FakeDbException(string message)
			: base(message)
		{
		}
	}

	private sealed class FakeDbConnection : DbConnection
	{
		private ConnectionState _state = ConnectionState.Closed;

		/// <summary>When true, every command execution raises a <see cref="FakeDbException"/>.</summary>
		public bool ThrowOnEveryExecute { get; set; }

		/// <summary>When true, the fake transaction's Rollback throws.</summary>
		public bool RollbackThrows { get; set; }

		/// <summary>Number of times any command executed (non-query) was attempted.</summary>
		public int ExecuteCount { get; private set; }

		/// <summary>Largest parameter count seen across the executed commands.</summary>
		public int MaxParametersPerCommand { get; private set; }

		/// <summary>All parameter values captured across executed commands.</summary>
		public List<object> CapturedParameterValues { get; } = [];

		/// <summary>All parameter names captured across executed commands.</summary>
		public List<string> CapturedParameterNames { get; } = [];

		internal void OnExecute(FakeDbCommand cmd)
		{
			ExecuteCount++;

			if (cmd.Parameters.Count > MaxParametersPerCommand)
				MaxParametersPerCommand = cmd.Parameters.Count;

			foreach (FakeDbParameter p in cmd.Parameters)
			{
				CapturedParameterNames.Add(p.ParameterName);
				CapturedParameterValues.Add(p.Value);
			}

			if (ThrowOnEveryExecute)
				throw new FakeDbException("simulated transient driver fault");
		}

		public override string ConnectionString { get; set; } = "fake";
		public override string Database => "fake";
		public override string DataSource => "fake";
		public override string ServerVersion => "0.0";
		public override ConnectionState State => _state;

		public override void ChangeDatabase(string databaseName) { }
		public override void Close() => _state = ConnectionState.Closed;
		public override void Open() => _state = ConnectionState.Open;

		public override Task OpenAsync(CancellationToken cancellationToken)
		{
			_state = ConnectionState.Open;
			return Task.CompletedTask;
		}

		protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
			=> new FakeDbTransaction(this);

		protected override DbCommand CreateDbCommand()
			=> new FakeDbCommand(this);
	}

	private sealed class FakeDbTransaction(FakeDbConnection connection) : DbTransaction
	{
		protected override DbConnection DbConnection { get; } = connection;
		public override IsolationLevel IsolationLevel => IsolationLevel.Unspecified;

		public override void Commit() { }

		public override void Rollback()
		{
			if (connection.RollbackThrows)
				throw new InvalidOperationException("rollback failed: connection is dead");
		}
	}

	private sealed class FakeDbCommand(FakeDbConnection connection) : DbCommand
	{
		private readonly FakeDbParameterCollection _parameters = new();

		public override string CommandText { get; set; } = string.Empty;
		public override int CommandTimeout { get; set; }
		public override CommandType CommandType { get; set; }
		public override bool DesignTimeVisible { get; set; }
		public override UpdateRowSource UpdatedRowSource { get; set; }
		protected override DbConnection DbConnection { get; set; } = connection;
		protected override DbParameterCollection DbParameterCollection => _parameters;
		protected override DbTransaction DbTransaction { get; set; }

		public override void Cancel() { }
		public override void Prepare() { }

		protected override DbParameter CreateDbParameter() => new FakeDbParameter();

		public override int ExecuteNonQuery()
		{
			connection.OnExecute(this);
			return 1;
		}

		public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
		{
			connection.OnExecute(this);
			return Task.FromResult(1);
		}

		public override object ExecuteScalar()
		{
			connection.OnExecute(this);
			return null;
		}

		protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
			=> throw new NotSupportedException("Reader path is not exercised by these tests.");
	}

	private sealed class FakeDbParameter : DbParameter
	{
		public override DbType DbType { get; set; }
		public override ParameterDirection Direction { get; set; }
		public override bool IsNullable { get; set; }
		public override string ParameterName { get; set; }
		public override int Size { get; set; }
		public override string SourceColumn { get; set; }
		public override bool SourceColumnNullMapping { get; set; }
		public override object Value { get; set; }

		public override void ResetDbType() { }
	}

	private sealed class FakeDbParameterCollection : DbParameterCollection
	{
		private readonly List<DbParameter> _items = [];

		public override int Count => _items.Count;
		public override object SyncRoot { get; } = new();

		public override int Add(object value)
		{
			_items.Add((DbParameter)value);
			return _items.Count - 1;
		}

		public override void AddRange(Array values)
		{
			foreach (var v in values)
				_items.Add((DbParameter)v);
		}

		public override void Clear() => _items.Clear();
		public override bool Contains(object value) => _items.Contains((DbParameter)value);
		public override bool Contains(string value) => IndexOf(value) >= 0;
		public override void CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);
		public override IEnumerator GetEnumerator() => _items.GetEnumerator();
		public override int IndexOf(object value) => _items.IndexOf((DbParameter)value);

		public override int IndexOf(string parameterName)
		{
			for (var i = 0; i < _items.Count; i++)
			{
				if (_items[i].ParameterName == parameterName)
					return i;
			}

			return -1;
		}

		public override void Insert(int index, object value) => _items.Insert(index, (DbParameter)value);
		public override void Remove(object value) => _items.Remove((DbParameter)value);
		public override void RemoveAt(int index) => _items.RemoveAt(index);

		public override void RemoveAt(string parameterName)
		{
			var i = IndexOf(parameterName);
			if (i >= 0)
				_items.RemoveAt(i);
		}

		protected override DbParameter GetParameter(int index) => _items[index];

		protected override DbParameter GetParameter(string parameterName)
		{
			var i = IndexOf(parameterName);
			return i >= 0 ? _items[i] : null;
		}

		protected override void SetParameter(int index, DbParameter value) => _items[index] = value;

		protected override void SetParameter(string parameterName, DbParameter value)
		{
			var i = IndexOf(parameterName);
			if (i >= 0)
				_items[i] = value;
			else
				_items.Add(value);
		}
	}

	/// <summary>
	/// Delegating dialect that forwards every member to an inner dialect and counts
	/// <see cref="PrepareParameter"/> invocations so the test can observe whether the
	/// Ado parameter path calls it.
	/// </summary>
	private sealed class PrepareCountingDialect(ISqlDialect inner) : ISqlDialect
	{
		public int PrepareCount { get; private set; }

		public void PrepareParameter(DbParameter parameter)
		{
			PrepareCount++;
			inner.PrepareParameter(parameter);
		}

		public int MaxParameters => inner.MaxParameters;
		public string ParameterPrefix => inner.ParameterPrefix;
		public string ConcatOperator => inner.ConcatOperator;
		public string TrueLiteral => inner.TrueLiteral;
		public string FalseLiteral => inner.FalseLiteral;
		public string BooleanCastSqlType => inner.BooleanCastSqlType;
		public string DecimalComparisonCastSqlType => inner.DecimalComparisonCastSqlType;
		public string UnicodePrefix => inner.UnicodePrefix;
		public string EmptyBinaryLiteral => inner.EmptyBinaryLiteral;
		public string LenFunction => inner.LenFunction;
		public string IsNullFunction => inner.IsNullFunction;
		public string BatchSeparator => inner.BatchSeparator;
		public bool SupportsInsertReturning => inner.SupportsInsertReturning;
		public bool SupportsAddForeignKeyViaAlter => inner.SupportsAddForeignKeyViaAlter;

		public string QuoteIdentifier(string identifier) => inner.QuoteIdentifier(identifier);
		public string GetSqlTypeName(Type clrType) => inner.GetSqlTypeName(clrType);
		public string GetSqlTypeName(Type clrType, int maxLength) => inner.GetSqlTypeName(clrType, maxLength);
		public object ConvertToDbValue(object value, Type clrType) => inner.ConvertToDbValue(value, clrType);
		public object ConvertFromDbValue(object value, Type targetType) => inner.ConvertFromDbValue(value, targetType);
		public string GetIdentitySelect(string idCol) => inner.GetIdentitySelect(idCol);
		public string FormatSkip(string skip) => inner.FormatSkip(skip);
		public string FormatTake(string take) => inner.FormatTake(take);
		public string Now() => inner.Now();
		public string UtcNow() => inner.UtcNow();
		public string SysNow() => inner.SysNow();
		public string SysUtcNow() => inner.SysUtcNow();
		public string NewId() => inner.NewId();
		public string GetIdentityColumnSuffix() => inner.GetIdentityColumnSuffix();
		public string GetForeignKeyConstraint(string tableName, string columnName, string refTableName, string refColumnName)
			=> inner.GetForeignKeyConstraint(tableName, columnName, refTableName, refColumnName);
		public string GetColumnDefinition(Type clrType, bool isNullable, int maxLength = 0, int precision = 0, int scale = 0)
			=> inner.GetColumnDefinition(clrType, isNullable, maxLength, precision, scale);
		public string NormalizeDbType(string dbTypeName) => inner.NormalizeDbType(dbTypeName);
		public string GetDefaultLiteral(Type clrType) => inner.GetDefaultLiteral(clrType);

		public void AppendCreateTable(System.Text.StringBuilder sb, string tableName, string columnDefs)
			=> inner.AppendCreateTable(sb, tableName, columnDefs);
		public void AppendDropTable(System.Text.StringBuilder sb, string tableName)
			=> inner.AppendDropTable(sb, tableName);
		public void AppendPagination(System.Text.StringBuilder sb, long? skip, long? take, bool hasOrderBy)
			=> inner.AppendPagination(sb, skip, take, hasOrderBy);
		public void AppendPaginationParams(System.Text.StringBuilder sb, string skipParamExpr, string takeParamExpr)
			=> inner.AppendPaginationParams(sb, skipParamExpr, takeParamExpr);
		public void AppendFallbackOrderBy(System.Text.StringBuilder sb)
			=> inner.AppendFallbackOrderBy(sb);
		public void AppendUpsert(System.Text.StringBuilder sb, string tableName, string[] allColumns, string[] keyColumns)
			=> inner.AppendUpsert(sb, tableName, allColumns, keyColumns);
		public void AppendInsertReturningClause(System.Text.StringBuilder sb, string idColumn)
			=> inner.AppendInsertReturningClause(sb, idColumn);
		public void AppendAddForeignKey(System.Text.StringBuilder sb, string tableName, string columnName, string refTableName, string refColumnName)
			=> inner.AppendAddForeignKey(sb, tableName, columnName, refTableName, refColumnName);
		public void AppendDropForeignKey(System.Text.StringBuilder sb, string tableName, string constraintName)
			=> inner.AppendDropForeignKey(sb, tableName, constraintName);
		public void AppendCreateIndex(System.Text.StringBuilder sb, string indexName, string tableName, string columnName, bool unique)
			=> inner.AppendCreateIndex(sb, indexName, tableName, columnName, unique);
		public void AppendAddColumn(System.Text.StringBuilder sb, string tableName, string columnName, string columnDef)
			=> inner.AppendAddColumn(sb, tableName, columnName, columnDef);
		public void AppendAlterColumn(System.Text.StringBuilder sb, string tableName, string columnName, Type clrType, bool isNullable, int maxLength = 0, int precision = 0, int scale = 0)
			=> inner.AppendAlterColumn(sb, tableName, columnName, clrType, isNullable, maxLength, precision, scale);
		public void AppendDropColumn(System.Text.StringBuilder sb, string tableName, string columnName)
			=> inner.AppendDropColumn(sb, tableName, columnName);
		public void AppendUpdateWhereNull(System.Text.StringBuilder sb, string tableName, string columnName, string defaultLiteral)
			=> inner.AppendUpdateWhereNull(sb, tableName, columnName, defaultLiteral);
		public void AppendUpdateBy(System.Text.StringBuilder sb, string tableName, string[] setColumns, string[] whereColumns)
			=> inner.AppendUpdateBy(sb, tableName, setColumns, whereColumns);
		public void AppendDeleteBy(System.Text.StringBuilder sb, string tableName, string[] whereColumns)
			=> inner.AppendDeleteBy(sb, tableName, whereColumns);
		public void AppendDatePartOpen(System.Text.StringBuilder sb, string part)
			=> inner.AppendDatePartOpen(sb, part);
		public void AppendDatePartClose(System.Text.StringBuilder sb)
			=> inner.AppendDatePartClose(sb);
		public void AppendDateAdd(System.Text.StringBuilder sb, string part, string amountSql, string sourceSql)
			=> inner.AppendDateAdd(sb, part, amountSql, sourceSql);
		public void AppendDateDiff(System.Text.StringBuilder sb, string part, string startSql, string endSql)
			=> inner.AppendDateDiff(sb, part, startSql, endSql);
		public void AppendTrimOpen(System.Text.StringBuilder sb)
			=> inner.AppendTrimOpen(sb);
		public void AppendTrimClose(System.Text.StringBuilder sb)
			=> inner.AppendTrimClose(sb);

		public Task<IReadOnlyList<DbColumnInfo>> ReadDbSchemaAsync(DbConnection connection, string tableSchema = null, CancellationToken cancellationToken = default)
			=> inner.ReadDbSchemaAsync(connection, tableSchema, cancellationToken);
		public Task<IReadOnlyList<DbForeignKeyInfo>> ReadDbForeignKeysAsync(DbConnection connection, string tableSchema = null, CancellationToken cancellationToken = default)
			=> inner.ReadDbForeignKeysAsync(connection, tableSchema, cancellationToken);
		public Task<IReadOnlyList<DbIndexInfo>> ReadDbIndexesAsync(DbConnection connection, string tableSchema = null, CancellationToken cancellationToken = default)
			=> inner.ReadDbIndexesAsync(connection, tableSchema, cancellationToken);
	}

	#endregion
}
