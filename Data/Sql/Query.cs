namespace Ecng.Data.Sql;

using System.Linq;
using System.Diagnostics;

using Ecng.Collections;
using Ecng.Common;

/// <summary>
/// Represents a composable SQL query built from a sequence of render actions.
/// </summary>
[Serializable]
[DebuggerDisplay($"{{{nameof(DebuggerString)}}}")]
public class Query
{
	private string DebuggerString => $"Query({Actions.Count} actions)";

	/// <summary>
	/// List of render actions that produce the SQL text.
	/// </summary>
	public IList<Action<ISqlDialect, StringBuilder>> Actions { get; } = [];

	/// <summary>
	/// Copies all actions from this query to the <paramref name="destination"/> query.
	/// </summary>
	/// <param name="destination">Target query to copy actions into.</param>
	public void CopyTo(Query destination)
		=> destination.CheckOnNull(nameof(destination)).Actions.AddRange(Actions);

	#region Render

	/// <summary>
	/// Renders the query to a SQL string using the specified dialect.
	/// </summary>
	/// <param name="dialect">SQL dialect used for quoting and formatting.</param>
	/// <returns>Rendered SQL string.</returns>
	public virtual string Render(ISqlDialect dialect)
	{
		var builder = new StringBuilder();

		foreach (var action in Actions)
			action(dialect, builder);

		return builder.ToString();
	}

	#endregion

	/// <summary>
	/// Appends CASE keyword.
	/// </summary>
	public Query Case()
		=> AddAction((dialect, builder) => builder.Append("case"));

	/// <summary>
	/// Appends WHEN keyword.
	/// </summary>
	public Query When()
		=> AddAction((dialect, builder) => builder.Append("when "));

	/// <summary>
	/// Appends THEN keyword.
	/// </summary>
	public Query Then()
		=> AddAction((dialect, builder) => builder.Append(" then "));

	/// <summary>
	/// Appends IF keyword.
	/// </summary>
	public Query If()
		=> AddAction((dialect, builder) => builder.Append("if "));

	/// <summary>
	/// Appends ELSE keyword.
	/// </summary>
	public Query Else()
		=> AddAction((dialect, builder) => builder.Append("else "));

	/// <summary>
	/// Appends BEGIN keyword.
	/// </summary>
	public Query Begin()
		=> AddAction((dialect, builder) => builder.Append("begin"));

	/// <summary>
	/// Appends END keyword.
	/// </summary>
	public Query End()
		=> AddAction((dialect, builder) => builder.Append("end"));

	#region Join

	/// <summary>
	/// Appends INNER JOIN keyword.
	/// </summary>
	public Query InnerJoin() => Raw("inner join ");

	/// <summary>
	/// Appends LEFT JOIN keyword.
	/// </summary>
	public Query LeftJoin() => Raw("left join ");

	#endregion

	#region On

	/// <summary>
	/// Appends ON keyword.
	/// </summary>
	public Query On()
		=> Raw(" on ");

	#endregion

	/// <summary>
	/// Appends SELECT keyword.
	/// </summary>
	public Query Select()
		=> AddAction((dialect, builder) => builder.Append("select "));

	/// <summary>
	/// Appends DISTINCT keyword.
	/// </summary>
	public Query Distinct()
		=> AddAction((dialect, builder) => builder.Append("distinct "));

	/// <summary>
	/// Appends TOP clause with the specified row count.
	/// </summary>
	/// <param name="top">Maximum number of rows.</param>
	public Query Top(long top) => AddAction((dialect, builder) => builder.Append($"top {top} "));

	/// <summary>
	/// Appends COUNT keyword.
	/// </summary>
	public Query Count()
		=> AddAction((dialect, builder) => builder.Append("count"));

	/// <summary>
	/// Appends the * wildcard.
	/// </summary>
	public Query Star()
		=> AddAction((dialect, builder) => builder.Append('*'));

	/// <summary>
	/// Appends alias.* to select all columns from a table alias.
	/// </summary>
	/// <param name="tableAlias">Table alias.</param>
	public Query All(string tableAlias)
		=> AddAction((dialect, builder) => builder.Append($"{dialect.QuoteIdentifier(tableAlias)}.*"));

