#if NET10_0_OR_GREATER

namespace Ecng.Tests.Data;

using System.Text;

using Ecng.Data;
using Ecng.Serialization;

#region Test entities for ColumnAttribute

public class ColAttrInner
{
	[Column(MaxLength = 50)]
	public string Tag { get; set; }

	public int Score { get; set; }
}

[Entity(Name = "Ecng_ColAttrTest")]
public class ColAttrTestEntity : IDbPersistable
{
	public long Id { get; set; }

	[Column(MaxLength = 128)]
	public string Name { get; set; }

	[Column(IsNullable = true)]
	public string Description { get; set; }

	[Column(IsNullable = true, MaxLength = 64)]
	public string Tag { get; set; }

	public string Plain { get; set; }

	public int? NullableInt { get; set; }

	public int RequiredInt { get; set; }

	public ColAttrInner Meta { get; set; }

	object IDbPersistable.GetIdentity() => Id;
	void IDbPersistable.SetIdentity(object id) => Id = id.To<long>();
	public void Save(SettingsStorage storage) { }
	public ValueTask LoadAsync(SettingsStorage storage, IStorage db, CancellationToken ct) => default;
}

#endregion

[TestClass]
public class ColumnAttributeTests : BaseTestClass
{
	#region SchemaRegistry + ColumnAttribute

	[TestMethod]
	public void ColumnAttr_MaxLength_SetsMaxLengthOnColumn()
	{
		var schema = SchemaRegistry.Get(typeof(ColAttrTestEntity));
		var col = schema.Columns.First(c => c.Name == "Name");

		col.MaxLength.AssertEqual(128);
	}

	[TestMethod]
	public void ColumnAttr_IsNullable_OverridesDefault()
	{
		var schema = SchemaRegistry.Get(typeof(ColAttrTestEntity));
		var col = schema.Columns.First(c => c.Name == "Description");

		col.IsNullable.AssertTrue();
	}

	[TestMethod]
	public void ColumnAttr_Both_SetsNullableAndMaxLength()
	{
		var schema = SchemaRegistry.Get(typeof(ColAttrTestEntity));
		var col = schema.Columns.First(c => c.Name == "Tag");

		col.IsNullable.AssertTrue();
		col.MaxLength.AssertEqual(64);
	}

	[TestMethod]
	public void NoAttribute_String_DefaultsToNotNull()
	{
		var schema = SchemaRegistry.Get(typeof(ColAttrTestEntity));
		var col = schema.Columns.First(c => c.Name == "Plain");

		col.IsNullable.AssertFalse();
		col.MaxLength.AssertEqual(0);
	}

	[TestMethod]
	public void NoAttribute_NullableValueType_IsNullable()
	{
		var schema = SchemaRegistry.Get(typeof(ColAttrTestEntity));
		var col = schema.Columns.First(c => c.Name == "NullableInt");

		col.IsNullable.AssertTrue();
	}

	[TestMethod]
	public void NoAttribute_ValueType_NotNullable()
	{
		var schema = SchemaRegistry.Get(typeof(ColAttrTestEntity));
		var col = schema.Columns.First(c => c.Name == "RequiredInt");

		col.IsNullable.AssertFalse();
	}

	[TestMethod]
	public void InnerSchema_ColumnAttr_PropagatedToFlattenedColumn()
	{
		var schema = SchemaRegistry.Get(typeof(ColAttrTestEntity));
		var col = schema.Columns.First(c => c.Name == "MetaTag");

		col.MaxLength.AssertEqual(50);
	}

	#endregion

	#region GetColumnDefinition — SQL Server

	[TestMethod]
	public void GetColumnDef_SqlServer_StringWithMaxLength()
	{
		var def = SqlServerDialect.Instance.GetColumnDefinition(typeof(string), false, 128);

		def.AssertEqual("NVARCHAR(128) NOT NULL");
	}

	[TestMethod]
	public void GetColumnDef_SqlServer_StringUnlimited()
	{
		var def = SqlServerDialect.Instance.GetColumnDefinition(typeof(string), true, 0);

		def.AssertEqual("NVARCHAR(MAX) NULL");
	}

	[TestMethod]
	public void GetColumnDef_SqlServer_ByteArrayWithMaxLength()
	{
		var def = SqlServerDialect.Instance.GetColumnDefinition(typeof(byte[]), false, 256);

		def.AssertEqual("VARBINARY(256) NOT NULL");
	}

	[TestMethod]
	public void GetColumnDef_SqlServer_ByteArrayUnlimited()
	{
		var def = SqlServerDialect.Instance.GetColumnDefinition(typeof(byte[]), true, 0);

		def.AssertEqual("VARBINARY(MAX) NULL");
	}

	[TestMethod]
	public void GetColumnDef_SqlServer_Int()
	{
		var def = SqlServerDialect.Instance.GetColumnDefinition(typeof(int), false);

		def.AssertEqual("INT NOT NULL");
	}

