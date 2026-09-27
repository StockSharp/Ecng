namespace Ecng.Data;

using System.Data.Common;
using System.Threading.Tasks;

/// <summary>
/// Contract for a database-specific SQL dialect. Implementations are usually
/// derived from <see cref="SqlDialectBase"/>, which already provides
/// reasonable defaults for everything that is not strictly dialect-specific —
/// concrete dialects only override what differs (quoting, type mapping,
/// pagination shape, identity bookkeeping). This interface intentionally
/// holds <em>signatures only</em>; default bodies live in
/// <see cref="SqlDialectBase"/> so we never have to keep two copies of the
/// same implementation in sync.
/// </summary>
public interface ISqlDialect
{
	/// <summary>
	/// Maximum number of parameters allowed in a single statement.
	/// SQL Server: 2100, SQLite: 999, PostgreSQL: 65535, MySQL: 65535.
	/// </summary>
	int MaxParameters { get; }

	/// <summary>
	/// Parameter prefix (e.g. <c>@</c> for SQL Server, <c>$</c> for PostgreSQL).
	/// </summary>
	string ParameterPrefix { get; }

	/// <summary>
	/// SQL string concatenation operator.
	/// </summary>
	string ConcatOperator { get; }

	/// <summary>
	/// SQL literal for boolean <c>true</c>.
	/// </summary>
	string TrueLiteral { get; }

	/// <summary>
	/// SQL literal for boolean <c>false</c>.
	/// </summary>
	string FalseLiteral { get; }

	/// <summary>
	/// SQL type name to cast a boolean expression to when materialising
	/// <c>EXISTS(...)</c> projections (e.g. <c>bit</c> on SQL Server). Return
	/// <see langword="null"/> if the dialect represents booleans natively
	/// and no cast is needed (e.g. PostgreSQL).
	/// </summary>
	string BooleanCastSqlType { get; }

	/// <summary>
	/// SQL type name used to cast decimal operands before comparisons when
	/// the dialect stores decimals in a non-numeric affinity. Return
	/// <see langword="null"/> when no comparison cast is required.
	/// </summary>
	string DecimalComparisonCastSqlType { get; }

	/// <summary>
	/// SQL type a whole number is cast to when a query converts it to <see cref="decimal"/>.
	/// </summary>
	/// <param name="precision">Number of digits the source integer type can hold.</param>
	/// <returns>The SQL type name.</returns>
	string GetIntegerToDecimalCastSqlType(int precision);

	/// <summary>
	/// Whether a decimal column keeps a declared precision and scale. A dialect that stores decimals as text
	/// keeps neither, so there is nothing to compare a declaration against.
	/// </summary>
	bool KeepsDecimalDigits { get; }

	/// <summary>
	/// Whether ALTER COLUMN can change a column that an index, a primary key or a foreign key depends on.
	/// SQL Server cannot: those have to be dropped before and recreated after.
	/// </summary>
	bool CanAlterColumnWithDependents { get; }

	/// <summary>
	/// Whether an existing column's type or nullability can be changed in place. SQLite cannot: such a change
	/// means rebuilding the table.
	/// </summary>
	bool CanAlterColumn { get; }

	/// <summary>
	/// Unicode string-literal prefix (e.g. <c>N</c> for SQL Server).
	/// </summary>
	string UnicodePrefix { get; }

	/// <summary>
	/// SQL literal for an empty binary value (e.g. <c>0x</c> for SQL Server,
	/// <c>X''</c> for the SQL standard, an empty bytea for PostgreSQL).
	/// </summary>
	string EmptyBinaryLiteral { get; }

	/// <summary>
	/// Function name for string length.
	/// </summary>
	string LenFunction { get; }

	/// <summary>
	/// Function name for null-coalescing.
	/// </summary>
	string IsNullFunction { get; }

	/// <summary>
	/// Batch separator (e.g. <c>GO</c> for SQL Server). Empty when no
	/// separation is needed.
	/// </summary>
	string BatchSeparator { get; }

	/// <summary>
	/// Quotes an identifier (table name, column name).
	/// </summary>
	string QuoteIdentifier(string identifier);

