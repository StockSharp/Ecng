namespace Ecng.Data.Sql;

using Ecng.Data.Sql.Model;

/// <summary>
/// https://stackoverflow.com/questions/7731905/how-to-convert-an-expression-tree-to-a-partial-sql-query
/// </summary>
class ExpressionQueryTranslator(Schema meta) : ExpressionVisitor
{
	public const string CancellationTokenKey = "cancellationToken";

	public IDictionary<string, (Type, object)> Parameters => Context.Parameters;

	private readonly Schema _meta = meta ?? throw new ArgumentNullException(nameof(meta));

	public Context Context { get; internal set; }

	private Query Curr
	{
		get => Context.Curr;
		set => Context.Curr = value;
	}

	public Queue<Action<bool, Query>> WrapColumn => Context.WrapColumn;

	public Query GenerateSql(Expression expression)
	{
		Context = new() { TableAlias = Extensions.DefaultAlias };

		Visit(expression);

		var query = Context.Build(_meta);

		if (Context.Count)
		{
			if (expression is MethodCallExpression methodExp && methodExp.Arguments.Count > 1 && methodExp.Arguments[1] is ConstantExpression constExp)
			{
				Context.Parameters.Add(CancellationTokenKey, (constExp.Type, constExp.Value));
			}
		}

		return query;
	}

	protected override Expression VisitMethodCall(MethodCallExpression m)
	{
		// Each LINQ operator is handled by a dedicated method below; the
		// switch is the single point where new operators are wired in.
		// Non-LINQ method calls (DateTime.AddDays, string.Length, etc.)
		// fall through to the visitor registry / SQL-external dispatch.
		switch (m.Method.Name)
		{
			case nameof(Queryable.Where):
				return VisitWhereCall(m);

			case nameof(Queryable.Take):
				Context.Take = m.Arguments[1].GetConstant<long>();
				return Visit(m.Arguments[0]);

			case nameof(QueryableExtensions.FirstOrDefaultAsync):
				Context.Take = 1;
				Context.Parameters.Add(CancellationTokenKey, (((ConstantExpression)m.Arguments[1]).Type, ((ConstantExpression)m.Arguments[1]).Value));
				return Visit(m.Arguments[0]);

			case nameof(Queryable.Skip):
			case nameof(QueryableExtensions.SkipLong):
				Context.Skip = m.Arguments[1].GetConstant<long>();
				return Visit(m.Arguments[0]);

			case nameof(Queryable.OrderBy):
			case nameof(Queryable.ThenBy):
				ParseOrderByExpression(m, true);
				return Visit(m.Arguments[0]);

			case nameof(Queryable.OrderByDescending):
			case nameof(Queryable.ThenByDescending):
				ParseOrderByExpression(m, false);
				return Visit(m.Arguments[0]);

			case nameof(QueryableExtensions.CountAsync):
				Context.Count = true;
				return Visit(m.Arguments[0]);

			case nameof(Queryable.Select):
				return VisitSelectCall(m);

			case nameof(Queryable.SelectMany):
				return VisitSelectManyCall(m);

			case nameof(Queryable.Join):
				return VisitJoinCall(m);

			case nameof(Queryable.Distinct):
				Context.Distinct = true;
				return Visit(m.Arguments[0]);

			case nameof(Queryable.GroupJoin):
				return VisitGroupJoinCall(m);

			case nameof(Queryable.GroupBy):
				return VisitGroupByCall(m);

			default:
				return VisitNonLinqMethodCall(m);
		}
	}

	private Expression VisitWhereCall(MethodCallExpression m)
	{
		var c = Curr = new();

		Visit(m.Arguments[0]);

		Curr = c;
		var lambda = (LambdaExpression)m.Arguments[1].StripQuotes();

		RegisterLambdaParameters(lambda, Context.TableAlias.IsEmpty() ? Extensions.DefaultAlias : Context.TableAlias);

		var isBoolEqual = lambda.Body is MemberExpression me && me.Type == typeof(bool);

		if (isBoolEqual)
			Curr.OpenBracket();

		var isWhere = Context.IsWhere;
		Context.IsWhere = true;
		Visit(lambda.Body);
		Context.IsWhere = isWhere;

		if (isBoolEqual)
		{
			Curr.IsTrue();
			Curr.CloseBracket();
		}

		if (lambda.Parameters.Count > 0 && lambda.Parameters[0].Type.GetGenericType(typeof(IGrouping<,>)) is not null)
			Context.HavingParts.Add(Curr);
		else
			Context.WhereParts.Add(Curr);

		return m;
	}

	private Expression VisitSelectCall(MethodCallExpression m)
	{
		RegisterLambdaParameters(m.Arguments[1].GetOperand(), Context.TableAlias.IsEmpty() ? Extensions.DefaultAlias : Context.TableAlias);

		// Pre-extract the key-selector body so projection visits like
		// `g.Key.X` can re-resolve it back to the original join-aware
		// expression. (The full source visit runs only at the end of this
		// method via `Visit(m.Arguments[0])`.)
		if (Context.GroupKeySelector is null &&
			m.Arguments[0] is MethodCallExpression src &&
			src.Method.DeclaringType == typeof(Queryable) &&
			src.Method.Name == nameof(Queryable.GroupBy) &&
			src.Arguments.Count >= 2 &&
			src.Arguments[1].StripQuotes() is LambdaExpression keyLambda)
		{
			Context.GroupKeySelector = keyLambda.Body;
		}

		// Pre-walk the source chain to surface Join inner-alias names up-front.
		// The projection body below may resolve transparent-id chains like
		// `td.j.Field` via `ResolveTransIdAlias`, which keys off
		// `Context.JoinParts`. Without this peek, `JoinParts` is still empty
		// at projection-visit time (it gets populated by the recursive
		// `Visit(m.Arguments[0])` at the end of this method) and the
		// resolver falls back to the main FROM alias. We populate
		// PreknownJoinAliases (a name-only set) instead of JoinParts so the
		// real `VisitJoinCall` later does not see a self-collision and
		// rename `i` → `i1`.
		PreregisterJoinAliases(m.Arguments[0]);

		var body = m.Arguments[1].GetOperand().Body;

		// Record member→alias mapping for an anonymous-projection body so a
		// downstream Where/Select like `p.Topic.X` resolves `p.Topic` back
		// to the underlying alias instead of the main FROM alias. Same idea
		// as in VisitJoinCall, applied to user-written `.Select(x => new { ... })`.
		if (body is NewExpression bodyNew &&
			bodyNew.Members is { } memList &&
			bodyNew.Type.IsAutoGenerated())
		{
			for (var ki = 0; ki < bodyNew.Arguments.Count; ki++)
			{
				var arg = bodyNew.Arguments[ki];
				string aliasForArg = null;

				if (arg is ParameterExpression argPe)
					aliasForArg = GetAlias(argPe);
				else if (arg is MemberExpression argMe)
					aliasForArg = ResolveTransIdAlias(argMe);

				if (aliasForArg is not null)
					Context.AnonProjectionAliases[(bodyNew.Type, memList[ki])] = aliasForArg;
			}
		}

		if (body is MemberInitExpression)
		{
			Visit(body);
		}
		else if (body is NewExpression)
		{
			var isSelect = Context.IsSelect;
			Context.IsSelect = true;
			Visit(body);
			Context.IsSelect = isSelect;
		}
		else if (body is MemberExpression me)
		{
			// Selecting entity from anonymous join type (transparent identifier)?
			var memberType = me.Member.GetMemberType();
			if (!memberType.IsSerializablePrimitive() && SchemaRegistry.TryGet(memberType, out _))
			{
				var isAnonymousAccess =
					(me.Expression is ParameterExpression anonPe && anonPe.Type.IsAutoGenerated()) ||
					(me.Expression is MemberExpression anonMe &&
						(anonMe.Type.IsAutoGenerated() || anonMe.Member.DeclaringType.IsAutoGenerated()));

				if (isAnonymousAccess)
				{
					Context.SelectAlias = me.Member.Name;
					Context.SelectColumns.Add(new());
					return Visit(m.Arguments[0]);
				}
			}

			var curr = Curr;

			var selCols = new ContextSelectColumns();
			Context.SelectColumns.Add(selCols);

			var q = new Query { WrapColumn = WrapColumn };
			Curr = q;

			if (
				me.Expression is MemberExpression me2 &&
				me2.Member.Name == nameof(IGrouping<int, int>.Key) &&
				me2.Member.ReflectedType.GetGenericType(typeof(IGrouping<,>)) is not null)
			{
				var me3 = (MemberExpression)((LambdaExpression)((MethodCallExpression)((MethodCallExpression)m.Arguments[0]).Arguments[0]).Arguments[1].StripQuotes()).Body;
				selCols.Add(me3.Member, (q, me));
				Visit(me3);
			}
			else
			{
				selCols.Add(me.Member, (q, me));
				Visit(body);
			}

			Curr = curr;
		}
		else if (body is UnaryExpression ue && ue.Method is not null)
		{
			if (!ue.Method.TryGetVisitor(out var visitor))
				throw new NotSupportedException();

			var curr = Curr;

			var selCols = new ContextSelectColumns();
			Context.SelectColumns.Add(selCols);

			var me2 = (MemberExpression)ue.Operand;

			var q = new Query { WrapColumn = WrapColumn };
			selCols.Add(me2.Member, (q, me2));

			Curr = q;
			visitor.Visit(this, me2);

			Curr = curr;
		}
		else
			throw new NotSupportedException();

		return Visit(m.Arguments[0]);
	}

