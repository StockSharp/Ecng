#if NET10_0_OR_GREATER

namespace Ecng.Tests.Data;

using Ecng.Data;
using Ecng.Data.Sql;
using Ecng.Serialization;

/// <summary>
/// Pure (no-database) checks over the compression pass of <see cref="SchemaMigrator.Compare"/>: a table
/// packed differently from what its entity declares is reported, and the statement that repacks it is
/// generated.
/// </summary>
[TestClass]
public class SchemaMigratorCompressionTests : BaseTestClass
{
	[DataCompression(DataCompressions.Page)]
	private sealed class PackedRow
	{
		public long Id { get; set; }
	}

	private sealed class PlainRow
	{
		public long Id { get; set; }
	}

	private const string _packed = "PackedRow";
	private const string _plain = "PlainRow";

	private static Schema BuildSchema(string table, Type type)
		=> new()
		{
			TableName = table,
			EntityType = type,
			Identity = new SchemaColumn { Name = "Id", ClrType = typeof(long), IsReadOnly = true },
			Columns = [],
			Factory = () => Activator.CreateInstance(type, true),
		};

	private static List<DbColumnInfo> Columns() =>
	[
		new(_packed, "Id", "bigint", false, null, null, null),
		new(_plain, "Id", "bigint", false, null, null, null),
	];

	private static IReadOnlyList<SchemaDiff> Compare(IReadOnlyList<DbTableCompressionInfo> compressions)
		=> SchemaMigrator.Compare(
			[BuildSchema(_packed, typeof(PackedRow)), BuildSchema(_plain, typeof(PlainRow))],
			Columns(),
			SqlServerDialect.Instance,
			skipComputed: false,
			dbCompressions: compressions);

	[TestMethod]
	public void UnpackedTableThatAsksToBePackedIsReported()
	{
		var diffs = Compare([new(_packed, DataCompressions.None), new(_plain, DataCompressions.None)]);

		var diff = diffs.FirstOrDefault(d => d.Kind == SchemaDiffKind.CompressionMismatch);

		IsNotNull(diff);
		AreEqual(_packed, diff.TableName);
		AreEqual(nameof(DataCompressions.Page), diff.Expected);
		AreEqual(nameof(DataCompressions.None), diff.Actual);

		// The table that declares nothing and is packed as nothing has no say in this.
		AreEqual(1, diffs.Count(d => d.Kind == SchemaDiffKind.CompressionMismatch));
	}

	[TestMethod]
	public void TableAlreadyPackedAsDeclaredIsNotReported()
	{
		var diffs = Compare([new(_packed, DataCompressions.Page), new(_plain, DataCompressions.None)]);

		AreEqual(0, diffs.Count(d => d.Kind == SchemaDiffKind.CompressionMismatch));
	}

	/// <summary>
	/// Packing a table that declares none is as much a difference as the other way round - otherwise a
	/// table packed by hand would silently keep a setting no entity asks for.
	/// </summary>
	[TestMethod]
	public void TablePackedWithoutBeingAskedIsReported()
	{
		var diffs = Compare([new(_packed, DataCompressions.Page), new(_plain, DataCompressions.Row)]);

		var diff = diffs.Single(d => d.Kind == SchemaDiffKind.CompressionMismatch);

		AreEqual(_plain, diff.TableName);
		AreEqual(nameof(DataCompressions.None), diff.Expected);
		AreEqual(nameof(DataCompressions.Row), diff.Actual);
	}

	/// <summary>
	/// A dialect that cannot pack anything reports nothing, and nothing is what the comparison may
	/// conclude from that - not that every table is wrongly packed.
	/// </summary>
	[TestMethod]
	public void DialectThatReportsNothingProducesNoDiffs()
	{
		AreEqual(0, Compare([]).Count(d => d.Kind == SchemaDiffKind.CompressionMismatch));
	}

	[TestMethod]
	public void MigrationSqlRepacksTheTable()
	{
		var diffs = Compare([new(_packed, DataCompressions.None), new(_plain, DataCompressions.None)]);

		var sql = SchemaMigrator.GenerateMigrationSql(SqlServerDialect.Instance, diffs, [BuildSchema(_packed, typeof(PackedRow))]);

		IsTrue(sql.Contains("ALTER INDEX ALL ON [PackedRow] REBUILD WITH (DATA_COMPRESSION = PAGE)"), sql);
	}

	/// <summary>
	/// Every dialect has to answer the question, and the ones without the concept answer it the same way:
	/// nothing is packed. Without this a PostgreSQL or SQLite database would be compared against an empty
	/// reading it never produced, or the comparison would throw where it used to work.
	/// </summary>
	[DataTestMethod]
	[DynamicData(nameof(DialectsWithoutCompression), DynamicDataSourceType.Property)]
	public async Task DialectWithoutCompressionReportsNothing(ISqlDialect dialect)
	{
		// The base reading looks at no connection, which is what lets it answer for a database it cannot ask.
		var read = await dialect.ReadDbCompressionsAsync(null, cancellationToken: CancellationToken);

		IsNotNull(read);
		AreEqual(0, read.Count);

		Throws<NotSupportedException>(() => dialect.AppendSetCompression(new StringBuilder(), "T", DataCompressions.Page));
	}

	public static IEnumerable<object[]> DialectsWithoutCompression
		=> [[PostgreSqlDialect.Instance], [SQLiteDialect.Instance]];

	[TestMethod]
	public void DialectWritesEachSetting()
	{
		static string Emit(DataCompressions compression)
		{
			var sb = new StringBuilder();
			SqlServerDialect.Instance.AppendSetCompression(sb, "T", compression);
			return sb.ToString();
		}

		AreEqual("ALTER INDEX ALL ON [T] REBUILD WITH (DATA_COMPRESSION = NONE)", Emit(DataCompressions.None));
		AreEqual("ALTER INDEX ALL ON [T] REBUILD WITH (DATA_COMPRESSION = ROW)", Emit(DataCompressions.Row));
		AreEqual("ALTER INDEX ALL ON [T] REBUILD WITH (DATA_COMPRESSION = PAGE)", Emit(DataCompressions.Page));
	}
}

#endif