	/// <summary>
	/// Returns the SQL type name for a CLR type.
	/// </summary>
	string GetSqlTypeName(Type clrType);

	/// <summary>
	/// Canonical SQL type name with length applied. Dialects override
	/// when the type differs by length (e.g. PostgreSQL TEXT vs VARCHAR(N)).
	/// </summary>
	string GetSqlTypeName(Type clrType, int maxLength);

	/// <summary>
	/// Converts a CLR value to the form a database driver expects.
	/// </summary>
	object ConvertToDbValue(object value, Type clrType);

	/// <summary>
	/// Final-stage massaging of a fully populated <see cref="DbParameter"/>
	/// before it leaves the ORM layer. Lets a dialect adjust provider-specific
	/// type metadata that the generic <see cref="System.Data.DbType"/> hint can't express —
	/// e.g. PostgreSQL needs <c>timestamptz</c> binding for UTC-kind
	/// <see cref="DateTime"/> values, which only Npgsql's own
	/// <c>NpgsqlDbType</c> property can express. Default is a no-op.
	/// </summary>
	void PrepareParameter(DbParameter parameter);

	/// <summary>
	/// Converts a database value to the requested CLR target type.
	/// </summary>
	object ConvertFromDbValue(object value, Type targetType);

	/// <summary>
	/// SQL expression that returns the identity value of the last insert.
	/// </summary>
	string GetIdentitySelect(string idCol);

	/// <summary>
	/// Renders a SKIP/OFFSET clause from a parameter expression.
	/// </summary>
	string FormatSkip(string skip);

	/// <summary>
	/// Renders a TAKE/FETCH clause from a parameter expression.
	/// </summary>
	string FormatTake(string take);

	/// <summary>
	/// Current local date/time.
	/// </summary>
	string Now();

	/// <summary>
	/// Current UTC date/time.
	/// </summary>
	string UtcNow();

	/// <summary>
	/// System local date/time with offset.
	/// </summary>
	string SysNow();

	/// <summary>
	/// System UTC date/time with offset.
	/// </summary>
	string SysUtcNow();

	/// <summary>
	/// Generates a new unique identifier.
	/// </summary>
	string NewId();

	/// <summary>
	/// Suffix for an identity (auto-increment primary key) column definition.
	/// </summary>
	string GetIdentityColumnSuffix();

	/// <summary>
	/// Same as <see cref="GetIdentityColumnSuffix()"/>, but names the <c>PRIMARY KEY</c>
	/// constraint. Without a name the database invents one — <c>PK__Table__hash</c> on
	/// SQL Server, <c>table_pkey</c> on PostgreSQL — which no naming convention can match.
	/// </summary>
	/// <param name="pkConstraintName">Constraint name, normally from <see cref="SchemaNaming.PrimaryKey"/>.</param>
	/// <returns>The column-definition suffix carrying the named constraint.</returns>
	/// <remarks>
	/// Defaults to the unnamed form so implementations outside this assembly keep working;
	/// <see cref="SqlDialectBase"/> supplies the real behaviour.
	/// </remarks>
	string GetIdentityColumnSuffix(string pkConstraintName) => GetIdentityColumnSuffix();

	/// <summary>
	/// Inline FOREIGN KEY constraint clause for use inside CREATE TABLE.
	/// </summary>
	string GetForeignKeyConstraint(string tableName, string columnName, string refTableName, string refColumnName);

	/// <summary>
	/// Full column definition: SQL type + NULL/NOT NULL.
	/// </summary>
	string GetColumnDefinition(Type clrType, bool isNullable, int maxLength = 0, int precision = 0, int scale = 0);

	/// <summary>
	/// The SQL type a column is created with: the type name together with its length, precision or scale.
	/// A decimal that declares only a scale gets <see cref="SqlDialectBase.DefaultDecimalPrecision"/>; one that
	/// declares a precision keeps its scale as declared, zero included.
	/// </summary>
	string GetColumnTypeName(Type clrType, int maxLength, int precision, int scale);

	/// <summary>
	/// The length the database reports for a column created with the declared length: -1 when the dialect
	/// stores it unbounded, as SQL Server does past NVARCHAR(4000) and VARBINARY(8000).
	/// </summary>
	int GetStoredMaxLength(Type clrType, int maxLength);