	[TestMethod]
	public void GetColumnDef_SqlServer_NullableLong()
	{
		var def = SqlServerDialect.Instance.GetColumnDefinition(typeof(long?), true);

		def.AssertEqual("BIGINT NULL");
	}

	#endregion

	#region GetColumnDefinition — PostgreSQL

	[TestMethod]
	public void GetColumnDef_PostgreSql_StringWithMaxLength()
	{
		var def = PostgreSqlDialect.Instance.GetColumnDefinition(typeof(string), false, 128);

		def.AssertEqual("VARCHAR(128) NOT NULL");
	}

	[TestMethod]
	public void GetColumnDef_PostgreSql_StringUnlimited()
	{
		var def = PostgreSqlDialect.Instance.GetColumnDefinition(typeof(string), true, 0);

		def.AssertEqual("TEXT NULL");
	}

	[TestMethod]
	public void GetColumnDef_PostgreSql_ByteArray()
	{
		var def = PostgreSqlDialect.Instance.GetColumnDefinition(typeof(byte[]), false, 256);

		def.AssertEqual("BYTEA NOT NULL");
	}

	[TestMethod]
	public void GetColumnDef_PostgreSql_Int()
	{
		var def = PostgreSqlDialect.Instance.GetColumnDefinition(typeof(int), true);

		def.AssertEqual("INTEGER NULL");
	}

	#endregion

	#region GetColumnDefinition — SQLite

	[TestMethod]
	public void GetColumnDef_SQLite_StringIgnoresMaxLength()
	{
		var def = SQLiteDialect.Instance.GetColumnDefinition(typeof(string), false, 128);

		// SQLite uses dynamic typing, maxLength is ignored in base implementation
		def.AssertEqual("TEXT NOT NULL");
	}

	[TestMethod]
	public void GetColumnDef_SQLite_Int()
	{
		var def = SQLiteDialect.Instance.GetColumnDefinition(typeof(int), true);

		def.AssertEqual("INTEGER NULL");
	}

	#endregion

	#region DDL generation (AppendAddColumn, AppendAlterColumn, AppendDropColumn)

	[TestMethod]
	[DataRow("SqlServer")]
	[DataRow("PostgreSql")]
	[DataRow("SQLite")]
	public void AppendAddColumn_GeneratesCorrectDdl(string dialectName)
	{
		var dialect = GetDialect(dialectName);
		var q = QuoteFn(dialectName);
		var sb = new StringBuilder();

		dialect.AppendAddColumn(sb, "Users", "Email", "NVARCHAR(256) NOT NULL");

		sb.ToString().AssertEqual($"ALTER TABLE {q("Users")} ADD {q("Email")} NVARCHAR(256) NOT NULL");
	}

	[TestMethod]
	public void AppendAlterColumn_SqlServer_StandardSyntax()
	{
		var sb = new StringBuilder();

		SqlServerDialect.Instance.AppendAlterColumn(sb, "Users", "Email", typeof(string), true, 512);

		sb.ToString().AssertEqual("ALTER TABLE [Users] ALTER COLUMN [Email] NVARCHAR(512) NULL");
	}

	[TestMethod]
	public void AppendAlterColumn_PostgreSql_SeparatesTypeAndNullability()
	{
		var sb = new StringBuilder();

		PostgreSqlDialect.Instance.AppendAlterColumn(sb, "Users", "Email", typeof(string), true, 512);

		var sql = sb.ToString();
		sql.Contains("SET DATA TYPE VARCHAR(512)").AssertTrue($"Expected SET DATA TYPE, got: {sql}");
		sql.Contains("DROP NOT NULL").AssertTrue($"Expected DROP NOT NULL, got: {sql}");
		sql.Contains("NULL").AssertTrue($"Expected nullability clause, got: {sql}");
	}

	[TestMethod]
	public void AppendAlterColumn_PostgreSql_NotNull()
	{
		var sb = new StringBuilder();

		PostgreSqlDialect.Instance.AppendAlterColumn(sb, "Users", "Email", typeof(string), false, 256);

		var sql = sb.ToString();
		sql.Contains("SET DATA TYPE VARCHAR(256)").AssertTrue($"Expected SET DATA TYPE, got: {sql}");
		sql.Contains("SET NOT NULL").AssertTrue($"Expected SET NOT NULL, got: {sql}");
	}

	[TestMethod]
	[DataRow("SqlServer")]
	[DataRow("PostgreSql")]
	[DataRow("SQLite")]
	public void AppendDropColumn_GeneratesCorrectDdl(string dialectName)
	{
		var dialect = GetDialect(dialectName);
		var q = QuoteFn(dialectName);
		var sb = new StringBuilder();

		dialect.AppendDropColumn(sb, "Users", "OldCol");

		sb.ToString().AssertEqual($"ALTER TABLE {q("Users")} DROP COLUMN {q("OldCol")}");
	}

	#endregion

