namespace Ecng.Data.Sql;

using System.Text;
using System.Diagnostics;

[Serializable]
[DebuggerDisplay($"{{{nameof(DebuggerString)}}}")]
public class Query
{
	private string DebuggerString => $"Query({Actions.Count} actions)";

	public IList<Action<ISqlDialect, StringBuilder>> Actions { get; } = [];

	public void CopyTo(Query destination)
		=> destination.CheckOnNull(nameof(destination)).Actions.AddRange(Actions);

	#region Render

	public virtual string Render(ISqlDialect dialect)
	{
		var builder = new StringBuilder();

		foreach (var action in Actions)
			action(dialect, builder);

		return builder.ToString();
	}

	#endregion

	public Query Case()
		=> AddAction((dialect, builder) => builder.Append("case"));

	public Query When()
		=> AddAction((dialect, builder) => builder.Append("when "));

	public Query Then()
		=> AddAction((dialect, builder) => builder.Append(" then "));

	public Query If()
		=> AddAction((dialect, builder) => builder.Append("if "));

	public Query Else()
		=> AddAction((dialect, builder) => builder.Append("else "));

	public Query Begin()
		=> AddAction((dialect, builder) => builder.Append("begin"));

	public Query End()
		=> AddAction((dialect, builder) => builder.Append("end"));

	#region Join

	public Query InnerJoin() => Raw("inner join ");

	public Query LeftJoin() => Raw("left join ");

	#endregion

	#region On

	public Query On()
		=> Raw(" on ");

	#endregion

	public Query Select()
		=> AddAction((dialect, builder) => builder.Append("select "));

	public Query Distinct()
		=> AddAction((dialect, builder) => builder.Append("distinct "));

	public Query Top(long top) => AddAction((dialect, builder) => builder.Append($"top {top} "));

	public Query Count()
		=> AddAction((dialect, builder) => builder.Append("count"));

	public Query Star()
		=> AddAction((dialect, builder) => builder.Append('*'));

	public Query All(string tableAlias)
		=> AddAction((dialect, builder) => builder.Append($"{dialect.QuoteIdentifier(tableAlias)}.*"));

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

	public Query Identity(string idCol)
		=> AddAction((dialect, builder) => builder.Append(dialect.GetIdentitySelect(idCol)));

	public Query From() => Raw("from ");

	public Query Table(string tableName, string alias)
		=> AddAction((dialect, builder) => builder.AppendFormat("{0} {1}", dialect.QuoteIdentifier(tableName), alias));

	public Query GroupBy()
		=> AddAction((dialect, builder) => builder.Append("group by "));

	public Query Having()
		=> AddAction((dialect, builder) => builder.Append("having"));

	public Query NewLine()
		=> AddAction((dialect, builder) => builder.AppendLine());

	#region Keywords

	public Query In()
		=> AddAction((dialect, builder) => builder.Append(" in "));

	public Query Where()
		=> AddAction((dialect, builder) => builder.Append("where"));

	public Query BitwiseAnd()
		=> AddAction((dialect, builder) => builder.Append(" & "));

	public Query BitwiseOr()
		=> AddAction((dialect, builder) => builder.Append(" | "));

	public Query And()
		=> AddAction((dialect, builder) => builder.Append(" and "));

	public Query Or()
		=> AddAction((dialect, builder) => builder.Append(" or "));

	public Query Is()
		=> AddAction((dialect, builder) => builder.Append(" is "));

	public Query IsNot()
		=> AddAction((dialect, builder) => builder.Append(" is not "));

	public Query Not()
		=> AddAction((dialect, builder) => builder.Append("not "));

	public Query Equal()
		=> AddAction((dialect, builder) => builder.Append(" = "));

	public Query NotEqual()
		=> AddAction((dialect, builder) => builder.Append(" <> "));

	public Query Less()
		=> AddAction((dialect, builder) => builder.Append(" < "));

	public Query LessOrEqual()
		=> AddAction((dialect, builder) => builder.Append(" <= "));

	public Query More()
		=> AddAction((dialect, builder) => builder.Append(" > "));

	public Query MoreOrEqual()
		=> AddAction((dialect, builder) => builder.Append(" >= "));

	public Query Null()
		=> AddAction((dialect, builder) => builder.Append("null"));

	public Queue<Action<bool, Query>> WrapColumn;

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

	public Query Column(string column)
		=> AddAction((dialect, builder) => builder.Append(dialect.QuoteIdentifier(column)));

	public Query Len()
		=> Raw("len");

	public Query DataLength()
		=> Raw("dataLength");

	public Query With()
		=> Raw("with ");

	public Query Comma()
		=> Raw(", ");

	public Query Plus()
		=> Raw(" + ");

	public Query IsTrue()
		=> Raw(" = 1");

	public Query IsFalse()
		=> Raw(" = 0");

	public Query Cast()
		=> Raw("cast");

	public Query Date()
		=> Raw("date");

	public Query Raw(string name)
		=> AddAction((dialect, builder) => builder.Append(name));