	/// <summary>
	/// Normalises a raw database type name to the canonical form used by
	/// this dialect.
	/// </summary>
	string NormalizeDbType(string dbTypeName);

	/// <summary>
	/// Returns a SQL literal representing the default value for the given
	/// CLR type. Used during migrations to backfill NOT NULL columns.
	/// </summary>
	string GetDefaultLiteral(Type clrType);

	/// <summary>
	/// Appends CREATE TABLE [IF NOT EXISTS] statement.
	/// </summary>
	void AppendCreateTable(StringBuilder sb, string tableName, string columnDefs);

	/// <summary>
	/// Appends DROP TABLE [IF EXISTS] statement.
	/// </summary>
	void AppendDropTable(StringBuilder sb, string tableName);

	/// <summary>
	/// Appends literal pagination (LIMIT/OFFSET or OFFSET/FETCH) with values.
	/// </summary>
	void AppendPagination(StringBuilder sb, long? skip, long? take, bool hasOrderBy);

	/// <summary>
	/// Appends parameterised pagination with already-prefixed parameter
	/// expressions (e.g. <c>@skip</c>, <c>@take</c>).
	/// </summary>
	void AppendPaginationParams(StringBuilder sb, string skipParamExpr, string takeParamExpr);

	/// <summary>
	/// Appends a fallback ORDER BY clause when there is neither explicit
	/// ordering nor an identity column. SQL Server requires ORDER BY for
	/// OFFSET/FETCH; other dialects may emit a deterministic shim or
	/// nothing at all.
	/// </summary>
	void AppendFallbackOrderBy(StringBuilder sb);

	/// <summary>
	/// Appends a UPSERT statement (MERGE on SQL Server, INSERT … ON
	/// CONFLICT on PostgreSQL/SQLite).
	/// </summary>
	void AppendUpsert(StringBuilder sb, string tableName, string[] allColumns, string[] keyColumns);

	/// <summary>
	/// Appends a dialect-specific RETURNING clause to an INSERT, scoped to
	/// the given identity column. PostgreSQL emits <c>RETURNING "Id"</c>;
	/// SQL Server and SQLite have other identity-read mechanisms and emit
	/// nothing here.
	/// </summary>
	void AppendInsertReturningClause(StringBuilder sb, string idColumn);

	/// <summary>
	/// True when this dialect can return a server-generated identity in
	/// the same statement via <see cref="AppendInsertReturningClause"/>;
	/// callers use it to choose between a single-statement INSERT…RETURNING
	/// and a two-statement INSERT + SELECT identity batch.
	/// </summary>
	bool SupportsInsertReturning { get; }

	/// <summary>
	/// True when this dialect can add a foreign key to an existing table via
	/// <see cref="AppendAddForeignKey"/>. When false (SQLite), a foreign key can
	/// only be declared inline inside CREATE TABLE, so the migrator keeps new-table
	/// FKs inline and creates the referenced tables first.
	/// </summary>
	bool SupportsAddForeignKeyViaAlter { get; }

	/// <summary>
	/// Appends an ALTER TABLE ADD CONSTRAINT for a new foreign key.
	/// </summary>
	void AppendAddForeignKey(StringBuilder sb, string tableName, string columnName, string refTableName, string refColumnName);

	/// <summary>
	/// Appends an ALTER TABLE DROP CONSTRAINT for an existing foreign key.
	/// </summary>
	void AppendDropForeignKey(StringBuilder sb, string tableName, string constraintName);

	/// <summary>
	/// Appends a CREATE INDEX (or CREATE UNIQUE INDEX) on a single column.
	/// </summary>
	void AppendCreateIndex(StringBuilder sb, string indexName, string tableName, string columnName, bool unique);

	/// <summary>
	/// Appends DROP INDEX.
	/// </summary>
	void AppendDropIndex(StringBuilder sb, string tableName, string indexName);

	/// <summary>
	/// Appends ALTER TABLE ADD COLUMN.
	/// </summary>
	void AppendAddColumn(StringBuilder sb, string tableName, string columnName, string columnDef);

