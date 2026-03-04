#if NET10_0_OR_GREATER

namespace Ecng.Tests.Data;

using System.Linq.Expressions;
using System.Reflection;

using Ecng.Data;
using Ecng.Data.Sql;
using Ecng.Serialization;

/// <summary>
/// Tests for ExpressionQueryTranslator SQL generation,
/// particularly subquery alias handling.
/// </summary>
[TestClass]
public class ExpressionQueryTranslatorTests : BaseTestClass
{
	private static readonly ISqlDialect _dialect = SqlServerDialect.Instance;

	private class DummyQueryContext : IQueryContext
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

	private static IQueryable<T> CreateQueryable<T>()
		=> new DefaultQueryable<T>(new DefaultQueryProvider<T>(new DummyQueryContext()), null);

	private static string GenerateSql<TSource>(IQueryable queryable)
	{
		var expression = queryable.Expression;
		var meta = SchemaRegistry.Get(typeof(TSource));

		// ExpressionQueryTranslator is internal — use reflection
		var asm = typeof(Database).Assembly;
		var translatorType = asm.GetType("Ecng.Data.Sql.ExpressionQueryTranslator");
		var translator = Activator.CreateInstance(translatorType, [meta]);
		var query = (Query)translatorType.GetMethod("GenerateSql").Invoke(translator, [expression]);

		return query.Render(_dialect);
	}

	[TestMethod]
	public void SubqueryAny_ShouldNotProduceEmptyAlias()
	{
		var persons = CreateQueryable<TestPerson>();
		var tasks = CreateQueryable<TestTask>();

		var query = from p in persons
					select new VTestPersonWithTasks
					{
						AllColumns = p.AllColumns,
						HasTasks = (from t in tasks where t.Person.Id == p.Id select t).Any(),
					};

		var sql = GenerateSql<TestPerson>(query);

		sql.Contains("[]").AssertFalse($"SQL should not contain empty alias '[]', got: {sql}");
		sql.Contains("[e].*").AssertTrue($"Expected main table alias '[e].*', got: {sql}");
	}

	[TestMethod]
	public void SubqueryCount_ShouldNotProduceEmptyAlias()
	{
		var persons = CreateQueryable<TestPerson>();
		var tasks = CreateQueryable<TestTask>();

		var query = from p in persons
					select new VTestPersonWithTasks
					{
						AllColumns = p.AllColumns,
						TaskCount = (from t in tasks where t.Person.Id == p.Id select t).Count(),
					};

		var sql = GenerateSql<TestPerson>(query);

		sql.Contains("[]").AssertFalse($"SQL should not contain empty alias '[]', got: {sql}");
	}

	[TestMethod]
	public void MultipleSubqueries_ShouldNotProduceEmptyAlias()
	{
		var persons = CreateQueryable<TestPerson>();
		var tasks = CreateQueryable<TestTask>();

		var query = from p in persons
					select new VTestPersonWithTasks
					{
						AllColumns = p.AllColumns,
						HasTasks = (from t in tasks where t.Person.Id == p.Id select t).Any(),
						TaskCount = (from t in tasks where t.Person.Id == p.Id select t).Count(),
					};

		var sql = GenerateSql<TestPerson>(query);

		sql.Contains("[]").AssertFalse($"SQL should not contain empty alias '[]', got: {sql}");
	}
}

public class VTestPersonWithTasks : IDbPersistable
{
	[AllColumnsField]
	public object AllColumns { get; set; }

	public bool HasTasks { get; set; }
	public int TaskCount { get; set; }

	object IDbPersistable.GetIdentity() => default;
	void IDbPersistable.SetIdentity(object id) { }

	public void Save(SettingsStorage storage) { }

	public ValueTask LoadAsync(SettingsStorage storage, IStorage db, CancellationToken cancellationToken)
		=> default;
}

#endif