	/// <summary>
	/// Lightweight peek that walks the LINQ method chain rooted at
	/// <paramref name="source"/> looking for <c>Queryable.Join</c>
	/// calls and records the inner alias name in
	/// <see cref="Context.PreknownJoinAliases"/>. Lets projection-time
	/// visitors that resolve transparent-id chains see the join alias before
	/// the recursive source visit registers the full join SQL — without
	/// pre-populating <c>JoinParts</c> itself, which would cause the real
	/// <see cref="VisitJoinCall"/> to see a self-collision and rename
	/// `i` → `i1`.
	/// </summary>
	private void PreregisterJoinAliases(Expression source)
	{
		while (source is MethodCallExpression mc)
		{
			if (mc.Method.DeclaringType == typeof(Queryable) &&
				mc.Method.Name == nameof(Queryable.Join) &&
				mc.Arguments.Count >= 4 &&
				mc.Arguments[3].StripQuotes() is LambdaExpression innerKey &&
				innerKey.Parameters.Count > 0)
			{
				Context.PreknownJoinAliases.Add(innerKey.Parameters[0].Name);
			}

			// SelectMany after GroupJoin (the LINQ shape
			// `from t2 in g.DefaultIfEmpty()`) introduces the second
			// result-selector parameter as the LEFT-JOIN alias. Surface
			// its name here so projection-time `t2.X` references resolve
			// correctly before the recursive source visit registers the
			// real Aliases entry.
			if (mc.Method.DeclaringType == typeof(Queryable) &&
				mc.Method.Name == nameof(Queryable.SelectMany) &&
				mc.Arguments.Count >= 3 &&
				mc.Arguments[2].StripQuotes() is LambdaExpression resultSel &&
				resultSel.Parameters.Count > 1)
			{
				Context.PreknownJoinAliases.Add(resultSel.Parameters[1].Name);
			}

			source = mc.Arguments.Count > 0 ? mc.Arguments[0] : null;
		}
	}

	private Expression VisitSelectManyCall(MethodCallExpression m)
	{
		var resultSelector = m.Arguments[2].GetOperand();

		var leftJoinAlias = Context.LeftJoinAlias;
		Context.LeftJoinAlias = resultSelector.Parameters[1].Name;
		var retVal = Visit(m.Arguments[0]);
		Context.LeftJoinAlias = leftJoinAlias;

		// Bind the result-selector parameters to their backing aliases so
		// downstream visitors (and any sub-query that inherits Aliases)
		// can resolve `<transparentId>.<field>` and `<rightSide>` chains.
		// First param projects onto the running outer scope (FROM table or
		// previous transparent identifier); second param maps to the LEFT
		// JOIN alias produced by the source GroupJoin.
		if (resultSelector.Parameters.Count > 0)
		{
			var outerAlias = Context.TableAlias.IsEmpty() ? Extensions.DefaultAlias : Context.TableAlias;
			Context.Aliases[resultSelector.Parameters[0]] = outerAlias;
		}

		if (resultSelector.Parameters.Count > 1)
			Context.Aliases[resultSelector.Parameters[1]] = resultSelector.Parameters[1].Name;

		if (resultSelector.Body is MemberInitExpression)
		{
			// final projection folded into SelectMany result selector
			Visit(resultSelector.Body);
		}

		return retVal;
	}