	/// <summary>
	/// Appends ALTER TABLE ALTER COLUMN that gives a column the declared type and nullability.
	/// </summary>
	/// <param name="sb">Where the statement goes.</param>
	/// <param name="tableName">The table.</param>
	/// <param name="columnName">The column.</param>
	/// <param name="clrType">The property type the column holds.</param>
	/// <param name="isNullable">Whether the column should allow NULLs.</param>
	/// <param name="maxLength">The declared length, 0 for the default.</param>
	/// <param name="precision">The declared precision, 0 for the default.</param>
	/// <param name="scale">The declared scale.</param>
	/// <param name="live">The column as the database holds it, or null for a column this script has just added;
	/// a dialect uses it to keep what the declaration does not decide, and to convert the stored values.</param>
	void AppendAlterColumn(StringBuilder sb, string tableName, string columnName, Type clrType, bool isNullable, int maxLength, int precision, int scale, DbColumnInfo live);

	/// <summary>
	/// Makes a column nullable or not, leaving its type as the database holds it.
	/// </summary>
	/// <param name="sb">Where the statement goes.</param>
	/// <param name="tableName">The table.</param>
	/// <param name="columnName">The column.</param>
	/// <param name="isNullable">Whether the column should allow NULLs.</param>
	/// <param name="live">The column as the database holds it.</param>
	void AppendAlterNullability(StringBuilder sb, string tableName, string columnName, bool isNullable, DbColumnInfo live);

	/// <summary>
	/// Appends ALTER TABLE DROP COLUMN.
	/// </summary>
	void AppendDropColumn(StringBuilder sb, string tableName, string columnName);

	/// <summary>
	/// Appends UPDATE … SET column = literal WHERE column IS NULL — used
	/// during migrations to fill default values before altering nullability.
	/// </summary>
	void AppendUpdateWhereNull(StringBuilder sb, string tableName, string columnName, string defaultLiteral);

	/// <summary>
	/// Appends an UPDATE … SET … WHERE statement.
	/// </summary>
	void AppendUpdateBy(StringBuilder sb, string tableName, string[] setColumns, string[] whereColumns);

	/// <summary>
	/// Appends a DELETE … WHERE statement.
	/// </summary>
	void AppendDeleteBy(StringBuilder sb, string tableName, string[] whereColumns);

	/// <summary>
	/// Opens a date-part extraction expression (closed by
	/// <see cref="AppendDatePartClose"/>).
	/// </summary>
	void AppendDatePartOpen(StringBuilder sb, string part);

	/// <summary>
	/// Closes a date-part extraction expression opened by
	/// <see cref="AppendDatePartOpen"/>.
	/// </summary>
	void AppendDatePartClose(StringBuilder sb);

	/// <summary>
	/// Appends a DATEADD-style expression.
	/// </summary>
	void AppendDateAdd(StringBuilder sb, string part, string amountSql, string sourceSql);

	/// <summary>
	/// Appends a DATEDIFF-style expression computing <paramref name="endSql"/> minus
	/// <paramref name="startSql"/>, expressed in <paramref name="part"/> units.
	/// </summary>
	/// <param name="sb">Target builder.</param>
	/// <param name="part">Unit of the result: <c>year</c>, <c>month</c>, <c>week</c>, <c>day</c>, <c>hour</c>, <c>minute</c>, <c>second</c> or <c>millisecond</c>.</param>
	/// <param name="startSql">SQL of the start date.</param>
	/// <param name="endSql">SQL of the end date.</param>
	void AppendDateDiff(StringBuilder sb, string part, string startSql, string endSql);

	/// <summary>
	/// Appends the remainder of dividing one decimal value by another, fraction included and with
	/// the sign of the dividend, as the C# <c>%</c> operator gives it.
	/// </summary>
	/// <param name="sb">Target builder.</param>
	/// <param name="dividendSql">SQL of the dividend.</param>
	/// <param name="divisorSql">SQL of the divisor.</param>
	void AppendDecimalModulo(StringBuilder sb, string dividendSql, string divisorSql);

	/// <summary>
	/// Appends the larger of two values, the way <see cref="Math.Max(decimal, decimal)"/> picks it:
	/// a scalar function of two arguments, not the MAX aggregate.
	/// </summary>
	/// <param name="sb">Target builder.</param>
	/// <param name="leftSql">SQL of the first value.</param>
	/// <param name="rightSql">SQL of the second value.</param>
	void AppendGreatest(StringBuilder sb, string leftSql, string rightSql);

