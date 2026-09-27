#if NET10_0_OR_GREATER

namespace Ecng.Tests.Data;

using System.Text;

using Ecng.Data;
using Ecng.Serialization;

using Microsoft.Data.Sqlite;

#region Test entities for ColumnAttribute

public class ColAttrInner
{
	[Column(MaxLength = 50)]
	public string Tag { get; set; }

	public int Score { get; set; }

	[Column(Precision = 18, Scale = 4)]
	public decimal Weight { get; set; }
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

	[Column(Precision = 18, Scale = 6)]
	public decimal Amount { get; set; }

	public decimal PlainAmount { get; set; }

	// Plain id column that declares its FK target without being a navigation relation.
	[ForeignKey(typeof(ColAttrTestEntity))]
	public long ParentId { get; set; }

	public ColAttrInner Meta { get; set; }

	object IDbPersistable.GetIdentity() => Id;
	void IDbPersistable.SetIdentity(object id) => Id = id.To<long>();
	public void Save(SettingsStorage storage) { }
	public ValueTask LoadAsync(SettingsStorage storage, IStorage db, CancellationToken ct) => default;
}

// Inner type without ORM-driven digits, as a DTO from an assembly that does not reference the ORM.
public class ColOverrideMoney
{
	public decimal Amount { get; set; }

	[Column(Precision = 20, Scale = 6)]
	public decimal Fee { get; set; }

	[Column(Precision = 20, Scale = 6)]
	public decimal Rate { get; set; }

	public decimal Tax { get; set; }

	public decimal Price { get; set; }
}

[Entity(Name = "Ecng_ColOverridePrecision")]
public class ColOverridePrecisionEntity : IDbPersistable
{
	public long Id { get; set; }

	[ColumnOverride(nameof(ColOverrideMoney.Amount), Precision = 18, Scale = 2)]
	[ColumnOverride(nameof(ColOverrideMoney.Fee), IsNullable = true)]
	[ColumnOverride(nameof(ColOverrideMoney.Rate), Precision = 10)]
	[ColumnOverride(nameof(ColOverrideMoney.Tax), Scale = 3)]
	[ColumnOverride(nameof(ColOverrideMoney.Price), IsNullable = true, Precision = 12, Scale = 4)]
	public ColOverrideMoney Money { get; set; }

	object IDbPersistable.GetIdentity() => Id;
	void IDbPersistable.SetIdentity(object id) => Id = id.To<long>();
	public void Save(SettingsStorage storage) { }
	public ValueTask LoadAsync(SettingsStorage storage, IStorage db, CancellationToken ct) => default;
}

public class ColOverrideBalanceBase
{
	public decimal Balance { get; set; }

	public decimal Reserved { get; set; }
}

[Entity(Name = "Ecng_ColOverrideInherited")]
[ColumnOverride(nameof(ColOverrideBalanceBase.Balance), Precision = 18, Scale = 2)]
public class ColOverrideInheritedEntity : ColOverrideBalanceBase, IDbPersistable
{
	public long Id { get; set; }

	object IDbPersistable.GetIdentity() => Id;
	void IDbPersistable.SetIdentity(object id) => Id = id.To<long>();
	public void Save(SettingsStorage storage) { }
	public ValueTask LoadAsync(SettingsStorage storage, IStorage db, CancellationToken ct) => default;
}

// Inner type without ORM-driven lengths, as a DTO from an assembly that does not reference the ORM.
public class ColOverrideText
{
	public string Tag { get; set; }

	[Column(MaxLength = 50)]
	public string Code { get; set; }

	public string Note { get; set; }

	[Column(MaxLength = 100)]
	public string Body { get; set; }

	public byte[] Hash { get; set; }
}

[Entity(Name = "Ecng_ColOverrideLength")]
public class ColOverrideLengthEntity : IDbPersistable
{
	public long Id { get; set; }

	[ColumnOverride(nameof(ColOverrideText.Tag), MaxLength = 64)]
	[ColumnOverride(nameof(ColOverrideText.Code), IsNullable = true)]
	[ColumnOverride(nameof(ColOverrideText.Note), IsNullable = true, MaxLength = 200)]
	[ColumnOverride(nameof(ColOverrideText.Body), MaxLength = ColumnAttribute.Max)]
	[ColumnOverride(nameof(ColOverrideText.Hash), MaxLength = 32)]
	public ColOverrideText Text { get; set; }

	object IDbPersistable.GetIdentity() => Id;
	void IDbPersistable.SetIdentity(object id) => Id = id.To<long>();
	public void Save(SettingsStorage storage) { }
	public ValueTask LoadAsync(SettingsStorage storage, IStorage db, CancellationToken ct) => default;
}

public class ColOverrideLabelBase
{
	public string Label { get; set; }

	public string Remark { get; set; }
}

[Entity(Name = "Ecng_ColOverrideInheritedLength")]
[ColumnOverride(nameof(ColOverrideLabelBase.Label), MaxLength = 32)]
public class ColOverrideInheritedLengthEntity : ColOverrideLabelBase, IDbPersistable
{
	public long Id { get; set; }

	object IDbPersistable.GetIdentity() => Id;
	void IDbPersistable.SetIdentity(object id) => Id = id.To<long>();
	public void Save(SettingsStorage storage) { }
	public ValueTask LoadAsync(SettingsStorage storage, IStorage db, CancellationToken ct) => default;
}

#endregion

// Base class contributing an inherited column — mirrors the soft-delete `Deleted`
// column that lives on a shared base entity in real models and so cannot be reached
// by a property-level [Index] on a derived entity without applying to every table.
public class TypeIndexBase
{
	public bool Flag { get; set; }
}

// Composite index declared at the TYPE level via [Index(FieldName=...)], spanning an
// own column (Code) and the inherited base column (Flag).
[Index(FieldName = "Code", Name = "IX_TypeIndex_CodeFlag", Order = 0)]
[Index(FieldName = "Flag", Name = "IX_TypeIndex_CodeFlag", Order = 1)]
public class TypeIndexEntity : TypeIndexBase, IDbPersistable
{
	public long Id { get; set; }

	[Column(MaxLength = 64)]
	public string Code { get; set; }

	object IDbPersistable.GetIdentity() => Id;
	void IDbPersistable.SetIdentity(object id) => Id = id.To<long>();
	public void Save(SettingsStorage storage) { }
	public ValueTask LoadAsync(SettingsStorage storage, IStorage db, CancellationToken ct) => default;
}

// Column A participates in BOTH a non-unique single index (its own [Index]) and a unique
// composite (A, B). Uniqueness must be tracked per index, not per column, so the single
// index on A stays non-unique while the composite is unique.
[Unique(FieldName = "A", Name = "UX_PerIndex_AB", Order = 0)]
[Unique(FieldName = "B", Name = "UX_PerIndex_AB", Order = 1)]
public class PerIndexUniqueEntity : IDbPersistable
{
	public long Id { get; set; }

	[Index]
	public long A { get; set; }

	public long B { get; set; }

	object IDbPersistable.GetIdentity() => Id;
	void IDbPersistable.SetIdentity(object id) => Id = id.To<long>();
	public void Save(SettingsStorage storage) { }
	public ValueTask LoadAsync(SettingsStorage storage, IStorage db, CancellationToken ct) => default;
}

// Composite index declared with the EF-Core-style column-list constructor: one attribute
// lists the ordered columns, the index name is auto-generated, no Order/Name needed.
[Index(nameof(A), nameof(B))]
public class ColumnListIndexEntity : IDbPersistable
{
	public long Id { get; set; }

	public long A { get; set; }

	public long B { get; set; }