	/// <summary>
	/// Appends a comma-separated list of specific columns from a table alias.
	/// </summary>
	/// <param name="tableAlias">Table alias.</param>
	/// <param name="columns">Column names to include.</param>
	public Query Exact(string tableAlias, params string[] columns)
	{
		return AddAction((dialect, builder) =>
		{
			foreach (var column in columns)
				builder.AppendFormat("{0}.{1}, ", tableAlias, dialect.QuoteIdentifier(column));

			if (columns.Length > 0)
				builder.RemoveLast(2);
		});
	}

	/// <summary>
	/// Appends the dialect-specific identity select expression.
	/// </summary>
	/// <param name="idCol">Identity column name.</param>
	public Query Identity(string idCol)
		=> AddAction((dialect, builder) => builder.Append(dialect.GetIdentitySelect(idCol)));

	/// <summary>
	/// Appends FROM keyword.
	/// </summary>
	public Query From() => Raw("from ");

	/// <summary>
	/// Appends a quoted table name followed by its alias.
	/// </summary>
	/// <param name="tableName">Table name.</param>
	/// <param name="alias">Table alias.</param>
	public Query Table(string tableName, string alias)
		=> AddAction((dialect, builder) =>
		{
			builder.Append(dialect.QuoteIdentifier(tableName));
			if (!alias.IsEmpty())
				builder.Append(' ').Append(alias);
		});

	/// <summary>
	/// Appends GROUP BY keyword.
	/// </summary>
	public Query GroupBy()
		=> AddAction((dialect, builder) => builder.Append("group by "));

	/// <summary>
	/// Appends HAVING keyword.
	/// </summary>
	public Query Having()
		=> AddAction((dialect, builder) => builder.Append("having"));

	/// <summary>
	/// Appends a new line.
	/// </summary>
	public Query NewLine()
		=> AddAction((dialect, builder) => builder.AppendLine());

	#region Keywords

	/// <summary>
	/// Appends IN keyword.
	/// </summary>
	public Query In()
		=> AddAction((dialect, builder) => builder.Append(" in "));

	/// <summary>
	/// Appends WHERE keyword.
	/// </summary>
	public Query Where()
		=> AddAction((dialect, builder) => builder.Append("where"));

	/// <summary>
	/// Appends bitwise AND operator.
	/// </summary>
	public Query BitwiseAnd()
		=> AddAction((dialect, builder) => builder.Append(" & "));

	/// <summary>
	/// Appends bitwise OR operator.
	/// </summary>
	public Query BitwiseOr()
		=> AddAction((dialect, builder) => builder.Append(" | "));

	/// <summary>
	/// Appends AND keyword.
	/// </summary>
	public Query And()
		=> AddAction((dialect, builder) => builder.Append(" and "));

	/// <summary>
	/// Appends OR keyword.
	/// </summary>
	public Query Or()
		=> AddAction((dialect, builder) => builder.Append(" or "));

	/// <summary>
	/// Appends IS keyword.
	/// </summary>
	public Query Is()
		=> AddAction((dialect, builder) => builder.Append(" is "));

	/// <summary>
	/// Appends IS NOT keyword.
	/// </summary>
	public Query IsNot()
		=> AddAction((dialect, builder) => builder.Append(" is not "));

	/// <summary>
	/// Appends NOT keyword.
	/// </summary>
	public Query Not()
		=> AddAction((dialect, builder) => builder.Append("not "));

	/// <summary>
	/// Appends = operator.
	/// </summary>
	public Query Equal()
		=> AddAction((dialect, builder) => builder.Append(" = "));

	/// <summary>
	/// Appends &lt;&gt; operator.
	/// </summary>
	public Query NotEqual()
		=> AddAction((dialect, builder) => builder.Append(" <> "));

	/// <summary>
	/// Appends &lt; operator.
	/// </summary>
	public Query Less()
		=> AddAction((dialect, builder) => builder.Append(" < "));