	public Query Avg()
		=> Raw("avg");

	public Query Sum()
		=> Raw("sum");

	public Query Convert()
		=> Raw("convert");

	public Query Now()
		=> AddAction((dialect, builder) => builder.Append(dialect.Now()));

	public Query UtcNow()
		=> AddAction((dialect, builder) => builder.Append(dialect.UtcNow()));

	public Query SysNow()
		=> AddAction((dialect, builder) => builder.Append(dialect.SysNow()));

	public Query SysUtcNow()
		=> AddAction((dialect, builder) => builder.Append(dialect.SysUtcNow()));

	public Query Max()
		=> Raw("max");

	public Query Min()
		=> Raw("min");

	public Query DateAdd()
		=> Raw("dateAdd");

	public Query DateDiff()
		=> Raw("dateDiff");

	public Query DatePart()
		=> Raw("datePart");

	public Query Upper()
		=> Raw("Upper");

	public Query Lower()
		=> Raw("Lower");

	public Query LTrim()
		=> Raw("LTrim");

	public Query RTrim()
		=> Raw("RTrim");

	public Query SubString()
		=> Raw("SubString");

	public Query NewId()
		=> AddAction((dialect, builder) => builder.Append(dialect.NewId()));

	public Query Rand()
		=> Raw("rand()");

	public Query RowNumber()
		=> Raw("row_number()");

	public Query Over()
		=> Raw("over");

	public Query CharIndex()
		=> Raw("charIndex");

	public Query Replace()
		=> Raw("replace");

	public Query Exists()
		=> Raw("exists");

	public Query Between(string low, string high)
		=> AddAction((dialect, builder) => builder.Append($" between {low} and {high}"));

	public Query OpenBracket()
		=> AddAction((dialect, builder) => builder.Append('('));

	public Query CloseBracket()
		=> AddAction((dialect, builder) => builder.Append(')'));

	public Query Like(string columnName)
		=> AddAction((dialect, builder) => builder.Append($"{dialect.QuoteIdentifier(columnName)} like {dialect.ParameterPrefix}{columnName}"));

	#endregion

	#region IsNull

	public Query IsNull(string columnName)
	{
		return AddAction((dialect, builder) => builder.Append($"{dialect.QuoteIdentifier(columnName)} is null"));
	}

	public Query IsParamNull(string columnName)
	{
		return AddAction((dialect, builder) => builder.Append($"{dialect.ParameterPrefix}{columnName} is null"));
	}

	#endregion

	internal Query AddAction(Action<ISqlDialect, StringBuilder> action)
	{
		ArgumentNullException.ThrowIfNull(action);

		Actions.Add(action);
		return this;
	}

	#region Insert

	public Query Insert()
		=> AddAction((dialect, builder) => builder.Append("insert"));

	#endregion

	#region Into

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

	public Query OrderBy() => Raw("order by ");

	public Query Asc() => Raw(" asc");
	public Query Desc() => Raw(" desc");

	public Query Skip(string skip)
		=> AddAction((dialect, builder) => builder.AppendLine(dialect.FormatSkip(dialect.ParameterPrefix + skip)));

	public Query Take(string take)
		=> AddAction((dialect, builder) => builder.AppendLine(dialect.FormatTake(dialect.ParameterPrefix + take)));

	#region Values

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

	public Query Delete()
		=> AddAction((dialect, builder) => builder.Append("delete"));

	#endregion

	#region Update

	public Query Update(string tableName)
	{
		return AddAction((dialect, builder) =>
			builder
					.AppendFormat("update {0}", dialect.QuoteIdentifier(tableName))
					.AppendLine());
	}

	#endregion

	#region Set

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

		builder.RemoveLast(3);
		builder.AppendLine();
	}

	#endregion

	#region Equals

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

	public Query NullIf() => AddAction((dialect, builder) => builder.Append("nullif"));
	public Query IsNull() => AddAction((dialect, builder) => builder.Append("isnull"));

	public Query Like() => AddAction((dialect, builder) => builder.Append(" like "));

	public Query As() => AddAction((dialect, builder) => builder.Append(" as "));

	public Query Param(string name) => AddAction((dialect, builder) => builder.Append(dialect.ParameterPrefix + name));

	public Query Union() => AddAction((dialect, builder) => builder.Append("union"));
	public Query UnionAll() => AddAction((dialect, builder) => builder.Append("union all"));

	public Query FormatMessage() => AddAction((dialect, builder) => builder.Append("formatmessage"));
}

public class SetPart(string column, string valueName)
{
	private readonly string _column = column;
	public string Column => _column;

	private readonly string _valueName = valueName;
	public string ValueName => _valueName;

	public override string ToString() => $"{Column}={ValueName}";
}

[Serializable]
public class BatchQuery : Query
{
	public List<Query> Queries { get; } = [];

	public override string Render(ISqlDialect dialect)
	{
		var retVal = new StringBuilder();

		foreach (var query in Queries)
			retVal.AppendLine(query.Render(dialect)).AppendLine();

		return retVal.ToString();
	}
}
