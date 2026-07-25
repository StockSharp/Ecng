#if NET10_0_OR_GREATER

namespace Ecng.Tests.Data;

using Ecng.Data;
using Ecng.Data.Sql;
using Ecng.Serialization;

/// <summary>
/// Pure (no-database) checks over the naming-convention pass of
/// <see cref="SchemaMigrator.Compare"/>: primary keys, foreign keys and indexes
/// in the live database must be named per <see cref="SchemaNaming"/>.
/// </summary>
[TestClass]
public class SchemaMigratorNamingTests : BaseTestClass
{
	private sealed class NamingClient
	{
		public long Id { get; set; }
	}

	private sealed class NamingOrder
	{
		public long Id { get; set; }
		public long Client { get; set; }
		public string Code { get; set; }
	}

	private const string _orderTable = "NamingOrder";
	private const string _clientTable = "NamingClient";

	private static (Schema order, Schema client) BuildSchemas()
	{
		var client = new Schema
		{
			TableName = _clientTable,
			EntityType = typeof(NamingClient),
			Identity = new SchemaColumn { Name = "Id", ClrType = typeof(long), IsReadOnly = true },
			Columns = [],
			Factory = () => new NamingClient(),
		};

		var order = new Schema
		{
			TableName = _orderTable,
			EntityType = typeof(NamingOrder),
			Identity = new SchemaColumn { Name = "Id", ClrType = typeof(long), IsReadOnly = true },
			Columns =
			[
				new SchemaColumn
				{
					Name = "Client",
					ClrType = typeof(long),
					ReferencedEntityType = typeof(NamingClient),
				},
				new SchemaColumn
				{
					Name = "Code",
					ClrType = typeof(string),
					MaxLength = 32,
					IsUnique = true,
				},
			],
			Factory = () => new NamingOrder(),
		};

		// ResolveForeignKeyTarget reads the referenced entity's schema from the
		// registry, so the Client side must be registered before comparing.
		SchemaRegistry.Register(client);
		SchemaRegistry.Register(order);

		return (order, client);
	}

	// Both tables fully present, so nothing but naming can differ.
	private static List<DbColumnInfo> BuildDbColumns() =>
	[
		new(_orderTable, "Id", "bigint", false, null, null, null),
		new(_orderTable, "Client", "bigint", false, null, null, null),
		new(_orderTable, "Code", "nvarchar", true, 32, null, null),
		new(_clientTable, "Id", "bigint", false, null, null, null),
	];

	// The FK points at the right target, so only its NAME can be wrong.
	private static List<DbForeignKeyInfo> Fk(string constraintName) =>
		[new(constraintName, _orderTable, "Client", _clientTable, "Id")];

	private static DbIndexInfo Pk(string indexName) =>
		new(indexName, _orderTable, "Id", 1, IsUnique: true, IsPrimaryKey: true);

	private static DbIndexInfo CodeIndex(string indexName, bool unique) =>
		new(indexName, _orderTable, "Code", 1, IsUnique: unique, IsPrimaryKey: false);

	private static IReadOnlyList<SchemaDiff> Compare(
		IReadOnlyList<DbForeignKeyInfo> dbForeignKeys,
		IReadOnlyList<DbIndexInfo> dbIndexes,
		bool checkNaming)
		=> Compare(BuildDbColumns(), dbForeignKeys, dbIndexes, checkNaming);

	private static IReadOnlyList<SchemaDiff> Compare(
		IReadOnlyList<DbColumnInfo> dbColumns,
		IReadOnlyList<DbForeignKeyInfo> dbForeignKeys,
		IReadOnlyList<DbIndexInfo> dbIndexes,
		bool checkNaming)
	{
		var (order, client) = BuildSchemas();

		return SchemaMigrator.Compare(
			[order, client],
			dbColumns,
			SqlServerDialect.Instance,
			skipComputed: false,
			dbForeignKeys: dbForeignKeys,
			dbIndexes: dbIndexes,
			detectExtraTables: false,
			checkNaming: checkNaming);
	}

	private static SchemaDiff[] NamingDiffs(IEnumerable<SchemaDiff> diffs, SchemaDiffKind kind)
		=> [.. diffs.Where(d => d.Kind == kind)];

	private static string Describe(IEnumerable<SchemaDiff> diffs)
		=> string.Join(", ", diffs.Select(d => $"{d.Kind} {d.TableName}.{d.ColumnName} expected='{d.Expected}' actual='{d.Actual}'"));

	/// <summary>
	/// The database assigns its own primary-key name when the DDL does not supply one
	/// (<c>PK__Table__hash</c> on SQL Server, <c>table_pkey</c> on PostgreSQL). That is
	/// exactly the case the check exists to surface.
	/// </summary>
	[TestMethod]
	public void PrimaryKey_WithDatabaseAssignedName_IsReported()
	{
		var diffs = Compare(Fk(SchemaNaming.ForeignKey(_orderTable, "Client")), [Pk("PK__NamingOrder__3213E83F")], checkNaming: true);
		var pk = NamingDiffs(diffs, SchemaDiffKind.PrimaryKeyNameMismatch);

		pk.Length.AssertEqual(1, $"Expected one PK naming diff. Got: {Describe(diffs)}");
		pk[0].TableName.AssertEqual(_orderTable);
		pk[0].Expected.AssertEqual("PK_NamingOrder");
		pk[0].Actual.AssertEqual("PK__NamingOrder__3213E83F");
	}