	/// <summary>
	/// Appends &lt;= operator.
	/// </summary>
	public Query LessOrEqual()
		=> AddAction((dialect, builder) => builder.Append(" <= "));

	/// <summary>
	/// Appends &gt; operator.
	/// </summary>
	public Query More()
		=> AddAction((dialect, builder) => builder.Append(" > "));

	/// <summary>
	/// Appends &gt;= operator.
	/// </summary>
	public Query MoreOrEqual()
		=> AddAction((dialect, builder) => builder.Append(" >= "));

	/// <summary>
	/// Appends NULL keyword.
	/// </summary>
	public Query Null()
		=> AddAction((dialect, builder) => builder.Append("null"));

	/// <summary>
	/// Queue of wrapping actions applied around column references.
	/// </summary>
	public Queue<Action<bool, Query>> WrapColumn;

	/// <summary>
	/// Appends a qualified column reference (owner.column).
	/// </summary>
	/// <param name="owner">Table alias or owner name.</param>
	/// <param name="column">Column name.</param>
	public Query Column(string owner, string column)
	{
		var actions = WrapColumn?.ToArray();

		if (actions is not null)
		{
			foreach (var action in actions)
				action(true, this);
		}

		AddAction((dialect, builder) =>
			builder
				.Append(dialect.QuoteIdentifier(owner))
				.Append('.')
				.Append(dialect.QuoteIdentifier(column)));

		if (actions is not null)
		{
			foreach (var action in Enumerable.Reverse(actions))
				action(false, this);
		}

		return this;
	}

	/// <summary>
	/// Appends a quoted column reference.
	/// </summary>
	/// <param name="column">Column name.</param>
	public Query Column(string column)
		=> AddAction((dialect, builder) => builder.Append(dialect.QuoteIdentifier(column)));

	/// <summary>
	/// Appends dialect-specific length function (LEN for SQL Server, LENGTH for PostgreSQL/SQLite).
	/// </summary>
	public Query Len()
		=> AddAction((dialect, builder) => builder.Append(dialect.LenFunction));

	/// <summary>
	/// Appends DATALENGTH function name.
	/// </summary>
	public Query DataLength()
		=> Raw("dataLength");

	/// <summary>
	/// Appends WITH keyword.
	/// </summary>
	public Query With()
		=> Raw("with ");

	/// <summary>
	/// Appends a comma separator.
	/// </summary>
	public Query Comma()
		=> Raw(", ");

	/// <summary>
	/// Appends + arithmetic operator.
	/// </summary>
	public Query Plus()
		=> Raw(" + ");

	/// <summary>
	/// Appends dialect-specific string concatenation operator (+ for SQL Server, || for PostgreSQL/SQLite).
	/// </summary>
	public Query Concat()
		=> AddAction((dialect, builder) => builder.Append($" {dialect.ConcatOperator} "));

	/// <summary>
	/// Appends dialect-specific boolean true comparison (= 1 for SQL Server/SQLite, = TRUE for PostgreSQL).
	/// </summary>
	public Query IsTrue()
		=> AddAction((dialect, builder) => builder.Append($" = {dialect.TrueLiteral}"));

	/// <summary>
	/// Appends dialect-specific boolean false comparison (= 0 for SQL Server/SQLite, = FALSE for PostgreSQL).
	/// </summary>
	public Query IsFalse()
		=> AddAction((dialect, builder) => builder.Append($" = {dialect.FalseLiteral}"));

	/// <summary>
	/// Appends CAST function name.
	/// </summary>
	public Query Cast()
		=> Raw("cast");

	/// <summary>
	/// Appends DATE type name.
	/// </summary>
	public Query Date()
		=> Raw("date");

	/// <summary>
	/// Appends a raw SQL string.
	/// </summary>
	/// <param name="name">Raw SQL text to append.</param>
	public Query Raw(string name)
		=> AddAction((dialect, builder) => builder.Append(name));

	/// <summary>
	/// Appends AVG function name.
	/// </summary>
	public Query Avg()
		=> Raw("avg");