	object IDbPersistable.GetIdentity() => Id;
	void IDbPersistable.SetIdentity(object id) => Id = id.To<long>();
	public void Save(SettingsStorage storage) { }
	public ValueTask LoadAsync(SettingsStorage storage, IStorage db, CancellationToken ct) => default;
}

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

	[TestMethod]
	public void ColumnAttr_PrecisionScale_SetsPrecisionScaleOnColumn()
	{
		var schema = SchemaRegistry.Get(typeof(ColAttrTestEntity));
		var col = schema.Columns.First(c => c.Name == "Amount");

		col.Precision.AssertEqual(18);
		col.Scale.AssertEqual(6);
	}

	[TestMethod]
	public void NoAttribute_Decimal_LeavesPrecisionScaleToDialect()
	{
		var schema = SchemaRegistry.Get(typeof(ColAttrTestEntity));
		var col = schema.Columns.First(c => c.Name == "PlainAmount");

		col.Precision.AssertEqual(0);
		col.Scale.AssertEqual(0);
	}

	[TestMethod]
	public void InnerSchema_PrecisionScale_PropagatedToFlattenedColumn()
	{
		var schema = SchemaRegistry.Get(typeof(ColAttrTestEntity));
		var col = schema.Columns.First(c => c.Name == "MetaWeight");

		col.Precision.AssertEqual(18);
		col.Scale.AssertEqual(4);
	}

	[TestMethod]
	public void ColumnOverride_PrecisionScale_AppliedToFlattenedColumn()
	{
		var schema = SchemaRegistry.Get(typeof(ColOverridePrecisionEntity));
		var col = schema.Columns.First(c => c.Name == "MoneyAmount");

		col.Precision.AssertEqual(18);
		col.Scale.AssertEqual(2);
		col.IsNullable.AssertFalse();
	}

	[TestMethod]
	public void ColumnOverride_WithoutDigits_KeepsInnerColumnDigits()
	{
		var schema = SchemaRegistry.Get(typeof(ColOverridePrecisionEntity));
		var col = schema.Columns.First(c => c.Name == "MoneyFee");

		col.Precision.AssertEqual(20);
		col.Scale.AssertEqual(6);
		col.IsNullable.AssertTrue();
	}

	[TestMethod]
	public void ColumnOverride_PrecisionOnly_MakesScaleLiteral()
	{
		var schema = SchemaRegistry.Get(typeof(ColOverridePrecisionEntity));
		var col = schema.Columns.First(c => c.Name == "MoneyRate");

		col.Precision.AssertEqual(10);
		col.Scale.AssertEqual(0);
	}

	[TestMethod]
	public void ColumnOverride_ScaleOnly_LeavesPrecisionToDialect()
	{
		var schema = SchemaRegistry.Get(typeof(ColOverridePrecisionEntity));
		var col = schema.Columns.First(c => c.Name == "MoneyTax");

		col.Precision.AssertEqual(0);
		col.Scale.AssertEqual(3);
	}

	[TestMethod]
	public void ColumnOverride_NullabilityAndPrecision_BothApplied()
	{
		var schema = SchemaRegistry.Get(typeof(ColOverridePrecisionEntity));
		var col = schema.Columns.First(c => c.Name == "MoneyPrice");

		col.IsNullable.AssertTrue();
		col.Precision.AssertEqual(12);
		col.Scale.AssertEqual(4);
	}

	[TestMethod]
	public void EntityLevelColumnOverride_PrecisionScale_AppliedToInheritedColumn()
	{
		var schema = SchemaRegistry.Get(typeof(ColOverrideInheritedEntity));

		var balance = schema.Columns.First(c => c.Name == "Balance");
		balance.Precision.AssertEqual(18);
		balance.Scale.AssertEqual(2);

		var reserved = schema.Columns.First(c => c.Name == "Reserved");
		reserved.Precision.AssertEqual(0);
		reserved.Scale.AssertEqual(0);
	}

	[TestMethod]
	public void Compare_OverriddenFlattenedColumn_AgainstWiderLiveColumn_Detected()
	{
		var schema = SchemaRegistry.Get(typeof(ColOverridePrecisionEntity));

		var diffs = SchemaMigrator.Compare([schema], [new DbColumnInfo(schema.TableName, "MoneyAmount", "decimal", false, null, 18, 8)], SqlServerDialect.Instance, false);

		diffs.Any(d => d.ColumnName == "MoneyAmount" && d.Kind == SchemaDiffKind.PrecisionMismatch).AssertTrue(
			"The override declares DECIMAL(18,2), which a DECIMAL(18,8) column is not");
	}

	[TestMethod]
	public void ColumnOverride_MaxLength_AppliedToFlattenedColumn()
	{
		var schema = SchemaRegistry.Get(typeof(ColOverrideLengthEntity));
		var col = schema.Columns.First(c => c.Name == "TextTag");

		col.MaxLength.AssertEqual(64);
		col.IsNullable.AssertFalse();
	}

	[TestMethod]
	public void ColumnOverride_MaxLength_AppliedToFlattenedBinaryColumn()
	{
		var schema = SchemaRegistry.Get(typeof(ColOverrideLengthEntity));

		schema.Columns.First(c => c.Name == "TextHash").MaxLength.AssertEqual(32);
	}

	[TestMethod]
	public void ColumnOverride_WithoutMaxLength_KeepsInnerColumnLength()
	{
		var schema = SchemaRegistry.Get(typeof(ColOverrideLengthEntity));
		var col = schema.Columns.First(c => c.Name == "TextCode");

		col.MaxLength.AssertEqual(50);
		col.IsNullable.AssertTrue();
	}

	[TestMethod]
	public void ColumnOverride_NullabilityAndMaxLength_BothApplied()
	{
		var schema = SchemaRegistry.Get(typeof(ColOverrideLengthEntity));
		var col = schema.Columns.First(c => c.Name == "TextNote");

		col.IsNullable.AssertTrue();
		col.MaxLength.AssertEqual(200);
	}

	[TestMethod]
	public void ColumnOverride_MaxLengthMax_ReplacesInnerLengthWithUnbounded()
	{
		var schema = SchemaRegistry.Get(typeof(ColOverrideLengthEntity));

		schema.Columns.First(c => c.Name == "TextBody").MaxLength.AssertEqual(ColumnAttribute.Max);
	}

	[TestMethod]
	public void EntityLevelColumnOverride_MaxLength_AppliedToInheritedColumn()
	{
		var schema = SchemaRegistry.Get(typeof(ColOverrideInheritedLengthEntity));

		schema.Columns.First(c => c.Name == "Label").MaxLength.AssertEqual(32);
		schema.Columns.First(c => c.Name == "Remark").MaxLength.AssertEqual(0);
	}

	[TestMethod]
	public void Compare_OverriddenFlattenedStringColumn_AgainstWiderLiveColumn_Detected()
	{
		var schema = SchemaRegistry.Get(typeof(ColOverrideLengthEntity));

		var diffs = SchemaMigrator.Compare([schema], [new DbColumnInfo(schema.TableName, "TextTag", "nvarchar", false, 256, null, null)], SqlServerDialect.Instance, false);

		diffs.Any(d => d.ColumnName == "TextTag" && d.Kind == SchemaDiffKind.MaxLengthMismatch).AssertTrue(
			"The override declares NVARCHAR(64), which an NVARCHAR(256) column is not");
	}

	[TestMethod]
	public void GenerateSql_OverriddenFlattenedStringColumn_AltersToTheDeclaredLength()
	{
		var column = SchemaRegistry.Get(typeof(ColOverrideLengthEntity)).Columns.First(c => c.Name == "TextTag");

		var sql = MigrateColumn(SqlServerDialect.Instance, column, new DbColumnInfo("Tags", "TextTag", "nvarchar", false, 256, null, null));

		sql.ContainsIgnoreCase("ALTER COLUMN [TextTag] NVARCHAR(64) NOT NULL").AssertTrue(sql);
	}

	#endregion

	#region GetColumnDefinition (driver-agnostic)

	[TestMethod]
	[DataRow("SqlServer", "NVARCHAR(128) NOT NULL")]
	[DataRow("PostgreSql", "VARCHAR(128) NOT NULL")]
	// SQLite has dynamic typing — maxLength is intentionally dropped, the
	// affinity stays TEXT.
	[DataRow("SQLite", "TEXT NOT NULL")]
	public void GetColumnDef_StringWithMaxLength(string dialectName, string expected)
	{
		var def = GetDialect(dialectName).GetColumnDefinition(typeof(string), isNullable: false, maxLength: 128);

		def.AssertEqual(expected);
	}

	[TestMethod]
	[DataRow("SqlServer", "NVARCHAR(MAX) NULL")]
	[DataRow("PostgreSql", "TEXT NULL")]
	[DataRow("SQLite", "TEXT NULL")]
	public void GetColumnDef_StringUnlimited(string dialectName, string expected)
	{
		var def = GetDialect(dialectName).GetColumnDefinition(typeof(string), isNullable: true, maxLength: 0);

		def.AssertEqual(expected);
	}

	[TestMethod]
	[DataRow("SqlServer", "NVARCHAR(MAX) NOT NULL")]
	[DataRow("PostgreSql", "TEXT NOT NULL")]
	[DataRow("SQLite", "TEXT NOT NULL")]
	public void GetColumnDef_StringMaxSentinel_MapsToUnbounded(string dialectName, string expected)
	{
		var def = GetDialect(dialectName).GetColumnDefinition(typeof(string), isNullable: false, maxLength: ColumnAttribute.Max);

		def.AssertEqual(expected);
	}

	[TestMethod]
	[DataRow("SqlServer", "VARBINARY(256) NOT NULL")]
	// PostgreSQL and SQLite use a single binary affinity; explicit length is dropped.
	[DataRow("PostgreSql", "BYTEA NOT NULL")]
	[DataRow("SQLite", "BLOB NOT NULL")]
	public void GetColumnDef_ByteArrayWithMaxLength(string dialectName, string expected)
	{
		var def = GetDialect(dialectName).GetColumnDefinition(typeof(byte[]), isNullable: false, maxLength: 256);

		def.AssertEqual(expected);
	}

	[TestMethod]
	[DataRow("SqlServer", "VARBINARY(MAX) NULL")]
	[DataRow("PostgreSql", "BYTEA NULL")]
	[DataRow("SQLite", "BLOB NULL")]
	public void GetColumnDef_ByteArrayUnlimited(string dialectName, string expected)
	{
		var def = GetDialect(dialectName).GetColumnDefinition(typeof(byte[]), isNullable: true, maxLength: 0);

		def.AssertEqual(expected);
	}

	[TestMethod]
	[DataRow("SqlServer", "VARBINARY(MAX) NULL")]
	[DataRow("PostgreSql", "BYTEA NULL")]
	[DataRow("SQLite", "BLOB NULL")]
	public void GetColumnDef_ByteArrayMaxSentinel_MapsToUnbounded(string dialectName, string expected)
	{
		var def = GetDialect(dialectName).GetColumnDefinition(typeof(byte[]), isNullable: true, maxLength: ColumnAttribute.Max);

		def.AssertEqual(expected);
	}

	[TestMethod]
	[DataRow("SqlServer", "INT NOT NULL")]
	[DataRow("PostgreSql", "INTEGER NOT NULL")]
	[DataRow("SQLite", "INTEGER NOT NULL")]
	public void GetColumnDef_Int(string dialectName, string expected)
	{
		var def = GetDialect(dialectName).GetColumnDefinition(typeof(int), isNullable: false);

		def.AssertEqual(expected);
	}

	[TestMethod]
	[DataRow("SqlServer", "BIGINT NULL")]
	[DataRow("PostgreSql", "BIGINT NULL")]
	[DataRow("SQLite", "INTEGER NULL")]
	public void GetColumnDef_NullableLong(string dialectName, string expected)
	{
		var def = GetDialect(dialectName).GetColumnDefinition(typeof(long?), isNullable: true);

		def.AssertEqual(expected);
	}

	[TestMethod]
	[DataRow("SqlServer", "NVARCHAR(MAX)")]
	[DataRow("PostgreSql", "SET DATA TYPE TEXT")]
	// SQLite has no ALTER COLUMN at all; that path throws by design and is
	// covered separately in the dialect tests.
	public void AppendAlterColumn_StringMaxSentinel_MapsToUnbounded(string dialectName, string expectedFragment)
	{
		var sb = new StringBuilder();

		GetDialect(dialectName).AppendAlterColumn(sb, "Users", "Notes", typeof(string), isNullable: true, maxLength: ColumnAttribute.Max, precision: 0, scale: 0, live: null);

		sb.ToString().Contains(expectedFragment).AssertTrue($"Expected '{expectedFragment}', got: {sb}");
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

		SqlServerDialect.Instance.AppendAlterColumn(sb, "Users", "Email", typeof(string), true, 512, 0, 0, null);

		sb.ToString().AssertEqual("ALTER TABLE [Users] ALTER COLUMN [Email] NVARCHAR(512) NULL");
	}

	[TestMethod]
	public void AppendAlterColumn_PostgreSql_SeparatesTypeAndNullability()
	{
		var sb = new StringBuilder();

		PostgreSqlDialect.Instance.AppendAlterColumn(sb, "Users", "Email", typeof(string), true, 512, 0, 0, null);

		var sql = sb.ToString();
		sql.Contains("SET DATA TYPE VARCHAR(512)").AssertTrue($"Expected SET DATA TYPE, got: {sql}");
		sql.Contains("DROP NOT NULL").AssertTrue($"Expected DROP NOT NULL, got: {sql}");
		sql.Contains("NULL").AssertTrue($"Expected nullability clause, got: {sql}");
	}

	[TestMethod]
	public void AppendAlterColumn_PostgreSql_NotNull()
	{
		var sb = new StringBuilder();

		PostgreSqlDialect.Instance.AppendAlterColumn(sb, "Users", "Email", typeof(string), false, 256, 0, 0, null);

		var sql = sb.ToString();
		sql.Contains("SET DATA TYPE VARCHAR(256)").AssertTrue($"Expected SET DATA TYPE, got: {sql}");
		sql.Contains("SET NOT NULL").AssertTrue($"Expected SET NOT NULL, got: {sql}");
	}

	[TestMethod]
	[DataRow("SqlServer")]
	[DataRow("PostgreSql")]
	public void AppendDropColumn_GeneratesCorrectDdl(string dialectName)
	{
		var dialect = GetDialect(dialectName);
		var q = QuoteFn(dialectName);
		var sb = new StringBuilder();

		dialect.AppendDropColumn(sb, "Users", "OldCol");

		sb.ToString().AssertEqual($"ALTER TABLE {q("Users")} DROP COLUMN {q("OldCol")}");
	}

	[TestMethod]
	public void AppendDropColumn_SQLite_ThrowsExplicitError()
	{
		// SQLite's DROP COLUMN is version-dependent; this dialect refuses
		// to emit it and tells callers to use the table-rename pattern.
		Assert.ThrowsExactly<NotSupportedException>(() =>
			SQLiteDialect.Instance.AppendDropColumn(new StringBuilder(), "Users", "OldCol"));
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

	[TestMethod]
	public void Compare_MaxSentinel_AgainstSqlServerMaxColumn_NoMaxLengthDiff()
	{
		// Entity declares Max; SqlServer reports max_length = -1 for NVARCHAR(MAX).
		// Both encode "unbounded" — must not produce a perpetual MaxLengthMismatch.
		var schema = new Schema
		{
			TableName = "TestTable",
			EntityType = typeof(ColAttrTestEntity),
			Columns =
			[
				new SchemaColumn { Name = "Notes", ClrType = typeof(string), MaxLength = ColumnAttribute.Max },
			],
			Factory = () => new ColAttrTestEntity(),
		};

		var dbCols = new[]
		{
			new DbColumnInfo("TestTable", "Notes", "nvarchar", true, -1, null, null),
		};

		var diffs = SchemaMigrator.Compare([schema], dbCols, SqlServerDialect.Instance, false);

		diffs.Any(d => d.Kind == SchemaDiffKind.MaxLengthMismatch).AssertFalse(
			$"Expected no MaxLengthMismatch (both unbounded). Diffs: [{string.Join(", ", diffs.Select(d => $"{d.Kind} {d.ColumnName}"))}]");
	}

	[TestMethod]
	public void Compare_MaxSentinel_AgainstBoundedDbColumn_EmitsMaxLengthDiff()
	{
		// Entity wants unbounded (Max), DB has bounded NVARCHAR(100) — mismatch.
		var schema = new Schema
		{
			TableName = "TestTable",
			EntityType = typeof(ColAttrTestEntity),
			Columns =
			[
				new SchemaColumn { Name = "Notes", ClrType = typeof(string), MaxLength = ColumnAttribute.Max },
			],
			Factory = () => new ColAttrTestEntity(),
		};

		var dbCols = new[]
		{
			new DbColumnInfo("TestTable", "Notes", "nvarchar", true, 100, null, null),
		};

		var diffs = SchemaMigrator.Compare([schema], dbCols, SqlServerDialect.Instance, false);

		diffs.Any(d => d.Kind == SchemaDiffKind.MaxLengthMismatch && d.ColumnName == "Notes").AssertTrue(
			$"Expected MaxLengthMismatch for Notes (entity wants unbounded, DB has 100).");
	}

	[TestMethod]
	public void Compare_MaxLengthZero_AgainstNullDbMaxLength_NoDiff()
	{
		// Default MaxLength = 0 paired with PostgreSQL TEXT (DB MaxLength == null)
		// — historical behaviour, must remain unchanged.
		var schema = new Schema
		{
			TableName = "TestTable",
			EntityType = typeof(ColAttrTestEntity),
			Columns =
			[
				new SchemaColumn { Name = "Notes", ClrType = typeof(string), MaxLength = 0 },
			],
			Factory = () => new ColAttrTestEntity(),
		};

		var dbCols = new[]
		{
			new DbColumnInfo("TestTable", "Notes", "text", true, null, null, null),
		};

		var diffs = SchemaMigrator.Compare([schema], dbCols, PostgreSqlDialect.Instance, false);

		diffs.Any(d => d.Kind == SchemaDiffKind.MaxLengthMismatch).AssertFalse(
			$"MaxLength == 0 vs DB null must not emit MaxLengthMismatch. Diffs: [{string.Join(", ", diffs.Select(d => d.Kind))}]");
	}

	[TestMethod]
	public void ColumnAttribute_Max_EqualsIntMaxValue()
	{
		// Locks in the contract used by dialects: anything == int.MaxValue is
		// the unbounded sentinel.
		ColumnAttribute.Max.AssertEqual(int.MaxValue);
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

	[TestMethod]
	[DataRow("SqlServer", "DECIMAL(18,6) NOT NULL")]
	[DataRow("PostgreSql", "NUMERIC(18,6) NOT NULL")]
	public void GetColumnDefinition_DecimalScaleWithoutPrecision_KeepsTheScale(string dialectName, string expected)
	{
		ISqlDialect dialect = dialectName == "SqlServer" ? SqlServerDialect.Instance : PostgreSqlDialect.Instance;

		dialect.GetColumnDefinition(typeof(decimal), isNullable: false, scale: 6).AssertEqual(expected);
	}

	[TestMethod]
	public void GenerateSql_DecimalScaleWithoutPrecision_AltersToTheDeclaredScale()
	{
		var schema = new Schema
		{
			TableName = "Prices",
			EntityType = typeof(ColAttrTestEntity),
			Columns =
			[
				new SchemaColumn { Name = "Rate", ClrType = typeof(decimal), Scale = 6 },
			],
			Factory = () => new ColAttrTestEntity(),
		};

		var diffs = SchemaMigrator.Compare([schema], [new DbColumnInfo("Prices", "Rate", "decimal", false, null, 18, 8)], SqlServerDialect.Instance, false);
		var sql = SchemaMigrator.GenerateMigrationSql(SqlServerDialect.Instance, diffs, [schema]);

		sql.ContainsIgnoreCase("DECIMAL(18,6)").AssertTrue(sql);
	}

	[TestMethod]
	public void Compare_DecimalPrecisionWithZeroScale_AgainstAScaledColumn_Detected()
	{
		var schema = new Schema
		{
			TableName = "TestTable",
			EntityType = typeof(ColAttrTestEntity),
			Columns =
			[
				new SchemaColumn { Name = "Qty", ClrType = typeof(decimal), Precision = 18 },
			],
			Factory = () => new ColAttrTestEntity(),
		};

		var diffs = SchemaMigrator.Compare([schema], [new DbColumnInfo("TestTable", "Qty", "decimal", false, null, 18, 8)], SqlServerDialect.Instance, false);

		diffs.Any(d => d.ColumnName == "Qty" && d.Kind == SchemaDiffKind.PrecisionMismatch).AssertTrue(
			"DECIMAL(18) is DECIMAL(18,0), which a DECIMAL(18,8) column is not");
	}

	[TestMethod]
	public void Compare_DecimalDigits_AgainstUnconstrainedNumeric_Detected()
	{
		var schema = new Schema
		{
			TableName = "TestTable",
			EntityType = typeof(ColAttrTestEntity),
			Columns =
			[
				new SchemaColumn { Name = "Amount", ClrType = typeof(decimal), Precision = 18, Scale = 6 },
			],
			Factory = () => new ColAttrTestEntity(),
		};

		var diffs = SchemaMigrator.Compare([schema], [new DbColumnInfo("TestTable", "Amount", "numeric", false, null, null, null)], PostgreSqlDialect.Instance, false);

		diffs.Any(d => d.ColumnName == "Amount" && d.Kind == SchemaDiffKind.PrecisionMismatch).AssertTrue(
			"A numeric column without precision holds any digits, not the declared (18,6)");
	}

	[TestMethod]
	public void Compare_DecimalDigits_OnSQLite_AreNotCompared()
	{
		var schema = new Schema
		{
			TableName = "TestTable",
			EntityType = typeof(ColAttrTestEntity),
			Columns =
			[
				new SchemaColumn { Name = "Amount", ClrType = typeof(decimal), Precision = 18, Scale = 6 },
			],
			Factory = () => new ColAttrTestEntity(),
		};

		var diffs = SchemaMigrator.Compare([schema], [new DbColumnInfo("TestTable", "Amount", "TEXT", false, null, null, null)], SQLiteDialect.Instance, false);

		diffs.Count.AssertEqual(0);
	}

	[TestMethod]
	public void Compare_MissingColumn_ExpectedCarriesTheDeclaredDigits()
	{
		var schema = new Schema
		{
			TableName = "TestTable",
			EntityType = typeof(ColAttrTestEntity),
			Columns =
			[
				new SchemaColumn { Name = "Other", ClrType = typeof(int) },
				new SchemaColumn { Name = "Balance", ClrType = typeof(decimal), Precision = 18, Scale = 6 },
			],
			Factory = () => new ColAttrTestEntity(),
		};

		var diffs = SchemaMigrator.Compare([schema], [new DbColumnInfo("TestTable", "Other", "int", false, null, 10, 0)], SqlServerDialect.Instance, false);

		diffs.Single(d => d.Kind == SchemaDiffKind.MissingColumn).Expected.AssertEqual("DECIMAL(18,6) NOT NULL");
	}

	[TestMethod]
	[DataRow(typeof(DateTime), "SET DATA TYPE TIMESTAMPTZ(3);")]
	[DataRow(typeof(TimeOnly), "SET DATA TYPE TIME(3);")]
	public void PostgreSqlDialect_AppendAlterColumn_KeepsTheTimePrecision(Type clrType, string expected)
	{
		var sb = new StringBuilder();
		PostgreSqlDialect.Instance.AppendAlterColumn(sb, "T", "Created", clrType, isNullable: false, maxLength: 0, precision: 3, scale: 0, live: null);

		sb.ToString().ContainsIgnoreCase(expected).AssertTrue(sb.ToString());
	}

	private static string MigrateColumn(ISqlDialect dialect, SchemaColumn column, DbColumnInfo live)
	{
		var schema = new Schema
		{
			TableName = live.TableName,
			EntityType = typeof(ColAttrTestEntity),
			Columns = [column],
			Factory = () => new ColAttrTestEntity(),
		};

		var diffs = SchemaMigrator.Compare([schema], [live], dialect, false);
		return SchemaMigrator.GenerateMigrationSql(dialect, diffs, [schema]);
	}

	[TestMethod]
	public void GenerateSql_NullabilityOnly_KeepsTheLiveVarchar()
	{
		var sql = MigrateColumn(SqlServerDialect.Instance,
			new SchemaColumn { Name = "Email", ClrType = typeof(string), MaxLength = 256 },
			new DbColumnInfo("Users", "Email", "varchar", true, 256, null, null));

		sql.ContainsIgnoreCase("ALTER COLUMN [Email] VARCHAR(256) NOT NULL").AssertTrue(sql);
		sql.ContainsIgnoreCase("NVARCHAR").AssertFalse(sql);
	}

	[TestMethod]
	public void GenerateSql_NullabilityOnly_KeepsTheLiveDigitsOfAnUndeclaredDecimal()
	{
		var sql = MigrateColumn(SqlServerDialect.Instance,
			new SchemaColumn { Name = "Commission", ClrType = typeof(decimal?), IsNullable = true },
			new DbColumnInfo("Trades", "Commission", "decimal", false, null, 28, 10));

		sql.ContainsIgnoreCase("ALTER COLUMN [Commission] DECIMAL(28,10) NULL").AssertTrue(sql);
	}

	[TestMethod]
	public void GenerateSql_NullabilityOnly_KeepsTheLiveDatetime()
	{
		var sql = MigrateColumn(SqlServerDialect.Instance,
			new SchemaColumn { Name = "Created", ClrType = typeof(DateTime?), IsNullable = true },
			new DbColumnInfo("Users", "Created", "datetime", false, null, 3, null));

		sql.ContainsIgnoreCase("ALTER COLUMN [Created] DATETIME NULL").AssertTrue(sql);
	}

	[TestMethod]
	public void GenerateSql_NullabilityOnly_PostgreSql_ChangesOnlyTheNullability()
	{
		var sql = MigrateColumn(PostgreSqlDialect.Instance,
			new SchemaColumn { Name = "Amount", ClrType = typeof(decimal), Precision = 18, Scale = 6 },
			new DbColumnInfo("T", "Amount", "numeric", true, null, 18, 6));

		sql.ContainsIgnoreCase("SET DATA TYPE").AssertFalse(sql);
		sql.ContainsIgnoreCase("ALTER COLUMN \"Amount\" SET NOT NULL").AssertTrue(sql);
	}

	[TestMethod]
	public void GenerateSql_LengthOnly_KeepsTheLiveVarchar()
	{
		var sql = MigrateColumn(SqlServerDialect.Instance,
			new SchemaColumn { Name = "Email", ClrType = typeof(string), MaxLength = 512 },
			new DbColumnInfo("Users", "Email", "varchar", false, 256, null, null));

		sql.ContainsIgnoreCase("ALTER COLUMN [Email] VARCHAR(512) NOT NULL").AssertTrue(sql);
	}

	[TestMethod]
	public void GenerateSql_SeveralDiffsOnOneColumn_AlterItOnce()
	{
		var sql = MigrateColumn(SqlServerDialect.Instance,
			new SchemaColumn { Name = "Amount", ClrType = typeof(decimal), Precision = 18, Scale = 6 },
			new DbColumnInfo("T", "Amount", "decimal", true, null, 18, 2));

		sql.Split("ALTER COLUMN").Length.AssertEqual(2, sql);
		sql.ContainsIgnoreCase("DECIMAL(18,6) NOT NULL").AssertTrue(sql);
	}

	[TestMethod]
	[DataRow("SqlServer")]
	[DataRow("PostgreSql")]
	public void GenerateSql_FewerDecimalPlaces_IsCalledOut(string dialectName)
	{
		var sql = MigrateColumn(GetDialect(dialectName),
			new SchemaColumn { Name = "Amount", ClrType = typeof(decimal), Precision = 18, Scale = 6 },
			new DbColumnInfo("T", "Amount", "decimal", false, null, 18, 8));

		sql.Split('\n').Any(l => l.TrimStart().StartsWith("--") && l.Contains("Amount") && l.ContainsIgnoreCase("round")).AssertTrue(sql);
	}

	[TestMethod]
	public void GenerateSql_FewerIntegerDigits_IsCalledOut()
	{
		var sql = MigrateColumn(SqlServerDialect.Instance,
			new SchemaColumn { Name = "Amount", ClrType = typeof(decimal), Precision = 18, Scale = 6 },
			new DbColumnInfo("T", "Amount", "decimal", false, null, 20, 6));

		sql.Split('\n').Any(l => l.TrimStart().StartsWith("--") && l.Contains("Amount") && l.Contains("12")).AssertTrue(sql);
	}

	[TestMethod]
	public void GenerateSql_WiderDecimal_IsNotCalledOut()
	{
		var sql = MigrateColumn(SqlServerDialect.Instance,
			new SchemaColumn { Name = "Amount", ClrType = typeof(decimal), Precision = 22, Scale = 6 },
			new DbColumnInfo("T", "Amount", "decimal", false, null, 18, 2));

		sql.Contains("--").AssertFalse(sql);
	}

	private static string MigrateIndexedColumn(ISqlDialect dialect, IReadOnlyList<DbIndexInfo> indexes, IReadOnlyList<DbForeignKeyInfo> foreignKeys)
	{
		var schema = new Schema
		{
			TableName = "T",
			EntityType = typeof(ColAttrTestEntity),
			Columns =
			[
				new SchemaColumn { Name = "Amount", ClrType = typeof(decimal), Precision = 18, Scale = 6, Indexes = [new(null, 0)] },
			],
			Factory = () => new ColAttrTestEntity(),
		};

		var live = new DbColumnInfo("T", "Amount", "decimal", false, null, 18, 2);
		var diffs = SchemaMigrator.Compare([schema], [live], dialect, false, foreignKeys, indexes);

		return SchemaMigrator.GenerateMigrationSql(dialect, diffs, [schema]);
	}

	[TestMethod]
	public void GenerateSql_AlteringAColumnAnUndeclaredIndexCovers_SqlServer_SaysSo()
	{
		var sql = MigrateIndexedColumn(SqlServerDialect.Instance,
			[
				new DbIndexInfo("IX_T_Amount", "T", "Amount", 1, false, false),
				new DbIndexInfo("IX_Hand_Made", "T", "Other", 1, false, false),
				new DbIndexInfo("IX_Hand_Made", "T", "Amount", 2, false, false),
			],
			[]);

		sql.Split('\n').Any(l => l.StartsWith("-- T.Amount:") && l.Contains("IX_Hand_Made")).AssertTrue(sql);
		sql.Split('\n').Any(l => !l.TrimStart().StartsWith("--") && l.ContainsIgnoreCase("DROP INDEX")).AssertFalse(sql);
	}

	[TestMethod]
	public void GenerateSql_AlteringAForeignKeyColumn_SqlServer_SaysSo()
	{
		var sql = MigrateIndexedColumn(SqlServerDialect.Instance,
			[new DbIndexInfo("IX_T_Amount", "T", "Amount", 1, false, false)],
			[new DbForeignKeyInfo("FK_Other_Amount", "Other", "AmountRef", "T", "Amount")]);

		sql.Split('\n').Any(l => l.StartsWith("-- T.Amount:") && l.Contains("FK_Other_Amount")).AssertTrue(sql);
	}

	[TestMethod]
	public void GenerateSql_AlteringAnIndexedColumn_PostgreSql_LeavesTheIndex()
	{
		var sql = MigrateIndexedColumn(PostgreSqlDialect.Instance,
			[new DbIndexInfo("IX_T_Amount", "T", "Amount", 1, false, false)],
			[]);

		sql.ContainsIgnoreCase("DROP INDEX").AssertFalse(sql);
		sql.ContainsIgnoreCase("CREATE").AssertFalse(sql);
	}

	[TestMethod]
	public void GenerateSql_ColumnChange_ComesBeforeAMissingIndexOnIt()
	{
		var schema = new Schema
		{
			TableName = "T",
			EntityType = typeof(ColAttrTestEntity),
			Columns =
			[
				new SchemaColumn { Name = "Amount", ClrType = typeof(decimal), Precision = 18, Scale = 6, Indexes = [new(null, 0)] },
			],
			Factory = () => new ColAttrTestEntity(),
		};

		var live = new DbColumnInfo("T", "Amount", "decimal", false, null, 18, 2);
		var diffs = SchemaMigrator.Compare([schema], [live], SqlServerDialect.Instance, false, [], []);

		var sql = SchemaMigrator.GenerateMigrationSql(SqlServerDialect.Instance, [.. diffs.Reverse()], [schema]);

		(sql.IndexOf("ALTER COLUMN", StringComparison.Ordinal) < sql.IndexOf("CREATE INDEX", StringComparison.Ordinal)).AssertTrue(sql);
	}

	[TestMethod]
	public void GenerateSql_TimestampToTimestamptz_ReadsTheStoredValuesAsUtc()
	{
		var sql = MigrateColumn(PostgreSqlDialect.Instance,
			new SchemaColumn { Name = "Created", ClrType = typeof(DateTime) },
			new DbColumnInfo("T", "Created", "timestamp without time zone", false, null, 6, null));

		sql.ContainsIgnoreCase("SET DATA TYPE TIMESTAMPTZ USING \"Created\" AT TIME ZONE 'UTC'").AssertTrue(sql);
	}

	[TestMethod]
	[DataRow("SqlServer", "nvarchar")]
	[DataRow("PostgreSql", "character varying")]
	public void GenerateSql_NewRequiredReference_IsNotBackfilledWithADefault(string dialectName, string textType)
	{
		var dialect = GetDialect(dialectName);

		var schema = new Schema
		{
			TableName = "Child",
			EntityType = typeof(ColAttrTestEntity),
			Columns =
			[
				new SchemaColumn { Name = "Name", ClrType = typeof(string), MaxLength = 64 },
				new SchemaColumn { Name = "ParentId", ClrType = typeof(long), ReferencedEntityType = typeof(ColAttrTestEntity) },
			],
			Factory = () => new ColAttrTestEntity(),
		};

		var diffs = SchemaMigrator.Compare([schema], [new DbColumnInfo("Child", "Name", textType, false, 64, null, null)], dialect, false);
		var sql = SchemaMigrator.GenerateMigrationSql(dialect, diffs, [schema]);
		var runnable = sql.Split('\n').Where(l => !l.TrimStart().StartsWith("--")).JoinN();

		runnable.ContainsIgnoreCase("UPDATE").AssertFalse(sql);
		runnable.ContainsIgnoreCase("NOT NULL").AssertFalse(sql);
		runnable.ContainsIgnoreCase("FOREIGN KEY").AssertTrue(sql);
		sql.Split('\n').Any(l => l.StartsWith("-- Child.ParentId") && l.ContainsIgnoreCase("NOT NULL")).AssertTrue(sql);
	}

	private static Schema CreateSQLiteSchema(params SchemaColumn[] columns)
		=> new()
		{
			TableName = "T",
			EntityType = typeof(ColAttrTestEntity),
			Columns = [new SchemaColumn { Name = "Name", ClrType = typeof(string) }, .. columns],
			Factory = () => new ColAttrTestEntity(),
		};

	[TestMethod]
	public void GenerateSql_SQLite_NewRequiredColumn_IsAddedWithADefault()
	{
		var schema = CreateSQLiteSchema(new SchemaColumn { Name = "Fee", ClrType = typeof(decimal) });

		var diffs = SchemaMigrator.Compare([schema], [new DbColumnInfo("T", "Name", "TEXT", false, null, null, null)], SQLiteDialect.Instance, false);
		var sql = SchemaMigrator.GenerateMigrationSql(SQLiteDialect.Instance, diffs, [schema]);

		sql.Contains("ALTER TABLE \"T\" ADD \"Fee\" TEXT NOT NULL DEFAULT 0;").AssertTrue(sql);
	}

	[TestMethod]
	public void GenerateSql_SQLite_NewReference_IsAddedWithItsForeignKeyInline()
	{
		var schema = CreateSQLiteSchema(new SchemaColumn { Name = "ParentId", ClrType = typeof(long?), IsNullable = true, ReferencedEntityType = typeof(ColAttrTestEntity) });

		var diffs = SchemaMigrator.Compare([schema], [new DbColumnInfo("T", "Name", "TEXT", false, null, null, null)], SQLiteDialect.Instance, false);
		var sql = SchemaMigrator.GenerateMigrationSql(SQLiteDialect.Instance, diffs, [schema]);

		sql.Contains("ADD \"ParentId\" INTEGER NULL CONSTRAINT").AssertTrue(sql);
		sql.Contains("REFERENCES \"Ecng_ColAttrTest\" (\"Id\")").AssertTrue(sql);
	}

	[TestMethod]
	public void GenerateSql_SQLite_ColumnChange_IsLeftAsANote()
	{
		var schema = CreateSQLiteSchema(new SchemaColumn { Name = "Qty", ClrType = typeof(int) });

		var diffs = SchemaMigrator.Compare([schema],
			[
				new DbColumnInfo("T", "Name", "TEXT", false, null, null, null),
				new DbColumnInfo("T", "Qty", "TEXT", true, null, null, null),
			], SQLiteDialect.Instance, false);

		var sql = SchemaMigrator.GenerateMigrationSql(SQLiteDialect.Instance, diffs, [schema]);

		sql.Split('\n').Any(l => l.StartsWith("-- T.Qty")).AssertTrue(sql);
		sql.Split('\n').Any(l => !l.TrimStart().StartsWith("--") && l.ContainsIgnoreCase("Qty")).AssertFalse(sql);
	}

	[TestMethod]
	[DataRow(typeof(string), 5000, "nvarchar")]
	[DataRow(typeof(byte[]), 9000, "varbinary")]
	public void Compare_LengthTheDialectStoresAsMax_MatchesAMaxColumn(Type clrType, int maxLength, string dataType)
	{
		var schema = new Schema
		{
			TableName = "T",
			EntityType = typeof(ColAttrTestEntity),
			Columns = [new SchemaColumn { Name = "Body", ClrType = clrType, MaxLength = maxLength }],
			Factory = () => new ColAttrTestEntity(),
		};

		var diffs = SchemaMigrator.Compare([schema], [new DbColumnInfo("T", "Body", dataType, false, -1, null, null)], SqlServerDialect.Instance, false);

		diffs.Count.AssertEqual(0, diffs.Select(d => $"{d.Kind} {d.Expected} vs {d.Actual}").JoinCommaSpace());
	}

	[TestMethod]
	public void Compare_LengthWithinTheDialectLimit_StillDiffersFromMax()
	{
		var schema = new Schema
		{
			TableName = "T",
			EntityType = typeof(ColAttrTestEntity),
			Columns = [new SchemaColumn { Name = "Body", ClrType = typeof(string), MaxLength = 4000 }],
			Factory = () => new ColAttrTestEntity(),
		};

		var diffs = SchemaMigrator.Compare([schema], [new DbColumnInfo("T", "Body", "nvarchar", false, -1, null, null)], SqlServerDialect.Instance, false);

		diffs.Single().Kind.AssertEqual(SchemaDiffKind.MaxLengthMismatch);
	}

	[TestMethod]
	[DataRow("SqlServer", "-- DROP INDEX [IX_T_Old] ON [T];")]
	[DataRow("PostgreSql", "-- DROP INDEX \"IX_T_Old\";")]
	[DataRow("SQLite", "-- DROP INDEX \"IX_T_Old\";")]
	public void GenerateSql_ExtraIndex_CommentsOutADropTheDialectRuns(string dialectName, string expected)
	{
		var schema = new Schema
		{
			TableName = "T",
			EntityType = typeof(ColAttrTestEntity),
			Columns = [new SchemaColumn { Name = "Old", ClrType = typeof(int) }],
			Factory = () => new ColAttrTestEntity(),
		};

		var sql = SchemaMigrator.GenerateMigrationSql(GetDialect(dialectName), [new SchemaDiff("T", "IX_T_Old", SchemaDiffKind.ExtraIndex, "", "(Old)")], [schema]);

		sql.Contains(expected).AssertTrue(sql);
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
		sql.Contains("ADD").AssertTrue($"Expected ADD in SQL: {sql}");
		// NOT NULL column → 3-step: ADD NULL, UPDATE default, ALTER NOT NULL
		sql.Contains("NVARCHAR(256) NULL").AssertTrue($"Expected ADD as NULL first: {sql}");
		sql.Contains("NVARCHAR(256) NOT NULL").AssertTrue($"Expected NOT NULL in ALTER: {sql}");
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

		// Extra columns are never auto-dropped: the DROP COLUMN is emitted commented out,
		// ready for a human to review and uncomment.
		sql.Contains("DROP COLUMN").AssertTrue($"Expected a DROP COLUMN for the extra column: {sql}");
		sql.Contains("[OldCol]").AssertTrue($"Expected the extra column name: {sql}");
		sql.Split('\n').Where(l => l.Contains("DROP COLUMN")).All(l => l.TrimStart().StartsWith("--"))
			.AssertTrue($"DROP COLUMN for an extra column must be commented out: {sql}");
	}

	[TestMethod]
	public void GenerateSql_ExtraColumn_SQLite_FallsBackToComment()
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

		// SQLite refuses DROP COLUMN (version-dependent). The migrator must not throw and
		// must keep an informational comment instead of an executable statement.
		var sql = SchemaMigrator.GenerateMigrationSql(SQLiteDialect.Instance, diffs, [schema]);

		sql.Contains("OldCol").AssertTrue($"Expected the extra column noted: {sql}");
		sql.Split('\n').Where(l => l.Contains("DROP COLUMN")).All(l => l.TrimStart().StartsWith("--"))
			.AssertTrue($"SQLite must not emit an executable DROP COLUMN: {sql}");
	}

	[TestMethod]
	public void Compare_ExtraTable_DetectedOnlyWhenOptedIn()
	{
		var schema = new Schema
		{
			TableName = "Users",
			EntityType = typeof(ColAttrTestEntity),
			Identity = new SchemaColumn { Name = "Id", ClrType = typeof(long), IsReadOnly = true },
			Columns = [new SchemaColumn { Name = "Name", ClrType = typeof(string), IsNullable = true }],
			Factory = () => new ColAttrTestEntity(),
		};

		var dbCols = new[]
		{
			new DbColumnInfo("Users", "Id", "BIGINT", false, null, null, null),
			new DbColumnInfo("Users", "Name", "NVARCHAR", true, -1, null, null),
			// A table present in the DB that no entity maps to.
			new DbColumnInfo("LegacyAudit", "Id", "BIGINT", false, null, null, null),
			new DbColumnInfo("LegacyAudit", "Note", "NVARCHAR", true, -1, null, null),
		};

		// Default: extra tables are NOT tracked — a partial model must not flag unrelated tables.
		SchemaMigrator.Compare([schema], dbCols, SqlServerDialect.Instance, false)
			.Any(d => d.Kind == SchemaDiffKind.ExtraTable).AssertFalse("extra tables must be off by default");

		// Opt-in: the DB-only table surfaces as exactly one ExtraTable diff.
		var diffs = SchemaMigrator.Compare([schema], dbCols, SqlServerDialect.Instance, false, detectExtraTables: true);
		var extra = diffs.Where(d => d.Kind == SchemaDiffKind.ExtraTable).ToArray();
		extra.Length.AssertEqual(1);
		extra[0].TableName.AssertEqual("LegacyAudit");

		// GenerateMigrationSql emits it as a commented-out DROP TABLE (never auto-dropped).
		var sql = SchemaMigrator.GenerateMigrationSql(SqlServerDialect.Instance, diffs, [schema]);
		sql.Contains("DROP TABLE").AssertTrue($"Expected DROP TABLE for the extra table: {sql}");
		sql.Contains("[LegacyAudit]").AssertTrue($"Expected the extra table name: {sql}");
		sql.Split('\n').Where(l => l.Contains("DROP TABLE")).All(l => l.TrimStart().StartsWith("--"))
			.AssertTrue($"DROP TABLE for an extra table must be commented out: {sql}");
	}

	[TestMethod]
	public void Compare_ExtraForeignKey_EmitsCommentedDropConstraint()
	{
		var schema = new Schema
		{
			TableName = "Users",
			EntityType = typeof(ColAttrTestEntity),
			Identity = new SchemaColumn { Name = "Id", ClrType = typeof(long), IsReadOnly = true },
			Columns = [new SchemaColumn { Name = "Name", ClrType = typeof(string), IsNullable = true }],
			Factory = () => new ColAttrTestEntity(),
		};

		var dbCols = new[]
		{
			new DbColumnInfo("Users", "Id", "BIGINT", false, null, null, null),
			new DbColumnInfo("Users", "Name", "NVARCHAR", true, -1, null, null),
		};

		// A foreign key present in the DB on a column the model does not declare as a relation.
		var dbFks = new[]
		{
			new DbForeignKeyInfo("FK_Users_Name", "Users", "Name", "Users", "Id"),
		};

		var diffs = SchemaMigrator.Compare([schema], dbCols, SqlServerDialect.Instance, false, dbFks);
		diffs.Count(d => d.Kind == SchemaDiffKind.ExtraForeignKey).AssertEqual(1);

		// Rendered as a commented-out DROP CONSTRAINT (never auto-dropped).
		var sql = SchemaMigrator.GenerateMigrationSql(SqlServerDialect.Instance, diffs, [schema]);
		sql.Contains("DROP CONSTRAINT").AssertTrue($"Expected DROP CONSTRAINT for the extra FK: {sql}");
		sql.Contains("[FK_Users_Name]").AssertTrue($"Expected the FK constraint name: {sql}");
		sql.Split('\n').Where(l => l.Contains("DROP CONSTRAINT")).All(l => l.TrimStart().StartsWith("--"))
			.AssertTrue($"DROP CONSTRAINT for an extra FK must be commented out: {sql}");
	}

	[TestMethod]
	public void Compare_ExtraIndex_EmitsCommentedDropIndex()
	{
		var schema = new Schema
		{
			TableName = "Users",
			EntityType = typeof(ColAttrTestEntity),
			Identity = new SchemaColumn { Name = "Id", ClrType = typeof(long), IsReadOnly = true },
			Columns = [new SchemaColumn { Name = "Name", ClrType = typeof(string), IsNullable = true }],
			Factory = () => new ColAttrTestEntity(),
		};

		var dbCols = new[]
		{
			new DbColumnInfo("Users", "Id", "BIGINT", false, null, null, null),
			new DbColumnInfo("Users", "Name", "NVARCHAR", true, -1, null, null),
		};

		// An index present in the DB that the model does not declare.
		var dbIndexes = new[]
		{
			new DbIndexInfo("IX_Users_Name", "Users", "Name", 1, false, false),
		};

		var diffs = SchemaMigrator.Compare([schema], dbCols, SqlServerDialect.Instance, false, null, dbIndexes);
		diffs.Count(d => d.Kind == SchemaDiffKind.ExtraIndex).AssertEqual(1);

		// Rendered as a commented-out DROP INDEX (never auto-dropped).
		var sql = SchemaMigrator.GenerateMigrationSql(SqlServerDialect.Instance, diffs, [schema]);
		sql.Contains("DROP INDEX").AssertTrue($"Expected DROP INDEX for the extra index: {sql}");
		sql.Contains("[IX_Users_Name]").AssertTrue($"Expected the index name: {sql}");
		sql.Split('\n').Where(l => l.Contains("DROP INDEX")).All(l => l.TrimStart().StartsWith("--"))
			.AssertTrue($"DROP INDEX for an extra index must be commented out: {sql}");
	}

	[TestMethod]
	public void Compare_UniqueIndex_MatchedByColumnsNotName_NotMissingNotExtra()
	{
		// Model declares a unique index on Code (legacy [Unique] -> generated name
		// IX_Users_Code). The DB has the same single-column unique index but under a
		// hand-chosen name (UX_Users_Code). The index is the SAME index — matching must
		// be by column set + uniqueness, not by name, so neither Missing nor Extra fires.
		var schema = new Schema
		{
			TableName = "Users",
			EntityType = typeof(ColAttrTestEntity),
			Identity = new SchemaColumn { Name = "Id", ClrType = typeof(long), IsReadOnly = true },
			Columns = [new SchemaColumn { Name = "Code", ClrType = typeof(string), MaxLength = 64, IsUnique = true }],
			Factory = () => new ColAttrTestEntity(),
		};

		var dbCols = new[]
		{
			new DbColumnInfo("Users", "Id", "BIGINT", false, null, null, null),
			new DbColumnInfo("Users", "Code", "NVARCHAR", false, 64, null, null),
		};

		var dbIndexes = new[]
		{
			new DbIndexInfo("UX_Users_Code", "Users", "Code", 1, true, false),
		};

		var diffs = SchemaMigrator.Compare([schema], dbCols, SqlServerDialect.Instance, false, null, dbIndexes);
		diffs.Count(d => d.Kind == SchemaDiffKind.MissingIndex).AssertEqual(0);
		diffs.Count(d => d.Kind == SchemaDiffKind.ExtraIndex).AssertEqual(0);
	}

	[TestMethod]
	public void Compare_CompositeIndex_MatchedByColumnsNotName_NotMissingNotExtra()
	{
		// A composite index declared in the model under one name (IX_Composite over
		// A, B) matches a DB index over the same ordered columns under a different name
		// (IX_DbHandName). Shape matching reconciles them regardless of the name.
		var schema = new Schema
		{
			TableName = "Users",
			EntityType = typeof(ColAttrTestEntity),
			Identity = new SchemaColumn { Name = "Id", ClrType = typeof(long), IsReadOnly = true },
			Columns =
			[
				new SchemaColumn { Name = "A", ClrType = typeof(long), Indexes = [new SchemaColumnIndex("IX_Composite", 0)] },
				new SchemaColumn { Name = "B", ClrType = typeof(long), Indexes = [new SchemaColumnIndex("IX_Composite", 1)] },
			],
			Factory = () => new ColAttrTestEntity(),
		};

		var dbCols = new[]
		{
			new DbColumnInfo("Users", "Id", "BIGINT", false, null, null, null),
			new DbColumnInfo("Users", "A", "BIGINT", false, null, null, null),
			new DbColumnInfo("Users", "B", "BIGINT", false, null, null, null),
		};

		var dbIndexes = new[]
		{
			new DbIndexInfo("IX_DbHandName", "Users", "A", 1, false, false),
			new DbIndexInfo("IX_DbHandName", "Users", "B", 2, false, false),
		};

		var diffs = SchemaMigrator.Compare([schema], dbCols, SqlServerDialect.Instance, false, null, dbIndexes);
		diffs.Count(d => d.Kind == SchemaDiffKind.MissingIndex).AssertEqual(0);
		diffs.Count(d => d.Kind == SchemaDiffKind.ExtraIndex).AssertEqual(0);
	}

	[TestMethod]
	public void Compare_Index_UniquenessDiffers_IsBothMissingAndExtra()
	{
		// Uniqueness is part of the index shape: a unique index in the model and a
		// non-unique index in the DB on the same column are NOT the same index. The
		// model's unique index is missing, and the DB's non-unique one is extra.
		var schema = new Schema
		{
			TableName = "Users",
			EntityType = typeof(ColAttrTestEntity),
			Identity = new SchemaColumn { Name = "Id", ClrType = typeof(long), IsReadOnly = true },
			Columns = [new SchemaColumn { Name = "Code", ClrType = typeof(string), MaxLength = 64, IsUnique = true }],
			Factory = () => new ColAttrTestEntity(),
		};

		var dbCols = new[]
		{
			new DbColumnInfo("Users", "Id", "BIGINT", false, null, null, null),
			new DbColumnInfo("Users", "Code", "NVARCHAR", false, 64, null, null),
		};

		var dbIndexes = new[]
		{
			new DbIndexInfo("IX_Users_Code", "Users", "Code", 1, false, false),
		};

		var diffs = SchemaMigrator.Compare([schema], dbCols, SqlServerDialect.Instance, false, null, dbIndexes);
		diffs.Count(d => d.Kind == SchemaDiffKind.MissingIndex).AssertEqual(1);
		diffs.Count(d => d.Kind == SchemaDiffKind.ExtraIndex).AssertEqual(1);
	}

	[TestMethod]
	public void TypeLevelIndex_CompositeOverInheritedColumn_AddsParticipationsToBothColumns()
	{
		// A type-level [Index(FieldName=...)] composite must attach its participation to
		// the named column even when that column is inherited from a base class (Flag).
		var schema = SchemaRegistry.Get(typeof(TypeIndexEntity));

		var code = schema.Columns.First(c => c.Name == "Code");
		var flag = schema.Columns.First(c => c.Name == "Flag");

		code.Indexes.Any(i => i.Name == "IX_TypeIndex_CodeFlag" && i.Order == 0)
			.AssertTrue("Code should carry the type-level composite participation at order 0.");
		flag.Indexes.Any(i => i.Name == "IX_TypeIndex_CodeFlag" && i.Order == 1)
			.AssertTrue("Inherited Flag should carry the type-level composite participation at order 1.");
	}

	[TestMethod]
	public void TypeLevelIndex_CompositeReconcilesWithDbIndexByShape()
	{
		// The type-level composite (Code, Flag) declared on the entity must reconcile with
		// a DB index over the same ordered columns under a different name — proving the
		// type-level declaration flows through to shape-based index matching end to end.
		var schema = SchemaRegistry.Get(typeof(TypeIndexEntity));

		var dbCols = new[]
		{
			new DbColumnInfo("TypeIndexEntity", "Id", "BIGINT", false, null, null, null),
			new DbColumnInfo("TypeIndexEntity", "Code", "NVARCHAR", true, 64, null, null),
			new DbColumnInfo("TypeIndexEntity", "Flag", "BIT", false, null, null, null),
		};

		var dbIndexes = new[]
		{
			new DbIndexInfo("IX_DbHandName_CodeFlag", "TypeIndexEntity", "Code", 1, false, false),
			new DbIndexInfo("IX_DbHandName_CodeFlag", "TypeIndexEntity", "Flag", 2, false, false),
		};

		var diffs = SchemaMigrator.Compare([schema], dbCols, SqlServerDialect.Instance, false, null, dbIndexes);
		diffs.Count(d => d.Kind == SchemaDiffKind.MissingIndex).AssertEqual(0);
		diffs.Count(d => d.Kind == SchemaDiffKind.ExtraIndex).AssertEqual(0);
	}

	[TestMethod]
	public void PerIndexUniqueness_SingleParticipationIsNonUnique_CompositeIsUnique()
	{
		var schema = SchemaRegistry.Get(typeof(PerIndexUniqueEntity));
		var a = schema.Columns.First(c => c.Name == "A");

		// A carries two participations: its own single index (non-unique) and the
		// composite UX_PerIndex_AB (unique). Uniqueness lives on the participation.
		var single = a.Indexes.First(i => i.Name is null);
		var composite = a.Indexes.First(i => i.Name == "UX_PerIndex_AB");

		single.IsUnique.AssertFalse("Own single [Index] on A must stay non-unique.");
		composite.IsUnique.AssertTrue("The [Unique] composite participation must be unique.");
	}

	[TestMethod]
	public void Compare_ColumnInUniqueCompositeAndNonUniqueSingle_BothReconcile()
	{
		// The single non-unique index on A and the unique composite (A, B) must each
		// reconcile with their DB counterparts — the single must NOT be treated as unique
		// just because A also belongs to a unique composite.
		var schema = SchemaRegistry.Get(typeof(PerIndexUniqueEntity));

		var dbCols = new[]
		{
			new DbColumnInfo("PerIndexUniqueEntity", "Id", "BIGINT", false, null, null, null),
			new DbColumnInfo("PerIndexUniqueEntity", "A", "BIGINT", false, null, null, null),
			new DbColumnInfo("PerIndexUniqueEntity", "B", "BIGINT", false, null, null, null),
		};

		var dbIndexes = new[]
		{
			new DbIndexInfo("IX_db_A", "PerIndexUniqueEntity", "A", 1, false, false),
			new DbIndexInfo("UX_db_AB", "PerIndexUniqueEntity", "A", 1, true, false),
			new DbIndexInfo("UX_db_AB", "PerIndexUniqueEntity", "B", 2, true, false),
		};

		var diffs = SchemaMigrator.Compare([schema], dbCols, SqlServerDialect.Instance, false, null, dbIndexes);
		diffs.Count(d => d.Kind == SchemaDiffKind.MissingIndex).AssertEqual(0);
		diffs.Count(d => d.Kind == SchemaDiffKind.ExtraIndex).AssertEqual(0);
	}

	[TestMethod]
	public void TypeLevelIndex_ColumnListForm_GroupsColumnsIntoOneAutoNamedComposite()
	{
		// [Index(nameof(A), nameof(B))] must produce ONE composite: both columns share a single
		// auto-generated name and are ordered by argument position — no Name/Order in the source.
		var schema = SchemaRegistry.Get(typeof(ColumnListIndexEntity));

		var a = schema.Columns.First(c => c.Name == "A").Indexes.Single();
		var b = schema.Columns.First(c => c.Name == "B").Indexes.Single();

		a.Name.AssertEqual("IX_ColumnListIndexEntity_A_B");
		b.Name.AssertEqual("IX_ColumnListIndexEntity_A_B");
		a.Order.AssertEqual(0);
		b.Order.AssertEqual(1);
	}

	[TestMethod]
	public void Compare_ColumnListComposite_ReconcilesWithDbByShape()
	{
		// The column-list composite reconciles with a DB index over the same ordered columns
		// under any name — proving auto-named composites flow through shape matching.
		var schema = SchemaRegistry.Get(typeof(ColumnListIndexEntity));

		var dbCols = new[]
		{
			new DbColumnInfo("ColumnListIndexEntity", "Id", "BIGINT", false, null, null, null),
			new DbColumnInfo("ColumnListIndexEntity", "A", "BIGINT", false, null, null, null),
			new DbColumnInfo("ColumnListIndexEntity", "B", "BIGINT", false, null, null, null),
		};

		var dbIndexes = new[]
		{
			new DbIndexInfo("IX_some_db_name", "ColumnListIndexEntity", "A", 1, false, false),
			new DbIndexInfo("IX_some_db_name", "ColumnListIndexEntity", "B", 2, false, false),
		};

		var diffs = SchemaMigrator.Compare([schema], dbCols, SqlServerDialect.Instance, false, null, dbIndexes);
		diffs.Count(d => d.Kind == SchemaDiffKind.MissingIndex).AssertEqual(0);
		diffs.Count(d => d.Kind == SchemaDiffKind.ExtraIndex).AssertEqual(0);
	}

	[TestMethod]
	public void ForeignKeyAttribute_SetsReferencedEntityTypeOnPlainIdColumn()
	{
		var schema = SchemaRegistry.Get(typeof(ColAttrTestEntity));
		var col = schema.Columns.First(c => c.Name == "ParentId");

		// [ForeignKey(typeof(X))] declares the FK target for schema comparison...
		col.ReferencedEntityType.AssertEqual(typeof(ColAttrTestEntity));
		// ...while the column stays a plain scalar id (no navigation type swap).
		col.ClrType.AssertEqual(typeof(long));
	}

	[TestMethod]
	public void ForeignKeyAttribute_KnownFk_NotExtra_AndMissingWhenAbsent()
	{
		var schema = SchemaRegistry.Get(typeof(ColAttrTestEntity));
		var table = schema.TableName;

		var dbCols = new[]
		{
			new DbColumnInfo(table, "Id", "BIGINT", false, null, null, null),
			new DbColumnInfo(table, "ParentId", "BIGINT", false, null, null, null),
		};

		// No DB foreign key on the declared column -> MissingForeignKey.
		var noFk = SchemaMigrator.Compare([schema], dbCols, SqlServerDialect.Instance, false, []);
		noFk.Any(d => d.Kind == SchemaDiffKind.MissingForeignKey && d.ColumnName == "ParentId")
			.AssertTrue("a [ForeignKey] column without a DB FK must surface as MissingForeignKey");

		// A matching DB foreign key -> neither extra nor missing.
		var dbFks = new[] { new DbForeignKeyInfo("FK_ColAttr_Parent", table, "ParentId", table, "Id") };
		var withFk = SchemaMigrator.Compare([schema], dbCols, SqlServerDialect.Instance, false, dbFks);
		withFk.Any(d => d.ColumnName == "ParentId" &&
				(d.Kind == SchemaDiffKind.ExtraForeignKey || d.Kind == SchemaDiffKind.MissingForeignKey))
			.AssertFalse("a [ForeignKey] column with a matching DB FK must be neither extra nor missing");
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

		// NOT NULL column → 3-step migration: ADD NULL, UPDATE, ALTER NOT NULL
		sql.Contains("VARCHAR(256) NULL").AssertTrue($"Expected ADD as NULL first: {sql}");
		sql.Contains("UPDATE").AssertTrue($"Expected UPDATE step: {sql}");
		sql.Contains("NOT NULL").AssertTrue($"Expected NOT NULL in final ALTER: {sql}");
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

	/// <summary>
	/// Compare must accept VARCHAR(N) for [MaxLength=N] string on PostgreSQL,
	/// otherwise Compare→Emit→Apply never converges.
	/// </summary>
	[TestMethod]
	public void Compare_PostgreSql_StringWithMaxLength_ConvergesAgainstVarchar()
	{
		var schema = new Schema
		{
			TableName = "TestTable",
			EntityType = typeof(ColAttrTestEntity),
			Columns =
			[
				new SchemaColumn { Name = "Code", ClrType = typeof(string), MaxLength = 64 },
			],
			Factory = () => new ColAttrTestEntity(),
		};

		// Mirrors what PostgreSqlDialect.GetColumnDefinition emits
		// (VARCHAR(64)) and what information_schema reports for it.
		var dbCols = new[]
		{
			new DbColumnInfo("TestTable", "Code", "character varying", false, 64, null, null),
		};

		var diffs = SchemaMigrator.Compare([schema], dbCols, PostgreSqlDialect.Instance, false);

		diffs.Count.AssertEqual(0,
			$"Expected Compare to converge for [MaxLength=64] string vs VARCHAR(64); " +
			$"got diffs: [{string.Join(", ", diffs.Select(d => $"{d.Kind} {d.ColumnName} expected={d.Expected} actual={d.Actual}"))}]");
	}

	// SqlServer-side regression guard for the same shape.
	[TestMethod]
	public void Compare_SqlServer_StringWithMaxLength_ConvergesAgainstNvarchar()
	{
		var schema = new Schema
		{
			TableName = "TestTable",
			EntityType = typeof(ColAttrTestEntity),
			Columns =
			[
				new SchemaColumn { Name = "Code", ClrType = typeof(string), MaxLength = 128 },
			],
			Factory = () => new ColAttrTestEntity(),
		};

		var dbCols = new[]
		{
			new DbColumnInfo("TestTable", "Code", "nvarchar", false, 128, null, null),
		};

		var diffs = SchemaMigrator.Compare([schema], dbCols, SqlServerDialect.Instance, false);

		diffs.Count.AssertEqual(0,
			$"Expected Compare to converge for [MaxLength=128] string vs NVARCHAR(128); " +
			$"got diffs: [{string.Join(", ", diffs.Select(d => $"{d.Kind} {d.ColumnName} expected={d.Expected} actual={d.Actual}"))}]");
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

	/// <summary>
	/// The MissingColumn + NOT NULL migration path backfills the new column
	/// via GetDefaultLiteral. On PostgreSQL a byte[] column must not emit the
	/// SQL Server-only 0x binary literal, which Npgsql rejects with
	/// "syntax error at or near 0x".
	/// </summary>
	[TestMethod]
	[DataRow("PostgreSql")]
	public void MissingColumn_NotNullBinary_DoesNotEmitSqlServerBinaryLiteral(string dialectName)
	{
		var dialect = GetDialect(dialectName);

		var schema = new Schema
		{
			TableName = "TestTable",
			EntityType = typeof(ColAttrTestEntity),
			Columns =
			[
				new SchemaColumn { Name = "Id", ClrType = typeof(long), IsNullable = false },
				new SchemaColumn { Name = "Data", ClrType = typeof(byte[]), IsNullable = false },
			],
			Factory = () => new ColAttrTestEntity(),
		};

		// Table exists (Id present) but the byte[] column is missing,
		// so Compare yields a MissingColumn diff for Data.
		var dbCols = new[]
		{
			new DbColumnInfo("TestTable", "Id", "BIGINT", false, null, null, null),
		};

		var diffs = SchemaMigrator.Compare([schema], dbCols, dialect, false);

		diffs.Any(d => d.Kind == SchemaDiffKind.MissingColumn && d.ColumnName == "Data").AssertTrue(
			$"Expected a MissingColumn diff for Data; got: " +
			$"[{string.Join(", ", diffs.Select(d => $"{d.Kind} {d.ColumnName}"))}]");

		var sql = SchemaMigrator.GenerateMigrationSql(dialect, diffs, [schema]);

		sql.Contains("0x").AssertFalse(
			$"{dialectName} migration SQL must not contain the SQL Server-only " +
			$"0x binary literal; got: {sql}");
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
	[DataRow("SqlServer", "DATE")]
	[DataRow("PostgreSql", "DATE")]
	[DataRow("SQLite", "TEXT")]
	public void GetSqlTypeName_DateOnly(string dialectName, string expected)
		=> GetDialect(dialectName).GetSqlTypeName(typeof(DateOnly)).AssertEqual(expected);

	[TestMethod]
	[DataRow("SqlServer", "TIME")]
	[DataRow("PostgreSql", "TIME")]
	[DataRow("SQLite", "TEXT")]
	public void GetSqlTypeName_TimeOnly(string dialectName, string expected)
		=> GetDialect(dialectName).GetSqlTypeName(typeof(TimeOnly)).AssertEqual(expected);

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

	#region SchemaMigrator end-to-end: DB has more/fewer columns than entity

	[TestMethod]
	public void Migration_DbHasExtraColumns_DetectsAndGeneratesComments()
	{
		// C# class has 2 columns, DB has 4 (2 extra)
		var schema = new Schema
		{
			TableName = "Products",
			EntityType = typeof(ColAttrTestEntity),
			Identity = new SchemaColumn { Name = "Id", ClrType = typeof(long), IsReadOnly = true },
			Columns =
			[
				new SchemaColumn { Name = "Name", ClrType = typeof(string), MaxLength = 128 },
				new SchemaColumn { Name = "Price", ClrType = typeof(decimal), Precision = 10, Scale = 2 },
			],
			Factory = () => new ColAttrTestEntity(),
		};

		var dbCols = new[]
		{
			new DbColumnInfo("Products", "Id", "BIGINT", false, null, null, null),
			new DbColumnInfo("Products", "Name", "NVARCHAR", false, 128, null, null),
			new DbColumnInfo("Products", "Price", "DECIMAL", false, null, 10, 2),
			new DbColumnInfo("Products", "OldDescription", "NVARCHAR", true, -1, null, null),
			new DbColumnInfo("Products", "LegacyCode", "INT", false, null, null, null),
		};

		var diffs = SchemaMigrator.Compare([schema], dbCols, SqlServerDialect.Instance, false);

		diffs.Count.AssertEqual(2);
		diffs.All(d => d.Kind == SchemaDiffKind.ExtraColumn).AssertTrue();
		diffs.Any(d => d.ColumnName == "OldDescription").AssertTrue();
		diffs.Any(d => d.ColumnName == "LegacyCode").AssertTrue();

		var sql = SchemaMigrator.GenerateMigrationSql(SqlServerDialect.Instance, diffs, [schema]);

		sql.Contains("DROP COLUMN").AssertTrue($"Expected DROP COLUMN for extra columns: {sql}");
		sql.Contains("[OldDescription]").AssertTrue($"Expected OldDescription in SQL: {sql}");
		sql.Contains("[LegacyCode]").AssertTrue($"Expected LegacyCode in SQL: {sql}");
		sql.Split('\n').Where(l => l.Contains("DROP COLUMN")).All(l => l.TrimStart().StartsWith("--"))
			.AssertTrue($"extra-column DROP COLUMN must be commented out: {sql}");
	}

	[TestMethod]
	public void Migration_DbHasMissingNullableColumns_GeneratesSimpleAdd()
	{
		// C# class has 3 columns, DB has only 1 (2 missing, both nullable)
		var schema = new Schema
		{
			TableName = "Users",
			EntityType = typeof(ColAttrTestEntity),
			Identity = new SchemaColumn { Name = "Id", ClrType = typeof(long), IsReadOnly = true },
			Columns =
			[
				new SchemaColumn { Name = "Name", ClrType = typeof(string), MaxLength = 128 },
				new SchemaColumn { Name = "Bio", ClrType = typeof(string), IsNullable = true },
				new SchemaColumn { Name = "AvatarUrl", ClrType = typeof(string), IsNullable = true, MaxLength = 512 },
			],
			Factory = () => new ColAttrTestEntity(),
		};

		var dbCols = new[]
		{
			new DbColumnInfo("Users", "Id", "BIGINT", false, null, null, null),
			new DbColumnInfo("Users", "Name", "NVARCHAR", false, 128, null, null),
		};

		var diffs = SchemaMigrator.Compare([schema], dbCols, SqlServerDialect.Instance, false);

		diffs.Count.AssertEqual(2);
		diffs.All(d => d.Kind == SchemaDiffKind.MissingColumn).AssertTrue();

		var sql = SchemaMigrator.GenerateMigrationSql(SqlServerDialect.Instance, diffs, [schema]);

		// nullable columns → simple ADD, no UPDATE/ALTER
		sql.Contains("ALTER TABLE [Users] ADD [Bio]").AssertTrue($"Expected ADD Bio: {sql}");
		sql.Contains("ALTER TABLE [Users] ADD [AvatarUrl]").AssertTrue($"Expected ADD AvatarUrl: {sql}");
		sql.Contains("NULL").AssertTrue($"Expected NULL in column def: {sql}");
		sql.Contains("UPDATE").AssertFalse($"Nullable columns should not need UPDATE: {sql}");
	}

	[TestMethod]
	public void Migration_DbHasMissingNotNullColumns_Generates3StepMigration()
	{
		// C# class has NOT NULL columns missing from DB → 3-step migration
		var schema = new Schema
		{
			TableName = "Orders",
			EntityType = typeof(ColAttrTestEntity),
			Identity = new SchemaColumn { Name = "Id", ClrType = typeof(long), IsReadOnly = true },
			Columns =
			[
				new SchemaColumn { Name = "Status", ClrType = typeof(int) },
				new SchemaColumn { Name = "CustomerName", ClrType = typeof(string), MaxLength = 256 },
			],
			Factory = () => new ColAttrTestEntity(),
		};

		var dbCols = new[]
		{
			new DbColumnInfo("Orders", "Id", "BIGINT", false, null, null, null),
		};

		var diffs = SchemaMigrator.Compare([schema], dbCols, SqlServerDialect.Instance, false);

		diffs.Count.AssertEqual(2);
		diffs.All(d => d.Kind == SchemaDiffKind.MissingColumn).AssertTrue();

		var sql = SchemaMigrator.GenerateMigrationSql(SqlServerDialect.Instance, diffs, [schema]);

		// Step 1: ADD as NULL
		sql.Contains("ADD [Status] INT NULL").AssertTrue($"Expected ADD as NULL first: {sql}");
		sql.Contains("ADD [CustomerName] NVARCHAR(256) NULL").AssertTrue($"Expected ADD as NULL first: {sql}");

		// Step 2: UPDATE with default
		sql.Contains("UPDATE [Orders] SET [Status] = 0 WHERE [Status] IS NULL").AssertTrue($"Expected UPDATE with default: {sql}");
		sql.Contains("UPDATE [Orders] SET [CustomerName] = N'' WHERE [CustomerName] IS NULL").AssertTrue($"Expected UPDATE with default: {sql}");

		// Step 3: ALTER to NOT NULL
		sql.Contains("ALTER COLUMN [Status] INT NOT NULL").AssertTrue($"Expected ALTER to NOT NULL: {sql}");
		sql.Contains("ALTER COLUMN [CustomerName] NVARCHAR(256) NOT NULL").AssertTrue($"Expected ALTER to NOT NULL: {sql}");
	}

	[TestMethod]
	public void Migration_DbHasMissingNotNullColumns_SqlServer_HasBatchSeparator()
	{
		var schema = new Schema
		{
			TableName = "Items",
			EntityType = typeof(ColAttrTestEntity),
			Identity = new SchemaColumn { Name = "Id", ClrType = typeof(long), IsReadOnly = true },
			Columns =
			[
				new SchemaColumn { Name = "Value", ClrType = typeof(int) },
			],
			Factory = () => new ColAttrTestEntity(),
		};

		var diffs = new[]
		{
			new SchemaDiff("Items", "Value", SchemaDiffKind.MissingColumn, "INT NOT NULL", string.Empty),
		};

		var sql = SchemaMigrator.GenerateMigrationSql(SqlServerDialect.Instance, diffs, [schema]);

		// SQL Server uses GO batch separator between steps
		sql.Contains("GO").AssertTrue($"Expected GO batch separator for SQL Server: {sql}");
	}

	[TestMethod]
	public void Migration_DbHasMissingNotNullColumns_PostgreSql_NoBatchSeparator()
	{
		var schema = new Schema
		{
			TableName = "Items",
			EntityType = typeof(ColAttrTestEntity),
			Identity = new SchemaColumn { Name = "Id", ClrType = typeof(long), IsReadOnly = true },
			Columns =
			[
				new SchemaColumn { Name = "Value", ClrType = typeof(int) },
			],
			Factory = () => new ColAttrTestEntity(),
		};

		var diffs = new[]
		{
			new SchemaDiff("Items", "Value", SchemaDiffKind.MissingColumn, "INTEGER NOT NULL", string.Empty),
		};

		var sql = SchemaMigrator.GenerateMigrationSql(PostgreSqlDialect.Instance, diffs, [schema]);

		sql.Contains("GO").AssertFalse($"PostgreSql should not have GO separator: {sql}");
		sql.Contains("UPDATE").AssertTrue($"Expected UPDATE step: {sql}");
	}

	[TestMethod]
	public void Migration_MixedExtraAndMissing_BothDetected()
	{
		// C# has columns A, B; DB has columns A, C → B missing, C extra
		var schema = new Schema
		{
			TableName = "Mixed",
			EntityType = typeof(ColAttrTestEntity),
			Identity = new SchemaColumn { Name = "Id", ClrType = typeof(long), IsReadOnly = true },
			Columns =
			[
				new SchemaColumn { Name = "Active", ClrType = typeof(bool) },
				new SchemaColumn { Name = "NewField", ClrType = typeof(string), IsNullable = true },
			],
			Factory = () => new ColAttrTestEntity(),
		};

		var dbCols = new[]
		{
			new DbColumnInfo("Mixed", "Id", "BIGINT", false, null, null, null),
			new DbColumnInfo("Mixed", "Active", "BIT", false, null, null, null),
			new DbColumnInfo("Mixed", "Removed", "NVARCHAR", true, -1, null, null),
		};

		var diffs = SchemaMigrator.Compare([schema], dbCols, SqlServerDialect.Instance, false);

		diffs.Count.AssertEqual(2);
		diffs.Any(d => d.Kind == SchemaDiffKind.MissingColumn && d.ColumnName == "NewField").AssertTrue();
		diffs.Any(d => d.Kind == SchemaDiffKind.ExtraColumn && d.ColumnName == "Removed").AssertTrue();

		var sql = SchemaMigrator.GenerateMigrationSql(SqlServerDialect.Instance, diffs, [schema]);

		sql.Contains("ADD [NewField]").AssertTrue($"Expected ADD for missing column: {sql}");
		sql.Contains("DROP COLUMN [Removed]").AssertTrue($"Expected commented DROP COLUMN for extra column: {sql}");
		sql.Split('\n').Where(l => l.Contains("DROP COLUMN")).All(l => l.TrimStart().StartsWith("--"))
			.AssertTrue($"extra-column DROP COLUMN must be commented out: {sql}");
	}

	[TestMethod]
	public void Migration_NotNullColumnTypes_CorrectDefaults()
	{
		// Verify correct default literals for different NOT NULL types
		var schema = new Schema
		{
			TableName = "Defaults",
			EntityType = typeof(ColAttrTestEntity),
			Columns =
			[
				new SchemaColumn { Name = "IntCol", ClrType = typeof(int) },
				new SchemaColumn { Name = "BoolCol", ClrType = typeof(bool) },
				new SchemaColumn { Name = "StringCol", ClrType = typeof(string) },
				new SchemaColumn { Name = "DateCol", ClrType = typeof(DateTime) },
				new SchemaColumn { Name = "GuidCol", ClrType = typeof(Guid) },
				new SchemaColumn { Name = "DecimalCol", ClrType = typeof(decimal), Precision = 18, Scale = 8 },
			],
			Factory = () => new ColAttrTestEntity(),
		};

		var diffs = schema.Columns.Select(c =>
			new SchemaDiff("Defaults", c.Name, SchemaDiffKind.MissingColumn, "expected", string.Empty)
		).ToArray();

		var sql = SchemaMigrator.GenerateMigrationSql(SqlServerDialect.Instance, diffs, [schema]);

		sql.Contains("SET [IntCol] = 0 WHERE [IntCol] IS NULL").AssertTrue($"Int default: {sql}");
		sql.Contains("SET [BoolCol] = 0 WHERE [BoolCol] IS NULL").AssertTrue($"Bool default: {sql}");
		sql.Contains("SET [StringCol] = N'' WHERE [StringCol] IS NULL").AssertTrue($"String default: {sql}");
		sql.Contains("SET [DateCol] = '0001-01-01T00:00:00' WHERE [DateCol] IS NULL").AssertTrue($"DateTime default: {sql}");
		sql.Contains("SET [GuidCol] = '00000000-0000-0000-0000-000000000000' WHERE [GuidCol] IS NULL").AssertTrue($"Guid default: {sql}");
		sql.Contains("SET [DecimalCol] = 0 WHERE [DecimalCol] IS NULL").AssertTrue($"Decimal default: {sql}");
	}

	#endregion

	#region Finding #5: DateTime precision in DDL

	[TestMethod]
	[DataRow("SqlServer", "DATETIME2(3) NOT NULL")]
	[DataRow("PostgreSql", "TIMESTAMPTZ(3) NOT NULL")]
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

	#region SchemaMigrator.GenerateMigrationSql validation

	[TestMethod]
	public void GenerateMigrationSql_UniqueStringWithoutMaxLength_Throws()
	{
		var schema = new Schema
		{
			TableName = "Bad",
			EntityType = typeof(ColAttrTestEntity),
			Identity = new SchemaColumn { Name = "Id", ClrType = typeof(long), IsReadOnly = true },
			Columns =
			[
				new SchemaColumn { Name = "Name", ClrType = typeof(string), IsUnique = true },
			],
			Factory = () => new ColAttrTestEntity(),
		};

		var diffs = new[] { new SchemaDiff("Bad", string.Empty, SchemaDiffKind.MissingTable, "expected", "missing") };

		var ex = Throws<InvalidOperationException>(
			() => SchemaMigrator.GenerateMigrationSql(SqlServerDialect.Instance, diffs, [schema]));

		ex.Message.Contains("Bad.Name").AssertTrue(ex.Message);
		ex.Message.Contains("MaxLength").AssertTrue(ex.Message);
	}

	[TestMethod]
	public void GenerateMigrationSql_IndexStringWithoutMaxLength_Throws()
	{
		var schema = new Schema
		{
			TableName = "Bad2",
			EntityType = typeof(ColAttrTestEntity),
			Identity = new SchemaColumn { Name = "Id", ClrType = typeof(long), IsReadOnly = true },
			Columns =
			[
				new SchemaColumn { Name = "Slug", ClrType = typeof(string), IsIndex = true },
			],
			Factory = () => new ColAttrTestEntity(),
		};

		var diffs = new[] { new SchemaDiff("Bad2", string.Empty, SchemaDiffKind.MissingTable, "expected", "missing") };

		var ex = Throws<InvalidOperationException>(
			() => SchemaMigrator.GenerateMigrationSql(SqlServerDialect.Instance, diffs, [schema]));

		ex.Message.Contains("Bad2.Slug").AssertTrue(ex.Message);
		ex.Message.Contains("MaxLength").AssertTrue(ex.Message);
	}

	[TestMethod]
	public void GenerateMigrationSql_UniqueStringWithMaxLength_Ok()
	{
		var schema = new Schema
		{
			TableName = "Good",
			EntityType = typeof(ColAttrTestEntity),
			Identity = new SchemaColumn { Name = "Id", ClrType = typeof(long), IsReadOnly = true },
			Columns =
			[
				new SchemaColumn { Name = "Name", ClrType = typeof(string), IsUnique = true, MaxLength = 128 },
			],
			Factory = () => new ColAttrTestEntity(),
		};

		var diffs = new[] { new SchemaDiff("Good", string.Empty, SchemaDiffKind.MissingTable, "expected", "missing") };

		var sql = SchemaMigrator.GenerateMigrationSql(SqlServerDialect.Instance, diffs, [schema]);

		sql.Contains("NVARCHAR(128)").AssertTrue(sql);
		sql.Contains("CREATE UNIQUE INDEX").AssertTrue(sql);
	}

	[TestMethod]
	public void GenerateMigrationSql_NonIndexedStringWithoutMaxLength_Ok()
	{
		var schema = new Schema
		{
			TableName = "Plain",
			EntityType = typeof(ColAttrTestEntity),
			Identity = new SchemaColumn { Name = "Id", ClrType = typeof(long), IsReadOnly = true },
			Columns =
			[
				new SchemaColumn { Name = "Description", ClrType = typeof(string) },
			],
			Factory = () => new ColAttrTestEntity(),
		};

		var diffs = new[] { new SchemaDiff("Plain", string.Empty, SchemaDiffKind.MissingTable, "expected", "missing") };

		var sql = SchemaMigrator.GenerateMigrationSql(SqlServerDialect.Instance, diffs, [schema]);

		sql.Contains("NVARCHAR(MAX)").AssertTrue(sql);
	}

	[TestMethod]
	public void GenerateMigrationSql_UniqueStringWithoutMaxLength_ThrowsOnPostgreSql()
	{
		// universal rule: indexed string without MaxLength is invalid regardless of dialect.
		var schema = new Schema
		{
			TableName = "Bad",
			EntityType = typeof(ColAttrTestEntity),
			Identity = new SchemaColumn { Name = "Id", ClrType = typeof(long), IsReadOnly = true },
			Columns =
			[
				new SchemaColumn { Name = "Name", ClrType = typeof(string), IsUnique = true },
			],
			Factory = () => new ColAttrTestEntity(),
		};

		var diffs = new[] { new SchemaDiff("Bad", string.Empty, SchemaDiffKind.MissingTable, "expected", "missing") };

		Throws<InvalidOperationException>(
			() => SchemaMigrator.GenerateMigrationSql(PostgreSqlDialect.Instance, diffs, [schema]));
	}

	#endregion

	#region SchemaMigrator.ApplyAsync batch splitting

	private static async Task<SqliteConnection> OpenMemorySqlite(CancellationToken token)
	{
		var conn = new SqliteConnection("Data Source=:memory:");
		await conn.OpenAsync(token);
		return conn;
	}

	private static bool TableExists(SqliteConnection conn, string name)
	{
		using var cmd = conn.CreateCommand();
		cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $name";
		var p = cmd.CreateParameter();
		p.ParameterName = "$name";
		p.Value = name;
		cmd.Parameters.Add(p);
		return (long)cmd.ExecuteScalar() == 1;
	}

	[TestMethod]
	public async Task ApplyAsync_EmptySql_Noop()
	{
		using var conn = await OpenMemorySqlite(CancellationToken);

		await SchemaMigrator.ApplyAsync(conn, "", SqlServerDialect.Instance, CancellationToken);
		await SchemaMigrator.ApplyAsync(conn, null, SqlServerDialect.Instance, CancellationToken);
		await SchemaMigrator.ApplyAsync(conn, "", PostgreSqlDialect.Instance, CancellationToken);
	}

	[TestMethod]
	public async Task ApplyAsync_NullDialect_Throws()
	{
		using var conn = await OpenMemorySqlite(CancellationToken);

		await ThrowsExactlyAsync<ArgumentNullException>(
			() => SchemaMigrator.ApplyAsync(conn, "CREATE TABLE X (id INTEGER)", null, CancellationToken));
	}

	[TestMethod]
	public async Task ApplyAsync_NullConnection_Throws()
	{
		await ThrowsExactlyAsync<ArgumentNullException>(
			() => SchemaMigrator.ApplyAsync(null, "CREATE TABLE X (id INTEGER)", SqlServerDialect.Instance, CancellationToken));
	}

	[TestMethod]
	public async Task ApplyAsync_PostgreSqlDialect_NoSeparator_SingleBatch()
	{
		using var conn = await OpenMemorySqlite(CancellationToken);

		await SchemaMigrator.ApplyAsync(conn, "CREATE TABLE T_pg (id INTEGER)", PostgreSqlDialect.Instance, CancellationToken);

		TableExists(conn, "T_pg").AssertTrue();
	}

	[TestMethod]
	public async Task ApplyAsync_SqlServerDialect_SplitsByGo()
	{
		using var conn = await OpenMemorySqlite(CancellationToken);

		var sql = string.Join("\n",
		[
			"CREATE TABLE T_a (id INTEGER);",
			"GO",
			"CREATE TABLE T_b (id INTEGER);",
			"GO",
			"CREATE TABLE T_c (id INTEGER);",
		]);

		await SchemaMigrator.ApplyAsync(conn, sql, SqlServerDialect.Instance, CancellationToken);

		TableExists(conn, "T_a").AssertTrue();
		TableExists(conn, "T_b").AssertTrue();
		TableExists(conn, "T_c").AssertTrue();
	}

	[TestMethod]
	public async Task ApplyAsync_SqlServerDialect_GoCaseInsensitiveAndPadded()
	{
		using var conn = await OpenMemorySqlite(CancellationToken);

		var sql = string.Join("\n",
		[
			"CREATE TABLE T_case_a (id INTEGER);",
			"  go  ",
			"CREATE TABLE T_case_b (id INTEGER);",
			"\tGo\t",
			"CREATE TABLE T_case_c (id INTEGER);",
		]);

		await SchemaMigrator.ApplyAsync(conn, sql, SqlServerDialect.Instance, CancellationToken);

		TableExists(conn, "T_case_a").AssertTrue();
		TableExists(conn, "T_case_b").AssertTrue();
		TableExists(conn, "T_case_c").AssertTrue();
	}

	[TestMethod]
	public async Task ApplyAsync_SqlServerDialect_EmptyAndTrailingBatchesSkipped()
	{
		using var conn = await OpenMemorySqlite(CancellationToken);

		var sql = string.Join("\n",
		[
			"GO",
			"CREATE TABLE T_skip1 (id INTEGER);",
			"GO",
			"GO",
			"CREATE TABLE T_skip2 (id INTEGER);",
			"GO",
			"",
		]);

		await SchemaMigrator.ApplyAsync(conn, sql, SqlServerDialect.Instance, CancellationToken);

		TableExists(conn, "T_skip1").AssertTrue();
		TableExists(conn, "T_skip2").AssertTrue();
	}

	[TestMethod]
	public async Task ApplyAsync_SqlServerDialect_GoInsideIdentifier_NotSplit()
	{
		using var conn = await OpenMemorySqlite(CancellationToken);

		await SchemaMigrator.ApplyAsync(conn, "CREATE TABLE T_GOAL (id INTEGER)", SqlServerDialect.Instance, CancellationToken);

		TableExists(conn, "T_GOAL").AssertTrue();
	}

	#endregion
}

#endif