	#region SchemaMigrator.Compare

	[TestMethod]
	public void Compare_MissingColumn_Detected()
	{
		var schema = new Schema
		{
			TableName = "TestTable",
			EntityType = typeof(ColAttrTestEntity),
			Columns =
			[
				new SchemaColumn { Name = "Name", ClrType = typeof(string), MaxLength = 128 },
				new SchemaColumn { Name = "NewCol", ClrType = typeof(int) },
			],
			Factory = () => new ColAttrTestEntity(),
		};

		var dbCols = new[]
		{
			new DbColumnInfo("TestTable", "Name", "nvarchar", false, 128, null, null),
		};

		var diffs = SchemaMigrator.Compare([schema], dbCols, SqlServerDialect.Instance, false);

		diffs.Count.AssertEqual(1);
		diffs[0].Kind.AssertEqual(SchemaDiffKind.MissingColumn);
		diffs[0].ColumnName.AssertEqual("NewCol");
	}

	[TestMethod]
	public void Compare_ExtraColumn_Detected()
	{
		var schema = new Schema
		{
			TableName = "TestTable",
			EntityType = typeof(ColAttrTestEntity),
			Columns =
			[
				new SchemaColumn { Name = "Name", ClrType = typeof(string) },
			],
			Factory = () => new ColAttrTestEntity(),
		};

		var dbCols = new[]
		{
			new DbColumnInfo("TestTable", "Name", "nvarchar", false, -1, null, null),
			new DbColumnInfo("TestTable", "Obsolete", "int", false, null, null, null),
		};

		var diffs = SchemaMigrator.Compare([schema], dbCols, SqlServerDialect.Instance, false);

		diffs.Any(d => d.Kind == SchemaDiffKind.ExtraColumn && d.ColumnName == "Obsolete").AssertTrue();
	}

	[TestMethod]
	public void Compare_NullabilityMismatch_Detected()
	{
		var schema = new Schema
		{
			TableName = "TestTable",
			EntityType = typeof(ColAttrTestEntity),
			Columns =
			[
				new SchemaColumn { Name = "Phone", ClrType = typeof(string), IsNullable = true },
			],
			Factory = () => new ColAttrTestEntity(),
		};

		var dbCols = new[]
		{
			new DbColumnInfo("TestTable", "Phone", "nvarchar", false, -1, null, null),
		};

		var diffs = SchemaMigrator.Compare([schema], dbCols, SqlServerDialect.Instance, false);

		diffs.Count.AssertEqual(1);
		diffs[0].Kind.AssertEqual(SchemaDiffKind.NullabilityMismatch);
		diffs[0].Expected.AssertEqual("NULL");
		diffs[0].Actual.AssertEqual("NOT NULL");
	}

	[TestMethod]
	public void Compare_TypeMismatch_Detected()
	{
		var schema = new Schema
		{
			TableName = "TestTable",
			EntityType = typeof(ColAttrTestEntity),
			Columns =
			[
				new SchemaColumn { Name = "Status", ClrType = typeof(long) },
			],
			Factory = () => new ColAttrTestEntity(),
		};

		var dbCols = new[]
		{
			new DbColumnInfo("TestTable", "Status", "int", false, null, null, null),
		};

		var diffs = SchemaMigrator.Compare([schema], dbCols, SqlServerDialect.Instance, false);

		diffs.Any(d => d.Kind == SchemaDiffKind.TypeMismatch && d.ColumnName == "Status").AssertTrue();
	}

	[TestMethod]
	public void Compare_NoDifferences_ReturnsEmpty()
	{
		var schema = new Schema
		{
			TableName = "TestTable",
			EntityType = typeof(ColAttrTestEntity),
			Columns =
			[
				new SchemaColumn { Name = "Name", ClrType = typeof(string) },
				new SchemaColumn { Name = "Value", ClrType = typeof(int) },
			],
			Factory = () => new ColAttrTestEntity(),
		};

		var dbCols = new[]
		{
			new DbColumnInfo("TestTable", "Name", "nvarchar", false, -1, null, null),
			new DbColumnInfo("TestTable", "Value", "int", false, null, null, null),
		};

		var diffs = SchemaMigrator.Compare([schema], dbCols, SqlServerDialect.Instance, false);

		diffs.Count.AssertEqual(0);
	}

	[TestMethod]
	public void Compare_ViewSchema_Skipped()
	{
		var schema = new Schema
		{
			TableName = "TestView",
			EntityType = typeof(ColAttrTestEntity),
			IsView = true,
			Columns =
			[
				new SchemaColumn { Name = "Name", ClrType = typeof(string) },
			],
			Factory = () => new ColAttrTestEntity(),
		};

		var dbCols = new[]
		{
			new DbColumnInfo("TestView", "Name", "nvarchar", false, -1, null, null),
			new DbColumnInfo("TestView", "Extra", "int", false, null, null, null),
		};

		var diffs = SchemaMigrator.Compare([schema], dbCols, SqlServerDialect.Instance, false);

		diffs.Count.AssertEqual(0);
	}

