#if NET10_0_OR_GREATER

namespace Ecng.Tests.Data;

using System.Data.Common;

using Ecng.Data;
using Ecng.Data.Sql;
using Ecng.Serialization;

/// <summary>
/// Verifies the schema migrator backfills missing single-column indexes —
/// the pre-existing pipeline only emitted <c>CREATE INDEX</c> as part of
/// <c>CREATE TABLE</c> (the <see cref="SchemaDiffKind.MissingTable"/>
/// branch), so a table that pre-dated an entity's <c>[Index]</c> attribute
/// stayed un-indexed forever.
/// </summary>
[TestClass]
[TestCategory("Integration")]
[TestCategory("Database")]
[DoNotParallelize]
public class SchemaMigratorIndexTests : BaseTestClass
{
	[ClassInitialize]
	public static void ClassInit(TestContext context) => DbTestHelper.RegisterAll();

	[ClassCleanup]
	public static void ClassCleanup() => DbTestHelper.ClearSQLitePools();

	private static void SetUp(string provider)
	{
		DbTestHelper.SkipIfUnavailable(provider);

		// Drop + recreate fresh — EnsureTable emits CREATE TABLE without any
		// CREATE INDEX, leaving the [Index]-decorated columns un-indexed
		// (the precondition the migrator must repair).
		DbTestHelper.DropTable(provider, "Ecng_TestIndexed");
		DbTestHelper.EnsureTable(provider, SchemaRegistry.Get(typeof(TestIndexed)));
	}

	private static async Task<DbConnection> OpenAsync(string provider, CancellationToken cancellationToken)
	{
		var factory = DbTestHelper.GetFactory(provider);
		var connStr = DbTestHelper.TryGetConnectionString(provider);
		var conn = factory.CreateConnection();
		conn.ConnectionString = connStr;
		await conn.OpenAsync(cancellationToken);
		return conn;
	}

	private static async Task<int> CountIndexesOn(string provider, string tableName, CancellationToken cancellationToken)
	{
		var dialect = DbTestHelper.GetDialect(provider);
		using var conn = await OpenAsync(provider, cancellationToken);
		var indexes = await dialect.ReadDbIndexesAsync(conn, cancellationToken: cancellationToken);

		// Filter out the PK backing index — it's implicit and not what the
		// [Index] attribute tracks. The migrator skips it via the same rule
		// in AppendIndexDiffs.
		return indexes.Count(i => i.TableName.EqualsIgnoreCase(tableName) && !i.IsPrimaryKey);
	}