	[TestMethod]
	public void PrimaryKey_WithConventionalName_IsNotReported()
	{
		var diffs = Compare(Fk(SchemaNaming.ForeignKey(_orderTable, "Client")), [Pk("PK_NamingOrder")], checkNaming: true);

		NamingDiffs(diffs, SchemaDiffKind.PrimaryKeyNameMismatch).Length.AssertEqual(
			0, $"A conventionally named PK must not be reported. Got: {Describe(diffs)}");
	}

	[TestMethod]
	public void ForeignKey_WithNonConventionalName_IsReported()
	{
		var diffs = Compare(Fk("FK_Order_ClientRef"), [Pk("PK_NamingOrder")], checkNaming: true);
		var fk = NamingDiffs(diffs, SchemaDiffKind.ForeignKeyNameMismatch);

		fk.Length.AssertEqual(1, $"Expected one FK naming diff. Got: {Describe(diffs)}");
		fk[0].TableName.AssertEqual(_orderTable);
		fk[0].ColumnName.AssertEqual("Client");
		fk[0].Expected.AssertEqual("FK_NamingOrder_Client");
		fk[0].Actual.AssertEqual("FK_Order_ClientRef");
	}

	[TestMethod]
	public void ForeignKey_WithConventionalName_IsNotReported()
	{
		var diffs = Compare(Fk("FK_NamingOrder_Client"), [Pk("PK_NamingOrder")], checkNaming: true);

		NamingDiffs(diffs, SchemaDiffKind.ForeignKeyNameMismatch).Length.AssertEqual(
			0, $"A conventionally named FK must not be reported. Got: {Describe(diffs)}");
	}

	/// <summary>
	/// A unique index carries the <c>UX_</c> prefix so uniqueness is visible in the name.
	/// </summary>
	[TestMethod]
	public void UniqueIndex_NamedWithIxPrefix_IsReported()
	{
		var diffs = Compare(
			Fk(SchemaNaming.ForeignKey(_orderTable, "Client")),
			[Pk("PK_NamingOrder"), CodeIndex("IX_NamingOrder_Code", unique: true)],
			checkNaming: true);

		var ix = NamingDiffs(diffs, SchemaDiffKind.IndexNameMismatch);

		ix.Length.AssertEqual(1, $"Expected one index naming diff. Got: {Describe(diffs)}");
		ix[0].Expected.AssertEqual("UX_NamingOrder_Code");
		ix[0].Actual.AssertEqual("IX_NamingOrder_Code");
	}

	[TestMethod]
	public void UniqueIndex_NamedWithUxPrefix_IsNotReported()
	{
		var diffs = Compare(
			Fk(SchemaNaming.ForeignKey(_orderTable, "Client")),
			[Pk("PK_NamingOrder"), CodeIndex("UX_NamingOrder_Code", unique: true)],
			checkNaming: true);

		NamingDiffs(diffs, SchemaDiffKind.IndexNameMismatch).Length.AssertEqual(
			0, $"A conventionally named unique index must not be reported. Got: {Describe(diffs)}");
	}

	/// <summary>
	/// PostgreSQL folds unquoted identifiers to lower case, so the stored name never
	/// matches the generated casing exactly — comparison must ignore case.
	/// </summary>
	[TestMethod]
	public void Naming_ComparisonIgnoresCase()
	{
		var diffs = Compare(
			Fk("fk_namingorder_client"),
			[Pk("pk_namingorder"), CodeIndex("ux_namingorder_code", unique: true)],
			checkNaming: true);

		var naming = diffs.Where(d => d.Kind
			is SchemaDiffKind.PrimaryKeyNameMismatch
			or SchemaDiffKind.ForeignKeyNameMismatch
			or SchemaDiffKind.IndexNameMismatch).ToArray();

		naming.Length.AssertEqual(0, $"Case-folded names must not be reported. Got: {Describe(naming)}");
	}

	/// <summary>
	/// SQLite names the index backing a UNIQUE constraint <c>sqlite_autoindex_{Table}_{N}</c>.
	/// Those are database internals that no DDL of ours can name, so flagging them would be noise.
	/// </summary>
	[TestMethod]
	public void DatabaseInternalIndex_IsNotReported()
	{
		var diffs = Compare(
			Fk(SchemaNaming.ForeignKey(_orderTable, "Client")),
			[Pk("PK_NamingOrder"), CodeIndex("sqlite_autoindex_NamingOrder_1", unique: true)],
			checkNaming: true);

		NamingDiffs(diffs, SchemaDiffKind.IndexNameMismatch).Length.AssertEqual(
			0, $"Database-internal index names must not be reported. Got: {Describe(diffs)}");
	}

