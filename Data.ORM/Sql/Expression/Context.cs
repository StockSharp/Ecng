namespace Ecng.Data.Sql;

class ContextSelectColumns : Dictionary<MemberInfo, (Query, Expression)>
{
	public readonly Dictionary<MemberInfo, MemberInfo> Map = [];
}

class Context
{
	public (string origAlias, string modifiedAlias) CurrJoinAlias;
	public readonly List<(string tableAlias, Query query)> JoinParts = [];

	/// <summary>
	/// Names of join inner aliases discovered by a pre-walk of the source
	/// chain. Used to resolve transparent-id chains during projection-time
	/// visits — before <see cref="JoinParts"/> itself is populated by the
	/// recursive source visit at the end of <see cref="ExpressionQueryTranslator.VisitSelectCall"/>.
	/// </summary>
	public readonly HashSet<string> PreknownJoinAliases = new(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Records the SQL alias backing each member of an anonymous projection
	/// produced by a Join's result selector (e.g. <c>(a, b) =&gt; new { A=a, B=b }</c>).
	/// Lets a downstream <c>Where(p =&gt; p.B.Field)</c> resolve <c>p.B</c>
	/// through the underlying join alias instead of the main FROM alias.
	/// </summary>
	public readonly Dictionary<(Type AnonType, MemberInfo Member), string> AnonProjectionAliases = [];
	public readonly Query FromPart = new();
	public readonly List<Query> WhereParts = [];
	public readonly Query OrderByPart = new();
	public readonly Query GroupByPart = new();
	public readonly List<Query> HavingParts = [];
	public readonly Query SwitchPart = new();
	public readonly Query UnionPart = new();
	public Query Curr;
	public readonly List<ContextSelectColumns> SelectColumns = [];
	public string TableAlias;
	public string LeftJoinAlias;
	public bool IsGroup;
	public bool IsSelect;
	public bool IsWhere;

	/// <summary>
	/// Body of the most recent GroupBy key selector lambda. Used by
	/// <c>VisitNew</c> to project <c>g.Key</c> back into the resolved
	/// grouping column when rendering the Select that follows GroupBy.
	/// </summary>
	public Expression GroupKeySelector;
	public readonly Dictionary<(Type, MemberInfo), MemberExpression> Members = [];

	/// <summary>
	/// Symbol table mapping each LINQ-lambda parameter to the SQL alias
	/// it refers to. Filled in lazily by the translator as it descends
	/// into Where/Select/Join/OrderBy lambdas; consulted by alias
	/// resolution in place of the string-name heuristic when present.
	/// </summary>
	public readonly Dictionary<ParameterExpression, string> Aliases = [];

	public readonly Queue<Action<bool, Query>> WrapColumn = new();

	public long? Skip;
	public long? Take;
	public readonly List<(string alias, string columnName, bool asc)> OrderBy = [];
	public string SelectAlias;
	public bool Count;
	public bool Distinct;
	public bool Exists;

	public IEnumerable<(Query, MemberInfo)> GetColumns(MemberInfo mi, int i, MemberInfo baseMi, bool ignoreView)
	{
		if (i >= SelectColumns.Count)
			return null;

		var selCols = SelectColumns[i];

		if (selCols.Map.TryGetValue(mi, out var mi2))
		{
			var columns = GetColumns(mi2, i + 1, mi, ignoreView);

			if (columns is not null)
				return columns;
		}

		var allColumns = mi.GetAttribute<AllColumnsFieldAttribute>() is not null;

		if (allColumns)
		{
			return new[] { (default(Query), mi) };
		}

		var memberType = mi.GetMemberType();

		if (!ignoreView && !memberType.IsNullable() && !memberType.IsSerializablePrimitive() && SchemaRegistry.TryGet(memberType, out var memberMeta) && memberMeta.IsView && SelectColumns.Count > (i + 1))
		{
			var nextCols = SelectColumns[i + 1];

			if (nextCols.Count == 1)
				return GetColumns(mi, i + 1, default, ignoreView);

			var viewCols = nextCols.Where(p => p.Key.DeclaringType.IsAssignableFrom(memberType));
			return viewCols.Select(p => (p.Value.Item1, p.Key));
		}

		if (!selCols.ContainsKey(mi))
		{
			var key = mi is PropertyInfo ? selCols.Keys.FirstOrDefault(k => mi.DeclaringType.IsAssignableFrom(k.DeclaringType) && k.Name == mi.Name) : null;

			if (key is null)
				return null;

			mi = key;
		}

		return new[] { (selCols[mi].Item1, baseMi ?? mi) };
	}

	public Query Build(Schema schema)
	{
		if (TableAlias.IsEmpty())
			TableAlias = Extensions.DefaultAlias;

		// Pre-canonical query shapes that bypass the SELECT pipeline and
		// short-circuit straight to the caller.
		if (UnionPart.Actions.Count > 0)
		{
			var unionQuery = new Query();
			UnionPart.CopyTo(unionQuery);
			return unionQuery;
		}

		if (SwitchPart.Actions.Count > 0)
		{
			var switchQuery = new Query();
			SwitchPart.CopyTo(switchQuery);
			return switchQuery;
		}

		var query = new Query();

		EmitExistsPrologue(query);
		EmitSelectClause(query);
		EmitFromAndJoins(query, schema);
		EmitWhereClause(query);
		query = EmitGroupAndHaving(query);
		EmitOrderByAndPagination(query, schema);
		EmitExistsEpilogue(query);

		return query;
	}

	private void EmitExistsPrologue(Query query)
	{
		if (!Exists)
			return;

		// Open `cast(` only when the dialect actually wraps the boolean
		// expression in a cast. PostgreSQL represents booleans natively and
		// rejects `cast(<bool> as bit)`, so the prologue is a no-op there
		// and the epilogue stops short of emitting `as bit )`.
		query.AddAction((d, sb) =>
		{
			if (d.BooleanCastSqlType is not null)
				sb.Append("cast(");
		});

		query
			.NewLine()
				.Case()
				.NewLine()
				.When()
				.Exists()
					.OpenBracket();
	}

	private void EmitExistsEpilogue(Query query)
	{
		if (!Exists)
			return;

		query
			.CloseBracket()
			.Then().AddAction((d, sb) => sb.Append(d.TrueLiteral))
			.NewLine()
			.Else().AddAction((d, sb) => sb.Append(d.FalseLiteral))
			.NewLine().End()
			.AddAction((d, sb) =>
			{
				if (d.BooleanCastSqlType is not null)
					sb.Append(" as ").Append(d.BooleanCastSqlType).Append(')');
			});
	}

	private void EmitSelectClause(Query query)
	{
		query.Select();

		if (Distinct && !Count)
			query.Distinct();

		if (Count)
		{
			query.Count().OpenBracket();

			if (Distinct)
				query.Distinct().Column(TableAlias, Extensions.IdColName);
			else
				query.Star();

			query.CloseBracket();
			return;
		}

		NormaliseSkipTake();

		if (SelectColumns.Count > 0 && SelectColumns[0].Count > 0)
			EmitProjectedColumns(query);
		else
			EmitDefaultProjection(query);
	}

	private void NormaliseSkipTake()
	{
		if (Take is not null)
			Skip ??= 0;
		else if (Skip is not null)
			Take = int.MaxValue;

		if (Skip == 0)
			Skip = null;
	}

	private void EmitProjectedColumns(Query query)
	{
		var idx = 0;
		var top = SelectColumns[0];

		foreach (var pair in top)
		{
			var columns = GetColumns(pair.Key, default, default, default)
				?? throw new InvalidOperationException();

			foreach (var (q, mi) in columns)
			{
				if (idx++ > 0)
					query.Comma();

				var isAll = mi.GetAttribute<AllColumnsFieldAttribute>() is not null;

				if (isAll)
				{
					query.All(TableAlias);
				}
				else
				{
					q.CopyTo(query);

					if (pair.Value.Item2 is not MemberExpression || q.Actions.Count > 1 || (top.Map.TryGetValue(mi, out var mi3) && mi.Name != mi3.Name))
						query.As().Column(mi.Name);
				}
			}
		}
	}

	private void EmitDefaultProjection(Query query)
	{
		var selectFrom = TableAlias;
		if (SelectAlias is not null && JoinParts.Any(j => j.tableAlias.EqualsIgnoreCase(SelectAlias)))
			selectFrom = SelectAlias;
		query.All(selectFrom);

		// include computed columns from deeper MemberInit layers
		// (when outermost Select is anonymous access like e => e.Tag)
		foreach (var layer in SelectColumns)
		{
			foreach (var pair in layer)
			{
				var mi = pair.Key;

				if (mi.GetAttribute<AllColumnsFieldAttribute>() is not null)
					continue;

				var memberType = mi.GetMemberType();

				if (!memberType.IsSerializablePrimitive() && SchemaRegistry.TryGet(memberType, out _))
					continue;

				var q = pair.Value.Item1;

				if (q?.Actions.Count > 1)
				{
					query.Comma();
					q.CopyTo(query);
					query.As().Column(mi.Name);
				}
			}
		}
	}

	private void EmitFromAndJoins(Query query, Schema schema)
	{
		query.NewLine().From();

		if (FromPart.Actions.Count == 0)
			query.Table(schema.Name, TableAlias);
		else
		{
			FromPart.CopyTo(query);
			query.As().Column(TableAlias);
		}

		query.NewLine();

		foreach (var (_, joinPart) in JoinParts)
		{
			if (joinPart.Actions.Count > 0)
				joinPart.CopyTo(query);
		}
	}

	private void EmitWhereClause(Query query)
	{
		if (WhereParts.Count == 0)
			return;

		query.Where().NewLine();

		var idx = 0;

		foreach (var part in WhereParts)
		{
			part.CopyTo(query);

			if (++idx < WhereParts.Count)
				query.And();
		}

		query.NewLine();
	}

	private Query EmitGroupAndHaving(Query query)
	{
		if (GroupByPart.Actions.Count == 0)
			return query;

		GroupByPart.CopyTo(query);
		query.NewLine();

		// COUNT(*) on a grouped result and Skip/Take on a grouped result
		// both wrap the grouped query in a CTE so the outer pagination /
		// counting consumes a flat row set instead of fighting the GROUP BY.
		if (Count)
			query = WrapInCteCount(query);
		else if (Skip is not null || Take is not null)
			query = WrapInCtePassthrough(query);

		if (HavingParts.Count > 0)
		{
			query.Having().NewLine();

			var idx = 0;

			foreach (var part in HavingParts)
			{
				part.CopyTo(query);

				if (++idx < HavingParts.Count)
					query.And();
			}

			query.NewLine();
		}

		return query;
	}

	private Query WrapInCteCount(Query inner)
	{
		var cte = new Query();
		const string tableRes = "cteresults";

		cte.With().Raw(tableRes).OpenBracket().Raw("cnt").CloseBracket().As().NewLine()
			.OpenBracket().NewLine();

		inner.CopyTo(cte);

		cte.NewLine().CloseBracket().NewLine();

		cte
			.Select()
			.Count().OpenBracket().Star().CloseBracket()
			.NewLine()
			.From()
			.Table(tableRes, TableAlias = "p")
			.NewLine();

		return cte;
	}

	private Query WrapInCtePassthrough(Query inner)
	{
		var cte = new Query();
		const string tableRes = "cteresults";

		cte.With().Raw(tableRes).As().NewLine()
			.OpenBracket().NewLine();

		inner.CopyTo(cte);

		cte.NewLine().CloseBracket().NewLine();

		cte
			.Select()
			.All("p")
			.NewLine()
			.From()
			.Table(tableRes, TableAlias = "p")
			.NewLine();

		// ORDER BY collected from the LINQ tree still references inner table
		// aliases (e.g. [e].[Id]), but those aliases are now hidden inside
		// the CTE. Rebind every entry to the CTE outer alias so the final
		// query reads `order by [p].[Column]`.
		for (var i = 0; i < OrderBy.Count; i++)
		{
			var (_, name, asc) = OrderBy[i];
			OrderBy[i] = ("p", name, asc);
		}

		return cte;
	}

	private void EmitOrderByAndPagination(Query query, Schema schema)
	{
		if (Count)
			return;

		if (OrderByPart.Actions.Count > 0)
		{
			query.OrderBy();
			OrderByPart.CopyTo(query);
			query.NewLine();
		}

		if (OrderBy.Count == 0)
		{
			if (Skip is not null || Take is not null)
			{
				// SELECT DISTINCT requires every ORDER BY column to also be
				// in the SELECT list. A blind `[TableAlias].[Identity]`
				// fallback usually picks a column that isn't in the
				// projection — pick the first projected member name instead.
				if (Distinct && SelectColumns.Count > 0 && SelectColumns[0].Keys.FirstOrDefault() is { } firstKey)
					query.OrderBy().Column(firstKey.Name);
				else if (schema.Identity is not null)
					query.OrderBy().Column(TableAlias, schema.Identity.Name);
				else
					query.AddAction((d, sb) => d.AppendFallbackOrderBy(sb));
			}
		}
		else
		{
			query.OrderBy();

			var isFirstColumn = true;

			foreach (var (alias, columnName, asc) in OrderBy)
			{
				if (isFirstColumn)
					isFirstColumn = false;
				else
					query.Comma();

				// If the column matches a projected output (SELECT-list alias)
				// from a view processor or explicit Select, emit unqualified —
				// `[e].[MessageCount]` would fail because MessageCount is a
				// computed projection, not a real column on the underlying
				// table. SQL's ORDER BY allows referencing SELECT-list aliases
				// by bare name. Check every SelectColumns layer because
				// chained Select(...).Distinct() pushes the projection deeper.
				var isProjectedAlias = SelectColumns.Any(layer => layer.Keys.Any(k => k.Name == columnName));

				if (alias is not null && !isProjectedAlias)
					query.Column(alias, columnName);
				else
					query.Column(columnName);

				if (!asc)
					query.Desc();
			}
		}

		query.NewLine();

		var skipParamName = Skip is not null ? TryAddParam("skip", typeof(long), Skip.Value) : null;
		var takeParamName = Take is not null ? TryAddParam("take", typeof(long), Take.Value) : null;

		if (skipParamName is not null || takeParamName is not null)
		{
			query.AddAction((dialect, builder) =>
			{
				var skipExpr = skipParamName is not null ? dialect.ParameterPrefix + skipParamName : null;
				var takeExpr = takeParamName is not null ? dialect.ParameterPrefix + takeParamName : null;
				dialect.AppendPaginationParams(builder, skipExpr, takeExpr);
			});
		}
	}

	private int _subCount;

	public void AddParamsFromSubquery(Dictionary<string, (Type, object)> subParams, bool change = true)
	{
		if (change)
			Parameters.AddRange(subParams.Select(p => ($"{p.Key}_{_subCount}", p.Value).ToPair()));
		else
		{
			foreach (var p in subParams)
				Parameters[p.Key] = p.Value;
		}

		_subCount++;
	}

	public readonly Dictionary<string, (Type, object)> Parameters = [];

	public void TryAddParam(MemberInfo member, object instance)
	{
		var value = member.GetMemberValue(instance);

		if (value is not IQueryable)
		{
			if (value is null)
				Curr.Null();
			else
			{
				if (value is Array arr && value is not byte[])
				{
					var itemType = arr.GetType().GetItemType();

					for (var i = 0; i < arr.Length; i++)
					{
						if (i > 0)
							Curr.Comma();

						Curr.Param(TryAddParam(member.Name, itemType, arr.GetValue(i)));
					}
				}
				else
					Curr.Param(TryAddParam(member.Name, member.GetMemberType(), value));
			}
		}
	}

	public int ParamCountOffset;

	public string TryAddParam(string name, Type type, object value)
	{
		name += Parameters.Count + ParamCountOffset;

		Parameters.Add(name, (type, value));

		return name;
	}
}