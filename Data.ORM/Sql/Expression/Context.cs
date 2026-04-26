namespace Ecng.Data.Sql;

class ContextSelectColumns : Dictionary<MemberInfo, (Query, Expression)>
{
	public readonly Dictionary<MemberInfo, MemberInfo> Map = [];
}

class Context
{
	public (string origAlias, string modifiedAlias) CurrJoinAlias;
	public readonly List<(string tableAlias, Query query)> JoinParts = [];
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
	public readonly Dictionary<(Type, MemberInfo), MemberExpression> Members = [];

	public readonly Queue<Action<bool, Query>> WrapColumn = new();

	public long? Skip;
	public long? Take;
	public readonly List<(MemberInfo member, bool asc)> OrderBy = [];
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

		var query = new Query();

		if (UnionPart.Actions.Count > 0)
		{
			UnionPart.CopyTo(query);
			return query;
		}

		if (SwitchPart.Actions.Count > 0)
		{
			SwitchPart.CopyTo(query);
			return query;
		}

		if (Exists)
		{
			query
				.Cast()
				.OpenBracket()
				.NewLine()
					.Case()
					.NewLine()
					.When()
					.Exists()
						.OpenBracket();
		}

		query.Select();

		if (Distinct)
		{
			if (Count)
			{
			}
			else
				query.Distinct();
		}

		if (Count)
		{
			query.Count().OpenBracket();

			if (Distinct)
				query.Distinct().Column(TableAlias, Extensions.IdColName);
			else
				query.Star();

			query.CloseBracket();
		}
		else
		{
			if (Take is not null)
			{
				Skip ??= 0;
			}
			else
			{
				if (Skip is not null)
					Take = int.MaxValue;
			}

			if (Skip == 0)
			{
				Skip = null;
			}

			if (SelectColumns.Count > 0 && SelectColumns[0].Count > 0)
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
			else
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
		}

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

		if (WhereParts.Count > 0)
		{
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

		if (GroupByPart.Actions.Count > 0)
		{
			GroupByPart.CopyTo(query);

			query.NewLine();

			if (Count)
			{
				var cte = new Query();

				const string tableRes = "cteresults";

				cte.With().Raw(tableRes).OpenBracket().Raw("cnt").CloseBracket().As().NewLine()
					.OpenBracket().NewLine();

				query.CopyTo(cte);

				cte.NewLine().CloseBracket().NewLine();

				cte
					.Select()
					.Count().OpenBracket().Star().CloseBracket()
					.NewLine()
					.From()
					.Table(tableRes, TableAlias = "p")
					.NewLine();

				query = cte;
			}
			else if (Skip is not null || Take is not null)
			{
				var cte = new Query();

				const string tableRes = "cteresults";

				cte.With().Raw(tableRes).As().NewLine()
					.OpenBracket().NewLine();

				query.CopyTo(cte);

				cte.NewLine().CloseBracket().NewLine();

				cte
					.Select()
					.All("p")
					.NewLine()
					.From()
					.Table(tableRes, TableAlias = "p")
					.NewLine();

				query = cte;
			}

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
		}

		if (Count)
		{

		}
		else
		{
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
					if (schema.Identity is not null)
						query.OrderBy().Column(TableAlias, schema.Identity.Name);
					else
						query.AddAction((d, sb) => d.AppendFallbackOrderBy(sb));
				}
			}
			else
			{
				query.OrderBy();

				var isFirstColumn = true;

				foreach (var (member, asc) in OrderBy)
				{
					if (isFirstColumn)
						isFirstColumn = false;
					else
						query.Comma();

					if (schema.EntityType == member.DeclaringType)
						query.Column(TableAlias, member.Name);
					else
						query.Column(member.Name);

					if (!asc)
						query.Desc();
				}
			}

			query.NewLine();

			{
				var skipParamName = Skip is not null ? TryAddParam("skip", typeof(long), Skip.Value) : null;
				var takeParamName = Take is not null ? TryAddParam("take", typeof(long), Take.Value) : null;

				if (skipParamName is not null || takeParamName is not null)
					query.AddAction((dialect, builder) =>
					{
						var skipExpr = skipParamName is not null ? dialect.ParameterPrefix + skipParamName : null;
						var takeExpr = takeParamName is not null ? dialect.ParameterPrefix + takeParamName : null;
						dialect.AppendPaginationParams(builder, skipExpr, takeExpr);
					});
			}
		}

		if (Exists)
		{
			query
				.CloseBracket()
				.Then().AddAction((d, sb) => sb.Append(d.TrueLiteral))
				.NewLine()
				.Else().AddAction((d, sb) => sb.Append(d.FalseLiteral))
				.NewLine().End().As().Raw("bit").CloseBracket();
		}

		return query;
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