	/// <summary>
	/// Tables and columns are reconciled case-insensitively, so a database column named
	/// <c>code</c> silently satisfies an entity declaring <c>Code</c>. The drift is real —
	/// it breaks case-sensitive collations and quoted SQL — and only the naming pass can see it.
	/// </summary>
	[TestMethod]
	public void ColumnName_CaseDrift_IsReported()
	{
		List<DbColumnInfo> dbColumns =
		[
			new(_orderTable, "Id", "bigint", false, null, null, null),
			new(_orderTable, "Client", "bigint", false, null, null, null),
			new(_orderTable, "code", "nvarchar", true, 32, null, null),
			new(_clientTable, "Id", "bigint", false, null, null, null),
		];

		var diffs = Compare(
			dbColumns,
			Fk(SchemaNaming.ForeignKey(_orderTable, "Client")),
			[Pk("PK_NamingOrder"), CodeIndex("UX_NamingOrder_Code", unique: true)],
			checkNaming: true);

		var drift = NamingDiffs(diffs, SchemaDiffKind.ColumnNameCaseMismatch);

		drift.Length.AssertEqual(1, $"Expected one column case diff. Got: {Describe(diffs)}");
		drift[0].TableName.AssertEqual(_orderTable);
		drift[0].Expected.AssertEqual("Code");
		drift[0].Actual.AssertEqual("code");
	}

	[TestMethod]
	public void TableName_CaseDrift_IsReported()
	{
		List<DbColumnInfo> dbColumns =
		[
			new("namingorder", "Id", "bigint", false, null, null, null),
			new("namingorder", "Client", "bigint", false, null, null, null),
			new("namingorder", "Code", "nvarchar", true, 32, null, null),
			new(_clientTable, "Id", "bigint", false, null, null, null),
		];

		var diffs = Compare(
			dbColumns,
			Fk(SchemaNaming.ForeignKey(_orderTable, "Client")),
			[Pk("PK_NamingOrder"), CodeIndex("UX_NamingOrder_Code", unique: true)],
			checkNaming: true);

		var drift = NamingDiffs(diffs, SchemaDiffKind.TableNameCaseMismatch);

		drift.Length.AssertEqual(1, $"Expected one table case diff. Got: {Describe(diffs)}");
		drift[0].Expected.AssertEqual(_orderTable);
		drift[0].Actual.AssertEqual("namingorder");
	}

	[TestMethod]
	public void ExactlyCasedTableAndColumns_AreNotReported()
	{
		var diffs = Compare(
			Fk(SchemaNaming.ForeignKey(_orderTable, "Client")),
			[Pk("PK_NamingOrder"), CodeIndex("UX_NamingOrder_Code", unique: true)],
			checkNaming: true);

		var drift = diffs.Where(d => d.Kind
			is SchemaDiffKind.TableNameCaseMismatch
			or SchemaDiffKind.ColumnNameCaseMismatch).ToArray();

		drift.Length.AssertEqual(0, $"Matching case must not be reported. Got: {Describe(drift)}");
	}

	/// <summary>
	/// An existing database can violate the convention on every object, so the pass is
	/// opt-in — the default comparison must behave exactly as before.
	/// </summary>
	[TestMethod]
	public void NamingChecks_AreOffByDefault()
	{
		var diffs = Compare(
			Fk("SomeLegacyFkName"),
			[Pk("PK__NamingOrder__3213E83F"), CodeIndex("IX_NamingOrder_Code", unique: true)],
			checkNaming: false);

		var naming = diffs.Where(d => d.Kind
			is SchemaDiffKind.PrimaryKeyNameMismatch
			or SchemaDiffKind.ForeignKeyNameMismatch
			or SchemaDiffKind.IndexNameMismatch).ToArray();

		naming.Length.AssertEqual(0, $"Naming diffs must require opt-in. Got: {Describe(naming)}");
	}

	/// <summary>
	/// Renaming a constraint is a destructive schema operation, so the naming diffs are
	/// informational only — the migration generator must never emit SQL for them.
	/// </summary>
	[TestMethod]
	public void NamingDiffs_GenerateNoSql()
	{
		var (order, client) = BuildSchemas();

		var diffs = Compare(
			Fk("SomeLegacyFkName"),
			[Pk("PK__NamingOrder__3213E83F"), CodeIndex("IX_NamingOrder_Code", unique: true)],
			checkNaming: true);

		var naming = diffs.Where(d => d.Kind
			is SchemaDiffKind.PrimaryKeyNameMismatch
			or SchemaDiffKind.ForeignKeyNameMismatch
			or SchemaDiffKind.IndexNameMismatch).ToArray();

		naming.Length.AssertGreater(0, "Precondition: the fixture must produce naming diffs.");

		var sql = SchemaMigrator.GenerateMigrationSql(SqlServerDialect.Instance, naming, [order, client]);

		sql.Trim().IsEmpty().AssertTrue($"Naming diffs must not generate DDL, got: {sql}");
	}
}

#endif