	/// <summary>
	/// Appends SUM function name.
	/// </summary>
	public Query Sum()
		=> Raw("sum");

	/// <summary>
	/// Appends CONVERT function name.
	/// </summary>
	public Query Convert()
		=> Raw("convert");

	/// <summary>
	/// Appends dialect-specific NOW() expression.
	/// </summary>
	public Query Now()
		=> AddAction((dialect, builder) => builder.Append(dialect.Now()));

	/// <summary>
	/// Appends dialect-specific UTC NOW() expression.
	/// </summary>
	public Query UtcNow()
		=> AddAction((dialect, builder) => builder.Append(dialect.UtcNow()));

	/// <summary>
	/// Appends dialect-specific SYSDATETIME() expression.
	/// </summary>
	public Query SysNow()
		=> AddAction((dialect, builder) => builder.Append(dialect.SysNow()));

	/// <summary>
	/// Appends dialect-specific SYSUTCDATETIME() expression.
	/// </summary>
	public Query SysUtcNow()
		=> AddAction((dialect, builder) => builder.Append(dialect.SysUtcNow()));

	/// <summary>
	/// Appends MAX function name.
	/// </summary>
	public Query Max()
		=> Raw("max");

	/// <summary>
	/// Appends MIN function name.
	/// </summary>
	public Query Min()
		=> Raw("min");

	/// <summary>
	/// Appends DATEADD function name.
	/// </summary>
	public Query DateAdd()
		=> Raw("dateAdd");

	/// <summary>
	/// Appends DATEDIFF function name.
	/// </summary>
	public Query DateDiff()
		=> Raw("dateDiff");

	/// <summary>
	/// Appends DATEPART function name.
	/// </summary>
	public Query DatePart()
		=> Raw("datePart");

	/// <summary>
	/// Appends UPPER function name.
	/// </summary>
	public Query Upper()
		=> Raw("Upper");

	/// <summary>
	/// Appends LOWER function name.
	/// </summary>
	public Query Lower()
		=> Raw("Lower");

	/// <summary>
	/// Appends LTRIM function name.
	/// </summary>
	public Query LTrim()
		=> Raw("LTrim");

	/// <summary>
	/// Appends RTRIM function name.
	/// </summary>
	public Query RTrim()
		=> Raw("RTrim");

	/// <summary>
	/// Appends SUBSTRING function name.
	/// </summary>
	public Query SubString()
		=> Raw("SubString");

	/// <summary>
	/// Appends dialect-specific NEWID() expression.
	/// </summary>
	public Query NewId()
		=> AddAction((dialect, builder) => builder.Append(dialect.NewId()));

	/// <summary>
	/// Appends RAND() function call.
	/// </summary>
	public Query Rand()
		=> Raw("rand()");

	/// <summary>
	/// Appends ROW_NUMBER() function call.
	/// </summary>
	public Query RowNumber()
		=> Raw("row_number()");

	/// <summary>
	/// Appends OVER keyword.
	/// </summary>
	public Query Over()
		=> Raw("over");

	/// <summary>
	/// Appends CHARINDEX function name.
	/// </summary>
	public Query CharIndex()
		=> Raw("charIndex");

	/// <summary>
	/// Appends REPLACE function name.
	/// </summary>
	public Query Replace()
		=> Raw("replace");

	/// <summary>
	/// Appends EXISTS keyword.
	/// </summary>
	public Query Exists()
		=> Raw("exists");

	/// <summary>
	/// Appends a BETWEEN clause with the specified bounds.
	/// </summary>
	/// <param name="low">Lower bound parameter name.</param>
	/// <param name="high">Upper bound parameter name.</param>
	public Query Between(string low, string high)
		=> AddAction((dialect, builder) => builder.Append($" between {low} and {high}"));

	/// <summary>
	/// Appends an opening parenthesis.
	/// </summary>
	public Query OpenBracket()
		=> AddAction((dialect, builder) => builder.Append('('));

