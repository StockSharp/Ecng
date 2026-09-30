#if NET10_0_OR_GREATER

namespace Ecng.Tests.Data;

using System.Linq.Expressions;

using Ecng.Data;
using Ecng.Data.Sql;
using Ecng.Linq;
using Ecng.Serialization;

/// <summary>
/// SQL-generation tests for GROUP BY + HAVING. Closes the gap where these
/// constructs were exercised only by integration tests against a real DB,
/// leaving translator regressions invisible until full-stack runs.
/// </summary>
[TestClass]
public class GroupByTranslationTests : BaseTestClass
{
	private static IQueryable<T> CreateQueryable<T>()
		=> new DefaultQueryable<T>(new DefaultQueryProvider<T>(new DummyQueryContext()), null);

	private sealed class DummyQueryContext : IQueryContext
	{

		IAsyncEnumerable<TResult> IQueryContext.ExecuteEnumAsync<TSource, TResult>(Expression expression)
			=> throw new NotSupportedException();

		ValueTask IQueryContext.ExecuteAsync<TSource>(Expression expression)
			=> throw new NotSupportedException();

		ValueTask<TResult> IQueryContext.ExecuteResultAsync<TSource, TResult>(Expression expression)
			=> throw new NotSupportedException();
	}

	private static string Translate<TSource>(IQueryable queryable)
		=> Translate<TSource>(queryable.Expression);

	private static string Translate<TSource>(Expression expression)
	{
		var translator = new ExpressionQueryTranslator(SchemaRegistry.Get(typeof(TSource)));
		return translator.GenerateSql(expression).Render(SqlServerDialect.Instance);
	}

	[TestMethod]
	public void GroupBy_SingleKey_WithCount_EmitsGroupByClause()
	{
		var items = CreateQueryable<TestItem>();

		var query = items
			.GroupBy(i => i.Priority)
			.Select(g => new { g.Key, Count = g.Count() });

		var sql = Translate<TestItem>(query);

		sql.ContainsIgnoreCase("group by").AssertTrue($"Expected GROUP BY clause, got: {sql}");
		sql.Contains("[Priority]").AssertTrue($"Expected grouping key column [Priority], got: {sql}");
		sql.ContainsIgnoreCase("count(").AssertTrue($"Expected COUNT aggregate, got: {sql}");
	}

	[TestMethod]
	public void GroupBy_WithSum_EmitsAggregateInSelect()
	{
		var items = CreateQueryable<TestItem>();

		var query = items
			.GroupBy(i => i.Priority)
			.Select(g => new { g.Key, Total = g.Sum(i => i.Price) });

		var sql = Translate<TestItem>(query);

		sql.ContainsIgnoreCase("group by").AssertTrue($"Expected GROUP BY clause, got: {sql}");
		sql.ContainsIgnoreCase("sum(").AssertTrue($"Expected SUM aggregate, got: {sql}");
		sql.Contains("[Price]").AssertTrue($"Expected aggregated column [Price], got: {sql}");
	}

	[TestMethod]
	public void GroupBy_WhereOnGrouping_PromotesToHaving_NotWhere()
	{
		var items = CreateQueryable<TestItem>();

		var query = items
			.GroupBy(i => i.Priority)
			.Where(g => g.Count() > 5)
			.Select(g => new { g.Key, Count = g.Count() });

		var sql = Translate<TestItem>(query);

		sql.ContainsIgnoreCase("group by").AssertTrue($"Expected GROUP BY clause, got: {sql}");
		sql.ContainsIgnoreCase("having").AssertTrue(
			$"Expected HAVING clause for filter over IGrouping, got: {sql}");
	}

	[TestMethod]
	public void GroupBy_WhereBeforeGroup_StaysAsWhere()
	{
		// Filter on the source rows must remain a WHERE — the HAVING upgrade
		// must trigger only when the lambda parameter is IGrouping<,>.
		var items = CreateQueryable<TestItem>();

		var query = items
			.Where(i => i.IsActive)
			.GroupBy(i => i.Priority)
			.Select(g => new { g.Key, Count = g.Count() });

		var sql = Translate<TestItem>(query);

		sql.ContainsIgnoreCase("where").AssertTrue($"Expected WHERE clause for source filter, got: {sql}");
		sql.ContainsIgnoreCase("group by").AssertTrue($"Expected GROUP BY clause, got: {sql}");
		sql.Contains("[IsActive]").AssertTrue($"Expected source filter on [IsActive], got: {sql}");
	}

