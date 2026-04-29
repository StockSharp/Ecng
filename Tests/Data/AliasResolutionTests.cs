#if NET10_0_OR_GREATER

namespace Ecng.Tests.Data;

using System.Linq.Expressions;

using Ecng.Data;
using Ecng.Data.Sql;
using Ecng.Serialization;

/// <summary>
/// Locks down alias resolution: lambda parameters introduced by LINQ
/// operators must be registered in the per-translation symbol table so
/// future code can resolve them without falling back to string-name
/// heuristics. The tests run end-to-end (full SQL emission) and then
/// inspect the translator state via reflection — the symbol table is
/// internal but the contract should not silently regress.
/// </summary>
[TestClass]
public class AliasResolutionTests : BaseTestClass
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

	private static (string sql, IDictionary<ParameterExpression, string> aliases) Translate<TSource>(IQueryable queryable)
	{
		var meta = SchemaRegistry.Get(typeof(TSource));
		var asm = typeof(Database).Assembly;
		var translatorType = asm.GetType("Ecng.Data.Sql.ExpressionQueryTranslator");
		var translator = Activator.CreateInstance(translatorType, [meta]);
		var query = (Query)translatorType.GetMethod("GenerateSql").Invoke(translator, [queryable.Expression]);

		var contextProp = translatorType.GetProperty("Context", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
		var context = contextProp.GetValue(translator);
		var aliasField = context.GetType().GetField("Aliases");
		var aliases = (IDictionary<ParameterExpression, string>)aliasField.GetValue(context);

		return (query.Render(SqlServerDialect.Instance), aliases);
	}

	[TestMethod]
	public void Where_RegistersLambdaParameter()
	{
		var items = CreateQueryable<TestItem>();

		Expression<Func<TestItem, bool>> predicate = i => i.Name == "x";
		var (_, aliases) = Translate<TestItem>(items.Where(predicate));

		aliases.Count.AssertGreater(0, "Where lambda parameter must be registered in the alias table");
	}

	[TestMethod]
	public void Select_RegistersLambdaParameter()
	{
		var items = CreateQueryable<TestItem>();

		Expression<Func<TestItem, string>> projection = i => i.Name;
		var (_, aliases) = Translate<TestItem>(items.Select(projection));

		aliases.Count.AssertGreater(0, "Select lambda parameter must be registered in the alias table");
	}

	[TestMethod]
	public void OrderBy_RegistersLambdaParameter()
	{
		var items = CreateQueryable<TestItem>();

		Expression<Func<TestItem, int>> key = i => i.Priority;
		var (_, aliases) = Translate<TestItem>(items.OrderBy(key));

		aliases.Count.AssertGreater(0, "OrderBy lambda parameter must be registered in the alias table");
	}

	[TestMethod]
	public void RegisteredParameter_PointsToTableAliasOrJoinAlias()
	{
		// Every registered alias must be a known table or join alias —
		// never a stale string left over from previous queries.
		var items = CreateQueryable<TestItem>();

		Expression<Func<TestItem, bool>> predicate = item => item.Priority > 0;
		var (sql, aliases) = Translate<TestItem>(items.Where(predicate));

		foreach (var pair in aliases)
		{
			pair.Value.IsEmpty().AssertFalse($"Registered alias for parameter '{pair.Key.Name}' must not be empty");
			// The default FROM alias is "e" unless overridden by a join.
			(pair.Value == "e" || sql.ContainsIgnoreCase($"[{pair.Value}]")).AssertTrue(
				$"Registered alias '{pair.Value}' for parameter '{pair.Key.Name}' should appear in the SQL or be the default 'e', got: {sql}");
		}
	}
}

#endif
