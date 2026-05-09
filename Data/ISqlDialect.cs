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
	/// Unicode string-literal prefix (e.g. <c>N</c> for SQL Server).
	/// </summary>
	string UnicodePrefix { get; }

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
	/// Converts a CLR value to the form a database driver expects.
	/// </summary>
	object ConvertToDbValue(object value, Type clrType);

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
	/// Inline FOREIGN KEY constraint clause for use inside CREATE TABLE.
	/// </summary>
	string GetForeignKeyConstraint(string tableName, string columnName, string refTableName, string refColumnName);

	/// <summary>
	/// Full column definition: SQL type + NULL/NOT NULL.
	/// </summary>
	string GetColumnDefinition(Type clrType, bool isNullable, int maxLength = 0, int precision = 0, int scale = 0);

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
	/// Appends ALTER TABLE ADD COLUMN.
	/// </summary>
	void AppendAddColumn(StringBuilder sb, string tableName, string columnName, string columnDef);

	/// <summary>
	/// Appends ALTER TABLE ALTER COLUMN.
	/// </summary>
	void AppendAlterColumn(StringBuilder sb, string tableName, string columnName, Type clrType, bool isNullable, int maxLength = 0, int precision = 0, int scale = 0);

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
}