	[TestMethod]
	public void GroupBy_OverNavigationProperty_RegistersJoinAndGroupsByFkColumn()
	{
		// GroupBy(t => t.Person.Id) should resolve through the FK column on
		// the source table — the same FK shortcut that works in WHERE.
		SchemaRegistry.Get(typeof(TestPerson));
		var tasks = CreateQueryable<TestTask>();

		var query = tasks
			.GroupBy(t => t.Person.Id)
			.Select(g => new { PersonId = g.Key, Count = g.Count() });

		var sql = Translate<TestTask>(query);

		sql.ContainsIgnoreCase("group by").AssertTrue($"Expected GROUP BY clause, got: {sql}");
		sql.Contains("[Person]").AssertTrue($"Expected FK column [Person] in GROUP BY, got: {sql}");
	}

	[TestMethod]
	public void GroupBy_WithMaxAggregate_EmitsMaxFunction()
	{
		var items = CreateQueryable<TestItem>();

		var query = items
			.GroupBy(i => i.Priority)
			.Select(g => new { g.Key, MaxPrice = g.Max(i => i.Price) });

		var sql = Translate<TestItem>(query);

		sql.ContainsIgnoreCase("max(").AssertTrue($"Expected MAX aggregate, got: {sql}");
	}

	[TestMethod]
	public void GroupBy_WithMinAggregate_EmitsMinFunction()
	{
		var items = CreateQueryable<TestItem>();

		var query = items
			.GroupBy(i => i.Priority)
			.Select(g => new { g.Key, MinPrice = g.Min(i => i.Price) });

		var sql = Translate<TestItem>(query);

		sql.ContainsIgnoreCase("min(").AssertTrue($"Expected MIN aggregate, got: {sql}");
	}

	// A grouped view: the aggregates are members of the projected entity, so a filter on them
	// reaches the translator with the entity as the lambda parameter, not the grouping.
	private static IQueryable<VTestItemPriorityCount> PriorityCounts(IQueryable<TestItem> items)
		=>
			from i in items
			group i by new { i.Priority, i.IsActive } into g
			select new VTestItemPriorityCount
			{
				Id = 0,
				Priority = g.Key.Priority,
				IsActive = g.Key.IsActive,
				Count = g.Count(),
				Total = g.Sum(i => i.Price),
				Rank = g.Key.Priority == default ? -g.Count() : g.Key.Priority,
				LastId = g.Max(i => (long?)i.Id),
			};

	private Expression CountOf<T>(IQueryable<T> source)
		=> Expression.Call(
			typeof(QueryableExtensions).GetMethod(nameof(QueryableExtensions.CountAsync)).MakeGenericMethod(typeof(T)),
			source.Expression,
			Expression.Constant(CancellationToken));

	/// <summary>
	/// Returns the text of the clause that starts with <paramref name="keyword"/> up to the next
	/// clause keyword, or <see langword="null"/> when the query has no such clause.
	/// </summary>
	private static string Clause(string sql, string keyword)
	{
		var start = sql.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);

		if (start < 0)
			return null;

		start += keyword.Length;

		var end = new[] { "where", "group by", "having", "order by", "offset", "select" }
			.Select(k => sql.IndexOf(k, start, StringComparison.OrdinalIgnoreCase))
			.Where(i => i >= 0)
			.DefaultIfEmpty(sql.Length)
			.Min();