	/// <summary>
	/// Appends a closing parenthesis.
	/// </summary>
	public Query CloseBracket()
		=> AddAction((dialect, builder) => builder.Append(')'));

	/// <summary>
	/// Appends a LIKE condition for the specified column with a matching parameter.
	/// </summary>
	/// <param name="columnName">Column name to match.</param>
	public Query Like(string columnName)
		=> AddAction((dialect, builder) => builder.Append($"{dialect.QuoteIdentifier(columnName)} like {dialect.ParameterPrefix}{columnName}"));

	#endregion

	#region IsNull

	/// <summary>
	/// Appends a column IS NULL condition.
	/// </summary>
	/// <param name="columnName">Column name to check.</param>
	public Query IsNull(string columnName)
	{
		return AddAction((dialect, builder) => builder.Append($"{dialect.QuoteIdentifier(columnName)} is null"));
	}

	/// <summary>
	/// Appends a parameter IS NULL condition.
	/// </summary>
	/// <param name="columnName">Parameter name to check.</param>
	public Query IsParamNull(string columnName)
	{
		return AddAction((dialect, builder) => builder.Append($"{dialect.ParameterPrefix}{columnName} is null"));
	}

	#endregion

	/// <summary>
	/// Adds a render action to the query.
	/// </summary>
	/// <param name="action">Action that appends SQL text to a <see cref="StringBuilder"/>.</param>
	public Query AddAction(Action<ISqlDialect, StringBuilder> action)
	{
		ArgumentNullException.ThrowIfNull(action);

		Actions.Add(action);
		return this;
	}

	#region Insert

	/// <summary>
	/// Appends INSERT keyword.
	/// </summary>
	public Query Insert()
		=> AddAction((dialect, builder) => builder.Append("insert"));

	#endregion

	#region Into

	/// <summary>
	/// Appends INTO clause with table name and column list.
	/// </summary>
	/// <param name="tableName">Target table name.</param>
	/// <param name="columns">Column names for the insert.</param>
	public Query Into(string tableName, params string[] columns)
	{
		return AddAction((dialect, builder) =>
		{
			builder.AppendFormat(" into {0}", dialect.QuoteIdentifier(tableName));
			builder.AppendLine().Append('(');

			foreach (var column in columns)
				builder.AppendFormat("{0}, ", dialect.QuoteIdentifier(column));

			if (columns.Length > 0)
				builder.RemoveLast(2);

			builder.AppendLine(")");
		});
	}

	#endregion

	/// <summary>
	/// Appends ORDER BY keyword.
	/// </summary>
	public Query OrderBy() => Raw("order by ");

	/// <summary>
	/// Appends ASC keyword.
	/// </summary>
	public Query Asc() => Raw(" asc");

	/// <summary>
	/// Appends DESC keyword.
	/// </summary>
	public Query Desc() => Raw(" desc");

	/// <summary>
	/// Appends dialect-specific SKIP (OFFSET) clause.
	/// </summary>
	/// <param name="skip">Parameter name for the number of rows to skip.</param>
	public Query Skip(string skip)
		=> AddAction((dialect, builder) => builder.AppendLine(dialect.FormatSkip(dialect.ParameterPrefix + skip)));

	/// <summary>
	/// Appends dialect-specific TAKE (LIMIT/FETCH) clause.
	/// </summary>
	/// <param name="take">Parameter name for the number of rows to take.</param>
	public Query Take(string take)
		=> AddAction((dialect, builder) => builder.AppendLine(dialect.FormatTake(dialect.ParameterPrefix + take)));

	#region Values

	/// <summary>
	/// Appends a VALUES clause with parameterized value placeholders.
	/// </summary>
	/// <param name="valueNames">Parameter names for the values.</param>
	public Query Values(params string[] valueNames)
	{
		return AddAction((dialect, builder) =>
		{
			builder
				.Append("values")
				.AppendLine()
				.Append('(');

			foreach (var valueName in valueNames)
				builder.AppendFormat("{0}, ", dialect.ParameterPrefix + valueName);

			if (!valueNames.IsEmpty())
				builder.RemoveLast(2);

			builder.Append(')');
		});
	}

