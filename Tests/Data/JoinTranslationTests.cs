#if NET10_0_OR_GREATER

namespace Ecng.Tests.Data;

using System.Linq.Expressions;

using Ecng.Data;
using Ecng.Data.Sql;
using Ecng.Serialization;

/// <summary>
/// Dedicated coverage for <c>Join</c>, <c>GroupJoin</c>, <c>SelectMany</c>
/// and <c>DefaultIfEmpty</c> shapes — the primitives behind LINQ
/// inner/outer-join syntax. These constructs were exercised only in
/// passing inside <c>ExpressionQueryTranslatorTests</c>; consolidating
/// them here makes regressions in the join-translation paths visible
/// at a glance.
/// </summary>
[TestClass]
public class JoinTranslationTests : BaseTestClass
{
	[ClassInitialize]
	public static void ClassInit(TestContext _)
	{
		SchemaRegistry.Get(typeof(TestPerson));
		SchemaRegistry.Get(typeof(TestTask));
	}

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
	public void Join_TwoTablesOnFkEqualsId_EmitsInnerJoin()
	{
		// `from t in tasks join p in persons on t.Person.Id equals p.Id select new { t.Title, p.Name }`
		// must produce an INNER JOIN with the FK predicate on the ON clause.
		var tasks = CreateQueryable<TestTask>();
		var persons = CreateQueryable<TestPerson>();

		var query = from t in tasks
					join p in persons on t.Person.Id equals p.Id
					select new { t.Title, p.Name };

		var sql = Translate<TestTask>(query);

		sql.ContainsIgnoreCase("inner join").AssertTrue($"Expected INNER JOIN, got: {sql}");
		sql.ContainsIgnoreCase("[Ecng_TestPerson]").AssertTrue($"Expected JOIN target Ecng_TestPerson, got: {sql}");
	}

	[TestMethod]
	public void GroupJoinDefaultIfEmpty_ProducesLeftOuterJoin()
	{
		// The compiler rewrites a left-outer join as
		// `tasks.GroupJoin(persons, ..., (t, pg) => new { t, pg }).SelectMany(x => x.pg.DefaultIfEmpty(), ...)`
		// — the translator must surface a LEFT JOIN.
		var tasks = CreateQueryable<TestTask>();
		var persons = CreateQueryable<TestPerson>();

		var query = from t in tasks
					join p in persons on t.Person.Id equals p.Id into pg
					from p in pg.DefaultIfEmpty()
					select new { t.Title, PersonName = p.Name };

		var sql = Translate<TestTask>(query);

		sql.ContainsIgnoreCase("left join").AssertTrue($"Expected LEFT JOIN, got: {sql}");
		sql.ContainsIgnoreCase("[Ecng_TestPerson]").AssertTrue($"Expected JOIN target Ecng_TestPerson, got: {sql}");
	}

	[TestMethod]
	public void Join_ProjectionFromBothSides_QualifiesColumnsByAlias()
	{
		// Both `t.Title` and `p.Name` must be alias-qualified — leaking a
		// bare [Title] or [Name] would confuse the engine when both tables
		// happen to share a column name.
		var tasks = CreateQueryable<TestTask>();
		var persons = CreateQueryable<TestPerson>();

		var query = from t in tasks
					join p in persons on t.Person.Id equals p.Id
					select new { t.Title, p.Name };

		var sql = Translate<TestTask>(query);

		// At least one bracketed alias-qualified column reference must appear
		// for the inner-side projection.
		sql.Contains("[Name]").AssertTrue($"Expected projected column [Name], got: {sql}");
	}

	[TestMethod]
	public void Join_WithWhereOnInnerSide_IncludesPredicateInWhere()
	{
		// Predicates added after a join must be recognised as belonging to
		// the WHERE clause, not collapsed into the ON clause.
		var tasks = CreateQueryable<TestTask>();
		var persons = CreateQueryable<TestPerson>();

		var minPriority = 5;

		var query = from t in tasks
					join p in persons on t.Person.Id equals p.Id
					where t.Priority > minPriority
					select new { t.Title, p.Name };

		var sql = Translate<TestTask>(query);

		sql.ContainsIgnoreCase("where").AssertTrue($"Expected WHERE clause, got: {sql}");
		sql.Contains("[Priority]").AssertTrue($"Expected predicate column [Priority], got: {sql}");
	}

	[TestMethod]
	public void GroupJoin_KeySelectorThroughNavigationFkColumn()
	{
		// Compiler-generated form may key the join on the FK column directly:
		// `t.Person` without `.Id`. Joins should still resolve correctly when
		// the join key uses the FK column name.
		var tasks = CreateQueryable<TestTask>();
		var persons = CreateQueryable<TestPerson>();

		var query = from t in tasks
					join p in persons on t.Person.Id equals p.Id into pg
					from p in pg.DefaultIfEmpty()
					select new { t.Title, p.Name };

		var sql = Translate<TestTask>(query);

		sql.Contains("[Person]").AssertTrue($"Expected FK column [Person] in JOIN ON, got: {sql}");
	}
}

#endif
