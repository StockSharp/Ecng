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

	// Helper that produces same closure field name "value" for subquery tests
	private static IQueryable<TestTask> FilterTasksByName(IQueryable<TestTask> q, string value)
		=> q.Where(x => x.Title == value);

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

	[TestMethod]
	public void MultipleSubqueries_SameClosureFieldName_ParametersPreserved()
	{
		var persons = CreateQueryable<TestPerson>();
		var tasks = CreateQueryable<TestTask>();

		var left = FilterTasksByName(tasks, "AAA");
		var right = FilterTasksByName(tasks, "BBB");

		var query = from p in persons
					select new VTestPersonWithTasks
					{
						AllColumns = p.AllColumns,
						HasTasks = (from t in left where t.Person.Id == p.Id select t).Any(),
						TaskCount = (from t in right where t.Person.Id == p.Id select t).Count(),
					};

		var (_, parameters) = TranslateSql<TestPerson>(query);

		var values = parameters.Values.Select(v => v.Item2).ToArray();
		values.AssertContains("AAA");
		values.AssertContains("BBB");
	}

	#region Contains + Navigation Property Tests

	/// <summary>
	/// Ensures FK target entities are registered in SchemaRegistry
	/// before running tests that reference them in expressions.
	/// Without this, TryFlattenInnerSchema incorrectly treats unregistered
	/// FK targets as InnerSchema and doubles column names (e.g., PersonPerson).
	/// </summary>
	private static void EnsureFkEntitiesRegistered()
	{
		// Force registration of all FK target entity types.
		// In production, this happens during startup, but in unit tests
		// SchemaRegistry may not have seen these types yet.
		SchemaRegistry.Get(typeof(TestPerson));
		SchemaRegistry.Get(typeof(TestItem));
		SchemaRegistry.Get(typeof(TestCategory));
	}

	/// <summary>
	/// Verifies that Contains with a navigation property's Id
	/// generates SQL using the FK column, not the entity's own Id column.
	/// </summary>
	[TestMethod]
	public void Contains_NavigationPropertyId_ShouldUseFkColumn()
	{
		EnsureFkEntitiesRegistered();
		var tasks = CreateQueryable<TestTask>();
		var personIds = new long[] { 1, 2, 3 };

		var query = tasks.Where(t => personIds.Contains(t.Person.Id));

		var sql = GenerateSql<TestTask>(query);

		sql.ContainsIgnoreCase("[Person] in").AssertTrue(
			$"Expected FK column [Person] IN clause, got: {sql}");
	}

	/// <summary>
	/// Verifies that Contains works on entities without identity columns (BaseJoinEntity pattern).
	/// </summary>
	[TestMethod]
	public void Contains_NavigationPropertyId_NoIdentity_ShouldUseFkColumn()
	{
		EnsureFkEntitiesRegistered();
		var tags = CreateQueryable<TestItemTag>();
		var itemIds = new long[] { 10, 20 };

		var query = tags.Where(t => itemIds.Contains(t.Item.Id));

		var sql = GenerateSql<TestItemTag>(query);

		sql.ContainsIgnoreCase("[Item] in").AssertTrue(
			$"Expected FK column [Item] IN clause, got: {sql}");
	}

	/// <summary>
	/// Verifies that multiple Contains on different FK navigation properties
	/// produce correct SQL with both FK columns.
	/// </summary>
	[TestMethod]
	public void Contains_MultipleNavigationPropertyIds_ShouldUseFkColumns()
	{
		EnsureFkEntitiesRegistered();
		var ics = CreateQueryable<TestItemCategory>();
		var itemIds = new long[] { 1, 2 };
		var catIds = new long[] { 10, 20, 30 };

		var query = ics.Where(ic => itemIds.Contains(ic.Item.Id) && catIds.Contains(ic.Category.Id));

		var sql = GenerateSql<TestItemCategory>(query);

		sql.ContainsIgnoreCase("[Item] in").AssertTrue(
			$"Expected [Item] IN clause, got: {sql}");
		sql.ContainsIgnoreCase("[Category] in").AssertTrue(
			$"Expected [Category] IN clause, got: {sql}");
	}

	/// <summary>
	/// Verifies that Contains with FK navigation property works correctly
	/// when combined with other filter conditions on the same entity.
	/// </summary>
	[TestMethod]
	public void Contains_NavigationPropertyId_WithOtherFilter_ShouldCombine()
	{
		EnsureFkEntitiesRegistered();
		var tasks = CreateQueryable<TestTask>();
		var personIds = new long[] { 1, 2 };

		var query = tasks.Where(t => personIds.Contains(t.Person.Id) && t.Priority > 5);

		var sql = GenerateSql<TestTask>(query);

		sql.ContainsIgnoreCase("[Person] in").AssertTrue(
			$"Expected [Person] IN clause, got: {sql}");
		sql.Contains("[Priority]").AssertTrue(
			$"Expected [Priority] filter, got: {sql}");
	}

	/// <summary>
	/// Verifies that Contains parameters are correctly captured
	/// when filtering by navigation property Id.
	/// </summary>
	[TestMethod]
	public void Contains_NavigationPropertyId_ParametersCaptured()
	{
		EnsureFkEntitiesRegistered();
		var tasks = CreateQueryable<TestTask>();
		var personIds = new long[] { 100, 200 };

		var query = tasks.Where(t => personIds.Contains(t.Person.Id));

		var (sql, parameters) = TranslateSql<TestTask>(query);

		var values = parameters.Values.Select(v => v.Item2).ToArray();
		values.AssertContains(100L);
		values.AssertContains(200L);
	}

	/// <summary>
	/// Verifies that multi-element Contains on navigation property generates
	/// IN clause with all parameters (not just the first one).
	/// </summary>
	[TestMethod]
	public void Contains_NavigationPropertyId_MultiElement_AllParamsInSql()
	{
		EnsureFkEntitiesRegistered();
		var tasks = CreateQueryable<TestTask>();
		var personIds = new long[] { 10, 20, 30 };

		var query = tasks.Where(t => personIds.Contains(t.Person.Id));

		var (sql, parameters) = TranslateSql<TestTask>(query);

		// must have all 3 parameters in SQL
		3.AssertEqual(parameters.Count, $"Expected 3 parameters, got {parameters.Count}. SQL: {sql}");

		// all parameter placeholders must appear in the IN clause
		foreach (var paramName in parameters.Keys)
			sql.Contains(paramName).AssertTrue($"Parameter {paramName} not found in SQL: {sql}");

		sql.ContainsIgnoreCase("[Person] in").AssertTrue(
			$"Expected [Person] IN clause, got: {sql}");
	}

	/// <summary>
	/// Baseline: Contains on primary key generates correct IN clause.
	/// </summary>
	[TestMethod]
	public void Contains_PrimaryKey_Baseline_ShouldGenerateInClause()
	{
		var items = CreateQueryable<TestItem>();
		var ids = new long[] { 1, 2, 3 };

		var query = items.Where(x => ids.Contains(x.Id));

		var sql = GenerateSql<TestItem>(query);

		sql.ContainsIgnoreCase("in").AssertTrue(
			$"Expected IN clause for Contains, got: {sql}");
		sql.ContainsIgnoreCase("[Id] in").AssertTrue(
			$"Expected [Id] IN clause, got: {sql}");
	}

	#endregion

	#region OrderBy + Navigation Property Tests

	/// <summary>
	/// Verifies that OrderBy with navigation property's Id
	/// generates ORDER BY using the FK column name.
	/// </summary>
	[TestMethod]
	public void OrderBy_NavigationPropertyId_ShouldUseFkColumn()
	{
		EnsureFkEntitiesRegistered();
		var tasks = CreateQueryable<TestTask>();

		var query = tasks.OrderBy(t => t.Person.Id);

		var sql = GenerateSql<TestTask>(query);

		sql.ContainsIgnoreCase("ORDER BY").AssertTrue(
			$"Expected ORDER BY clause, got: {sql}");
		sql.Contains("[Person]").AssertTrue(
			$"Expected FK column [Person] in ORDER BY, got: {sql}");
	}

	/// <summary>
	/// Verifies that OrderByDescending with navigation property's Id
	/// generates correct ORDER BY DESC using the FK column.
	/// </summary>
	[TestMethod]
	public void OrderByDescending_NavigationPropertyId_ShouldUseFkColumn()
	{
		EnsureFkEntitiesRegistered();
		var tasks = CreateQueryable<TestTask>();

		var query = tasks.OrderByDescending(t => t.Person.Id);

		var sql = GenerateSql<TestTask>(query);

		sql.ContainsIgnoreCase("ORDER BY").AssertTrue(
			$"Expected ORDER BY clause, got: {sql}");
		sql.Contains("[Person]").AssertTrue(
			$"Expected FK column [Person] in ORDER BY DESC, got: {sql}");
		sql.ContainsIgnoreCase("DESC").AssertTrue(
			$"Expected DESC in ORDER BY, got: {sql}");
	}

	/// <summary>
	/// Verifies that OrderBy with FK navigation property works combined with Where.
	/// </summary>
	[TestMethod]
	public void OrderBy_NavigationPropertyId_WithWhere_ShouldUseFkColumn()
	{
		EnsureFkEntitiesRegistered();
		var tasks = CreateQueryable<TestTask>();

		var query = tasks.Where(t => t.Priority > 5).OrderBy(t => t.Person.Id);

		var sql = GenerateSql<TestTask>(query);

		sql.Contains("[Priority]").AssertTrue(
			$"Expected [Priority] in WHERE, got: {sql}");
		sql.ContainsIgnoreCase("ORDER BY").AssertTrue(
			$"Expected ORDER BY clause, got: {sql}");
		// ORDER BY should reference FK column, not entity's own Id
		sql.Contains("[Person]").AssertTrue(
			$"Expected FK column [Person] in ORDER BY, got: {sql}");
	}

	/// <summary>
	/// Verifies that ThenBy with navigation property's Id
	/// uses the FK column, not the entity's own Id column.
	/// Same underlying code path as OrderBy (ParseOrderByExpression).
	/// </summary>
	[TestMethod]
	public void ThenBy_NavigationPropertyId_ShouldUseFkColumn()
	{
		EnsureFkEntitiesRegistered();
		var tasks = CreateQueryable<TestTask>();

		var query = tasks.OrderBy(t => t.Priority).ThenBy(t => t.Person.Id);

		var sql = GenerateSql<TestTask>(query);

		sql.Contains("[Priority]").AssertTrue(
			$"Expected [Priority] in ORDER BY, got: {sql}");
		sql.Contains("[Person]").AssertTrue(
			$"Expected FK column [Person] in ThenBy, got: {sql}");
	}

	/// <summary>
	/// Verifies that ThenByDescending with navigation property's Id
	/// uses the FK column with DESC.
	/// </summary>
	[TestMethod]
	public void ThenByDescending_NavigationPropertyId_ShouldUseFkColumn()
	{
		EnsureFkEntitiesRegistered();
		var tasks = CreateQueryable<TestTask>();

		var query = tasks.OrderBy(t => t.Priority).ThenByDescending(t => t.Person.Id);

		var sql = GenerateSql<TestTask>(query);

		sql.Contains("[Priority]").AssertTrue(
			$"Expected [Priority] in ORDER BY, got: {sql}");
		sql.Contains("[Person]").AssertTrue(
			$"Expected FK column [Person] in ThenByDescending, got: {sql}");
	}

	/// <summary>
	/// Verifies that OrderBy with navigation property Id on a no-identity entity
	/// uses the FK column name correctly.
	/// </summary>
	[TestMethod]
	public void OrderBy_NavigationPropertyId_NoIdentity_ShouldUseFkColumn()
	{
		EnsureFkEntitiesRegistered();
		var tags = CreateQueryable<TestItemTag>();

		var query = tags.OrderBy(t => t.Item.Id);

		var sql = GenerateSql<TestItemTag>(query);

		sql.ContainsIgnoreCase("ORDER BY").AssertTrue(
			$"Expected ORDER BY clause, got: {sql}");
		sql.Contains("[Item]").AssertTrue(
			$"Expected FK column [Item] in ORDER BY, got: {sql}");
	}

	/// <summary>
	/// Verifies that OrderBy with Expression&lt;Func&lt;T, object&gt;&gt; over a value-type
	/// member (compiler emits Convert(member, object)) still resolves to the column.
	/// </summary>
	[TestMethod]
	public void OrderBy_ValueTypeConvertToObject_ShouldUseColumn()
	{
		var items = CreateQueryable<TestItem>();
		Expression<Func<TestItem, object>> key = x => x.Priority;

		var query = items.OrderBy(key);

		var sql = GenerateSql<TestItem>(query);

		sql.ContainsIgnoreCase("ORDER BY").AssertTrue($"Expected ORDER BY clause, got: {sql}");
		sql.Contains("[Priority]").AssertTrue($"Expected [Priority] in ORDER BY, got: {sql}");
	}

	/// <summary>
	/// Verifies that OrderByDescending with Expression&lt;Func&lt;T, object&gt;&gt; over a
	/// reference-type member works (no Convert emitted, must not regress).
	/// </summary>
	[TestMethod]
	public void OrderByDescending_ReferenceTypeAsObject_ShouldUseColumn()
	{
		var items = CreateQueryable<TestItem>();
		Expression<Func<TestItem, object>> key = x => x.Name;

		var query = items.OrderByDescending(key);

		var sql = GenerateSql<TestItem>(query);

		sql.ContainsIgnoreCase("ORDER BY").AssertTrue($"Expected ORDER BY clause, got: {sql}");
		sql.Contains("[Name]").AssertTrue($"Expected [Name] in ORDER BY, got: {sql}");
		sql.ContainsIgnoreCase("DESC").AssertTrue($"Expected DESC in ORDER BY, got: {sql}");
	}

	/// <summary>
	/// Verifies that OrderBy with Expression&lt;Func&lt;T, object&gt;&gt; over a navigation
	/// property's Id (Convert wraps the nav.Id access) still emits the FK column.
	/// </summary>
	[TestMethod]
	public void OrderBy_NavigationPropertyIdConvertToObject_ShouldUseFkColumn()
	{
		EnsureFkEntitiesRegistered();
		var tasks = CreateQueryable<TestTask>();
		Expression<Func<TestTask, object>> key = t => t.Person.Id;

		var query = tasks.OrderBy(key);

		var sql = GenerateSql<TestTask>(query);

		sql.ContainsIgnoreCase("ORDER BY").AssertTrue($"Expected ORDER BY clause, got: {sql}");
		sql.Contains("[Person]").AssertTrue($"Expected FK column [Person] in ORDER BY, got: {sql}");
	}

	#endregion

	#region String Parameterization Tests

	/// <summary>
	/// Verifies that string constants with single quotes are passed as parameters,
	/// not inlined into SQL.
	/// </summary>
	[TestMethod]
	public void StringConstant_WithSingleQuote_ShouldBeParameterized()
	{
		var items = CreateQueryable<TestItem>();

		var query = items.Where(x => x.Name == "O'Reilly");

		var (sql, parameters) = TranslateSql<TestItem>(query);

		sql.Contains("O'Reilly").AssertFalse(
			$"Literal string should not appear in SQL, got: {sql}");
		sql.Contains("@p").AssertTrue(
			$"Expected parameter placeholder in SQL, got: {sql}");

		var paramValue = parameters.Values.First(p => p.Item2 is string).Item2;
		paramValue.AssertEqual("O'Reilly", "parameter value mismatch");
	}

	/// <summary>
	/// Verifies that string constants with multiple single quotes are parameterized.
	/// </summary>
	[TestMethod]
	public void StringConstant_WithMultipleSingleQuotes_ShouldBeParameterized()
	{
		var items = CreateQueryable<TestItem>();

		var query = items.Where(x => x.Name == "it's a 'test'");

		var (sql, parameters) = TranslateSql<TestItem>(query);

		sql.Contains("it's").AssertFalse(
			$"Literal string should not appear in SQL, got: {sql}");

		var paramValue = parameters.Values.First(p => p.Item2 is string).Item2;
		paramValue.AssertEqual("it's a 'test'", "parameter value mismatch");
	}

	#endregion

	#region Anonymous-type Select Projection Tests

	/// <summary>
	/// Verifies that .Select(x =&gt; new { x.A, x.B }) over a table emits both columns
	/// in SELECT list.
	/// </summary>
	[TestMethod]
	public void SelectAnonymous_TwoMembers_ShouldEmitBothColumns()
	{
		var items = CreateQueryable<TestItem>();

		var query = items.Select(x => new { x.Id, x.Name });

		var sql = GenerateSql<TestItem>(query);

		sql.Contains("[Id]").AssertTrue($"Expected [Id] in SELECT, got: {sql}");
		sql.Contains("[Name]").AssertTrue($"Expected [Name] in SELECT, got: {sql}");
	}

	/// <summary>
	/// Verifies that .Select(x =&gt; new { Alias = x.Member }) emits the member
	/// with AS alias matching the anonymous property name.
	/// </summary>
	[TestMethod]
	public void SelectAnonymous_WithAlias_ShouldEmitAsAlias()
	{
		var items = CreateQueryable<TestItem>();

		var query = items.Select(x => new { ItemId = x.Id, Label = x.Name });

		var sql = GenerateSql<TestItem>(query);

		sql.Contains("[Id]").AssertTrue($"Expected underlying [Id] in SELECT, got: {sql}");
		sql.Contains("[Name]").AssertTrue($"Expected underlying [Name] in SELECT, got: {sql}");
		sql.ContainsIgnoreCase("AS [ItemId]").AssertTrue($"Expected 'AS [ItemId]' alias, got: {sql}");
		sql.ContainsIgnoreCase("AS [Label]").AssertTrue($"Expected 'AS [Label]' alias, got: {sql}");
	}

	/// <summary>
	/// Verifies that anonymous-type Select composes with Where.
	/// </summary>
	[TestMethod]
	public void SelectAnonymous_AfterWhere_ShouldEmitProjectionAndPredicate()
	{
		var items = CreateQueryable<TestItem>();

		var query = items
			.Where(x => x.Priority > 5)
			.Select(x => new { x.Id, x.Priority });

		var sql = GenerateSql<TestItem>(query);

		sql.Contains("[Id]").AssertTrue($"Expected [Id] in SELECT, got: {sql}");
		sql.Contains("[Priority]").AssertTrue($"Expected [Priority] in SELECT, got: {sql}");
		sql.ContainsIgnoreCase("WHERE").AssertTrue($"Expected WHERE clause, got: {sql}");
	}

	/// <summary>
	/// Verifies that anonymous projection mixing value-type and reference-type members works.
	/// </summary>
	[TestMethod]
	public void SelectAnonymous_MixedValueAndReferenceTypes_ShouldEmitBothColumns()
	{
		var items = CreateQueryable<TestItem>();

		var query = items.Select(x => new { x.IsActive, x.Name, x.Price });

		var sql = GenerateSql<TestItem>(query);

		sql.Contains("[IsActive]").AssertTrue($"Expected [IsActive] in SELECT, got: {sql}");
		sql.Contains("[Name]").AssertTrue($"Expected [Name] in SELECT, got: {sql}");
		sql.Contains("[Price]").AssertTrue($"Expected [Price] in SELECT, got: {sql}");
	}

	#endregion

	#region Where + Navigation Property Traversal Tests

	/// <summary>
	/// Verifies that a Where predicate accessing a non-Id column on a RelationSingle
	/// navigation property (t.Person.Name) emits an INNER JOIN to the related table
	/// so the multi-part identifier resolves at SQL execution time.
	/// </summary>
	[TestMethod]
	public void Where_NavigationProperty_NonIdColumn_EmitsJoin()
	{
		EnsureFkEntitiesRegistered();
		var tasks = CreateQueryable<TestTask>();

		var query = tasks.Where(t => t.Person.Name.Like("%Alice%"));

		var sql = GenerateSql<TestTask>(query);

		sql.ContainsIgnoreCase("join").AssertTrue(
			$"Expected JOIN to {nameof(TestPerson)} for navigation predicate, got: {sql}");
		sql.Contains("[Ecng_TestPerson]").AssertTrue(
			$"Expected join target [Ecng_TestPerson], got: {sql}");
		sql.ContainsIgnoreCase("like").AssertTrue(
			$"Expected LIKE in WHERE, got: {sql}");
	}

	/// <summary>
	/// Reproduction of bug from Data.ORM_OR_Join_Bug.md: a navigation predicate
	/// combined via OR with Contains over an FK column must still emit the JOIN.
	/// </summary>
	[TestMethod]
	public void Where_NavigationProperty_OrContainsFk_EmitsJoin()
	{
		EnsureFkEntitiesRegistered();
		var tasks = CreateQueryable<TestTask>();
		var personIds = new long[] { 1, 2, 3 };

		var query = tasks.Where(t => t.Person.Name.Like("%Alice%") || personIds.Contains(t.Person.Id));

		var sql = GenerateSql<TestTask>(query);

		sql.ContainsIgnoreCase("join").AssertTrue(
			$"Expected JOIN to {nameof(TestPerson)} for OR'ed navigation predicate, got: {sql}");
		sql.Contains("[Ecng_TestPerson]").AssertTrue(
			$"Expected join target [Ecng_TestPerson], got: {sql}");
		sql.ContainsIgnoreCase("like").AssertTrue(
			$"Expected LIKE in WHERE, got: {sql}");
		sql.ContainsIgnoreCase(" or ").AssertTrue(
			$"Expected OR in WHERE, got: {sql}");
	}

	#endregion

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
