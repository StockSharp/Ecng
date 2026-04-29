#if NET10_0_OR_GREATER

namespace Ecng.Tests.Data;

using System.Linq.Expressions;

using Ecng.Data;
using Ecng.Data.Sql;
using Ecng.Serialization;

/// <summary>
/// End-to-end regression matrix for closure-capture forms produced by the
/// C# compiler. Complements the unit tests on <c>ClosureMaterializer</c>
/// by asserting the final SQL emitted by the translator: every captured
/// local must surface as a SQL parameter, never as a raw column name.
/// Each entry guards a distinct DisplayClass shape that previously
/// required a dedicated branch in <c>VisitMember</c> and could regress
/// independently.
/// </summary>
[TestClass]
public class ClosureFormsRegressionTests : BaseTestClass
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

	private static (string sql, IDictionary<string, (Type, object)> parameters) Translate<TSource>(IQueryable queryable)
	{
		var meta = SchemaRegistry.Get(typeof(TSource));
		var asm = typeof(Database).Assembly;
		var translatorType = asm.GetType("Ecng.Data.Sql.ExpressionQueryTranslator");
		var translator = Activator.CreateInstance(translatorType, [meta]);
		var query = (Query)translatorType.GetMethod("GenerateSql").Invoke(translator, [queryable.Expression]);
		var parameters = (IDictionary<string, (Type, object)>)translatorType.GetProperty("Parameters").GetValue(translator);
		return (query.Render(SqlServerDialect.Instance), parameters);
	}

	[TestMethod]
	public void OneHopLocalCapture_ParametrisesValue()
	{
		// Simplest closure: a single local variable becomes a DisplayClass
		// field. The captured local name (`itemId`) must not appear as a
		// SQL identifier.
		var itemId = 42L;
		var items = CreateQueryable<TestItem>();

		var (sql, parameters) = Translate<TestItem>(items.Where(i => i.Id == itemId));

		sql.Contains("[itemId]").AssertFalse($"Captured local must not leak as a column, got: {sql}");
		parameters.Values.Any(p => Equals(p.Item2, 42L)).AssertTrue(
			$"Expected captured value 42 in parameter list, got: {string.Join(",", parameters)}");
	}

	[TestMethod]
	public void TwoHopOuterContainerThenInnerField_ParametrisesValue()
	{
		// Two-level capture: DisplayClass.container.Field. The intermediate
		// (`container`) must not leak either, only the final captured value.
		var container = new Container { FieldValue = "abc" };
		var items = CreateQueryable<TestItem>();

		var (sql, parameters) = Translate<TestItem>(items.Where(i => i.Name == container.FieldValue));

		sql.Contains("[container]").AssertFalse($"Outer holder name leaked, got: {sql}");
		sql.Contains("[FieldValue]").AssertFalse($"Inner field name leaked as identifier, got: {sql}");
		parameters.Values.Any(p => Equals(p.Item2, "abc")).AssertTrue(
			$"Expected captured value 'abc' in parameter list, got: {string.Join(",", parameters)}");
	}

	[TestMethod]
	public void NestedIfBlockLocal_TriggersNestedDisplayClass_StillReifies()
	{
		// Nested locals across an if-block produce a chain of DisplayClass
		// instances. This shape previously slipped past every individual
		// "is ConstantExpression" branch and ended up as a raw column.
		var depth = 1;
		string captured;

		if (depth == 1)
		{
			var inner = "inner-value";
			captured = inner;
		}
		else
		{
			captured = "outer-value";
		}

		var items = CreateQueryable<TestItem>();
		var (sql, parameters) = Translate<TestItem>(items.Where(i => i.Name == captured));

		sql.Contains("[captured]").AssertFalse($"Outer local leaked, got: {sql}");
		sql.Contains("[inner]").AssertFalse($"Inner local leaked, got: {sql}");
		parameters.Values.Any(p => Equals(p.Item2, "inner-value")).AssertTrue(
			$"Expected captured value 'inner-value' in parameter list, got: {string.Join(",", parameters)}");
	}

	[TestMethod]
	public async Task AsyncStateMachineCapture_StillReifies()
	{
		// Async methods stash locals on the state-machine struct, which is
		// itself reachable through a different DisplayClass shape than a
		// plain method's. The materialiser must walk it the same way.
		await Task.Yield();
		var asyncCaptured = 7L;

		var items = CreateQueryable<TestItem>();
		var (sql, parameters) = Translate<TestItem>(items.Where(i => i.Id == asyncCaptured));

		sql.Contains("[asyncCaptured]").AssertFalse($"Async local leaked, got: {sql}");
		parameters.Values.Any(p => Equals(p.Item2, 7L)).AssertTrue(
			$"Expected captured value 7 in parameter list, got: {string.Join(",", parameters)}");
	}

	[TestMethod]
	public void PrimaryCtorCapture_FromInstanceMethod_ReifiesField()
	{
		// Primary constructor parameter captured into a `this`-bound lambda.
		// The compiler synthesises a backing field on the enclosing class;
		// the closure shape is `<>4__this.<paramField>`.
		var holder = new PrimaryCtorHolder(99L);

		var items = CreateQueryable<TestItem>();
		var (sql, parameters) = Translate<TestItem>(holder.WhereIdEquals(items));

		sql.Contains("[primaryParam]").AssertFalse($"Primary-ctor parameter leaked, got: {sql}");
		parameters.Values.Any(p => Equals(p.Item2, 99L)).AssertTrue(
			$"Expected captured value 99 in parameter list, got: {string.Join(",", parameters)}");
	}

	[TestMethod]
	public void ArrayCapture_ExpandsIntoCommaSeparatedParameters()
	{
		// Array captures are expanded by the materialiser into one
		// parameter per element, in order. The local array name itself
		// must not appear in the SQL.
		var ids = new long[] { 10L, 20L, 30L };

		var items = CreateQueryable<TestItem>();

		// Use Contains so the translator visits the array slot.
		var (sql, parameters) = Translate<TestItem>(items.Where(i => ids.Contains(i.Id)));

		sql.Contains("[ids]").AssertFalse($"Array local leaked, got: {sql}");

		var expanded = parameters.Values.Where(p => p.Item2 is long).Select(p => (long)p.Item2).ToArray();
		(expanded.Length >= 3).AssertTrue($"Expected at least three expanded parameters, got: {string.Join(",", parameters)}");
		expanded.Contains(10L).AssertTrue($"Missing 10 in parameters, got: {string.Join(",", parameters)}");
		expanded.Contains(20L).AssertTrue($"Missing 20 in parameters, got: {string.Join(",", parameters)}");
		expanded.Contains(30L).AssertTrue($"Missing 30 in parameters, got: {string.Join(",", parameters)}");
	}

	private sealed class Container
	{
		public string FieldValue { get; set; }
	}

	private sealed class PrimaryCtorHolder(long primaryParam)
	{
		public IQueryable<TestItem> WhereIdEquals(IQueryable<TestItem> items)
			=> items.Where(i => i.Id == primaryParam);
	}
}

#endif