	[TestMethod]
	public void Compare_MaxLengthMismatch_Detected()
	{
		var schema = new Schema
		{
			TableName = "TestTable",
			EntityType = typeof(ColAttrTestEntity),
			Columns =
			[
				new SchemaColumn { Name = "Name", ClrType = typeof(string), MaxLength = 64 },
			],
			Factory = () => new ColAttrTestEntity(),
		};

		var dbCols = new[]
		{
			new DbColumnInfo("TestTable", "Name", "nvarchar", false, 256, null, null),
		};

		var diffs = SchemaMigrator.Compare([schema], dbCols, SqlServerDialect.Instance, false);

		diffs.Any(d => d.ColumnName == "Name").AssertTrue("MaxLength mismatch should be detected");
	}

	[TestMethod]
	public void Compare_MaxLengthMatch_NoDiff()
	{
		var schema = new Schema
		{
			TableName = "TestTable",
			EntityType = typeof(ColAttrTestEntity),
			Columns =
			[
				new SchemaColumn { Name = "Name", ClrType = typeof(string), MaxLength = 128 },
			],
			Factory = () => new ColAttrTestEntity(),
		};

		var dbCols = new[]
		{
			new DbColumnInfo("TestTable", "Name", "nvarchar", false, 128, null, null),
		};

		var diffs = SchemaMigrator.Compare([schema], dbCols, SqlServerDialect.Instance, false);

		diffs.Count.AssertEqual(0);
	}

	#endregion

	#region Precision/Scale support

	[TestMethod]
	public void Compare_DecimalPrecisionMismatch_Detected()
	{
		var schema = new Schema
		{
			TableName = "TestTable",
			EntityType = typeof(ColAttrTestEntity),
			Columns =
			[
				new SchemaColumn { Name = "Price", ClrType = typeof(decimal), Precision = 18, Scale = 8 },
			],
			Factory = () => new ColAttrTestEntity(),
		};

		var dbCols = new[]
		{
			new DbColumnInfo("TestTable", "Price", "decimal", false, null, 10, 2),
		};

		var diffs = SchemaMigrator.Compare([schema], dbCols, SqlServerDialect.Instance, false);

		diffs.Any(d => d.ColumnName == "Price").AssertTrue(
			"Decimal precision/scale mismatch (18,8 vs 10,2) should be detected");
	}

	[TestMethod]
	public void Compare_DecimalScaleOnlyMismatch_Detected()
	{
		var schema = new Schema
		{
			TableName = "TestTable",
			EntityType = typeof(ColAttrTestEntity),
			Columns =
			[
				new SchemaColumn { Name = "Rate", ClrType = typeof(decimal), Precision = 18, Scale = 8 },
			],
			Factory = () => new ColAttrTestEntity(),
		};

		// same precision, different scale
		var dbCols = new[]
		{
			new DbColumnInfo("TestTable", "Rate", "decimal", false, null, 18, 2),
		};

		var diffs = SchemaMigrator.Compare([schema], dbCols, SqlServerDialect.Instance, false);

		diffs.Any(d => d.ColumnName == "Rate").AssertTrue(
			"Scale mismatch (8 vs 2) should be detected even when precision matches");
	}

	[TestMethod]
	public void Compare_DecimalPrecisionMatch_NoDiff()
	{
		var schema = new Schema
		{
			TableName = "TestTable",
			EntityType = typeof(ColAttrTestEntity),
			Columns =
			[
				new SchemaColumn { Name = "Price", ClrType = typeof(decimal), Precision = 10, Scale = 2 },
			],
			Factory = () => new ColAttrTestEntity(),
		};

		var dbCols = new[]
		{
			new DbColumnInfo("TestTable", "Price", "decimal", false, null, 10, 2),
		};

		var diffs = SchemaMigrator.Compare([schema], dbCols, SqlServerDialect.Instance, false);

		diffs.Any(d => d.ColumnName == "Price").AssertFalse(
			"Matching precision/scale should not produce a diff");
	}

	[TestMethod]
	public void Compare_DateTimePrecisionMismatch_Detected()
	{
		var schema = new Schema
		{
			TableName = "TestTable",
			EntityType = typeof(ColAttrTestEntity),
			Columns =
			[
				new SchemaColumn { Name = "Created", ClrType = typeof(DateTime), Precision = 7 },
			],
			Factory = () => new ColAttrTestEntity(),
		};

		// DB has DATETIME2(3) — entity expects precision 7
		var dbCols = new[]
		{
			new DbColumnInfo("TestTable", "Created", "datetime2", false, null, 3, null),
		};

		var diffs = SchemaMigrator.Compare([schema], dbCols, SqlServerDialect.Instance, false);

		diffs.Any(d => d.ColumnName == "Created").AssertTrue(
			"DateTime precision mismatch (7 vs 3) should be detected");
	}