	/// <summary>
	/// Appends the smaller of two values, the way <see cref="Math.Min(decimal, decimal)"/> picks it:
	/// a scalar function of two arguments, not the MIN aggregate.
	/// </summary>
	/// <param name="sb">Target builder.</param>
	/// <param name="leftSql">SQL of the first value.</param>
	/// <param name="rightSql">SQL of the second value.</param>
	void AppendLeast(StringBuilder sb, string leftSql, string rightSql);

	/// <summary>
	/// Appends an expression that rounds a value the way
	/// <see cref="Math.Round(decimal, int, MidpointRounding)"/> does with the given mode.
	/// </summary>
	/// <param name="sb">Target builder.</param>
	/// <param name="valueSql">SQL of the value.</param>
	/// <param name="digitsSql">SQL of the number of fractional digits to keep, or <see langword="null"/> to round to a whole number.</param>
	/// <param name="mode">How the value is rounded; <see cref="MidpointRounding.ToZero"/> truncates.</param>
	/// <exception cref="NotSupportedException">The database cannot round exactly this way.</exception>
	void AppendRound(StringBuilder sb, string valueSql, string digitsSql, MidpointRounding mode);

	/// <summary>
	/// Opens a TRIM expression (closed by <see cref="AppendTrimClose"/>).
	/// </summary>
	void AppendTrimOpen(StringBuilder sb);

	/// <summary>
	/// Closes a TRIM expression opened by <see cref="AppendTrimOpen"/>.
	/// </summary>
	void AppendTrimClose(StringBuilder sb);

	/// <summary>
	/// Reads column metadata from a live database.
	/// </summary>
	Task<IReadOnlyList<DbColumnInfo>> ReadDbSchemaAsync(
		DbConnection connection,
		string tableSchema = null,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Reads foreign-key metadata from a live database. Used by the schema
	/// migrator to detect missing/extra FK constraints on tables that
	/// already exist.
	/// </summary>
	/// <param name="connection">An open database connection.</param>
	/// <param name="tableSchema">Schema name (e.g. <c>"dbo"</c> or <c>"public"</c>); dialect default when <see langword="null"/>.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>One row per FK column — compound FKs surface as multiple rows.</returns>
	Task<IReadOnlyList<DbForeignKeyInfo>> ReadDbForeignKeysAsync(
		DbConnection connection,
		string tableSchema = null,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Reads index metadata from a live database. Used by the schema
	/// migrator to detect missing entity-declared <c>[Index]</c> /
	/// <c>[Unique]</c> attributes on tables that already exist. Composite
	/// indexes are returned as multiple rows sharing
	/// <see cref="DbIndexInfo.IndexName"/> with ascending
	/// <see cref="DbIndexInfo.ColumnOrdinal"/>.
	/// </summary>
	/// <param name="connection">An open database connection.</param>
	/// <param name="tableSchema">Schema name (e.g. <c>"dbo"</c> or <c>"public"</c>); dialect default when <see langword="null"/>.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>One row per indexed column — composite indexes surface as multiple rows.</returns>
	Task<IReadOnlyList<DbIndexInfo>> ReadDbIndexesAsync(
		DbConnection connection,
		string tableSchema = null,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Reads how each table is packed on disk. Databases without the concept report every table as
	/// <see cref="DataCompressions.None"/>, which matches an entity that declares nothing.
	/// </summary>
	/// <param name="connection">An open database connection.</param>
	/// <param name="tableSchema">Schema name; dialect default when <see langword="null"/>.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>One row per table.</returns>
	Task<IReadOnlyList<DbTableCompressionInfo>> ReadDbCompressionsAsync(
		DbConnection connection,
		string tableSchema = null,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Writes the statement that repacks a table.
	/// </summary>
	/// <param name="builder">Where the statement is written.</param>
	/// <param name="tableName">Table to repack.</param>
	/// <param name="compression">The packing to apply.</param>
	void AppendSetCompression(StringBuilder builder, string tableName, DataCompressions compression);
}
