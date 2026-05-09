#if NET10_0_OR_GREATER

namespace Ecng.Tests.Data;

using System.Data.Common;

using Ecng.Data;
using Ecng.Data.Sql;
using Ecng.Serialization;

/// <summary>
/// Reproduces the FK-backfill gap: <see cref="DbTestHelper.EnsureTable"/>
/// builds CREATE TABLE statements without REFERENCES clauses, so a column
/// declared with <c>[RelationSingle]</c> ends up as a plain integer column.
/// The current <see cref="SchemaMigrator.Compare"/> only inspects column
/// metadata — it never reads the database FK catalogue, so subsequent
/// startups don't detect the missing constraint and never emit the
/// <c>ALTER TABLE … ADD CONSTRAINT</c> needed to backfill it.
/// </summary>
[TestClass]
[TestCategory("Integration")]
[TestCategory("Database")]
[DoNotParallelize]
public class SchemaMigratorFkTests : BaseTestClass
{
	[ClassInitialize]
	public static void ClassInit(TestContext context) => DbTestHelper.RegisterAll();

	[ClassCleanup]
	public static void ClassCleanup() => DbTestHelper.ClearSQLitePools();

	private static void SetUp(string provider)
	{
		DbTestHelper.SkipIfUnavailable(provider);

		// Drop child first so any leftover FK from a prior backfill no
		// longer references the parent we are about to drop.
		DbTestHelper.DropTable(provider, "Ecng_TestTask");
		DbTestHelper.DropTable(provider, "Ecng_TestPerson");

		// Recreate fresh — EnsureTable emits CREATE TABLE without any
		// REFERENCES clause, leaving the [RelationSingle] column FK-less
		// by design (the precondition the backfill must repair).
		DbTestHelper.EnsureTable(provider, SchemaRegistry.Get(typeof(TestPerson)));
		DbTestHelper.EnsureTable(provider, SchemaRegistry.Get(typeof(TestTask)));
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

	private static async Task<int> CountForeignKeysOn(string provider, string tableName, CancellationToken cancellationToken)
	{
		var dialect = DbTestHelper.GetDialect(provider);
		using var conn = await OpenAsync(provider, cancellationToken);
		var fks = await dialect.ReadDbForeignKeysAsync(conn, cancellationToken: cancellationToken);
		return fks.Count(fk => fk.TableName.EqualsIgnoreCase(tableName));
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	public async Task Backfill_AddsMissingForeignKey_OnExistingTable(string provider)
	{
		SetUp(provider);

		// Precondition: tables exist without any FK.
		(await CountForeignKeysOn(provider, "Ecng_TestTask", CancellationToken)).AssertEqual(0);

		var dialect = DbTestHelper.GetDialect(provider);
		using var conn = await OpenAsync(provider, CancellationToken);

		var schemas = new[]
		{
			SchemaRegistry.Get(typeof(TestPerson)),
			SchemaRegistry.Get(typeof(TestTask)),
		};

		var diffs = await SchemaMigrator.CompareAsync(
			schemas, conn, dialect, skipComputed: false, cancellationToken: CancellationToken);

		var sql = SchemaMigrator.GenerateMigrationSql(dialect, diffs, schemas);

		if (!sql.IsEmpty())
			await SchemaMigrator.ApplyAsync(conn, sql, dialect, CancellationToken);

		// Postcondition: TestTask.Person FK now references TestPerson.
		(await CountForeignKeysOn(provider, "Ecng_TestTask", CancellationToken)).AssertEqual(
			1,
			$"Expected the migrator pipeline to backfill the missing FK on Ecng_TestTask. " +
			$"Diffs: [{string.Join(", ", diffs.Select(d => $"{d.Kind} {d.TableName}.{d.ColumnName}"))}]. " +
			$"Generated SQL: {sql}");
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	public async Task Backfill_IsIdempotent(string provider)
	{
		SetUp(provider);

		var dialect = DbTestHelper.GetDialect(provider);
		using var conn = await OpenAsync(provider, CancellationToken);

		var schemas = new[]
		{
			SchemaRegistry.Get(typeof(TestPerson)),
			SchemaRegistry.Get(typeof(TestTask)),
		};

		// First pass — adds the FK.
		var diffs1 = await SchemaMigrator.CompareAsync(
			schemas, conn, dialect, skipComputed: false, cancellationToken: CancellationToken);
		var sql1 = SchemaMigrator.GenerateMigrationSql(dialect, diffs1, schemas);
		if (!sql1.IsEmpty())
			await SchemaMigrator.ApplyAsync(conn, sql1, dialect, CancellationToken);

		// Second pass — must produce no actionable diffs.
		var diffs2 = await SchemaMigrator.CompareAsync(
			schemas, conn, dialect, skipComputed: false, cancellationToken: CancellationToken);
		var sql2 = SchemaMigrator.GenerateMigrationSql(dialect, diffs2, schemas);

		sql2.IsEmptyOrWhiteSpace().AssertTrue(
			$"Second pass of the migrator should be a no-op; got SQL: {sql2}");
	}
}

#endif