	[TestMethod]
	public void Compare_DateTimeOffsetPrecisionMismatch_Detected()
	{
		var schema = new Schema
		{
			TableName = "TestTable",
			EntityType = typeof(ColAttrTestEntity),
			Columns =
			[
				new SchemaColumn { Name = "Modified", ClrType = typeof(DateTimeOffset), Precision = 7 },
			],
			Factory = () => new ColAttrTestEntity(),
		};

		// DB has DATETIMEOFFSET(3)
		var dbCols = new[]
		{
			new DbColumnInfo("TestTable", "Modified", "datetimeoffset", false, null, 3, null),
		};

		var diffs = SchemaMigrator.Compare([schema], dbCols, SqlServerDialect.Instance, false);

		diffs.Any(d => d.ColumnName == "Modified").AssertTrue(
			"DateTimeOffset precision mismatch (7 vs 3) should be detected");
	}

	#endregion

	#region SchemaMigrator.GenerateMigrationSql

	[TestMethod]
	public void GenerateSql_MissingColumn_WithMaxLength()
	{
		var schema = new Schema
		{
			TableName = "Users",
			EntityType = typeof(ColAttrTestEntity),
			Columns =
			[
				new SchemaColumn { Name = "Email", ClrType = typeof(string), MaxLength = 256 },
			],
			Factory = () => new ColAttrTestEntity(),
		};

		var diffs = new[]
		{
			new SchemaDiff("Users", "Email", SchemaDiffKind.MissingColumn, "NVARCHAR(256) NOT NULL", string.Empty),
		};

		var sql = SchemaMigrator.GenerateMigrationSql(SqlServerDialect.Instance, diffs, [schema]);

		sql.Contains("[Email]").AssertTrue($"Expected column name in SQL: {sql}");
		sql.Contains("NVARCHAR(256) NOT NULL").AssertTrue($"Expected column def in SQL: {sql}");
		sql.Contains("ADD").AssertTrue($"Expected ADD in SQL: {sql}");
	}

	[TestMethod]
	public void GenerateSql_NullabilityMismatch_GeneratesAlter()
	{
		var schema = new Schema
		{
			TableName = "Users",
			EntityType = typeof(ColAttrTestEntity),
			Columns =
			[
				new SchemaColumn { Name = "Phone", ClrType = typeof(string), IsNullable = true, MaxLength = 64 },
			],
			Factory = () => new ColAttrTestEntity(),
		};

		var diffs = new[]
		{
			new SchemaDiff("Users", "Phone", SchemaDiffKind.NullabilityMismatch, "NULL", "NOT NULL"),
		};

		var sql = SchemaMigrator.GenerateMigrationSql(SqlServerDialect.Instance, diffs, [schema]);

		sql.Contains("ALTER COLUMN").AssertTrue($"Expected ALTER COLUMN in SQL: {sql}");
		sql.Contains("NVARCHAR(64) NULL").AssertTrue($"Expected nullable column def in SQL: {sql}");
	}

	[TestMethod]
	public void GenerateSql_ExtraColumn_OnlyComment()
	{
		var schema = new Schema
		{
			TableName = "Users",
			EntityType = typeof(ColAttrTestEntity),
			Columns = [],
			Factory = () => new ColAttrTestEntity(),
		};

		var diffs = new[]
		{
			new SchemaDiff("Users", "OldCol", SchemaDiffKind.ExtraColumn, string.Empty, "exists in DB"),
		};

		var sql = SchemaMigrator.GenerateMigrationSql(SqlServerDialect.Instance, diffs, [schema]);

		sql.Contains("--").AssertTrue($"Expected comment for extra column: {sql}");
		sql.Contains("ALTER TABLE").AssertFalse($"Should not generate ALTER for extra column: {sql}");
	}

	[TestMethod]
	public void GenerateSql_PostgreSql_MissingColumn_UsesVarchar()
	{
		var schema = new Schema
		{
			TableName = "Users",
			EntityType = typeof(ColAttrTestEntity),
			Columns =
			[
				new SchemaColumn { Name = "Email", ClrType = typeof(string), MaxLength = 256 },
			],
			Factory = () => new ColAttrTestEntity(),
		};

		var diffs = new[]
		{
			new SchemaDiff("Users", "Email", SchemaDiffKind.MissingColumn, "VARCHAR(256) NOT NULL", string.Empty),
		};

		var sql = SchemaMigrator.GenerateMigrationSql(PostgreSqlDialect.Instance, diffs, [schema]);

		sql.Contains("VARCHAR(256) NOT NULL").AssertTrue($"Expected VARCHAR(256) in SQL: {sql}");
	}

