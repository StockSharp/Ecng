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

	private static (string sql, IDictionary<string, (Type, object)> parameters) TranslateSql<TSource>(IQueryable queryable)
	{
		var expression = queryable.Expression;
		var meta = SchemaRegistry.Get(typeof(TSource));

		// ExpressionQueryTranslator is internal — use reflection
		var asm = typeof(Database).Assembly;
		var translatorType = asm.GetType("Ecng.Data.Sql.ExpressionQueryTranslator");
		var translator = Activator.CreateInstance(translatorType, [meta]);
		var query = (Query)translatorType.GetMethod("GenerateSql").Invoke(translator, [expression]);
		var parameters = (IDictionary<string, (Type, object)>)translatorType.GetProperty("Parameters").GetValue(translator);

		return (query.Render(_dialect), parameters);
	}

	private static string GenerateSql<TSource>(IQueryable queryable)
		=> TranslateSql<TSource>(queryable).sql;

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

	[TestMethod]
	public void StringHelper_IsEmpty_TranslatesToSql()
	{
		var items = CreateQueryable<TestItem>();

		var query = items.Where(x => !x.Name.IsEmpty());

		var sql = GenerateSql<TestItem>(query);

		sql.ContainsIgnoreCase("is null").AssertTrue($"Expected 'is null' in SQL, got: {sql}");
		sql.Contains("like N''").AssertTrue($"Expected 'like N''' in SQL, got: {sql}");
	}

	[TestMethod]
	public void StringHelper_IsEmptyOrWhiteSpace_TranslatesToSql()
	{
		var items = CreateQueryable<TestItem>();

		var query = items.Where(x => !x.Name.IsEmptyOrWhiteSpace());

		var sql = GenerateSql<TestItem>(query);

		sql.ContainsIgnoreCase("is null").AssertTrue($"Expected 'is null' in SQL, got: {sql}");
		sql.Contains("= N''").AssertTrue($"Expected '= N''' in SQL, got: {sql}");
	}

	/// <summary>
	/// When C# compiler folds "select new VEntity { Prop = joined.Col }" into the
	/// last Join's result selector (inner join without "into"), the translator must
	/// visit the MemberInitExpression to include computed columns in SELECT.
	/// Without the fix, the SQL would be missing computed columns like JoinedCategoryName.
	/// </summary>
	[TestMethod]
	public void Join_MemberInitInResultSelector_IncludesComputedColumns()
	{
		var items = CreateQueryable<TestItem>();
		var categories = CreateQueryable<TestCategory>();

		// C# compiles this into Queryable.Join with MemberInit in the result selector (4th arg)
		var query = from item in items
					join cat in categories on item.Id equals cat.Id
					select new VTestItemWithCategory
					{
						Id = item.Id,
						Name = item.Name,
						JoinedCategoryName = cat.CategoryName,
					};

		var sql = GenerateSql<TestItem>(query);

		sql.Contains("JoinedCategoryName").AssertTrue(
			$"SQL must include computed column 'JoinedCategoryName' from Join result selector, got: {sql}");
		sql.Contains("[Ecng_TestCategory]").AssertTrue(
			$"SQL must include INNER JOIN to Ecng_TestCategory, got: {sql}");
	}

	/// <summary>
	/// When C# compiler folds "select new VEntity { Prop = joined.Col }" into the
	/// last SelectMany's result selector (left join via GroupJoin + SelectMany + DefaultIfEmpty),
	/// the translator must visit the MemberInitExpression to include computed columns.
	/// Without the fix, the SQL would be missing the left-joined computed columns.
	/// </summary>
	[TestMethod]
	public void SelectMany_MemberInitInResultSelector_IncludesComputedColumns()
	{
		var items = CreateQueryable<TestItem>();
		var categories = CreateQueryable<TestCategory>();

		// C# compiles this into GroupJoin + SelectMany with MemberInit in SelectMany's result selector
		var query = from item in items
					join cat in categories on item.Id equals cat.Id into g
					from cat in g.DefaultIfEmpty()
					select new VTestItemWithOptionalCategory
					{
						Id = item.Id,
						Name = item.Name,
						LeftJoinedDescription = cat.Description,
					};

		var sql = GenerateSql<TestItem>(query);

		sql.Contains("LeftJoinedDescription").AssertTrue(
			$"SQL must include computed column 'LeftJoinedDescription' from SelectMany result selector, got: {sql}");
		sql.Contains("[Ecng_TestCategory]").AssertTrue(
			$"SQL must include LEFT JOIN to Ecng_TestCategory, got: {sql}");
	}

	/// <summary>
	/// When multiple joins fold their MemberInit into the result selector,
	/// all computed columns from each join must appear in the generated SQL.
	/// </summary>
	[TestMethod]
	public void MultipleJoins_MemberInitInResultSelector_IncludesAllComputedColumns()
	{
		var items = CreateQueryable<TestItem>();
		var categories = CreateQueryable<TestCategory>();
		var persons = CreateQueryable<TestPerson>();

		// Two consecutive joins with MemberInit folded into the last Join's result selector
		var query = from item in items
					join cat in categories on item.Id equals cat.Id
					join p in persons on item.Id equals p.Id
					select new VTestItemWithCategory
					{
						Id = item.Id,
						Name = p.Name,
						JoinedCategoryName = cat.CategoryName,
					};

		var sql = GenerateSql<TestItem>(query);

		sql.Contains("JoinedCategoryName").AssertTrue(
			$"SQL must include 'JoinedCategoryName' from multi-join MemberInit, got: {sql}");
		sql.Contains("[Ecng_TestCategory]").AssertTrue(
			$"SQL must include JOIN to Ecng_TestCategory, got: {sql}");
		sql.Contains("[Ecng_TestPerson]").AssertTrue(
			$"SQL must include JOIN to Ecng_TestPerson, got: {sql}");
	}
	// Helper that produces same closure field name "value" for both calls
	private static IQueryable<TestItem> FilterByName(IQueryable<TestItem> q, string value)
		=> q.Where(x => x.Name == value);

	[TestMethod]
	public void Concat_DifferentConstants_ParametersPreserved()
	{
		var items = CreateQueryable<TestItem>();

		// Same closure field name "value" → same parameter base name → collision without fix
		var left = FilterByName(items, "AAA");
		var right = FilterByName(items, "BBB");
		var query = left.Concat(right);

		var (sql, parameters) = TranslateSql<TestItem>(query);

		var values = parameters.Values.Select(v => v.Item2).ToArray();
		values.AssertContains("AAA");
		values.AssertContains("BBB");
	}

	[TestMethod]
	public void Union_DifferentConstants_ParametersPreserved()
	{
		var items = CreateQueryable<TestItem>();

		var left = FilterByName(items, "XXX");
		var right = FilterByName(items, "YYY");
		var query = left.Union(right);

		var (sql, parameters) = TranslateSql<TestItem>(query);

		var values = parameters.Values.Select(v => v.Item2).ToArray();
		values.AssertContains("XXX");
		values.AssertContains("YYY");
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