		return sql[start..end];
	}

	[TestMethod]
	public void GroupedView_WhereOnAggregateMember_GoesToHaving()
	{
		var min = 3;
		var query = PriorityCounts(CreateQueryable<TestItem>()).Where(v => v.Count >= min);

		var sql = Translate<TestItem>(query);

		var having = Clause(sql, "having");
		IsNotNull(having, $"Expected HAVING clause for the aggregate filter, got: {sql}");
		having.ContainsIgnoreCase("count(").AssertTrue($"Expected COUNT in HAVING, got: {sql}");
		(Clause(sql, "where")?.ContainsIgnoreCase("count(") == true).AssertFalse($"An aggregate must not appear in WHERE, got: {sql}");
	}

	[TestMethod]
	public void GroupedView_WhereOnSumMember_GoesToHaving()
	{
		var min = 10m;
		var query = PriorityCounts(CreateQueryable<TestItem>()).Where(v => v.Total > min);

		var sql = Translate<TestItem>(query);

		(Clause(sql, "having")?.ContainsIgnoreCase("sum(") == true).AssertTrue($"Expected SUM in HAVING, got: {sql}");
		(Clause(sql, "where")?.ContainsIgnoreCase("sum(") == true).AssertFalse($"An aggregate must not appear in WHERE, got: {sql}");
	}

	[TestMethod]
	public void GroupedView_WhereOnMemberMixingKeyAndAggregate_GoesToHaving()
	{
		var query = PriorityCounts(CreateQueryable<TestItem>()).Where(v => v.Rank < 0);

		var sql = Translate<TestItem>(query);

		(Clause(sql, "having")?.ContainsIgnoreCase("count(") == true).AssertTrue($"Expected the CASE over COUNT in HAVING, got: {sql}");
		(Clause(sql, "where")?.ContainsIgnoreCase("count(") == true).AssertFalse($"An aggregate must not appear in WHERE, got: {sql}");
	}

	[TestMethod]
	public void GroupedView_WhereOnKeyMember_StaysInWhere()
	{
		var query = PriorityCounts(CreateQueryable<TestItem>()).Where(v => v.Priority > 1);

		var sql = Translate<TestItem>(query);

		(Clause(sql, "where")?.Contains("[Priority]") == true).AssertTrue($"Expected the key filter in WHERE, got: {sql}");
		IsNull(Clause(sql, "having"), $"A filter without aggregates needs no HAVING, got: {sql}");
	}

	[TestMethod]
	public void GroupedView_AggregateAndKeyFiltersInSeparateCalls_SplitBetweenHavingAndWhere()
	{
		var min = 3;
		var query = PriorityCounts(CreateQueryable<TestItem>())
			.Where(v => v.Count >= min)
			.Where(v => v.Priority > 1);

		var sql = Translate<TestItem>(query);

		var where = Clause(sql, "where");
		var having = Clause(sql, "having");

		(where?.Contains("[Priority]") == true).AssertTrue($"Expected the key filter in WHERE, got: {sql}");
		(where?.ContainsIgnoreCase("count(") == true).AssertFalse($"An aggregate must not appear in WHERE, got: {sql}");
		(having?.ContainsIgnoreCase("count(") == true).AssertTrue($"Expected COUNT in HAVING, got: {sql}");
	}

	[TestMethod]
	public void GroupedView_AggregateAndKeyFiltersInOneCall_SplitBetweenHavingAndWhere()
	{
		var min = 3;
		var query = PriorityCounts(CreateQueryable<TestItem>()).Where(v => v.Count >= min && v.Priority > 1);

		var sql = Translate<TestItem>(query);

		var where = Clause(sql, "where");
		var having = Clause(sql, "having");

		(where?.Contains("[Priority]") == true).AssertTrue($"Expected the key filter in WHERE, got: {sql}");
		(where?.ContainsIgnoreCase("count(") == true).AssertFalse($"An aggregate must not appear in WHERE, got: {sql}");
		(having?.ContainsIgnoreCase("count(") == true).AssertTrue($"Expected COUNT in HAVING, got: {sql}");
	}

	[TestMethod]
	public void GroupedView_BoolKeyAndAggregateInOneCall_SplitBetweenHavingAndWhere()
	{
		var min = 3;
		var query = PriorityCounts(CreateQueryable<TestItem>()).Where(v => v.IsActive && v.Count >= min);

		var sql = Translate<TestItem>(query);

		var where = Clause(sql, "where");

		(where?.Contains("[IsActive] = 1") == true).AssertTrue($"Expected the bool key filter in WHERE, got: {sql}");
		(where?.ContainsIgnoreCase("count(") == true).AssertFalse($"An aggregate must not appear in WHERE, got: {sql}");
		(Clause(sql, "having")?.ContainsIgnoreCase("count(") == true).AssertTrue($"Expected COUNT in HAVING, got: {sql}");
	}

	[TestMethod]
	public void GroupedAnonymousProjection_WhereOnAggregateMember_GoesToHaving()
	{
		var min = 3;
		var query = CreateQueryable<TestItem>()
			.GroupBy(i => new { i.Priority, i.IsActive })
			.Select(g => new { g.Key.Priority, Count = g.Count() })
			.Where(x => x.Count >= min);

		var sql = Translate<TestItem>(query);

		(Clause(sql, "having")?.ContainsIgnoreCase("count(") == true).AssertTrue($"Expected COUNT in HAVING, got: {sql}");
		(Clause(sql, "where")?.ContainsIgnoreCase("count(") == true).AssertFalse($"An aggregate must not appear in WHERE, got: {sql}");
	}

	[TestMethod]
	public void GroupBy_ConstantKey_WhereOnGrouping_KeepsHaving()
	{
		// A grand total emits no GROUP BY, but its filter on the aggregate must not be dropped.
		var query = CreateQueryable<TestItem>()
			.GroupBy(i => 1)
			.Where(g => g.Count() > 5)
			.Select(g => new { Count = g.Count() });

		var sql = Translate<TestItem>(query);

		(Clause(sql, "having")?.ContainsIgnoreCase("count(") == true).AssertTrue($"Expected COUNT in HAVING, got: {sql}");
	}

	[TestMethod]
	public void GroupedView_AggregateFilter_OrderedByAggregateAndPaged_FiltersInsideGroupedQuery()
	{
		var min = 3;
		var query = PriorityCounts(CreateQueryable<TestItem>())
			.Where(v => v.Count >= min)
			.OrderByDescending(v => v.Count)
			.Skip(10)
			.Take(5);

		var sql = Translate<TestItem>(query);

		var having = sql.IndexOf("having", StringComparison.OrdinalIgnoreCase);
		var groupBy = sql.IndexOf("group by", StringComparison.OrdinalIgnoreCase);
		var outer = sql.IndexOf("[cteresults]", StringComparison.OrdinalIgnoreCase);

		(having > groupBy && groupBy >= 0).AssertTrue($"Expected HAVING right after GROUP BY, got: {sql}");
		(having < outer).AssertTrue($"HAVING must filter the grouped query, not the paged rows over it, got: {sql}");
		(Clause(sql, "where")?.ContainsIgnoreCase("count(") == true).AssertFalse($"An aggregate must not appear in WHERE, got: {sql}");
		(Clause(sql, "order by")?.Contains("[Count]") == true).AssertTrue($"Expected ordering by the aggregate column, got: {sql}");
	}

	[TestMethod]
	public void GroupedView_AggregateFilter_Counted_FiltersInsideGroupedQuery()
	{
		var min = 3;
		var query = PriorityCounts(CreateQueryable<TestItem>()).Where(v => v.Count >= min);

		var sql = Translate<TestItem>(CountOf(query));

		var having = sql.IndexOf("having", StringComparison.OrdinalIgnoreCase);
		var outer = sql.IndexOf("[cteresults]", StringComparison.OrdinalIgnoreCase);

		(having >= 0 && having < outer).AssertTrue($"HAVING must filter the grouped query that is counted, got: {sql}");
	}

	[TestMethod]
	public void GroupBy_WhereOnGrouping_Paged_FiltersInsideGroupedQuery()
	{
		var items = CreateQueryable<TestItem>();

		var query = items
			.GroupBy(i => i.Priority)
			.Where(g => g.Count() > 5)
			.Select(g => new { Count = g.Count() })
			.OrderBy(x => x.Count)
			.Skip(10)
			.Take(5);

		var sql = Translate<TestItem>(query);

		var having = sql.IndexOf("having", StringComparison.OrdinalIgnoreCase);
		var outer = sql.IndexOf("[cteresults]", StringComparison.OrdinalIgnoreCase);

		(having >= 0 && having < outer).AssertTrue($"HAVING must filter the grouped query, not the paged rows over it, got: {sql}");
	}

	[TestMethod]
	public void GroupedView_Reprojected_WhereOnAggregateMember_GoesToHaving()
	{
		var min = 3;
		var query = PriorityCounts(CreateQueryable<TestItem>())
			.Select(v => new { v.Priority, v.Count })
			.Where(x => x.Count >= min);

		var sql = Translate<TestItem>(query);

		(Clause(sql, "having")?.ContainsIgnoreCase("count(") == true).AssertTrue($"Expected COUNT in HAVING, got: {sql}");
		(Clause(sql, "where")?.ContainsIgnoreCase("count(") == true).AssertFalse($"An aggregate must not appear in WHERE, got: {sql}");
	}

	[TestMethod]
	public void GroupedView_Reprojected_KeyAndAggregateFilters_SplitBetweenHavingAndWhere()
	{
		var min = 3;
		var query = PriorityCounts(CreateQueryable<TestItem>())
			.Select(v => new { v.Priority, v.Count })
			.Where(x => x.Priority > 1 && x.Count >= min);

		var sql = Translate<TestItem>(query);

		var where = Clause(sql, "where");

		(where?.Contains("[Priority]") == true).AssertTrue($"Expected the key filter in WHERE, got: {sql}");
		(where?.ContainsIgnoreCase("count(") == true).AssertFalse($"An aggregate must not appear in WHERE, got: {sql}");
		(Clause(sql, "having")?.ContainsIgnoreCase("count(") == true).AssertTrue($"Expected COUNT in HAVING, got: {sql}");
	}

	[TestMethod]
	public void GroupedView_WhereOnNullableLongAggregateMember_FiltersTheAggregateInHaving()
	{
		var min = 10L;
		var query = PriorityCounts(CreateQueryable<TestItem>()).Where(v => v.LastId >= min);

		var sql = Translate<TestItem>(query);

		var having = Clause(sql, "having");

		(having?.ContainsIgnoreCase("max(") == true).AssertTrue($"Expected MAX in HAVING, got: {sql}");
		(having?.Contains("[LastId]") == true).AssertFalse($"The projected name is not a column of the grouped table, got: {sql}");
		IsNull(Clause(sql, "where"), $"A filter on an aggregate needs no WHERE, got: {sql}");
	}

	[TestMethod]
	public void GroupedView_OrOfAggregateAndKey_GoesToHavingWhole()
	{
		var min = 3;
		var query = PriorityCounts(CreateQueryable<TestItem>()).Where(v => v.Count >= min || v.Priority == 1);

		var sql = Translate<TestItem>(query);

		var having = Clause(sql, "having");

		(having?.ContainsIgnoreCase("count(") == true).AssertTrue($"Expected COUNT in HAVING, got: {sql}");
		(having?.Contains("[Priority]") == true).AssertTrue($"The key operand of OR must stay with the aggregate in HAVING, got: {sql}");
		IsNull(Clause(sql, "where"), $"An OR over an aggregate cannot be split into WHERE, got: {sql}");
	}

	[TestMethod]
	public void GroupedView_NegatedConjunction_GoesToHavingWhole()
	{
		var min = 3;
		var query = PriorityCounts(CreateQueryable<TestItem>()).Where(v => !(v.Count >= min && v.Priority > 1));

		var sql = Translate<TestItem>(query);

		var having = Clause(sql, "having");

		(having?.ContainsIgnoreCase("not") == true).AssertTrue($"Expected the negation in HAVING, got: {sql}");
		(having?.ContainsIgnoreCase("count(") == true).AssertTrue($"Expected COUNT in HAVING, got: {sql}");
		(having?.Contains("[Priority]") == true).AssertTrue($"A negated conjunction must not be split, got: {sql}");
		IsNull(Clause(sql, "where"), $"A negated conjunction over an aggregate cannot be split into WHERE, got: {sql}");
	}

	[TestMethod]
	public void GroupBy_ConstantKey_WhereOnGrouping_Counted_CountsTheFilteredGroup()
	{
		var min = 5;
		var query = CreateQueryable<TestItem>()
			.GroupBy(i => 1)
			.Where(g => g.Count() > min)
			.Select(g => new { Count = g.Count() });

		var sql = Translate<TestItem>(CountOf(query));

		var having = sql.IndexOf("having", StringComparison.OrdinalIgnoreCase);
		var outer = sql.IndexOf("[cteresults]", StringComparison.OrdinalIgnoreCase);

		(having >= 0 && having < outer).AssertTrue($"The grand total must be counted as one group after HAVING, not as its rows, got: {sql}");
	}
}

#endif