	private Expression VisitJoinCall(MethodCallExpression m)
	{
		var joinPart = new Query();

		Curr = joinPart;

		if (m.Arguments[0].NodeType == ExpressionType.Call)
		{
			Visit(m.Arguments[0]);
			Curr = joinPart;
		}

		Curr.InnerJoin();

		var (meta1, alias1, col1) = ParseScalarJoinKey(m.Arguments[2]);
		var (meta2, alias2, col2) = ParseScalarJoinKey(m.Arguments[3]);

		var prevAlias = Context.CurrJoinAlias;

		try
		{
			var origAlias = alias2;

			while (Context.JoinParts.Any(t => t.tableAlias.EqualsIgnoreCase(alias2)))
			{
				alias2 += "1";
			}

			if (origAlias != alias2)
				Context.CurrJoinAlias = (origAlias, alias2);

			if (m.Arguments[1] is MethodCallExpression mce)
			{
				var attr = mce.Method.GetAttribute<SqlExternalAttribute>();

				if (attr is not null)
					this.RenderFuncCall(Curr, mce, attr);
				else
				{
					if (mce.Method.TryGetVisitor(out var visitor))
					{
						Curr.OpenBracket();
						visitor.Visit(this, mce);
						Curr.CloseBracket();
					}
					else
						throw new NotSupportedException(mce.Method.To<string>());
				}

				Curr.As().Column(alias2);
			}
			else
			{
				Curr.Table(meta2.Name, alias2);
			}

			Curr.On().Column(alias2, col2).Equal().Column(GetAlias(alias1), col1).NewLine();

			Context.JoinParts.Add((alias2, joinPart));

			var resultSelector = m.Arguments[4].GetOperand();
			if (resultSelector.Body is ParameterExpression resultParam &&
				resultParam == resultSelector.Parameters[1])
			{
				// select inner entity → SELECT [alias2].*
				Context.SelectAlias = alias2;
			}
			else if (resultSelector.Body is MemberInitExpression)
			{
				// final projection folded into Join result selector
				// (e.g., select new VError { Text = ec.Value, ... })
				Visit(resultSelector.Body);
			}
			else if (resultSelector.Body is NewExpression newExp &&
				!newExp.Arguments.All(a => a is ParameterExpression))
			{
				// Anonymous projection from both join sides (e.g.
				// `(t, p) => new { t.Title, p.Name }`). When every
				// argument is a bare ParameterExpression the new-expr
				// is a transparent identifier produced by the C#
				// compiler for chained joins — that case is handled
				// by the downstream Select/Join, so leave it alone.
				var isSelect = Context.IsSelect;
				Context.IsSelect = true;
				Visit(resultSelector.Body);
				Context.IsSelect = isSelect;
			}

			// Record member→alias mapping so a downstream Where/Select like
			// `p.Detail.X` (where `p` is the projected anonymous type) can
			// resolve `p.Detail` back to the join's alias `[i]` instead of
			// the main FROM alias `[e]`. We handle the explicit-anonymous
			// shape `(a, b) => new { Member = a, Member = b }` — the bare
			// transparent-identifier shape (every arg is a ParameterExpression
			// produced by the C# compiler for chained joins) is intentionally
			// captured here too, since downstream resolution still needs it.
			if (resultSelector.Body is NewExpression bodyNew &&
				bodyNew.Members is { } memList &&
				bodyNew.Type.IsAutoGenerated())
			{
				var outerAlias = GetAlias(resultSelector.Parameters[0].Name);

				for (var i = 0; i < bodyNew.Arguments.Count; i++)
				{
					var arg = bodyNew.Arguments[i];

					if (arg is ParameterExpression argParam)
					{
						var aliasForArg = argParam == resultSelector.Parameters[1]
							? alias2
							: GetAlias(argParam.Name);

						Context.AnonProjectionAliases[(bodyNew.Type, memList[i])] = aliasForArg;
					}
				}
			}

			return m;
		}
		finally
		{
			Context.CurrJoinAlias = prevAlias;
		}
	}

	private Expression VisitGroupJoinCall(MethodCallExpression m)
	{
		var joinPart = new Query();

		Curr = joinPart;

		if (m.Arguments[0].NodeType == ExpressionType.Call)
		{
			Visit(m.Arguments[0]);
			Curr = joinPart;
		}

		Curr.LeftJoin();

		var (meta1, alias1, col1) = ParseCompositeJoinKey(m.Arguments[2]);
		var (meta2, alias2, col2) = ParseCompositeJoinKey(m.Arguments[3]);

		if (col1.Length != col2.Length)
			throw new InvalidOperationException($"col1={col1.Length} <> col2={col2.Length}");

		alias2 = Context.LeftJoinAlias.IsEmpty(alias2);

		if (m.Arguments[1] is MethodCallExpression mce)
		{
			var attr = mce.Method.GetAttribute<SqlExternalAttribute>();

			if (attr is null)
				throw new NotSupportedException();

			this.RenderFuncCall(Curr, mce, attr);
			Curr.As().Column(alias2);
		}
		else
		{
			Curr.Table(meta2.Name, alias2);
		}

		Curr.On();

		for (var i = 0; i < col1.Length; i++)
		{
			if (i > 0)
				Curr.And();

			var isNull = (!col1[i].isColumn || !col2[i].isColumn) && (col1[i].value is null || col2[i].value is null);

			if (col1[i].isColumn)
				Curr.Column(GetAlias(alias1), (string)col1[i].value);
			else
			{
				if (isNull && col1[i].value is null)
				{
					if (col2[i].isColumn)
						Curr.Column(alias2, (string)col2[i].value);
					else
						Curr.Raw(col2[i].value.To<string>());
				}
				else
					Curr.Raw(col1[i].value.To<string>());
			}

			if (isNull)
				Curr.Is().Null();
			else
			{
				Curr.Equal();

				if (col2[i].isColumn)
					Curr.Column(alias2, (string)col2[i].value);
				else
					Curr.Raw(col2[i].value.To<string>());
			}
		}

		Curr.NewLine();

		Context.JoinParts.Add((alias2, joinPart));

		return m;
	}

	private Expression VisitGroupByCall(MethodCallExpression m)
	{
		Visit(m.Arguments[0]);

		var curr = Curr;
		Curr = Context.GroupByPart;

		Curr.GroupBy();

		var isGroup = Context.IsGroup;

		var keyLambda = (LambdaExpression)m.Arguments[1].StripQuotes();
		Context.GroupKeySelector = keyLambda.Body;

		// Register the key-selector lambda's parameter against the running
		// table alias so downstream VisitMember resolves `i.Field` to
		// `[e].[Field]` instead of leaking `i` (the parameter name) as a
		// raw SQL alias and producing "i.Field could not be bound".
		RegisterLambdaParameters(keyLambda, Context.TableAlias.IsEmpty() ? Extensions.DefaultAlias : Context.TableAlias);

		Context.IsGroup = true;
		var retVal = Visit(keyLambda);
		Context.IsGroup = isGroup;

		Curr = curr;

		return retVal;
	}

	private Expression VisitNonLinqMethodCall(MethodCallExpression m)
	{
		if (m.Method.TryGetVisitor(out var visitor))
		{
			visitor.Visit(this, m);
			return m;
		}

		var attr = m.Method.GetAttribute<SqlExternalAttribute>();

		if (attr is null)
			throw new NotSupportedException($"The method '{m.Method.Name}' is not supported");

		var needBrackets = false;

		if (Curr is null)
			Curr = Context.FromPart;
		else
			needBrackets = true;

		if (needBrackets)
			Curr.OpenBracket();

		this.RenderFuncCall(Curr, m, attr);

		if (needBrackets)
		{
			if (m.Method.ReturnType == typeof(bool))
				Curr.IsTrue();

			Curr.CloseBracket();
		}

		return m;
	}

	protected override Expression VisitConditional(ConditionalExpression node)
	{
		Curr = Context.SwitchPart;

		Curr
			.Case()
			.NewLine();

		Curr.When();
		Visit(node.Test);

		Curr.Then();
		Visit(node.IfTrue);

		Curr.NewLine();

		Curr.Else();
		Visit(node.IfFalse);

		Curr.NewLine().End();

		return node;
	}