	#endregion

	#region Delete

	/// <summary>
	/// Appends DELETE keyword.
	/// </summary>
	public Query Delete()
		=> AddAction((dialect, builder) => builder.Append("delete"));

	#endregion

	#region Update

	/// <summary>
	/// Appends UPDATE keyword with the specified table name.
	/// </summary>
	/// <param name="tableName">Table name to update.</param>
	public Query Update(string tableName)
	{
		return AddAction((dialect, builder) =>
			builder
					.AppendFormat("update {0}", dialect.QuoteIdentifier(tableName))
					.AppendLine());
	}

	#endregion

	#region Set

	/// <summary>
	/// Appends a SET clause assigning parameterized values to columns.
	/// </summary>
	/// <param name="tableAlias">Table alias used in the assignment.</param>
	/// <param name="columns">Column names to set.</param>
	public Query Set(string tableAlias, params string[] columns)
	{
		return AddAction((dialect, builder) =>
		{
			var parts = columns.Select(c => new SetPart(c, dialect.ParameterPrefix + c)).ToArray();
			Set(tableAlias, parts, dialect, builder);
		});
	}

	private static void Set(string tableAlias, ICollection<SetPart> parts, ISqlDialect dialect, StringBuilder builder)
	{
		if (parts.IsEmpty())
			throw new ArgumentOutOfRangeException(nameof(parts));

		builder.AppendLine("set");

		foreach (var part in parts)
		{
			builder
					.AppendFormat("\t{0}.{1} = {2},", tableAlias, dialect.QuoteIdentifier(part.Column), part.ValueName)
					.AppendLine();
		}

		builder.RemoveLast(1 + Environment.NewLine.Length); // remove trailing comma + newline
		builder.AppendLine();
	}

	#endregion

	#region Equals

	/// <summary>
	/// Appends equality conditions for the specified columns joined with AND.
	/// </summary>
	/// <param name="tableAlias">Table alias used in column references.</param>
	/// <param name="columns">Column names to compare against parameters.</param>
	public Query Equals(string tableAlias, params string[] columns)
	{
		return AddAction((dialect, builder) =>
		{
			for (var i = 0; i < columns.Length; i++)
			{
				Equals($"{tableAlias}.{dialect.QuoteIdentifier(columns[i])}", dialect.ParameterPrefix + columns[i], builder);

				if (i < columns.Length - 1)
					builder.AppendFormat(" and ");
			}
		});
	}

	private static void Equals(string column, string valueName, StringBuilder builder)
	{
		builder.AppendFormat("{0} = {1}", column, valueName);
	}

	#endregion

	/// <summary>
	/// Appends NULLIF function name.
	/// </summary>
	public Query NullIf() => AddAction((dialect, builder) => builder.Append("nullif"));

	/// <summary>
	/// Appends dialect-aware null-coalescing function name (ISNULL for SQL Server, COALESCE for PG/SQLite).
	/// </summary>
	public Query IsNull() => AddAction((dialect, builder) => builder.Append(dialect.IsNullFunction));

	/// <summary>
	/// Appends LIKE keyword.
	/// </summary>
	public Query Like() => AddAction((dialect, builder) => builder.Append(" like "));

	/// <summary>
	/// Appends AS keyword.
	/// </summary>
	public Query As() => AddAction((dialect, builder) => builder.Append(" as "));

	/// <summary>
	/// Appends a dialect-prefixed parameter reference.
	/// </summary>
	/// <param name="name">Parameter name.</param>
	public Query Param(string name) => AddAction((dialect, builder) => builder.Append(dialect.ParameterPrefix + name));

	/// <summary>
	/// Appends UNION keyword.
	/// </summary>
	public Query Union() => AddAction((dialect, builder) => builder.Append("union"));

	/// <summary>
	/// Appends UNION ALL keyword.
	/// </summary>
	public Query UnionAll() => AddAction((dialect, builder) => builder.Append("union all"));

	/// <summary>
	/// Appends FORMATMESSAGE function name.
	/// </summary>
	public Query FormatMessage() => AddAction((dialect, builder) => builder.Append("formatmessage"));

