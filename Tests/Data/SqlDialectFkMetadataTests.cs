#if NET10_0_OR_GREATER

namespace Ecng.Tests.Data;

using System.Data.Common;

using Ecng.Data;

/// <summary>
/// Coverage for the per-dialect <see cref="ISqlDialect.ReadDbForeignKeysAsync"/>
/// implementation. The schema migrator's backfill logic is built on top of
/// this read path; covering the read on its own keeps regressions in either
/// SQL query (sys.foreign_keys / information_schema / pragma) localised to a
/// dedicated test instead of leaking into the migrator-level scenario.
/// </summary>
[TestClass]
[TestCategory("Integration")]
[TestCategory("Database")]
[DoNotParallelize]
public class SqlDialectFkMetadataTests : BaseTestClass
{
	private const string ParentTable = "Ecng_FkMetaParent";
	private const string ChildTable = "Ecng_FkMetaChild";
	private const string ChildFkColumn = "ParentId";

	[ClassInitialize]
	public static void ClassInit(TestContext context) => DbTestHelper.RegisterAll();

	[ClassCleanup]
	public static void ClassCleanup() => DbTestHelper.ClearSQLitePools();

	private static async Task<DbConnection> OpenAsync(string provider, CancellationToken cancellationToken)
	{
		var factory = DbTestHelper.GetFactory(provider);
		var connStr = DbTestHelper.TryGetConnectionString(provider);
		var conn = factory.CreateConnection();
		conn.ConnectionString = connStr;
		await conn.OpenAsync(cancellationToken);
		return conn;
	}

	private static void DropAll(string provider)
	{
		// Child first so the FK no longer references the parent we drop next.
		DbTestHelper.DropTable(provider, ChildTable);
		DbTestHelper.DropTable(provider, ParentTable);
	}

	private static void CreateParentAndChildWithFk(string provider)
	{
		CreateIdOnlyTable(provider, ParentTable);

		var dialect = DbTestHelper.GetDialect(provider);
		var idType = dialect.GetSqlTypeName(typeof(long));
		var qId = dialect.QuoteIdentifier("Id");
		var qFk = dialect.QuoteIdentifier(ChildFkColumn);

		var childCols = string.Join(", ",
			$"{qId} {idType} NOT NULL PRIMARY KEY",
			$"{qFk} {idType} NOT NULL",
			dialect.GetForeignKeyConstraint(ChildTable, ChildFkColumn, ParentTable, "Id"));

		var sb = new StringBuilder();
		dialect.AppendCreateTable(sb, ChildTable, childCols);
		DbTestHelper.ExecuteRaw(provider, sb.ToString());
	}

	private static void CreateIdOnlyTable(string provider, string tableName)
	{
		var dialect = DbTestHelper.GetDialect(provider);
		var qId = dialect.QuoteIdentifier("Id");
		var idType = dialect.GetSqlTypeName(typeof(long));

		var sb = new StringBuilder();
		dialect.AppendCreateTable(sb, tableName, $"{qId} {idType} NOT NULL PRIMARY KEY");
		DbTestHelper.ExecuteRaw(provider, sb.ToString());
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task ReadDbForeignKeysAsync_ReturnsConstraintForChildTable(string provider)
	{
		DbTestHelper.SkipIfUnavailable(provider);

		DropAll(provider);
		CreateParentAndChildWithFk(provider);

		try
		{
			var dialect = DbTestHelper.GetDialect(provider);
			using var conn = await OpenAsync(provider, CancellationToken);

			var fks = await dialect.ReadDbForeignKeysAsync(conn, cancellationToken: CancellationToken);

			var match = fks.FirstOrDefault(fk =>
				fk.TableName.EqualsIgnoreCase(ChildTable) &&
				fk.ColumnName.EqualsIgnoreCase(ChildFkColumn));

			match.AssertNotNull(
				$"Expected an FK row for {ChildTable}.{ChildFkColumn}; got [{string.Join(", ", fks.Select(f => $"{f.TableName}.{f.ColumnName}->{f.RefTableName}.{f.RefColumnName}"))}]");

			match.RefTableName.EqualsIgnoreCase(ParentTable).AssertTrue(
				$"Expected ref table {ParentTable}, got {match.RefTableName}");
			match.RefColumnName.EqualsIgnoreCase("Id").AssertTrue(
				$"Expected ref column Id, got {match.RefColumnName}");
			match.ConstraintName.IsEmpty().AssertFalse(
				"Expected a non-empty ConstraintName");
		}
		finally
		{
			DropAll(provider);
		}
	}

	[TestMethod]
	[DataRow(DatabaseProviderRegistry.SqlServer)]
	[DataRow(DatabaseProviderRegistry.PostgreSql)]
	[DataRow(DatabaseProviderRegistry.SQLite)]
	public async Task ReadDbForeignKeysAsync_OmitsTablesWithoutForeignKeys(string provider)
	{
		DbTestHelper.SkipIfUnavailable(provider);

		DropAll(provider);
		CreateIdOnlyTable(provider, ParentTable);

		try
		{
			var dialect = DbTestHelper.GetDialect(provider);
			using var conn = await OpenAsync(provider, CancellationToken);
			var fks = await dialect.ReadDbForeignKeysAsync(conn, cancellationToken: CancellationToken);

			fks.Any(fk => fk.TableName.EqualsIgnoreCase(ParentTable)).AssertFalse(
				"Parent table has no FK columns; ReadDbForeignKeysAsync should not surface anything for it.");
		}
		finally
		{
			DropAll(provider);
		}
	}
}

#endif