	private Expression ProcessInitExpression(Expression maExp)
	{
		if (maExp.NodeType == ExpressionType.Convert)
		{
			maExp = ((UnaryExpression)maExp).Operand;
		}

		var ctx = Context;

		MethodVisitor visitor = default;

		var isSubquery = (maExp is MethodCallExpression ce /*&& ce.Arguments.Count == 1*/ && (!ce.Method.TryGetVisitor(out visitor) || (ce.Arguments.Count > 0 && ce.Arguments[0].NodeType == ExpressionType.Call)) &&
			(
				!(ce.Method.DeclaringType == typeof(Enumerable) && ce.Method.Name == nameof(Enumerable.Count)) ||
				(ce.Arguments.Count > 0 && ce.Arguments[0].NodeType != ExpressionType.Parameter)
			)) || maExp is ConditionalExpression;

		if (isSubquery)
		{
			Curr.OpenBracket();
			Curr.NewLine();

			// Pre-analyse the sub-query so the element type and the
			// lambda-parameter alias are known BEFORE Visit. Setting
			// TableAlias up front lets RegisterLambdaParameters bind the
			// inner lambda's parameter to the right alias instead of the
			// default one — without this the inner WHERE references "e"
			// while FROM declares "x" and the SQL fails to bind.
			AnalyseSubqueryShape(maExp, out var subItemType, out var subAlias);

			Context = new()
			{
				ParamCountOffset = ctx.Parameters.Count + ctx.ParamCountOffset,
				// AnalyseSubqueryShape only returns an alias for sub-queries
				// that begin with a method-call chain (e.g. .Where(x => ...)).
				// A bare ConditionalExpression — like a CASE wrapping
				// `g.Key.X` and `g.Count()` — yields a null subAlias even
				// though the body still references the OUTER query's
				// columns. Falling back to the outer TableAlias keeps
				// `g.Key.Member` resolving to `[e].[Member]` instead of
				// degenerating into the bogus `[Member].*` emit path.
				TableAlias = subAlias ?? ctx.TableAlias,
			};

			// Inherit outer alias bindings so a correlated reference like
			// `outerParam.Field` inside the sub-query resolves to the outer
			// FROM alias instead of leaking the transparent-id name.
			foreach (var kv in ctx.Aliases)
				Context.Aliases[kv.Key] = kv.Value;

			// Inherit JoinParts so transparent-id chains like `td.j.Field`
			// inside a CASE/conditional sub-projection still see the outer
			// JOIN alias `j` and resolve to `[j].[Field]` instead of the
			// main FROM alias.
			foreach (var jp in ctx.JoinParts)
				Context.JoinParts.Add(jp);

			// Same reason for the pre-walked alias set: while the recursive
			// source visit hasn't registered the real JoinParts yet, the
			// pre-walked names must still surface inside the sub-query.
			foreach (var preName in ctx.PreknownJoinAliases)
				Context.PreknownJoinAliases.Add(preName);

			// Inherit GroupKeySelector so `g.Key.X` references inside the
			// sub-query (e.g. a CASE expression in the projection) can be
			// rewritten back to the original key-selector argument and
			// resolved against the correct join alias.
			Context.GroupKeySelector = ctx.GroupKeySelector;

			if (visitor?.GetType().GetGenericType(typeof(CountVisitor<>)) != null)
				Context.Count = true;

			Visit(maExp);

			var subquery = Context.Build(SchemaRegistry.Get(subItemType ?? _meta.EntityType));
			var subParams = Context.Parameters;

			Context = ctx;
			subquery.CopyTo(Curr);
			Curr.CloseBracket();

			Context.AddParamsFromSubquery(subParams, false);
		}
		else
		{
			Visit(maExp);
		}

		return maExp;
	}

	/// <summary>
	/// Walks the outermost LINQ chain in <paramref name="maExp"/> to extract
	/// (a) the element type of the sub-query result and (b) the lambda
	/// parameter name of the deepest source. Both are needed to set up the
	/// sub-query <see cref="Context"/> before the visitor descends.
	/// </summary>
	private static void AnalyseSubqueryShape(Expression maExp, out Type itemType, out string alias)
	{
		itemType = null;
		alias = null;

		if (maExp is not MethodCallExpression mca)
			return;

		var args = mca.Method.GetGenericArguments();
		itemType = args.Length > 0 ? args[0] : typeof(int);

		while (mca.Arguments[0] is MethodCallExpression mca1)
			mca = mca1;

		alias = (mca.Arguments[1] is MemberExpression
			? mca.Arguments[2]
			: mca.Arguments[1])
			.GetOperand().Parameters[0].Name;

		if (itemType.IsSerializablePrimitive())
			itemType = ((MemberExpression)mca.Arguments[0]).Member.GetMemberType().GetGenericArguments()[0];
	}

	protected override Expression VisitMemberInit(MemberInitExpression i)
	{
		var selectColumns = new ContextSelectColumns();

		var curr = Curr;

		foreach (var b in i.Bindings)
		{
			Curr = new Query { WrapColumn = WrapColumn };

			var ma = (MemberAssignment)b;
			var maExp = ma.Expression;

			if (maExp is MemberExpression me &&
				me.Member is PropertyInfo pi &&
				pi.Name == nameof(IGrouping<int, int>.Key) &&
				pi.ReflectedType.GetGenericType(typeof(IGrouping<,>)) is not null)
			{
				Curr.Column(b.Member.Name);
			}
			else
			{
				if (maExp is MemberInitExpression mi)
				{
					var inner = ((MemberAssignment)mi.Bindings[0]).Expression;
					Visit(inner);

					if (inner is MemberExpression me2)
					{
						if (b.Member != me2.Member && me2.Member.Name == Extensions.IdColName && !me2.Member.DeclaringType.IsAutoGenerated() && me2.Expression is MemberExpression innerMember)
							selectColumns.Map.Add(b.Member, innerMember.Member);
						else
							selectColumns.Map.Add(b.Member, me2.Member);
					}
				}
				else
				{
					maExp = ProcessInitExpression(maExp);

					if (maExp is MemberExpression me2)
					{
						if (b.Member != me2.Member && me2.Member.Name == Extensions.IdColName && me2.Expression is MemberExpression innerMember)
							selectColumns.Map.Add(b.Member, innerMember.Member);
						else
							selectColumns.Map.Add(b.Member, me2.Member);

						if (me2.Member != ma.Member && !me2.Member.TryGetVisitor(out _))
							Context.Members[(i.Type, ma.Member)] = me2;
					}
				}
			}

			selectColumns.Add(b.Member, (Curr, maExp));
		}

		Curr = curr;

		Context.SelectColumns.Add(selectColumns);

		return i;
	}

