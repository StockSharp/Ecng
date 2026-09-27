#if NET10_0_OR_GREATER

namespace Ecng.Tests.Data;

using System.Data.Common;

using Ecng.Data;
using Ecng.Serialization;

/// <summary>
/// Runs the migration the migrator generates for a changed column on a real engine, then compares again:
/// the script has to execute, keep the data, and leave nothing to report.
/// </summary>
[TestClass]
[TestCategory("Integration")]
[TestCategory("Database")]
[DoNotParallelize]
public class SchemaMigratorColumnTests : BaseTestClass
{
	private const string _table = "Ecng_TestMigrColumns";

	[ClassInitialize]
	public static void ClassInit(TestContext context) => DbTestHelper.RegisterAll();

	[ClassCleanup]
	public static void ClassCleanup() => DbTestHelper.ClearSQLitePools();

	private static async Task<DbConnection> OpenAsync(string provider, CancellationToken cancellationToken)
	{
		var factory = DbTestHelper.GetFactory(provider);
		var conn = factory.CreateConnection();
		conn.ConnectionString = DbTestHelper.TryGetConnectionString(provider);
		await conn.OpenAsync(cancellationToken);
		return conn;
	}

	private static Schema CreateSchema(params SchemaColumn[] columns)
		=> new()
		{
			TableName = _table,
			EntityType = typeof(ColAttrTestEntity),
			Identity = new SchemaColumn { Name = "Id", ClrType = typeof(long), IsReadOnly = true },
			Columns = columns,
			Factory = () => new ColAttrTestEntity(),
		};

	private static void CreateTable(string provider, string columns)
	{
		DbTestHelper.DropTable(provider, _table);
		DbTestHelper.ExecuteRaw(provider, $"CREATE TABLE {Quote(provider, _table)} ({Quote(provider, "Id")} BIGINT NOT NULL PRIMARY KEY, {columns})");
	}

	private static string Quote(string provider, string identifier)
		=> DbTestHelper.GetDialect(provider).QuoteIdentifier(identifier);

	private async Task<IReadOnlyList<SchemaDiff>> CompareAsync(string provider, Schema schema)
	{
		using var conn = await OpenAsync(provider, CancellationToken);
		var diffs = await SchemaMigrator.CompareAsync([schema], conn, DbTestHelper.GetDialect(provider), skipComputed: false, cancellationToken: CancellationToken);
		return [.. diffs.Where(d => d.TableName.EqualsIgnoreCase(_table))];
	}

	private async Task<string> MigrateAsync(string provider, Schema schema, IReadOnlyList<SchemaDiff> diffs, string sessionSetup = null)
	{
		var dialect = DbTestHelper.GetDialect(provider);
		var sql = SchemaMigrator.GenerateMigrationSql(dialect, diffs, [schema]);

		using var conn = await OpenAsync(provider, CancellationToken);

		if (!sessionSetup.IsEmpty())
		{
			using var cmd = conn.CreateCommand();
			cmd.CommandText = sessionSetup;
			await cmd.ExecuteNonQueryAsync(CancellationToken);
		}

		await SchemaMigrator.ApplyAsync(conn, sql, dialect, CancellationToken);

		return sql;
	}

	private async Task<DbColumnInfo> ReadColumnAsync(string provider, string column)
	{
		using var conn = await OpenAsync(provider, CancellationToken);
		var columns = await DbTestHelper.GetDialect(provider).ReadDbSchemaAsync(conn, cancellationToken: CancellationToken);
		return columns.Single(c => c.TableName.EqualsIgnoreCase(_table) && c.ColumnName.EqualsIgnoreCase(column));
	}

	private static decimal ReadDecimal(string provider, string column)
		=> DbTestHelper.ExecuteScalarRaw(provider, $"SELECT {Quote(provider, column)} FROM {Quote(provider, _table)}").To<decimal>();