	private static IReadOnlyList<SchemaDiff> KeepOnlyIndexDiffs(IReadOnlyList<SchemaDiff> diffs)
		=> [.. diffs.Where(d => d.Kind is SchemaDiffKind.MissingIndex or SchemaDiffKind.ExtraIndex)];

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public void CreateTable_CompositeIndex_EmitsOneIndexWithBothColumns(string provider)
	{
		// Pure DDL-text check: when the migrator hits MissingTable for an
		// entity whose [Index(Name = "X", Order = N)] groups two columns,
		// the emitted SQL must contain one CREATE INDEX listing both
		// columns in the right order, plus a second CREATE INDEX for any
		// standalone single-column index on the same table.
		DbTestHelper.RegisterAll();
		var dialect = DbTestHelper.GetDialect(provider);
		var schema = SchemaRegistry.Get(typeof(TestCompositeIx));

		var diff = new SchemaDiff("Ecng_TestCompositeIx", string.Empty, SchemaDiffKind.MissingTable, "expected", "missing");
		var sql = SchemaMigrator.GenerateMigrationSql(dialect, [diff], [schema]);

		// Exactly one composite + one single-column index.
		System.Text.RegularExpressions.Regex.Matches(sql, "CREATE (UNIQUE )?INDEX", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
			.Count.AssertEqual(2, $"Expected 2 CREATE INDEX statements (1 composite + 1 single), got: {sql}");

		// Composite index references both columns in declared order.
		var compositeMatch = System.Text.RegularExpressions.Regex.Match(sql,
			@"CREATE\s+INDEX\s+""?\[?IX_TenantCreated\]?""?\s+ON\s+""?\[?Ecng_TestCompositeIx\]?""?\s*\(\s*""?\[?TenantId\]?""?\s*,\s*""?\[?CreatedAt\]?""?\s*\)",
			System.Text.RegularExpressions.RegexOptions.IgnoreCase);
		compositeMatch.Success.AssertTrue(
			$"Expected composite index IX_TenantCreated(TenantId, CreatedAt), got: {sql}");

		// Standalone Priority index uses the IX_{Table}_{Column} fallback name.
		sql.Contains("IX_Ecng_TestCompositeIx_Priority").AssertTrue(
			$"Expected single-column index IX_Ecng_TestCompositeIx_Priority alongside the composite, got: {sql}");
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public void CreateTable_MultipleAttributes_EmitsAllDeclaredIndexes(string provider)
	{
		// `TestMultiIx.TenantId` carries three [Index] attributes — its own
		// single-column index plus the leading slot of two distinct named
		// composites. The migrator must emit one CREATE INDEX per declared
		// index, not collapse them into the first attribute it finds.
		DbTestHelper.RegisterAll();
		var dialect = DbTestHelper.GetDialect(provider);
		var schema = SchemaRegistry.Get(typeof(TestMultiIx));

		var diff = new SchemaDiff("Ecng_TestMultiIx", string.Empty, SchemaDiffKind.MissingTable, "expected", "missing");
		var sql = SchemaMigrator.GenerateMigrationSql(dialect, [diff], [schema]);

		// Standalone single-column index on TenantId — uses IX_{Table}_{Column}.
		sql.Contains("IX_Ecng_TestMultiIx_TenantId").AssertTrue(
			$"Expected standalone TenantId index, got: {sql}");

		// First composite (TenantId, CreatedAt).
		System.Text.RegularExpressions.Regex.Match(sql,
			@"CREATE\s+INDEX\s+""?\[?IX_MultiIx_TenantCreated\]?""?\s+ON\s+""?\[?Ecng_TestMultiIx\]?""?\s*\(\s*""?\[?TenantId\]?""?\s*,\s*""?\[?CreatedAt\]?""?\s*\)",
			System.Text.RegularExpressions.RegexOptions.IgnoreCase).Success.AssertTrue(
				$"Expected composite IX_MultiIx_TenantCreated(TenantId, CreatedAt), got: {sql}");

		// Second composite (TenantId, EntryType) — TenantId shared with the first.
		System.Text.RegularExpressions.Regex.Match(sql,
			@"CREATE\s+INDEX\s+""?\[?IX_MultiIx_TenantEntryType\]?""?\s+ON\s+""?\[?Ecng_TestMultiIx\]?""?\s*\(\s*""?\[?TenantId\]?""?\s*,\s*""?\[?EntryType\]?""?\s*\)",
			System.Text.RegularExpressions.RegexOptions.IgnoreCase).Success.AssertTrue(
				$"Expected composite IX_MultiIx_TenantEntryType(TenantId, EntryType), got: {sql}");

		// Exactly three CREATE INDEX statements — one standalone + two composites.
		System.Text.RegularExpressions.Regex.Matches(sql, "CREATE (UNIQUE )?INDEX", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
			.Count.AssertEqual(3, $"Expected 3 CREATE INDEX statements (1 single + 2 composites), got: {sql}");
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Backfill_MultipleAttributes_AddsAllMissingIndexes(string provider)
	{
		DbTestHelper.SkipIfUnavailable(provider);
		DbTestHelper.DropTable(provider, "Ecng_TestMultiIx");
		DbTestHelper.EnsureTable(provider, SchemaRegistry.Get(typeof(TestMultiIx)));

		// Precondition: 0 user indexes.
		(await CountIndexesOn(provider, "Ecng_TestMultiIx", CancellationToken)).AssertEqual(0);

		var dialect = DbTestHelper.GetDialect(provider);
		using var conn = await OpenAsync(provider, CancellationToken);
		var schemas = new[] { SchemaRegistry.Get(typeof(TestMultiIx)) };

		var diffs = KeepOnlyIndexDiffs(await SchemaMigrator.CompareAsync(
			schemas, conn, dialect, skipComputed: false, cancellationToken: CancellationToken));
		var sql = SchemaMigrator.GenerateMigrationSql(dialect, diffs, schemas);

		if (!sql.IsEmpty())
			await SchemaMigrator.ApplyAsync(conn, sql, dialect, CancellationToken);

		// SQL Server / Postgres report one row per indexed column, so a
		// composite (TenantId, CreatedAt) shows up as TWO rows sharing
		// IndexName. Total rows = 1 (single TenantId) + 2 (IX_TenantCreated)
		// + 2 (IX_TenantEntryType) = 5.
		var dbIxs = await dialect.ReadDbIndexesAsync(conn, cancellationToken: CancellationToken);
		var userIxs = dbIxs.Where(i => i.TableName.EqualsIgnoreCase("Ecng_TestMultiIx") && !i.IsPrimaryKey).ToArray();

		userIxs.Length.AssertEqual(5,
			$"Expected 5 rows (1 single-col + 2 composites x 2 cols each), got: " +
			$"[{string.Join(", ", userIxs.Select(i => $"{i.IndexName}({i.ColumnName}@{i.ColumnOrdinal})"))}]");

		// Distinct index names: 3 — IX_Ecng_TestMultiIx_TenantId, IX_MultiIx_TenantCreated, IX_MultiIx_TenantEntryType.
		userIxs.Select(i => i.IndexName).Distinct(StringComparer.OrdinalIgnoreCase).Count().AssertEqual(3,
			$"Expected 3 distinct user indexes, got: [{string.Join(", ", userIxs.Select(i => i.IndexName).Distinct())}]");
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Backfill_AddsMissingIndex_OnExistingTable(string provider)
	{
		SetUp(provider);

		// Precondition: table exists with no indexes (PK aside).
		(await CountIndexesOn(provider, "Ecng_TestIndexed", CancellationToken)).AssertEqual(0);

		var dialect = DbTestHelper.GetDialect(provider);
		using var conn = await OpenAsync(provider, CancellationToken);

		var schemas = new[] { SchemaRegistry.Get(typeof(TestIndexed)) };

		// Narrow to index-only diffs so unrelated column-level drift from
		// DbTestHelper.EnsureTable (which doesn't preserve every column
		// attribute) doesn't interfere with the assertion under test.
		var allDiffs = await SchemaMigrator.CompareAsync(
			schemas, conn, dialect, skipComputed: false, cancellationToken: CancellationToken);
		var diffs = KeepOnlyIndexDiffs(allDiffs);

		var sql = SchemaMigrator.GenerateMigrationSql(dialect, diffs, schemas);

		if (!sql.IsEmpty())
			await SchemaMigrator.ApplyAsync(conn, sql, dialect, CancellationToken);

		// Postcondition: TestIndexed now carries two indexes (Priority + Owner).
		(await CountIndexesOn(provider, "Ecng_TestIndexed", CancellationToken)).AssertEqual(
			2,
			$"Expected migrator to backfill missing indexes for Priority + Owner. " +
			$"Diffs: [{string.Join(", ", diffs.Select(d => $"{d.Kind} {d.TableName}.{d.ColumnName}"))}]. " +
			$"Generated SQL: {sql}");
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task Backfill_IsIdempotent(string provider)
	{
		SetUp(provider);

		var dialect = DbTestHelper.GetDialect(provider);
		using var conn = await OpenAsync(provider, CancellationToken);

		var schemas = new[] { SchemaRegistry.Get(typeof(TestIndexed)) };

		// First pass — adds the indexes (index-only slice).
		var diffs1 = KeepOnlyIndexDiffs(await SchemaMigrator.CompareAsync(
			schemas, conn, dialect, skipComputed: false, cancellationToken: CancellationToken));
		var sql1 = SchemaMigrator.GenerateMigrationSql(dialect, diffs1, schemas);
		if (!sql1.IsEmpty())
			await SchemaMigrator.ApplyAsync(conn, sql1, dialect, CancellationToken);

		// Second pass — must produce no actionable MissingIndex diffs.
		var diffs2 = await SchemaMigrator.CompareAsync(
			schemas, conn, dialect, skipComputed: false, cancellationToken: CancellationToken);

		diffs2.Count(d => d.Kind == SchemaDiffKind.MissingIndex).AssertEqual(
			0,
			$"Second pass should not raise any MissingIndex; got: " +
			$"[{string.Join(", ", diffs2.Where(d => d.Kind == SchemaDiffKind.MissingIndex).Select(d => $"{d.TableName}.{d.ColumnName}"))}]");
	}
}

#endif