	protected override Expression VisitNew(NewExpression n)
	{
		var curr = Context.Curr;

		var isGroup = Context.IsGroup;
		var isSelect = Context.IsSelect;

		if (!isGroup && !isSelect)
			return n;

		var selCols = new ContextSelectColumns();

		var argCount = n.Arguments.Count;

		for (var i = 0; i < argCount; i++)
		{
			MemberInfo member;

			if (n.Members is not null)
			{
				member = n.Members[i];
			}
			else
			{
				// Positional constructor (e.g. record Dto(long Id, string Name)) — the
				// compiler does not populate NewExpression.Members. Derive the member
				// from the ctor parameter name by matching a property on the result type.
				var paramName = n.Constructor.GetParameters()[i].Name;
				member = n.Type.GetProperty(paramName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
					?? throw new NotSupportedException($"Cannot map ctor parameter '{paramName}' to a property of {n.Type}.");
			}

			if (!isSelect && i > 0)
				Curr.Comma();

			var arg = n.Arguments[i];

			if (arg is MemberExpression me &&
				me.Member is PropertyInfo pi &&
				pi.Name == nameof(IGrouping<int, int>.Key) &&
				pi.ReflectedType.GetGenericType(typeof(IGrouping<,>)) is not null)
			{
				// `g.Key` in a Select-after-GroupBy projection refers back to
				// the column the source was grouped on. Re-visit the saved
				// GroupBy key-selector body so the SELECT list emits, e.g.,
				// [e].[Priority] AS [Key] alongside the resolved columns.
				if (isSelect && Context.GroupKeySelector is { } keyBody)
				{
					Context.Curr = new();
					Visit(keyBody);
					selCols.Add(member, (Context.Curr, keyBody));
					Context.Curr = curr;
				}
			}
			else
			{
				var ctx = Context;

				if (isGroup)
				{
					Context = new();

					// Inherit outer alias bindings so members like `i.Field`
					// in a GroupBy key-selector resolve to the running table
					// alias instead of leaking the lambda parameter name as
					// a raw SQL alias.
					foreach (var kv in ctx.Aliases)
						Context.Aliases[kv.Key] = kv.Value;

					// Inherit JoinParts so transparent-id chains like
					// `td.i.Field` reach `ResolveTransIdAlias` with the join
					// alias `i` visible — otherwise key members from a
					// JOINED table get attributed to the main FROM alias.
					foreach (var jp in ctx.JoinParts)
						Context.JoinParts.Add(jp);

					foreach (var preName in ctx.PreknownJoinAliases)
						Context.PreknownJoinAliases.Add(preName);

					Context.TableAlias = ctx.TableAlias;
					Context.Curr = Context.GroupByPart;
				}
				else if (isSelect)
					Context.Curr = new();

				Visit(n.Arguments[i]);

				if (isGroup)
				{
					selCols.Add(member, (Context.Curr, arg));

					var subParams = Context.Parameters;

					var actions = Context.Curr.Actions.ToArray();

					ctx.Curr.Actions.AddRange(actions);

					Context = ctx;

					Context.AddParamsFromSubquery(subParams);
				}
				else if (isSelect)
				{
					if (arg is MemberExpression me2)
					{
						if (Context.SelectColumns.Count > 0)
						{
							var prev = Context.SelectColumns.Last();
							prev.Map.Add(member, me2.Member);

							selCols.Add(me2.Member, (Context.Curr, arg));
						}
						else
						{
							// Top-level anonymous projection over a table: no previous select
							// layer to attach Map to. Keep the rename map on the current
							// selCols and use the anonymous member as the key so rendering
							// emits the AS alias correctly.
							selCols.Map.Add(member, me2.Member);
							selCols.Add(member, (Context.Curr, arg));
						}
					}
					else if (Context.Curr.Actions.Count > 0)
					{
						// Computed projection (e.g. aggregate g.Count() / g.Sum(x.P) /
						// g.Max(x.P) / g.Min(x.P)) — Visit already rendered the SQL
						// fragment into Context.Curr; capture it under the result-DTO
						// member so Build emits "<sql> AS [Member]".
						selCols.Add(member, (Context.Curr, arg));
					}

					Context.Curr = curr;
				}
			}
		}

		Context.SelectColumns.Add(selCols);

		return n;
	}

	protected override Expression VisitUnary(UnaryExpression u)
	{
		switch (u.NodeType)
		{
			case ExpressionType.Not:
				if (u.Operand is MemberExpression me && me.Type == typeof(bool))
				{
					Curr.OpenBracket();

					Visit(u.Operand);
					Curr.IsFalse();

					Curr.CloseBracket();
				}
				else
				{
					Curr.Not();
					Curr.OpenBracket();

					Visit(u.Operand);

					Curr.CloseBracket();
				}
				
				break;
			case ExpressionType.Convert:
				if (u.Method is not null)
				{
					if (u.Method.TryGetVisitor(out var visitor))
					{
						visitor.Visit(this, u.Operand);
						break;
					}
					else
						throw new NotSupportedException();
				}
				else
					Visit(u.Operand);
				break;
			case ExpressionType.ArrayLength:
				Curr.DataLength();
				Curr.OpenBracket();
				Visit(u.Operand);
				Curr.CloseBracket();
				break;
			case ExpressionType.Negate:
				Curr.Raw(" -1 * ");
				ProcessInitExpression(u.Operand);
				break;
			case ExpressionType.Quote:
				Visit(u.StripQuotes());
				break;
			default:
				throw new NotSupportedException($"The unary operator '{u.NodeType}' is not supported");
		}

		return u;
	}

	protected override Expression VisitBinary(BinaryExpression b)
	{
		// `entityRef == null` / `entityRef != null` after a LEFT JOIN must
		// compare the joined identity column, not the entity reference
		// itself — emitting "[alias].* IS NULL" produces invalid SQL. Rewrite
		// to "entityRef.<Identity> {==,!=} null" before continuing.
		if ((b.NodeType == ExpressionType.Equal || b.NodeType == ExpressionType.NotEqual)
			&& TryRewriteEntityNullComparison(b, out var rewritten))
		{
			return Visit(rewritten);
		}

		Curr.OpenBracket();

		if (b.NodeType == ExpressionType.Coalesce)
		{
			Curr
				.IsNull()
				.OpenBracket();
		}

		var isLeftBool = b.Left is MemberExpression leftMe && leftMe.Member.GetMemberType() == typeof(bool) && (b.NodeType == ExpressionType.AndAlso || b.NodeType == ExpressionType.OrElse);

		if (isLeftBool)
			Curr.OpenBracket();

		Visit(b.Left);

		switch (b.NodeType)
		{
			case ExpressionType.And:
				if (b.Type == typeof(int))
					Curr.BitwiseAnd();
				else
					Curr.And();
				break;

			case ExpressionType.AndAlso:
				if (isLeftBool)
					Curr.IsTrue().CloseBracket();

				Curr.And();
				break;

			case ExpressionType.Or:
				if (b.Type == typeof(int))
					Curr.BitwiseOr();
				else
					Curr.Or();
				break;

			case ExpressionType.OrElse:
				if (isLeftBool)
					Curr.IsTrue().CloseBracket();

				Curr.Or();
				break;

			case ExpressionType.Equal:
				if (b.Right.IsNullConstant())
				{
					Curr.Is();
				}
				else
				{
					Curr.Equal();
				}
				break;

			case ExpressionType.NotEqual:
				if (b.Right.IsNullConstant())
				{
					Curr.IsNot();
				}
				else
				{
					Curr.NotEqual();
				}
				break;

			case ExpressionType.LessThan:
				Curr.Less();
				break;

			case ExpressionType.LessThanOrEqual:
				Curr.LessOrEqual();
				break;

			case ExpressionType.GreaterThan:
				Curr.More();
				break;

			case ExpressionType.GreaterThanOrEqual:
				Curr.MoreOrEqual();
				break;

			case ExpressionType.Coalesce:
				Curr.Comma();
				break;

			case ExpressionType.Add:
				if (b.Type == typeof(string))
					Curr.Concat();
				else
					Curr.Plus();
				break;

			default:
				throw new NotSupportedException($"The binary operator '{b.NodeType}' is not supported");

		}

		var isRightBool = (b.NodeType == ExpressionType.AndAlso || b.NodeType == ExpressionType.OrElse) && b.Right is MemberExpression rightMe && rightMe.Type == typeof(bool);

		if (isRightBool)
			Curr.OpenBracket();

		Visit(b.Right);

		if (isRightBool)
			Curr.IsTrue().CloseBracket();
		else if (b.NodeType == ExpressionType.Coalesce)
			Curr.CloseBracket();

		Curr.CloseBracket();
		return b;
	}

	private static bool TryRewriteEntityNullComparison(BinaryExpression b, out BinaryExpression rewritten)
	{
		rewritten = null;

		Expression entitySide;
		bool nullOnRight;

		if (b.Right.IsNullConstant())
		{
			entitySide = b.Left;
			nullOnRight = true;
		}
		else if (b.Left.IsNullConstant())
		{
			entitySide = b.Right;
			nullOnRight = false;
		}
		else
		{
			return false;
		}

		// Plain scalars/enums fall through to the existing null path; only
		// entity refs (a registered schema with an identity column) need
		// the column substitution.
		if (!SchemaRegistry.TryGet(entitySide.Type, out var schema) || schema.Identity is null)
			return false;

		var identityMember = (MemberInfo)entitySide.Type.GetProperty(schema.Identity.Name)
			?? entitySide.Type.GetField(schema.Identity.Name);

		if (identityMember is null)
			return false;

		Expression idSide = Expression.MakeMemberAccess(entitySide, identityMember);

		// Lift the identity to its nullable form so the constructed
		// BinaryExpression is well-typed against null.
		if (idSide.Type.IsValueType && Nullable.GetUnderlyingType(idSide.Type) is null)
			idSide = Expression.Convert(idSide, typeof(Nullable<>).MakeGenericType(idSide.Type));

		var nullSide = Expression.Constant(null, idSide.Type);

		rewritten = nullOnRight
			? Expression.MakeBinary(b.NodeType, idSide, nullSide)
			: Expression.MakeBinary(b.NodeType, nullSide, idSide);

		return true;
	}

	protected override Expression VisitConstant(ConstantExpression c)
	{
		var value = c.Value;

		if (value is not IQueryable)
		{
			if (value is null)
			{
				Curr.Null();
			}
			else if (value is bool bVal)
			{
				Curr.AddAction((d, sb) => sb.Append(bVal ? d.TrueLiteral : d.FalseLiteral));
			}
			else if (value is string str)
			{
				Curr.Param(Context.TryAddParam("p", typeof(string), str));
			}
			else
			{
				if (value is Enum)
					value = value.To<long>();

				Curr.Raw(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}", value));
			}
		}

		return c;
	}

	/// <summary>
	/// Walks the transparent-identifier chain produced by C#'s desugaring of
	/// nested LINQ <c>from</c>/<c>join</c>/<c>select</c> clauses and returns
	/// the SQL alias the leaf .Id refers to. Each member name is checked
	/// against the registered <see cref="Context.JoinParts"/>; if none
	/// matches, the chain's root <see cref="ParameterExpression"/> is
	/// resolved through <see cref="GetAlias(ParameterExpression)"/>, which
	/// falls back to the main FROM alias.
	/// </summary>
	private string ResolveTransIdAlias(MemberExpression chain)
	{
		while (chain is not null)
		{
			if (Context.JoinParts.Any(j => j.tableAlias.EqualsIgnoreCase(chain.Member.Name)) ||
				Context.PreknownJoinAliases.Contains(chain.Member.Name))
				return chain.Member.Name;

			if (chain.Expression is ParameterExpression pe)
				return GetAlias(pe);

			chain = chain.Expression as MemberExpression;
		}

		return Context.TableAlias.IsEmpty() ? Extensions.DefaultAlias : Context.TableAlias;
	}

	private string GetAlias(string alias)
	{
		if (Context.CurrJoinAlias.origAlias == alias)
			return Context.CurrJoinAlias.modifiedAlias;

		// If this alias belongs to a joined table, keep it as-is. Also honour
		// the pre-walked alias set so transparent-id resolution at projection
		// time (before the source visit registers the real JoinParts entry)
		// already keeps the join alias instead of collapsing onto the main
		// FROM alias.
		if (Context.JoinParts.Any(j => j.tableAlias.EqualsIgnoreCase(alias)) ||
			(alias is not null && Context.PreknownJoinAliases.Contains(alias)))
			return alias;

		// Otherwise, map to the main table alias.
		// When TableAlias is not yet set (e.g. inside a subquery before Build),
		// return the alias as-is so both inner and outer scope references are preserved.
		return Context.TableAlias.IsEmpty() ? alias : Context.TableAlias;
	}

	/// <summary>
	/// Lookup overload that resolves a <see cref="ParameterExpression"/>
	/// directly through <see cref="Context.Aliases"/> when the parameter
	/// has been registered by the enclosing LINQ-method handler. The
	/// stored value is already the canonical alias and must NOT be piped
	/// through the string-based fallback — that would remap every alias
	/// to <see cref="Context.TableAlias"/> and erase outer-scope
	/// references in sub-queries that inherit the parent's alias map.
	/// Falls back to the string-based <see cref="GetAlias(string)"/> when
	/// the parameter is not registered, so callers do not need to know
	/// whether registration happened.
	/// </summary>
	private string GetAlias(ParameterExpression parameter)
	{
		if (parameter is not null && Context.Aliases.TryGetValue(parameter, out var alias))
			return alias;

		return GetAlias(parameter?.Name);
	}

	/// <summary>
	/// Registers all parameters of <paramref name="lambda"/> against the
	/// supplied <paramref name="alias"/>. Used by Where/Select/Join/OrderBy
	/// lambdas to seed <see cref="Context.Aliases"/>.
	/// </summary>
	private void RegisterLambdaParameters(LambdaExpression lambda, string alias)
	{
		if (lambda is null || alias is null)
			return;

		foreach (var parameter in lambda.Parameters)
		{
			if (parameter is null)
				continue;

			Context.Aliases[parameter] = alias;
		}
	}

	protected override Expression VisitMember(MemberExpression m)
	{
		if (m.Expression != null/* && m.Expression.NodeType == ExpressionType.Parameter*/)
		{
			if (Context.Members.TryGetValue((m.Expression.Type, m.Member), out var exp))
			{
				Visit(exp);
				return exp;
			}

			// Any chain of member accesses rooted at a ConstantExpression
			// (compiler-generated DisplayClass capture, two-level container
			// access, deep nested-if locals — they all reduce to the same
			// shape) must be materialised once into a parameter; otherwise
			// the chain falls through to the column-emission branches and
			// the captured local name leaks as a raw SQL column.
			if (ClosureMaterializer.TryEvaluate(m, out var capturedValue))
			{
				EmitMaterialisedValue(m.Member, capturedValue);
				return m;
			}

			if (m.Expression.NodeType == ExpressionType.MemberAccess)
			{
				if (m.Member.TryGetVisitor(out var visitor))
				{
					visitor.Visit(this, m);
					return m;
				}

				var me = (MemberExpression)m.Expression;

				if (m.Member.Name == Extensions.IdColName)
				{
					if (me.Member.DeclaringType.IsAutoGenerated())
					{
						// Walk the transparent-id chain (e.g. `td2.td.p` or
						// `td2.t2`) outwards from the leaf until we hit either
						// a member name that matches a registered JOIN alias
						// (LEFT/INNER) — that's the table for the .Id we want —
						// or the original lambda parameter, which resolves to
						// the main FROM alias.
						Curr.Column(ResolveTransIdAlias(me), m.Member.Name);
					}
					else if (!TryEmitFromResolver(m))
					{
						var owner = GetAlias(me.GetMemberName());
						Curr.Column(owner, me.Member.Name);
					}
				}
				else if (me.Type.IsAutoGenerated())
				{
					// `g.Key.X` in a Select-after-GroupBy projection: instead of
					// letting the string-based GetAlias fallback emit `[X].*`,
					// look up the X-th member in the saved GroupKeySelector body
					// and visit THAT expression. Otherwise key members that
					// originated from a JOINED table (e.g. `er.Priority` rolled
					// into `Key.Priority`) get wrongly attributed to the main
					// FROM alias instead of the join alias.
					if (me.Member.Name == nameof(IGrouping<int, int>.Key) &&
						me.Member.ReflectedType?.GetGenericType(typeof(IGrouping<,>)) is not null &&
						Context.GroupKeySelector is NewExpression keyNew &&
						keyNew.Members is { } keyMembers)
					{
						for (var ki = 0; ki < keyMembers.Count; ki++)
						{
							if (keyMembers[ki].Name == m.Member.Name)
							{
								Visit(keyNew.Arguments[ki]);
								return m;
							}
						}
					}

					var owner = GetAlias(m.GetMemberName());

					if (owner == m.Member.Name)
						Curr.All(owner);
					else
						Curr.Column(owner, m.Member.Name);
				}
				else if (me.Expression is ParameterExpression pe && me.Member.DeclaringType.IsAutoGenerated() && !me.Member.GetMemberType().IsAutoGenerated() && !pe.Type.IsAutoGenerated())
				{
					Curr.Column(pe.Name, m.Member.Name);
				}
				else if (me.Expression is ParameterExpression anonPe &&
					anonPe.Type.IsAutoGenerated() &&
					Context.AnonProjectionAliases.TryGetValue((anonPe.Type, me.Member), out var anonAlias))
				{
					// `p.Detail.X` where p is an anonymous projection from a
					// Join's result selector — resolve `Detail` through the
					// recorded member→alias map so the leaf column gets the
					// underlying join alias instead of the main FROM alias.
					Curr.Column(anonAlias, m.Member.Name);
				}
				else
				{
					if (Context.Members.TryGetValue((me.Expression.Type, me.Member), out var exp1))
						me = exp1;

					if (!TryEmitFromResolver(m.Update(me)))
					{
						// Walk the transparent-id chain to the actual lambda
						// parameter and use its registered alias — using
						// <c>me.Member.Name</c> (the field name on the
						// compiler-generated transparent identifier) leaks as
						// a fake SQL alias and breaks any chain longer than
						// one hop.
						Curr.Column(ResolveTransIdAlias(me), m.Member.Name);
					}
				}
			}
			else
			{
				if (m.Member.TryGetVisitor(out var visitor))
				{
					visitor.Visit(this, m);
					return m;
				}

				var columns = Context.IsWhere && m.Member.GetMemberType() != typeof(long?) ? Context.GetColumns(m.Member, default, default, true) : null;

				if (columns is null)
				{
					// Resolve through Context.Aliases first when the member's
					// container is a parameter — that way a correlated reference
					// `outerParam.Field` inside a sub-query keeps the outer FROM
					// alias instead of being remapped to the sub-query's own
					// TableAlias by the string-based fallback.
					var pe = m.Expression as ParameterExpression
						?? (m.Expression as UnaryExpression)?.Operand as ParameterExpression;

					var name = pe is not null
						? GetAlias(pe)
						: GetAlias(m.GetMemberName());

					if (name == m.Member.Name)
						Curr.All(name);
					else
					{
						Curr.Column(name, m.Member.Name);
					}
				}
				else
				{
					var arr = columns.ToArray();

					if (arr.Length != 1)
						throw new InvalidOperationException();

					arr[0].Item1.CopyTo(Curr);
				}
			}

			return m;
		}
		else
		{
			if (m.Member.TryGetVisitor(out var visitor))
			{
				visitor.Visit(this, m);
				return m;
			}
			else
			{

			}
		}

		throw new NotSupportedException($"The member '{m.Member.Name}' is not supported");
	}

	/// <summary>
	/// Tries to resolve a member chain through the pure
	/// <see cref="MemberPathResolver"/>, register the implicit joins it
	/// reports, and emit the qualified leaf column. Returns false when the
	/// resolver cannot recognise the chain shape so the caller can fall
	/// back to the legacy resolution branches.
	/// </summary>
	private bool TryEmitFromResolver(MemberExpression m)
	{
		// Walk to the chain root: if it's a registered ParameterExpression
		// (e.g. a JOIN's lambda parameter), the resolver must use THAT
		// parameter's alias as the root, not blindly fall back to the main
		// `TableAlias` — otherwise `b.Cat.Id` on the join parameter `b`
		// becomes `[e].[Cat]` against the main FROM table that has no
		// `Cat` column.
		Expression cur = m;
		while (cur is MemberExpression me)
			cur = me.Expression;

		string rootAlias = null;
		if (cur is ParameterExpression pe)
			rootAlias = GetAlias(pe);

		if (rootAlias.IsEmpty())
			rootAlias = Context.TableAlias.IsEmpty() ? Extensions.DefaultAlias : Context.TableAlias;

		var res = MemberPathResolver.Resolve(m, rootAlias);

		if (res is null)
			return false;

		foreach (var jp in res.RequiredJoins)
			RegisterJoinPlan(jp);

		Curr.Column(res.Column.Alias, res.Column.Name);
		return true;
	}

	/// <summary>
	/// Registers a <see cref="JoinPlan"/> in <see cref="Context.JoinParts"/>
	/// idempotently — a join is added only once per alias.
	/// </summary>
	private void RegisterJoinPlan(JoinPlan jp)
	{
		if (Context.JoinParts.Any(j => j.tableAlias.EqualsIgnoreCase(jp.Alias)))
			return;

		var joinPart = new Query();

		if (jp.Kind == JoinKind.Inner)
			joinPart.InnerJoin();
		else
			joinPart.LeftJoin();

		joinPart
			.Table(jp.Table, jp.Alias)
			.On()
			.Column(jp.Alias, jp.OnChildColumn)
			.Equal()
			.Column(jp.ParentAlias, jp.OnParentColumn)
			.NewLine();

		Context.JoinParts.Add((jp.Alias, joinPart));
	}

	private void ParseOrderByExpression(MethodCallExpression expression, bool asc)
	{
		var curr = Curr;

		Curr = Context.OrderByPart;

		try
		{
			var lambdaExpression = expression.Arguments[1].GetOperand();

			RegisterLambdaParameters(lambdaExpression, Context.TableAlias.IsEmpty() ? Extensions.DefaultAlias : Context.TableAlias);

			var lambdaBody = lambdaExpression.Body;

			// Expression<Func<T, object>> over a value-type member emits Convert(member, object).
			if (lambdaBody is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } conv)
				lambdaBody = conv.Operand;

			var rootAlias = Context.TableAlias.IsEmpty() ? Extensions.DefaultAlias : Context.TableAlias;

			if (lambdaBody is MemberExpression body)
			{
				Context.OrderBy.Insert(0, ResolveOrderByColumn(body, rootAlias, asc));
			}
			else if (lambdaBody is BinaryExpression { NodeType: ExpressionType.Coalesce } coalesce)
			{
				// `OrderBy(e => e.A ?? e.B)` — primary column with a fallback.
				// Compiler may wrap either arm in Convert when widening a non-
				// nullable side to the result type; strip it before resolving.
				var primary = ResolveOrderByColumn(UnwrapConvert(coalesce.Left), rootAlias, asc);
				var fallback = ResolveOrderByColumn(UnwrapConvert(coalesce.Right), rootAlias, asc);

				Context.OrderBy.Insert(0, primary with
				{
					FallbackAlias = fallback.Alias,
					FallbackColumnName = fallback.ColumnName,
				});
			}
			else if (lambdaBody is MethodCallExpression call)
			{
				if (!call.Method.TryGetVisitor(out var visitor))
					throw new NotSupportedException();

				visitor.Visit(this, call);

				if (!asc)
					Curr.Desc();
			}
			else
				throw new NotSupportedException();
		}
		finally
		{
			Curr = curr;
		}
	}