	private static string Describe(IEnumerable<SchemaDiff> diffs)
		=> diffs.Select(d => $"{d.Kind} {d.ColumnName} ({d.Expected} vs {d.Actual})").JoinCommaSpace();

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	public async Task DeclaredDecimalDigits_AreMigrated_AndSettle(string provider)
	{
		DbTestHelper.SkipIfUnavailable(provider);

		CreateTable(provider,
			$"{Quote(provider, "Amount")} DECIMAL(18,2) NOT NULL, " +
			$"{Quote(provider, "Rate")} DECIMAL(18,8) NOT NULL, " +
			$"{Quote(provider, "Qty")} DECIMAL(18,8) NOT NULL");

		DbTestHelper.ExecuteRaw(provider, $"INSERT INTO {Quote(provider, _table)} VALUES (1, 12.34, 0.12345678, 5)");

		var schema = CreateSchema(
			new SchemaColumn { Name = "Amount", ClrType = typeof(decimal), Precision = 18, Scale = 6 },
			new SchemaColumn { Name = "Rate", ClrType = typeof(decimal), Scale = 6 },
			new SchemaColumn { Name = "Qty", ClrType = typeof(decimal), Precision = 18 });

		var diffs = await CompareAsync(provider, schema);

		diffs.Count(d => d.Kind == SchemaDiffKind.PrecisionMismatch).AssertEqual(3, Describe(diffs));

		var sql = await MigrateAsync(provider, schema, diffs);

		var after = await CompareAsync(provider, schema);
		after.Count.AssertEqual(0, $"{Describe(after)}; script: {sql}");

		var amount = await ReadColumnAsync(provider, "Amount");
		amount.NumericPrecision.AssertEqual(18);
		amount.NumericScale.AssertEqual(6);

		var rate = await ReadColumnAsync(provider, "Rate");
		rate.NumericPrecision.AssertEqual(18);
		rate.NumericScale.AssertEqual(6);

		var qty = await ReadColumnAsync(provider, "Qty");
		qty.NumericPrecision.AssertEqual(18);
		qty.NumericScale.AssertEqual(0);

		ReadDecimal(provider, "Amount").AssertEqual(12.34m);
		ReadDecimal(provider, "Rate").AssertEqual(0.123457m);
		ReadDecimal(provider, "Qty").AssertEqual(5m);
	}