	[TestMethod]
	public void GenerateSql_MissingTable_GeneratesCreateTable()
	{
		var schema = new Schema
		{
			TableName = "NewTable",
			EntityType = typeof(ColAttrTestEntity),
			Identity = new SchemaColumn { Name = "Id", ClrType = typeof(long), IsReadOnly = true },
			Columns =
			[
				new SchemaColumn { Name = "Name", ClrType = typeof(string), MaxLength = 128 },
				new SchemaColumn { Name = "Value", ClrType = typeof(int) },
			],
			Factory = () => new ColAttrTestEntity(),
		};

		var diffs = new[]
		{
			new SchemaDiff("NewTable", string.Empty, SchemaDiffKind.MissingTable, "expected", "missing"),
		};

		var sql = SchemaMigrator.GenerateMigrationSql(SqlServerDialect.Instance, diffs, [schema]);

		sql.Contains("CREATE TABLE").AssertTrue($"Expected CREATE TABLE in SQL: {sql}");
		sql.Contains("[NewTable]").AssertTrue($"Expected table name in SQL: {sql}");
		sql.Contains("[Id]").AssertTrue($"Expected identity column in SQL: {sql}");
		sql.Contains("[Name]").AssertTrue($"Expected Name column in SQL: {sql}");
		sql.Contains("[Value]").AssertTrue($"Expected Value column in SQL: {sql}");
	}

	[TestMethod]
	public void GenerateSql_PrecisionMismatch_UsesEntityPrecisionNotDefault()
	{
		// Finding #1: GenerateMigrationSql ignores Precision/Scale from schema —
		// always emits hardcoded DECIMAL(18,8) instead of the entity-specified precision.
		var schema = new Schema
		{
			TableName = "Prices",
			EntityType = typeof(ColAttrTestEntity),
			Columns =
			[
				new SchemaColumn { Name = "Amount", ClrType = typeof(decimal), Precision = 10, Scale = 2 },
			],
			Factory = () => new ColAttrTestEntity(),
		};

		var diffs = new[]
		{
			new SchemaDiff("Prices", "Amount", SchemaDiffKind.PrecisionMismatch, "(10,2)", "(18,8)"),
		};

		var sql = SchemaMigrator.GenerateMigrationSql(SqlServerDialect.Instance, diffs, [schema]);

		// The ALTER should use DECIMAL(10,2), not the hardcoded DECIMAL(18,8)
		sql.ContainsIgnoreCase("DECIMAL(10,2)").AssertTrue(
			$"Expected DECIMAL(10,2) in migration SQL, got: {sql}");
	}

	[TestMethod]
	public void GenerateSql_MissingTable_DecimalWithCustomPrecision()
	{
		// Finding #1: CREATE TABLE also uses hardcoded decimal type instead of entity precision.
		var schema = new Schema
		{
			TableName = "Accounts",
			EntityType = typeof(ColAttrTestEntity),
			Identity = new SchemaColumn { Name = "Id", ClrType = typeof(long), IsReadOnly = true },
			Columns =
			[
				new SchemaColumn { Name = "Balance", ClrType = typeof(decimal), Precision = 12, Scale = 4 },
			],
			Factory = () => new ColAttrTestEntity(),
		};

		var diffs = new[]
		{
			new SchemaDiff("Accounts", string.Empty, SchemaDiffKind.MissingTable, "expected", "missing"),
		};

		var sql = SchemaMigrator.GenerateMigrationSql(SqlServerDialect.Instance, diffs, [schema]);

		// CREATE TABLE should use DECIMAL(12,4), not DECIMAL(18,8)
		sql.ContainsIgnoreCase("DECIMAL(12,4)").AssertTrue(
			$"Expected DECIMAL(12,4) in CREATE TABLE SQL, got: {sql}");
	}

	[TestMethod]
	public void GenerateSql_MissingColumn_DecimalWithCustomPrecision()
	{
		// Finding #1: ADD COLUMN also uses hardcoded decimal type.
		var schema = new Schema
		{
			TableName = "Products",
			EntityType = typeof(ColAttrTestEntity),
			Columns =
			[
				new SchemaColumn { Name = "Weight", ClrType = typeof(decimal), Precision = 8, Scale = 3 },
			],
			Factory = () => new ColAttrTestEntity(),
		};

		var diffs = new[]
		{
			new SchemaDiff("Products", "Weight", SchemaDiffKind.MissingColumn, "DECIMAL(8,3) NOT NULL", string.Empty),
		};

		var sql = SchemaMigrator.GenerateMigrationSql(SqlServerDialect.Instance, diffs, [schema]);

		sql.ContainsIgnoreCase("DECIMAL(8,3)").AssertTrue(
			$"Expected DECIMAL(8,3) in ADD COLUMN SQL, got: {sql}");
	}