	#region Static Factories

	/// <summary>
	/// Creates an INSERT query.
	/// </summary>
	public static Query CreateInsert(string tableName, IEnumerable<string> columns)
	{
		var cols = columns.ToArray();
		return new Query().AddAction((dialect, sb) =>
		{
			var quotedCols = cols.Select(dialect.QuoteIdentifier);
			var paramCols = cols.Select(c => dialect.ParameterPrefix + c);
			sb.Append($"INSERT INTO {dialect.QuoteIdentifier(tableName)} ({quotedCols.JoinCommaSpace()}) VALUES ({paramCols.JoinCommaSpace()})");
		});
	}

	/// <summary>
	/// Creates an UPDATE query.
	/// </summary>
	public static Query CreateUpdate(string tableName, IEnumerable<string> columns, string whereClause)
	{
		var cols = columns.ToArray();
		return new Query().AddAction((dialect, sb) =>
		{
			var setClauses = cols.Select(c => $"{dialect.QuoteIdentifier(c)} = {dialect.ParameterPrefix}{c}");
			sb.Append($"UPDATE {dialect.QuoteIdentifier(tableName)} SET {setClauses.JoinCommaSpace()}");

			if (!whereClause.IsEmpty())
				sb.Append($" WHERE {whereClause}");
		});
	}

	/// <summary>
	/// Creates a DELETE query.
	/// </summary>
	public static Query CreateDelete(string tableName, string whereClause)
	{
		return new Query().AddAction((dialect, sb) =>
		{
			sb.Append($"DELETE FROM {dialect.QuoteIdentifier(tableName)}");

			if (!whereClause.IsEmpty())
				sb.Append($" WHERE {whereClause}");
		});
	}

	/// <summary>
	/// Creates a SELECT query with optional pagination.
	/// </summary>
	public static Query CreateSelect(string tableName, string whereClause, string orderByClause, long? skip, long? take)
	{
		return new Query().AddAction((dialect, sb) =>
		{
			sb.Append($"SELECT * FROM {dialect.QuoteIdentifier(tableName)}");

			if (!whereClause.IsEmpty())
				sb.Append($" WHERE {whereClause}");

			if (!orderByClause.IsEmpty())
				sb.Append($" ORDER BY {orderByClause}");

			dialect.AppendPagination(sb, skip, take, !orderByClause.IsEmpty());
		});
	}

	/// <summary>
	/// Creates a CREATE TABLE query.
	/// </summary>
	/// <param name="tableName">Table name.</param>
	/// <param name="columns">Column definitions (name → CLR type).</param>
	/// <param name="identityColumn">Optional auto-increment identity column name.</param>
	/// <param name="primaryKeyColumns">Optional primary key column(s) (without auto-increment). Ignored when <paramref name="identityColumn"/> is set.</param>
	public static Query CreateCreateTable(string tableName, IDictionary<string, Type> columns, string identityColumn = null, IEnumerable<string> primaryKeyColumns = null)
	{
		return new Query().AddAction((dialect, sb) =>
		{
			var colDefs = columns.Select(kv =>
			{
				var def = $"{dialect.QuoteIdentifier(kv.Key)} {dialect.GetSqlTypeName(kv.Value)}";
				if (identityColumn is not null && kv.Key.EqualsIgnoreCase(identityColumn))
					def += " " + (kv.Value.IsNumeric() ? dialect.GetIdentityColumnSuffix() : "PRIMARY KEY");
				return def;
			}).JoinCommaSpace();

			var pkCols = identityColumn is null ? primaryKeyColumns?.ToArray() : null;
			if (pkCols is not null && pkCols.Length > 0)
				colDefs += ", PRIMARY KEY (" + pkCols.Select(dialect.QuoteIdentifier).JoinCommaSpace() + ")";

			dialect.AppendCreateTable(sb, tableName, colDefs);
		});
	}