	[TestMethod]
	public async Task UnconstrainedNumeric_AgainstDeclaredDigits_IsMigrated()
	{
		const string provider = DatabaseProviderRegistry.PostgreSql;

		DbTestHelper.SkipIfUnavailable(provider);

		CreateTable(provider, $"{Quote(provider, "Amount")} NUMERIC NOT NULL");
		DbTestHelper.ExecuteRaw(provider, $"INSERT INTO {Quote(provider, _table)} VALUES (1, 1.5)");

		var schema = CreateSchema(new SchemaColumn { Name = "Amount", ClrType = typeof(decimal), Precision = 18, Scale = 6 });

		var diffs = await CompareAsync(provider, schema);
		diffs.Count(d => d.Kind == SchemaDiffKind.PrecisionMismatch).AssertEqual(1, Describe(diffs));

		var sql = await MigrateAsync(provider, schema, diffs);

		(await CompareAsync(provider, schema)).Count.AssertEqual(0, sql);

		var amount = await ReadColumnAsync(provider, "Amount");
		amount.NumericPrecision.AssertEqual(18);
		amount.NumericScale.AssertEqual(6);
		ReadDecimal(provider, "Amount").AssertEqual(1.5m);
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer, "DATETIME2(7)")]
	[DataRow(DatabaseProviderRegistry.PostgreSql, "TIMESTAMPTZ")]
	public async Task DeclaredTimePrecision_IsCompared_AndMigrated(string provider, string liveType)
	{
		DbTestHelper.SkipIfUnavailable(provider);

		CreateTable(provider, $"{Quote(provider, "Created")} {liveType} NOT NULL");

		var schema = CreateSchema(new SchemaColumn { Name = "Created", ClrType = typeof(DateTime), Precision = 3 });

		var diffs = await CompareAsync(provider, schema);
		diffs.Count(d => d.Kind == SchemaDiffKind.PrecisionMismatch).AssertEqual(1, Describe(diffs));

		var sql = await MigrateAsync(provider, schema, diffs);

		var after = await CompareAsync(provider, schema);
		after.Count.AssertEqual(0, $"{Describe(after)}; script: {sql}");

		(await ReadColumnAsync(provider, "Created")).NumericPrecision.AssertEqual(3);
	}

	[TestMethod]
	public async Task NullabilityChange_OnAColumnAViewReads_PostgreSql()
	{
		const string provider = DatabaseProviderRegistry.PostgreSql;

		DbTestHelper.SkipIfUnavailable(provider);

		const string view = "Ecng_TestMigrColumnsView";
		DbTestHelper.ExecuteRaw(provider, $"DROP VIEW IF EXISTS {Quote(provider, view)}");
		CreateTable(provider, $"{Quote(provider, "Amount")} NUMERIC(18,6) NULL");
		DbTestHelper.ExecuteRaw(provider, $"INSERT INTO {Quote(provider, _table)} VALUES (1, 2.5)");
		DbTestHelper.ExecuteRaw(provider, $"CREATE VIEW {Quote(provider, view)} AS SELECT {Quote(provider, "Amount")} FROM {Quote(provider, _table)}");

		try
		{
			var schema = CreateSchema(new SchemaColumn { Name = "Amount", ClrType = typeof(decimal), Precision = 18, Scale = 6 });

			var diffs = await CompareAsync(provider, schema);
			diffs.Single().Kind.AssertEqual(SchemaDiffKind.NullabilityMismatch);

			var sql = await MigrateAsync(provider, schema, diffs);

			(await CompareAsync(provider, schema)).Count.AssertEqual(0, sql);
			(await ReadColumnAsync(provider, "Amount")).IsNullable.AssertFalse();
		}
		finally
		{
			DbTestHelper.ExecuteRaw(provider, $"DROP VIEW IF EXISTS {Quote(provider, view)}");
		}
	}

	[TestMethod]
	public async Task NullabilityChange_OnAVarcharWithADefault_KeepsItVarchar_SqlServer()
	{
		const string provider = DatabaseProviderRegistry.SqlServer;

		DbTestHelper.SkipIfUnavailable(provider);

		CreateTable(provider, $"{Quote(provider, "Email")} VARCHAR(256) NULL CONSTRAINT DF_Ecng_TestMigrColumns_Email DEFAULT ''");
		DbTestHelper.ExecuteRaw(provider, $"INSERT INTO {Quote(provider, _table)} (Id, Email) VALUES (1, 'a@b.c')");

		var schema = CreateSchema(new SchemaColumn { Name = "Email", ClrType = typeof(string), MaxLength = 256 });

		var diffs = await CompareAsync(provider, schema);
		diffs.Single().Kind.AssertEqual(SchemaDiffKind.NullabilityMismatch);

		var sql = await MigrateAsync(provider, schema, diffs);

		(await CompareAsync(provider, schema)).Count.AssertEqual(0, sql);

		var email = await ReadColumnAsync(provider, "Email");
		email.DataType.AssertEqual("varchar");
		email.IsNullable.AssertFalse();
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	public async Task MissingFilteredIndex_IsCreatedWithItsFilter(string provider)
	{
		DbTestHelper.SkipIfUnavailable(provider);

		CreateTable(provider, $"{Quote(provider, "Code")} VARCHAR(32) NULL");
		DbTestHelper.ExecuteRaw(provider, $"INSERT INTO {Quote(provider, _table)} VALUES (1, NULL)");
		DbTestHelper.ExecuteRaw(provider, $"INSERT INTO {Quote(provider, _table)} VALUES (2, NULL)");

		var schema = CreateSchema(new SchemaColumn
		{
			Name = "Code",
			ClrType = typeof(string),
			MaxLength = 32,
			IsNullable = true,
			Indexes = [new(null, 0, IsUnique: true, Condition: "{Code} IS NOT NULL")],
		});

		var diffs = await CompareAsync(provider, schema);
		diffs.Single().Kind.AssertEqual(SchemaDiffKind.MissingIndex);

		var sql = await MigrateAsync(provider, schema, diffs);

		sql.ContainsIgnoreCase("WHERE").AssertTrue(sql);
		(await CompareAsync(provider, schema)).Count.AssertEqual(0, sql);

		DbTestHelper.ExecuteRaw(provider, $"INSERT INTO {Quote(provider, _table)} VALUES (3, 'A')");
		Throws<DbException>(() => DbTestHelper.ExecuteRaw(provider, $"INSERT INTO {Quote(provider, _table)} VALUES (4, 'A')"));
	}

	private async Task<string[]> ReadIndexNamesAsync(string provider)
	{
		using var conn = await OpenAsync(provider, CancellationToken);
		var indexes = await DbTestHelper.GetDialect(provider).ReadDbIndexesAsync(conn, cancellationToken: CancellationToken);
		return [.. indexes.Where(i => i.TableName.EqualsIgnoreCase(_table) && !i.IsPrimaryKey).Select(i => i.IndexName).Distinct()];
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	public async Task AlteringAnIndexedColumn_KeepsItsIndex(string provider)
	{
		DbTestHelper.SkipIfUnavailable(provider);

		CreateTable(provider,
			$"{Quote(provider, "Amount")} DECIMAL(18,2) NOT NULL, " +
			$"{Quote(provider, "Code")} VARCHAR(32) NULL");

		DbTestHelper.ExecuteRaw(provider, $"CREATE INDEX {Quote(provider, "IX_Custom_Amount")} ON {Quote(provider, _table)} ({Quote(provider, "Amount")})");
		DbTestHelper.ExecuteRaw(provider, $"CREATE UNIQUE INDEX {Quote(provider, "UX_Custom_Code")} ON {Quote(provider, _table)} ({Quote(provider, "Code")})");
		DbTestHelper.ExecuteRaw(provider, $"INSERT INTO {Quote(provider, _table)} VALUES (1, 12.34, 'A')");

		var schema = CreateSchema(
			new SchemaColumn { Name = "Amount", ClrType = typeof(decimal), Precision = 18, Scale = 6, Indexes = [new(null, 0)] },
			new SchemaColumn { Name = "Code", ClrType = typeof(string), MaxLength = 32, Indexes = [new(null, 0, IsUnique: true)] });

		var diffs = await CompareAsync(provider, schema);
		diffs.Select(d => d.Kind).OrderBy(k => k).SequenceEqual([SchemaDiffKind.NullabilityMismatch, SchemaDiffKind.PrecisionMismatch]).AssertTrue(Describe(diffs));

		var sql = await MigrateAsync(provider, schema, diffs);

		var after = await CompareAsync(provider, schema);
		after.Count.AssertEqual(0, $"{Describe(after)}; script: {sql}");

		(await ReadIndexNamesAsync(provider)).OrderBy(n => n, StringComparer.Ordinal).SequenceEqual(["IX_Custom_Amount", "UX_Custom_Code"]).AssertTrue(sql);
		ReadDecimal(provider, "Amount").AssertEqual(12.34m);
	}

	[TestMethod]
	public async Task NewRequiredColumns_AreAdded_SQLite()
	{
		const string provider = DatabaseProviderRegistry.SQLite;

		DbTestHelper.SkipIfUnavailable(provider);

		CreateTable(provider, $"{Quote(provider, "Name")} TEXT NOT NULL");
		DbTestHelper.ExecuteRaw(provider, $"INSERT INTO {Quote(provider, _table)} VALUES (1, 'a')");

		var schema = CreateSchema(
			new SchemaColumn { Name = "Name", ClrType = typeof(string) },
			new SchemaColumn { Name = "Fee", ClrType = typeof(decimal) },
			new SchemaColumn { Name = "Day", ClrType = typeof(DateOnly) });

		var diffs = await CompareAsync(provider, schema);
		diffs.Count(d => d.Kind == SchemaDiffKind.MissingColumn).AssertEqual(2, Describe(diffs));

		var sql = await MigrateAsync(provider, schema, diffs);

		(await CompareAsync(provider, schema)).Count.AssertEqual(0, sql);
		ReadDecimal(provider, "Fee").AssertEqual(0m);
	}

	[TestMethod]
	public async Task TimestampToTimestamptz_KeepsTheStoredMoment_PostgreSql()
	{
		const string provider = DatabaseProviderRegistry.PostgreSql;

		DbTestHelper.SkipIfUnavailable(provider);

		CreateTable(provider, $"{Quote(provider, "Created")} TIMESTAMP NOT NULL");
		DbTestHelper.ExecuteRaw(provider, $"INSERT INTO {Quote(provider, _table)} VALUES (1, '2026-01-01 00:00:00')");

		var schema = CreateSchema(new SchemaColumn { Name = "Created", ClrType = typeof(DateTime) });

		var diffs = await CompareAsync(provider, schema);
		diffs.Single().Kind.AssertEqual(SchemaDiffKind.TypeMismatch);

		var sql = await MigrateAsync(provider, schema, diffs, "SET TIME ZONE 'Europe/Moscow'");

		(await CompareAsync(provider, schema)).Count.AssertEqual(0, sql);

		DbTestHelper.ExecuteScalarRaw(provider, $"SELECT to_char({Quote(provider, "Created")} AT TIME ZONE 'UTC', 'YYYY-MM-DD HH24:MI') FROM {Quote(provider, _table)}")
			.AssertEqual("2026-01-01 00:00");
	}
}

#endif