	[TestMethod]
	public void GenerateSql_MissingTable_GuidIdentity_NoAutoIncrement()
	{
		var schema = new Schema
		{
			TableName = "GuidTable",
			EntityType = typeof(ColAttrTestEntity),
			Identity = new SchemaColumn { Name = "Id", ClrType = typeof(Guid), IsReadOnly = true },
			Columns =
			[
				new SchemaColumn { Name = "Name", ClrType = typeof(string) },
			],
			Factory = () => new ColAttrTestEntity(),
		};

		var diffs = new[]
		{
			new SchemaDiff("GuidTable", string.Empty, SchemaDiffKind.MissingTable, "expected", "missing"),
		};

		var sql = SchemaMigrator.GenerateMigrationSql(SqlServerDialect.Instance, diffs, [schema]);

		sql.Contains("IDENTITY(1,1)").AssertFalse($"GUID identity should not have IDENTITY(1,1): {sql}");
		sql.Contains("PRIMARY KEY").AssertTrue($"GUID identity should still be PRIMARY KEY: {sql}");
		sql.Contains("UNIQUEIDENTIFIER").AssertTrue($"Expected UNIQUEIDENTIFIER type: {sql}");
	}

	[TestMethod]
	public void GenerateSql_MissingTable_WithIndexColumns_GeneratesCreateIndex()
	{
		// Finding #4: GenerateMigrationSql does not create INDEX constraints
		// even though schema metadata marks columns as IsIndex.
		var schema = new Schema
		{
			TableName = "Orders",
			EntityType = typeof(ColAttrTestEntity),
			Identity = new SchemaColumn { Name = "Id", ClrType = typeof(long), IsReadOnly = true },
			Columns =
			[
				new SchemaColumn { Name = "CustomerId", ClrType = typeof(long), IsIndex = true },
				new SchemaColumn { Name = "Status", ClrType = typeof(int) },
			],
			Factory = () => new ColAttrTestEntity(),
		};

		var diffs = new[]
		{
			new SchemaDiff("Orders", string.Empty, SchemaDiffKind.MissingTable, "expected", "missing"),
		};

		var sql = SchemaMigrator.GenerateMigrationSql(SqlServerDialect.Instance, diffs, [schema]);

		sql.ContainsIgnoreCase("CREATE INDEX").AssertTrue(
			$"Expected CREATE INDEX for indexed column in SQL, got: {sql}");
		sql.ContainsIgnoreCase("CustomerId").AssertTrue(
			$"Expected CustomerId in index SQL, got: {sql}");
	}

	[TestMethod]
	public void GenerateSql_MissingTable_WithUniqueColumns_GeneratesUniqueConstraint()
	{
		// Finding #4: GenerateMigrationSql does not create UNIQUE constraints
		// even though schema metadata marks columns as IsUnique.
		var schema = new Schema
		{
			TableName = "Users",
			EntityType = typeof(ColAttrTestEntity),
			Identity = new SchemaColumn { Name = "Id", ClrType = typeof(long), IsReadOnly = true },
			Columns =
			[
				new SchemaColumn { Name = "Email", ClrType = typeof(string), MaxLength = 256, IsUnique = true, IsIndex = true },
				new SchemaColumn { Name = "Name", ClrType = typeof(string) },
			],
			Factory = () => new ColAttrTestEntity(),
		};

		var diffs = new[]
		{
			new SchemaDiff("Users", string.Empty, SchemaDiffKind.MissingTable, "expected", "missing"),
		};

		var sql = SchemaMigrator.GenerateMigrationSql(SqlServerDialect.Instance, diffs, [schema]);

		sql.ContainsIgnoreCase("UNIQUE").AssertTrue(
			$"Expected UNIQUE constraint for unique column in SQL, got: {sql}");
	}

	[TestMethod]
	public void Compare_MissingTable_Detected()
	{
		var schema = new Schema
		{
			TableName = "MissingTable",
			EntityType = typeof(ColAttrTestEntity),
			Columns =
			[
				new SchemaColumn { Name = "Name", ClrType = typeof(string) },
			],
			Factory = () => new ColAttrTestEntity(),
		};

		// empty DB columns — table doesn't exist
		var diffs = SchemaMigrator.Compare([schema], [], SqlServerDialect.Instance, false);

		diffs.Count.AssertEqual(1);
		diffs[0].Kind.AssertEqual(SchemaDiffKind.MissingTable);
		diffs[0].TableName.AssertEqual("MissingTable");
	}

	[TestMethod]
	public void Compare_PostgreSql_NoDifferencesForNativeTypes()
	{
		var schema = new Schema
		{
			TableName = "TestTable",
			EntityType = typeof(ColAttrTestEntity),
			Columns =
			[
				new SchemaColumn { Name = "Name", ClrType = typeof(string) },
				new SchemaColumn { Name = "Active", ClrType = typeof(bool) },
				new SchemaColumn { Name = "ExternalId", ClrType = typeof(Guid) },
			],
			Factory = () => new ColAttrTestEntity(),
		};

		// PostgreSQL returns these native type names
		var dbCols = new[]
		{
			new DbColumnInfo("TestTable", "Name", "text", false, null, null, null),
			new DbColumnInfo("TestTable", "Active", "boolean", false, null, null, null),
			new DbColumnInfo("TestTable", "ExternalId", "uuid", false, null, null, null),
		};

		var diffs = SchemaMigrator.Compare([schema], dbCols, PostgreSqlDialect.Instance, false);

		diffs.Count.AssertEqual(0);
	}