	/// <summary>
	/// Resolves an ORDER BY <see cref="MemberExpression"/> into a column
	/// reference for <see cref="Context.OrderBy"/>. Uses the join-aware
	/// <see cref="MemberPathResolver"/> when the chain rooted at a parameter
	/// can be classified, otherwise falls back to an unqualified bare column
	/// name (the legacy behaviour for projection / grouping outputs).
	/// </summary>
	private OrderByEntry ResolveOrderByColumn(Expression body, string rootAlias, bool asc)
	{
		if (body is MemberExpression me)
		{
			if (MemberPathResolver.Resolve(me, rootAlias) is { } res)
			{
				foreach (var jp in res.RequiredJoins)
					RegisterJoinPlan(jp);

				return new(res.Column.Alias, res.Column.Name, asc);
			}

			return new(null, me.Member.Name, asc);
		}

		throw new NotSupportedException($"Cannot use {body.NodeType} as an ORDER BY column.");
	}

	/// <summary>
	/// Strips a single <c>Convert</c>/<c>ConvertChecked</c> wrapper — the
	/// compiler inserts one around either arm of <c>??</c> when widening a
	/// non-nullable side to the nullable result type.
	/// </summary>
	private static Expression UnwrapConvert(Expression e)
		=> e is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } u ? u.Operand : e;

	/// <summary>
	/// Resolves a single join-key <see cref="MemberExpression"/> into the
	/// schema, alias and physical column name that participate in the ON
	/// clause. Shared by both <c>Join</c> and <c>GroupJoin</c>.
	///
	/// The walk handles three layered cases together:
	/// <list type="bullet">
	/// <item>Bare member access (<c>x.Foo</c>).</item>
	/// <item>FK-via-Id (<c>x.Person.Id</c>) — the leaf <c>.Id</c> is
	/// folded into the FK column name on the parent side.</item>
	/// <item>Transparent identifier (<c>tdx.t.Foo</c>) emitted by the
	/// compiler for chained joins — the join target is recovered from
	/// the inner-most non-anonymous declaring type.</item>
	/// </list>
	/// </summary>
	private static (Schema meta, string tableAlias, string column) ResolveJoinMember(MemberExpression member, Type fallbackOwner)
	{
		var tableAlias = member.Expression is ParameterExpression pe
			? pe.Name
			: member.GetMemberName();

		var memberName = member.Member.Name;
		var owner = fallbackOwner;

		if (memberName == Extensions.IdColName && member.Expression is MemberExpression innerMember)
		{
			if (innerMember.Member.DeclaringType.IsAutoGenerated())
			{
				// Transparent identifier wrapping the navigation property.
				owner = innerMember.Member.GetMemberType();
			}
			else
			{
				memberName = innerMember.Member.Name;

				if (!innerMember.Member.DeclaringType.IsAbstract)
					owner = innerMember.Member.DeclaringType;
			}
		}

		if (owner.IsAutoGenerated() && member.Expression is MemberExpression me2)
		{
			// Outer-Join transparent identifier shape: surface the
			// concrete navigation owner in place of the synthesised type.
			owner = me2.Type;
			memberName = member.Member.Name;
		}

		var meta = SchemaRegistry.Get(owner);
		return (meta, tableAlias, memberName);
	}

	/// <summary>
	/// Parses an inner/outer key-selector lambda into its
	/// <see cref="ResolveJoinMember"/> components, transparently
	/// stripping <c>Convert</c> wrappers and forwarding bare-parameter
	/// keys (<c>p =&gt; p</c>) to the implicit <c>Id</c> column.
	/// </summary>
	private static (Schema meta, string tableAlias, string column) ParseScalarJoinKey(Expression keyLambda)
	{
		var operand = keyLambda.GetOperand();

		if (operand.Body is ParameterExpression pe1)
			return (default, pe1.Name, Extensions.IdColName);

		var member = (MemberExpression)(operand.Body is UnaryExpression ue ? ue.Operand : operand.Body);

		return ResolveJoinMember(member, operand.Parameters[0].Type);
	}

	/// <summary>
	/// Parses a multi-column key-selector lambda (anonymous-type
	/// composite key as used by <c>GroupJoin</c>) into per-column
	/// participants. Constants and unary-wrapped constants pass
	/// through as raw <c>(false, value)</c> entries.
	/// </summary>
	private static (Schema meta, string tableAlias, (bool isColumn, object value)[] columns) ParseCompositeJoinKey(Expression keyLambda)
	{
		static object BoolAsInt(object value) => value is bool b ? (b ? 1 : 0) : value;

		var operand = keyLambda.GetOperand();
		var fallbackOwner = operand.Parameters[0].Type;

		Schema meta = default;
		string tableAlias = default;
		var cols = new List<(bool isColumn, object value)>();

		void AddColumnFromMember(MemberExpression member)
		{
			var (m, a, c) = ResolveJoinMember(member, fallbackOwner);
			meta = m;
			tableAlias = a;
			cols.Add((true, c));
		}

		if (operand.Body is NewExpression newExp)
		{
			foreach (var arg in newExp.Arguments)
			{
				var ae = arg.StripQuotes();

				if (ae is MemberExpression member)
					AddColumnFromMember(member);
				else if (ae is UnaryExpression u)
					cols.Add((false, BoolAsInt(u.Operand.GetConstant<object>())));
				else
					cols.Add((false, BoolAsInt(ae.GetConstant<object>())));
			}
		}
		else
		{
			AddColumnFromMember((MemberExpression)operand.Body);
		}

		return (meta, tableAlias, cols.ToArray());
	}

	/// <summary>
	/// Emits a value reified from a constant chain by
	/// <see cref="ClosureMaterializer.TryEvaluate"/> into the current
	/// query builder: scalars become parameters, arrays expand into a
	/// comma-separated parameter list, <see cref="IQueryable"/> values
	/// are re-visited so their expression tree contributes joins and
	/// parameters, and nulls collapse to <c>NULL</c>.
	/// </summary>
	private void EmitMaterialisedValue(MemberInfo leafMember, object value)
	{
		if (value is IQueryable q)
		{
			// Captured local IQueryable<T>: re-visit the wrapped expression
			// when it carries query operators, otherwise emit nothing.
			// Constant-rooted queryables (the common Query<T>() shape)
			// represent the bare table — the surrounding sub-query handler
			// resolves the element type to a FROM, so a value-emit here
			// would corrupt the sub-query's WHERE with a non-bindable
			// IQueryable parameter.
			if (q.Expression is not ConstantExpression)
				Visit(q.Expression);
			return;
		}

		if (value is null)
		{
			Curr.Null();
			return;
		}

		if (value is Array arr && value is not byte[])
		{
			var itemType = arr.GetType().GetItemType();

			for (var i = 0; i < arr.Length; i++)
			{
				if (i > 0)
					Curr.Comma();

				Curr.Param(Context.TryAddParam(leafMember.Name, itemType, arr.GetValue(i)));
			}
			return;
		}

		Curr.Param(Context.TryAddParam(leafMember.Name, leafMember.GetMemberType(), value));
	}

}