	/// <summary>
	/// Creates a DROP TABLE query.
	/// </summary>
	public static Query CreateDropTable(string tableName)
	{
		return new Query().AddAction((dialect, sb) =>
		{
			dialect.AppendDropTable(sb, tableName);
		});
	}

	/// <summary>
	/// Creates an UPSERT (INSERT or UPDATE) query.
	/// </summary>
	public static Query CreateUpsert(string tableName, IEnumerable<string> allColumns, IEnumerable<string> keyColumns)
	{
		var allCols = allColumns.ToArray();
		var keyCols = keyColumns.ToArray();
		return new Query().AddAction((dialect, sb) =>
		{
			dialect.AppendUpsert(sb, tableName, allCols, keyCols);
		});
	}

	/// <summary>
	/// Creates a single WHERE condition.
	/// </summary>
	public static Query CreateBuildCondition(string column, ComparisonOperator op, string paramName)
	{
		return new Query().AddAction((dialect, sb) =>
		{
			if (op == ComparisonOperator.Any)
			{
				sb.Append("1 = 1");
				return;
			}

			var quotedCol = dialect.QuoteIdentifier(column);

			if (paramName is null)
			{
				sb.Append(op switch
				{
					ComparisonOperator.Equal => $"{quotedCol} IS NULL",
					ComparisonOperator.NotEqual => $"{quotedCol} IS NOT NULL",
					_ => throw new ArgumentOutOfRangeException(nameof(op), op, "Only Equal and NotEqual are supported with NULL"),
				});
				return;
			}

			var param = dialect.ParameterPrefix + paramName;

			sb.Append(op switch
			{
				ComparisonOperator.Equal => $"{quotedCol} = {param}",
				ComparisonOperator.NotEqual => $"{quotedCol} <> {param}",
				ComparisonOperator.Greater => $"{quotedCol} > {param}",
				ComparisonOperator.GreaterOrEqual => $"{quotedCol} >= {param}",
				ComparisonOperator.Less => $"{quotedCol} < {param}",
				ComparisonOperator.LessOrEqual => $"{quotedCol} <= {param}",
				ComparisonOperator.Like => $"{quotedCol} LIKE {param}",
				ComparisonOperator.In => $"{quotedCol} IN ({param})",
				_ => throw new ArgumentOutOfRangeException(nameof(op), op, "Unsupported operator"),
			});
		});
	}

	/// <summary>
	/// Creates an IN condition.
	/// </summary>
	public static Query CreateBuildInCondition(string column, IEnumerable<string> paramNames)
	{
		var names = paramNames.ToArray();
		return new Query().AddAction((dialect, sb) =>
		{
			if (names.Length == 0)
			{
				sb.Append("1 = 0");
				return;
			}

			var quotedCol = dialect.QuoteIdentifier(column);
			var paramList = names.Select(p => dialect.ParameterPrefix + p).JoinCommaSpace();
			sb.Append($"{quotedCol} IN ({paramList})");
		});
	}

	#endregion
}

/// <summary>
/// Represents a column = value assignment in a SET clause.
/// </summary>
/// <param name="column">Column name.</param>
/// <param name="valueName">Value or parameter name.</param>
public class SetPart(string column, string valueName)
{
	private readonly string _column = column;

	/// <summary>
	/// Column name for the assignment.
	/// </summary>
	public string Column => _column;

	private readonly string _valueName = valueName;

	/// <summary>
	/// Value or parameter name for the assignment.
	/// </summary>
	public string ValueName => _valueName;

	/// <inheritdoc />
	public override string ToString() => $"{Column}={ValueName}";
}

/// <summary>
/// A query that renders multiple child queries separated by new lines.
/// </summary>
[Serializable]
public class BatchQuery : Query
{
	/// <summary>
	/// List of child queries to render as a batch.
	/// </summary>
	public List<Query> Queries { get; } = [];

	/// <inheritdoc />
	public override string Render(ISqlDialect dialect)
	{
		var retVal = new StringBuilder();

		foreach (var query in Queries)
			retVal.Append(query.Render(dialect)).AppendLine(";").AppendLine();

		return retVal.ToString();
	}
}