	[TestMethod]
	public void Compare_SQLite_NoDifferencesForNativeTypes()
	{
		var schema = new Schema
		{
			TableName = "TestTable",
			EntityType = typeof(ColAttrTestEntity),
			Columns =
			[
				new SchemaColumn { Name = "Name", ClrType = typeof(string) },
				new SchemaColumn { Name = "Count", ClrType = typeof(int) },
				new SchemaColumn { Name = "Data", ClrType = typeof(byte[]) },
			],
			Factory = () => new ColAttrTestEntity(),
		};

		var dbCols = new[]
		{
			new DbColumnInfo("TestTable", "Name", "TEXT", false, null, null, null),
			new DbColumnInfo("TestTable", "Count", "INTEGER", false, null, null, null),
			new DbColumnInfo("TestTable", "Data", "BLOB", false, null, null, null),
		};

		var diffs = SchemaMigrator.Compare([schema], dbCols, SQLiteDialect.Instance, false);

		diffs.Count.AssertEqual(0);
	}

	#endregion

	#region ColumnAttribute IsNullable inference

	[TestMethod]
	public void ColumnAttr_MaxLengthOnly_DoesNotForceNotNull()
	{
		// [Column(MaxLength = 128)] on string (non-nullable reference type)
		// should infer IsNullable from the CLR type, not default to false
		var schema = SchemaRegistry.Get(typeof(ColAttrTestEntity));
		var col = schema.Columns.First(c => c.Name == "Name");

		// string without ? in source ⇒ not nullable (correct)
		col.IsNullable.AssertFalse();
	}

	#endregion

	#region DateOnly / TimeOnly support

	[TestMethod]
	public void GetSqlTypeName_DateOnly_SqlServer()
	{
		SqlServerDialect.Instance.GetSqlTypeName(typeof(DateOnly)).AssertEqual("DATE");
	}

	[TestMethod]
	public void GetSqlTypeName_TimeOnly_SqlServer()
	{
		SqlServerDialect.Instance.GetSqlTypeName(typeof(TimeOnly)).AssertEqual("TIME");
	}

	[TestMethod]
	public void GetSqlTypeName_DateOnly_PostgreSql()
	{
		PostgreSqlDialect.Instance.GetSqlTypeName(typeof(DateOnly)).AssertEqual("DATE");
	}

	[TestMethod]
	public void GetSqlTypeName_TimeOnly_PostgreSql()
	{
		PostgreSqlDialect.Instance.GetSqlTypeName(typeof(TimeOnly)).AssertEqual("TIME");
	}

	[TestMethod]
	public void GetSqlTypeName_DateOnly_SQLite()
	{
		SQLiteDialect.Instance.GetSqlTypeName(typeof(DateOnly)).AssertEqual("TEXT");
	}

	[TestMethod]
	public void GetSqlTypeName_TimeOnly_SQLite()
	{
		SQLiteDialect.Instance.GetSqlTypeName(typeof(TimeOnly)).AssertEqual("TEXT");
	}

	#endregion

	#region Helpers

	private static ISqlDialect GetDialect(string name) => name switch
	{
		"SqlServer" => SqlServerDialect.Instance,
		"SQLite" => SQLiteDialect.Instance,
		"PostgreSql" => PostgreSqlDialect.Instance,
		_ => throw new ArgumentException($"Unknown dialect: {name}")
	};

	private static Func<string, string> QuoteFn(string dialectName) => dialectName switch
	{
		"SqlServer" => id => $"[{id}]",
		"SQLite" or "PostgreSql" => id => $"\"{id}\"",
		_ => id => id
	};

	#endregion

	#region Finding #5: DateTime precision in DDL

	[TestMethod]
	[DataRow("SqlServer", "DATETIME2(3) NOT NULL")]
	[DataRow("PostgreSql", "TIMESTAMP(3) NOT NULL")]
	public void GetColumnDefinition_DateTime_WithPrecision(string dialectName, string expected)
	{
		var dialect = GetDialect(dialectName);

		var result = dialect.GetColumnDefinition(typeof(DateTime), false, precision: 3);

		result.AssertEqual(expected);
	}

	[TestMethod]
	[DataRow("SqlServer", "DATETIMEOFFSET(3) NOT NULL")]
	[DataRow("PostgreSql", "TIMESTAMPTZ(3) NOT NULL")]
	public void GetColumnDefinition_DateTimeOffset_WithPrecision(string dialectName, string expected)
	{
		var dialect = GetDialect(dialectName);

		var result = dialect.GetColumnDefinition(typeof(DateTimeOffset), false, precision: 3);

		result.AssertEqual(expected);
	}

	#endregion
}

#endif
