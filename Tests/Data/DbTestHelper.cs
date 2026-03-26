namespace Ecng.Tests.Data;

using System.Data.Common;

using Ecng.Data;
using Ecng.Data.Sql;
using Ecng.IO;

using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;

/// <summary>
/// Shared infrastructure for parameterized database integration tests.
/// Provides provider resolution, registration, and helper methods.
/// </summary>
static class DbTestHelper
{
	private static readonly object _lock = new();
	private static bool _registered;
	private static string _sqliteDbPath;

	/// <summary>
	/// Registers all provider factories and dialects. Thread-safe, idempotent.
	/// </summary>
	public static void RegisterAll()
	{
		lock (_lock)
		{
			if (_registered)
				return;

#if NET10_0_OR_GREATER
			SqlServerDialect.Register(SqlClientFactory.Instance);
			PostgreSqlDialect.Register(Npgsql.NpgsqlFactory.Instance);
#endif
			SQLiteDialect.Register(SqliteFactory.Instance);

			_sqliteDbPath = Path.Combine(LocalFileSystem.Instance.GetTempPath(), "ecng_test.db");
			_registered = true;
		}
	}

	/// <summary>
	/// Returns connection string for the given provider, or null if unavailable.
	/// </summary>
	public static string TryGetConnectionString(string provider)
	{
		return provider switch
		{
			DatabaseProviderRegistry.SqlServer => TryGetEnv("SQLSERVER_CONNECTION_STRING"),
			DatabaseProviderRegistry.PostgreSql => TryGetEnv("PG_CONNECTION_STRING"),
			DatabaseProviderRegistry.SQLite => $"Data Source={_sqliteDbPath}",
			_ => throw new ArgumentException($"Unknown provider: {provider}"),
		};
	}

	private static string TryGetEnv(string name)
	{
		var value = Environment.GetEnvironmentVariable(name);
		if (!value.IsEmpty())
			return value;

		return Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Machine);
	}

	/// <summary>
	/// Returns DbProviderFactory for the given provider.
	/// </summary>
	public static DbProviderFactory GetFactory(string provider)
	{
		return provider switch
		{
			DatabaseProviderRegistry.SqlServer => SqlClientFactory.Instance,
#if NET10_0_OR_GREATER
			DatabaseProviderRegistry.PostgreSql => Npgsql.NpgsqlFactory.Instance,
#endif
			DatabaseProviderRegistry.SQLite => SqliteFactory.Instance,
			_ => throw new ArgumentException($"Unknown provider: {provider}"),
		};
	}

	/// <summary>
	/// Returns ISqlDialect for the given provider.
	/// </summary>
	public static ISqlDialect GetDialect(string provider)
	{
		return provider switch
		{
			DatabaseProviderRegistry.SqlServer => SqlServerDialect.Instance,
#if NET10_0_OR_GREATER
			DatabaseProviderRegistry.PostgreSql => PostgreSqlDialect.Instance,
#endif
			DatabaseProviderRegistry.SQLite => SQLiteDialect.Instance,
			_ => throw new ArgumentException($"Unknown provider: {provider}"),
		};
	}

	/// <summary>
	/// Creates a <see cref="DatabaseConnectionPair"/> for the given provider.
	/// Returns null if connection string is unavailable.
	/// </summary>
	public static DatabaseConnectionPair TryGetConnectionPair(string provider)
	{
		var connStr = TryGetConnectionString(provider);
		if (connStr.IsEmpty())
			return null;

		return new()
		{
			Provider = provider,
			ConnectionString = connStr,
		};
	}

	/// <summary>
	/// Skips the test if the provider is unavailable (missing connection string or unsupported framework).
	/// </summary>
	public static void SkipIfUnavailable(string provider)
	{
#if !NET10_0_OR_GREATER
		if (provider is DatabaseProviderRegistry.SqlServer or DatabaseProviderRegistry.PostgreSql)
			Assert.Inconclusive($"{provider} tests only run on .NET 10+");
#endif

		var connStr = TryGetConnectionString(provider);
		if (connStr.IsEmpty())
			Assert.Inconclusive($"Connection string not configured for {provider}.");
	}

#if NET10_0_OR_GREATER

	/// <summary>
	/// Creates a <see cref="Database"/> ORM instance for the given provider.
	/// </summary>
	public static Database CreateDatabase(string provider)
	{
		var connStr = TryGetConnectionString(provider);
		var factory = GetFactory(provider);
		var dialect = GetDialect(provider);

		var db = new Database($"Test_{provider}", connStr, factory, dialect);
		db.AllowDeleteAll = true;
		return db;
	}

	/// <summary>
	/// Creates a table from entity schema using dialect-aware SQL.
	/// </summary>
	/// <param name="provider">Database provider name.</param>
	/// <param name="meta">Entity schema.</param>
	/// <param name="autoIncrement">If true, identity column uses auto-increment. If false, PK without auto-increment.</param>
	public static void EnsureTable(string provider, Ecng.Serialization.Schema meta, bool autoIncrement = true)
	{
		var dialect = GetDialect(provider);

		// Drop existing table to ensure correct schema (e.g., IDENTITY columns)
		var dropSql = Query.CreateDropTable(meta.TableName).Render(dialect);
		ExecuteRaw(provider, dropSql);

		var columns = new Dictionary<string, Type>();

		if (meta.Identity is not null)
			columns[meta.Identity.Name] = meta.Identity.ClrType;

		foreach (var col in meta.Columns)
			columns[col.Name] = col.ClrType;

		var sql = autoIncrement
			? Query.CreateCreateTable(meta.TableName, columns, meta.Identity?.Name).Render(dialect)
			: Query.CreateCreateTable(meta.TableName, columns, primaryKeyColumns: meta.Identity is not null ? [meta.Identity.Name] : null).Render(dialect);
		ExecuteRaw(provider, sql);
	}

#endif

	/// <summary>
	/// Executes raw SQL using the given provider's factory and connection string.
	/// </summary>
	public static void ExecuteRaw(string provider, string sql)
	{
		var connStr = TryGetConnectionString(provider);
		var factory = GetFactory(provider);

		using var conn = factory.CreateConnection();
		conn.ConnectionString = connStr;
		conn.Open();
		using var cmd = conn.CreateCommand();
		cmd.CommandText = sql;
		cmd.ExecuteNonQuery();
	}

	/// <summary>
	/// Deletes all rows from a table using dialect-aware quoting.
	/// </summary>
	public static void DeleteAll(string provider, string tableName)
	{
		var dialect = GetDialect(provider);
		var quoted = dialect.QuoteIdentifier(tableName);
		ExecuteRaw(provider, $"DELETE FROM {quoted}");
	}

	/// <summary>
	/// Clears SQLite connection pool. Call in ClassCleanup.
	/// </summary>
	public static void ClearSQLitePools()
	{
		SqliteConnection.ClearAllPools();
	}
}
