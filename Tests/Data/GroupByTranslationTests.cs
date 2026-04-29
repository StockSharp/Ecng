#if NET10_0_OR_GREATER

namespace Ecng.Tests.Data;

using System.Linq.Expressions;

using Ecng.Data;
using Ecng.Data.Sql;
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
		IEnumerable<TResult> IQueryContext.ExecuteEnum<TSource, TResult>(Expression expression)
			=> throw new NotSupportedException();

		IAsyncEnumerable<TResult> IQueryContext.ExecuteEnumAsync<TSource, TResult>(Expression expression)
			=> throw new NotSupportedException();

		ValueTask IQueryContext.ExecuteAsync<TSource>(Expression expression)
			=> throw new NotSupportedException();

		TResult IQueryContext.ExecuteResult<TSource, TResult>(Expression expression)
			=> throw new NotSupportedException();

		ValueTask<TResult> IQueryContext.ExecuteResultAsync<TSource, TResult>(Expression expression)
			=> throw new NotSupportedException();
	}

	private static string Translate<TSource>(IQueryable queryable)
	{
		var meta = SchemaRegistry.Get(typeof(TSource));
		var asm = typeof(Database).Assembly;
		var translatorType = asm.GetType("Ecng.Data.Sql.ExpressionQueryTranslator");
		var translator = Activator.CreateInstance(translatorType, [meta]);
		var query = (Query)translatorType.GetMethod("GenerateSql").Invoke(translator, [queryable.Expression]);
		return query.Render(SqlServerDialect.Instance);
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
}

#endif
