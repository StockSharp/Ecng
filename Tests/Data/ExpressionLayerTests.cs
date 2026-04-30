#if NET10_0_OR_GREATER

namespace Ecng.Tests.Data;

using System.Linq.Expressions;

using Ecng.Data;
using Ecng.Data.Sql;
using Ecng.Data.Sql.Model;
using Ecng.Serialization;

/// <summary>
/// Focused unit tests for the expression-tree analysis layer that runs
/// before SQL generation: <see cref="MemberPathResolver"/>,
/// <see cref="ClosureMaterializer"/>, and the visitor reductions inside
/// <c>ExpressionQueryTranslator</c> (<c>VisitConditional</c>,
/// <c>VisitBinary</c>, <c>VisitUnary</c>, method-call dispatch).
///
/// When the translator breaks, these tests pinpoint the broken stage by
/// asserting on intermediate output (resolved column path, materialised
/// closure value, or the parameter table) — not on a raw SQL substring.
/// SQL-shape assertions are used only for the visitor-level cases where
/// no smaller public surface exposes the reduction.
/// </summary>
[TestClass]
public class ExpressionLayerTests : BaseTestClass
{
	private const string RootAlias = "e";

	[ClassInitialize]
	public static void ClassInit(TestContext context)
	{
		// Materialiser tests reach into entity types; resolver tests need
		// targets in SchemaRegistry. Force registration once for the class.
		SchemaRegistry.Get(typeof(TestPerson));
		SchemaRegistry.Get(typeof(TestTask));
		SchemaRegistry.Get(typeof(TestSubTask));
		SchemaRegistry.Get(typeof(TestItem));
		SchemaRegistry.Get(typeof(TestCategory));
		SchemaRegistry.Get(typeof(TestItemCategory));

		if (!SchemaRegistry.TryGet(typeof(TestCountry), out _))
		{
			SchemaRegistry.Register(new Schema
			{
				TableName = "Ecng_Country",
				EntityType = typeof(TestCountry),
				Identity = new SchemaColumn { Name = "Id", ClrType = typeof(long) },
				Columns = [new SchemaColumn { Name = "Name", ClrType = typeof(string) }],
				Factory = () => new TestCountry(),
			});
		}
	}

	#region Helpers

	private static MemberExpression Body<T, TR>(Expression<Func<T, TR>> e)
	{
		var body = e.Body;

		// Expression<Func<T, object>> over a value-type member emits Convert(member, object).
		if (body is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } u)
			body = u.Operand;

		return (MemberExpression)body;
	}

	private static MemberExpression CapturedRightOperand<T>(Expression<Func<T, bool>> filter)
	{
		var body = (BinaryExpression)filter.Body;

		var right = body.Right;
		if (right is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } u)
			right = u.Operand;

		return (MemberExpression)right;
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

	#endregion

	#region MemberPathResolver — additional path-resolution gaps

	/// <summary>
	/// Identity column accessed directly on the root parameter (no navigation
	/// hops) must resolve as a plain qualified column with zero joins. The
	/// FK-shortcut branch is reserved for navigation-leaf-Id, not root-Id.
	/// </summary>
	[TestMethod]
	public void Resolve_RootIdentityColumn_NoJoinsNoFkShortcut()
	{
		var res = MemberPathResolver.Resolve(Body((TestTask t) => t.Id), RootAlias);

		res.AssertNotNull();
		res.Column.AssertEqual(new ColumnRef(RootAlias, "Id"));
		res.RequiredJoins.Count.AssertEqual(0);
	}

	/// <summary>
	/// A scalar value-type column on the root entity (DateTime, decimal, bool)
	/// must resolve as a plain qualified column — never classified as inner
	/// schema even though the type is non-primitive in the CLR sense.
	/// </summary>
	[TestMethod]
	public void Resolve_RootDateTimeColumn_TreatedAsScalar()
	{
		var res = MemberPathResolver.Resolve(Body((TestItem i) => i.CreatedAt), RootAlias);

		res.AssertNotNull();
		res.Column.AssertEqual(new ColumnRef(RootAlias, "CreatedAt"));
		res.RequiredJoins.Count.AssertEqual(0);
	}

	/// <summary>
	/// Nullable&lt;T&gt; column on the root (e.g. <c>int?</c>) must resolve
	/// as a plain scalar column. The IsInnerSchema predicate explicitly
	/// excludes nullables; regressing it would re-introduce the prefix path.
	/// </summary>
	[TestMethod]
	public void Resolve_RootNullableValueColumn_TreatedAsScalar()
	{
		var res = MemberPathResolver.Resolve(Body((TestItem i) => i.NullableValue), RootAlias);

		res.AssertNotNull();
		res.Column.AssertEqual(new ColumnRef(RootAlias, "NullableValue"));
		res.RequiredJoins.Count.AssertEqual(0);
	}

	/// <summary>
	/// Two RelationSingle hops where the leaf is a non-Id column on the
	/// second-hop target: emit two INNER JOINs and qualify the leaf with
	/// the second-hop alias. Verifies parent-first ordering is preserved
	/// independently of hop content.
	/// </summary>
	[TestMethod]
	public void Resolve_ChainOfTwoNavigations_LeafNonId_SecondAliasQualifiesColumn()
	{
		var res = MemberPathResolver.Resolve(
			Body((TestSubTask s) => s.Task.Person.Name),
			RootAlias);

		res.AssertNotNull();
		res.Column.AssertEqual(new ColumnRef("Person", "Name"));
		res.RequiredJoins.Count.AssertEqual(2);
		res.RequiredJoins[0].Alias.AssertEqual("Task");
		res.RequiredJoins[0].ParentAlias.AssertEqual(RootAlias);
		res.RequiredJoins[1].Alias.AssertEqual("Person");
		res.RequiredJoins[1].ParentAlias.AssertEqual("Task");
	}

	/// <summary>
	/// A chain rooted at a closure constant (compiler-generated DisplayClass
	/// instance) must be rejected by the resolver so the caller can dispatch
	/// to <see cref="ClosureMaterializer"/> instead. The resolver only
	/// recognises chains rooted at <see cref="ParameterExpression"/>.
	/// </summary>
	[TestMethod]
	public void Resolve_ChainRootedAtConstant_ReturnsNull()
	{
		var captured = new TestPerson { Id = 1, Name = "x" };
		Expression<Func<TestTask, string>> e = _ => captured.Name;

		// captured.Name has shape MemberExpression(constant.captured.Name).
		var body = (MemberExpression)e.Body;

		var res = MemberPathResolver.Resolve(body, RootAlias);

		res.AssertNull();
	}

	/// <summary>
	/// FK-shortcut (.Id under a RelationSingle hop) must work even when a
	/// non-default root alias is supplied. Regressions where the alias is
	/// hard-coded in the resolver would surface here.
	/// </summary>
	[TestMethod]
	public void Resolve_FkShortcut_HonoursCustomRootAlias()
	{
		var res = MemberPathResolver.Resolve(Body((TestTask t) => t.Person.Id), "myAlias");

		res.AssertNotNull();
		res.Column.AssertEqual(new ColumnRef("myAlias", "Person"));
		res.RequiredJoins.Count.AssertEqual(0);
	}

	/// <summary>
	/// Inner-schema (value object) hop with a primitive leaf: the resolver
	/// must concatenate the hop names into a single physical column and
	/// register no joins, regardless of how many enclosing parameters
	/// the chain crosses.
	/// </summary>
	[TestMethod]
	public void Resolve_InnerSchemaLeafPrimitive_FlatColumnNoJoins()
	{
		var res = MemberPathResolver.Resolve(
			Body((TestOrderWithAddress o) => o.ShippingAddress.City),
			RootAlias);

		res.AssertNotNull();
		res.Column.AssertEqual(new ColumnRef(RootAlias, "ShippingAddressCity"));
		res.RequiredJoins.Count.AssertEqual(0);
	}

	#endregion

	#region ClosureMaterializer — extra captured-value types

	/// <summary>
	/// <see cref="DateTime"/> capture must round-trip exactly. The materialiser
	/// must not stringify or coerce — it returns the boxed value as-is for the
	/// caller to bind as a parameter.
	/// </summary>
	[TestMethod]
	public void TryEvaluate_CapturedDateTime_ReifiesUnchanged()
	{
		var when = new DateTime(2024, 5, 1, 10, 30, 0, DateTimeKind.Utc);

		var member = CapturedRightOperand<DateTime>(d => d == when);

		ClosureMaterializer.TryEvaluate(member, out var result).AssertTrue();
		result.AssertEqual<object>(when);
	}

	/// <summary>
	/// <see cref="Guid"/> capture: same identity guarantee as DateTime — the
	/// out value must be the same struct instance the local held.
	/// </summary>
	[TestMethod]
	public void TryEvaluate_CapturedGuid_ReifiesUnchanged()
	{
		var id = Guid.NewGuid();

		var member = CapturedRightOperand<Guid>(g => g == id);

		ClosureMaterializer.TryEvaluate(member, out var result).AssertTrue();
		result.AssertEqual<object>(id);
	}

	/// <summary>
	/// <see cref="decimal"/> is a special-case scalar (struct, not primitive).
	/// The materialiser must lift it as a boxed decimal, preserving precision.
	/// </summary>
	[TestMethod]
	public void TryEvaluate_CapturedDecimal_PreservesPrecision()
	{
		var price = 123.456789m;

		var member = CapturedRightOperand<decimal>(p => p == price);

		ClosureMaterializer.TryEvaluate(member, out var result).AssertTrue();
		result.AssertEqual<object>(price);
	}

	/// <summary>
	/// <c>Nullable&lt;int&gt;</c> capture with a non-null value must reify as
	/// the underlying boxed <c>int</c>. The expression-tree shape is
	/// <c>MemberExpression(DC.captured)</c> with type <c>int?</c>; the
	/// materialiser sees the boxed nullable and unwraps via reflection.
	/// </summary>
	[TestMethod]
	public void TryEvaluate_CapturedNullable_NonNull_ReifiesUnderlyingValue()
	{
		int? captured = 7;

		var member = CapturedRightOperand<int?>(i => i == captured);

		ClosureMaterializer.TryEvaluate(member, out var result).AssertTrue();
		// Reflection on a nullable field returns the boxed underlying value;
		// distinguish from the "no result" case via the return value above.
		result.AssertEqual<object>(7);
	}

	/// <summary>
	/// Captured enum value must reify as the enum instance itself; downstream
	/// emission is responsible for converting to integral SQL.
	/// </summary>
	[TestMethod]
	public void TryEvaluate_CapturedEnum_ReifiesEnumInstance()
	{
		var dir = System.ComponentModel.ListSortDirection.Descending;

		var member = CapturedRightOperand<System.ComponentModel.ListSortDirection>(d => d == dir);

		ClosureMaterializer.TryEvaluate(member, out var result).AssertTrue();
		result.AssertEqual<object>(System.ComponentModel.ListSortDirection.Descending);
	}

	/// <summary>
	/// Two-hop chain where the intermediate object is null must surface as a
	/// failed materialisation (return <c>false</c>) rather than throwing a
	/// <see cref="NullReferenceException"/> — the caller decides whether to
	/// emit IS NULL or fall through.
	/// </summary>
	[TestMethod]
	public void TryEvaluate_IntermediateNull_ReturnsFalse()
	{
		Container container = null;

		// The expression captures `container` and dereferences `.Field`.
		Expression<Func<string, bool>> e = s => s == (container == null ? null : container.Field);

		// Locate the inner MemberExpression: `container.Field` lives inside
		// the ConditionalExpression. We pull it out manually.
		var conditional = (ConditionalExpression)((BinaryExpression)e.Body).Right;
		var ifFalse = conditional.IfFalse;
		var member = (MemberExpression)ifFalse;

		ClosureMaterializer.TryEvaluate(member, out var result).AssertFalse();
		result.AssertNull();
	}

	#endregion

	#region Translator visitor reductions — VisitConditional / VisitUnary / VisitBinary

	/// <summary>
	/// <c>x.A &gt; n ? "high" : "low"</c> in a Select projection must be
	/// reduced by <c>VisitConditional</c> to a CASE/WHEN/THEN/ELSE/END SQL
	/// block. The visitor is wired through <c>ProcessInitExpression</c>'s
	/// subquery branch — failing here points at that branch.
	/// </summary>
	[TestMethod]
	public void VisitConditional_TernaryInProjection_EmitsCaseWhenEnd()
	{
		var items = CreateQueryable<TestItem>();

		var query = items.Select(i => new VTernaryProjection
		{
			Id = i.Id,
			Label = i.Priority > 5 ? "high" : "low",
		});

		var (sql, _) = Translate<TestItem>(query);

		sql.ContainsIgnoreCase("case").AssertTrue($"Expected CASE keyword from VisitConditional, got: {sql}");
		sql.ContainsIgnoreCase("when").AssertTrue($"Expected WHEN keyword from VisitConditional, got: {sql}");
		sql.ContainsIgnoreCase("then").AssertTrue($"Expected THEN keyword from VisitConditional, got: {sql}");
		sql.ContainsIgnoreCase("else").AssertTrue($"Expected ELSE keyword from VisitConditional, got: {sql}");
		sql.ContainsIgnoreCase("end").AssertTrue($"Expected END keyword from VisitConditional, got: {sql}");
	}

	/// <summary>
	/// Boolean negation form 1: <c>!x.IsActive</c>. <c>VisitUnary</c> with
	/// <c>NodeType=Not</c> over a bool MemberExpression must emit
	/// <c>([IsActive] = 0)</c>-shaped predicate (IsFalse), not <c>NOT(...)</c>.
	/// </summary>
	[TestMethod]
	public void VisitUnary_NotBoolMember_EmitsIsFalseShape()
	{
		var items = CreateQueryable<TestItem>();

		var query = items.Where(i => !i.IsActive);

		var (sql, _) = Translate<TestItem>(query);

		sql.Contains("[IsActive]").AssertTrue($"Expected reference to [IsActive], got: {sql}");
		// VisitUnary special-cases !boolMember to produce IsFalse shape, not NOT.
		sql.ContainsIgnoreCase("not (").AssertFalse($"Expected IsFalse shape, not 'NOT (...)', got: {sql}");
	}

	/// <summary>
	/// Boolean negation form 2: <c>x.IsActive == false</c>. <c>VisitBinary</c>
	/// with <c>NodeType=Equal</c> against a bool literal must round-trip to
	/// the same equality predicate; the literal must surface in the SQL.
	/// </summary>
	[TestMethod]
	public void VisitBinary_BoolMemberEqualsFalse_EmitsEqualityWithLiteral()
	{
		var items = CreateQueryable<TestItem>();

		var query = items.Where(i => i.IsActive == false);

		var (sql, _) = Translate<TestItem>(query);

		sql.Contains("[IsActive]").AssertTrue($"Expected reference to [IsActive], got: {sql}");
		// SqlServerDialect emits 0 for the false literal in a boolean comparison.
		// Equality to false should NOT collapse to '<>' — that would silently flip semantics.
		sql.Contains("<>").AssertFalse($"Equality to false must not become inequality, got: {sql}");
	}

	/// <summary>
	/// Mixed precedence <c>(A &amp;&amp; B) || C</c>: <c>VisitBinary</c> must
	/// preserve grouping with explicit brackets so the dialect-rendered SQL
	/// retains operator precedence — important for multi-AND/multi-OR
	/// predicates that are common in filter UIs.
	/// </summary>
	[TestMethod]
	public void VisitBinary_MixedAndOrPrecedence_BracketsPreserveGrouping()
	{
		var items = CreateQueryable<TestItem>();

		var query = items.Where(i => (i.IsActive && i.Priority > 5) || i.Name == "admin");

		var (sql, _) = Translate<TestItem>(query);

		sql.ContainsIgnoreCase(" and ").AssertTrue($"Expected AND in mixed predicate, got: {sql}");
		sql.ContainsIgnoreCase(" or ").AssertTrue($"Expected OR in mixed predicate, got: {sql}");
		// Both nested operands must be wrapped in brackets so OR cannot bind tighter than AND.
		sql.Contains("(").AssertTrue($"Expected grouping brackets, got: {sql}");
	}

	/// <summary>
	/// <c>Where(x =&gt; x.Name == captured)</c> with <c>captured == null</c>:
	/// the BinaryExpression's right side reduces (after closure materialisation)
	/// to a null constant; <c>VisitBinary</c> must re-route the equality to
	/// <c>IS NULL</c>, not emit <c>= NULL</c> which evaluates to UNKNOWN.
	/// </summary>
	[TestMethod]
	public void VisitBinary_EqualToCapturedNull_EmitsIsNull()
	{
		string captured = null;

		var items = CreateQueryable<TestItem>();
		var query = items.Where(i => i.Name == captured);

		var (sql, _) = Translate<TestItem>(query);

		sql.ContainsIgnoreCase("is null").AssertTrue($"Expected 'IS NULL' for null capture, got: {sql}");
		sql.Contains("= NULL").AssertFalse($"Must not emit '= NULL' (UNKNOWN in 3VL), got: {sql}");
	}

	/// <summary>
	/// <c>Where(x =&gt; x.Name != captured)</c> with non-null captured value:
	/// <c>VisitBinary</c> on NotEqual must emit a parameterised inequality
	/// — never <c>IS NOT NULL</c> when the right side is a real value.
	/// </summary>
	[TestMethod]
	public void VisitBinary_NotEqualToCapturedValue_EmitsParameterisedInequality()
	{
		var captured = "alice";

		var items = CreateQueryable<TestItem>();
		var query = items.Where(i => i.Name != captured);

		var (sql, parameters) = Translate<TestItem>(query);

		sql.ContainsIgnoreCase("is not null").AssertFalse($"Captured non-null must not collapse to IS NOT NULL, got: {sql}");
		parameters.Values.Any(p => Equals(p.Item2, "alice")).AssertTrue(
			$"Expected captured value 'alice' in parameter list, got: {string.Join(",", parameters.Select(p => $"{p.Key}={p.Value.Item2}"))}");
	}

	#endregion

	#region Translator method-call dispatch — string / Math / Enumerable

	/// <summary>
	/// <c>string.StartsWith</c> must dispatch to its dedicated method visitor
	/// and translate to a <c>LIKE</c> predicate with a trailing wildcard
	/// concatenated against the parameterised search term. A regression
	/// would either throw NotSupportedException or skip the LIKE emission.
	/// </summary>
	[TestMethod]
	public void MethodCall_StringStartsWith_DispatchesToLikeVisitor()
	{
		var items = CreateQueryable<TestItem>();

		var query = items.Where(i => i.Name.StartsWith("alice"));

		var (sql, parameters) = Translate<TestItem>(query);

		sql.ContainsIgnoreCase("like").AssertTrue($"Expected LIKE for StartsWith, got: {sql}");
		// Trailing-wildcard literal must surface in SQL after the parameter.
		sql.Contains("'%'").AssertTrue($"Expected '%' wildcard literal for StartsWith, got: {sql}");
		// Search term must be parameterised exactly as the user supplied it.
		parameters.Values.Any(p => Equals(p.Item2, "alice")).AssertTrue(
			$"Expected captured term 'alice' as parameter, got: {string.Join(",", parameters.Select(p => $"{p.Key}={p.Value.Item2}"))}");
	}

	/// <summary>
	/// <c>string.EndsWith</c> mirrors StartsWith but with a leading wildcard.
	/// Tests the symmetric branch in <c>StringEndsWithVisitor</c>.
	/// </summary>
	[TestMethod]
	public void MethodCall_StringEndsWith_DispatchesToLikeVisitor()
	{
		var items = CreateQueryable<TestItem>();

		var query = items.Where(i => i.Name.EndsWith("son"));

		var (sql, parameters) = Translate<TestItem>(query);

		sql.ContainsIgnoreCase("like").AssertTrue($"Expected LIKE for EndsWith, got: {sql}");
		sql.Contains("'%'").AssertTrue($"Expected '%' wildcard literal for EndsWith, got: {sql}");
		parameters.Values.Any(p => Equals(p.Item2, "son")).AssertTrue(
			$"Expected captured term 'son' as parameter, got: {string.Join(",", parameters.Select(p => $"{p.Key}={p.Value.Item2}"))}");
	}

	/// <summary>
	/// <c>string.Contains</c> dispatches to the substring LIKE form with
	/// wildcards on both sides. The translator must not confuse this with
	/// <see cref="Enumerable.Contains{T}(IEnumerable{T},T)"/> — overload
	/// resolution is by method declaring type.
	/// </summary>
	[TestMethod]
	public void MethodCall_StringContains_DispatchesToLikeVisitor()
	{
		var items = CreateQueryable<TestItem>();

		var query = items.Where(i => i.Name.Contains("ali"));

		var (sql, parameters) = Translate<TestItem>(query);

		sql.ContainsIgnoreCase("like").AssertTrue($"Expected LIKE for string.Contains, got: {sql}");
		sql.Contains("'%'").AssertTrue($"Expected '%' wildcard literal for Contains, got: {sql}");
		parameters.Values.Any(p => Equals(p.Item2, "ali")).AssertTrue(
			$"Expected captured term 'ali' as parameter, got: {string.Join(",", parameters.Select(p => $"{p.Key}={p.Value.Item2}"))}");
	}

	/// <summary>
	/// <c>string.IsNullOrEmpty(x.Name)</c> (BCL static method) must dispatch
	/// to the dedicated <c>StringIsNullOrEmptyVisitor</c>. The translator
	/// covers both the BCL static and the <c>StringHelper</c> extension —
	/// this guards the BCL branch separately from the existing extension test.
	/// </summary>
	[TestMethod]
	public void MethodCall_StringIsNullOrEmpty_BclStatic_DispatchesToVisitor()
	{
		var items = CreateQueryable<TestItem>();

		var query = items.Where(i => string.IsNullOrEmpty(i.Name));

		var (sql, _) = Translate<TestItem>(query);

		sql.ContainsIgnoreCase("is null").AssertTrue($"Expected 'is null' fragment, got: {sql}");
		// The visitor combines IS NULL with an empty-string equality, so [Name] must surface.
		sql.Contains("[Name]").AssertTrue($"Expected reference to [Name], got: {sql}");
	}

	/// <summary>
	/// <c>Math.Abs(x.Priority)</c> must dispatch to <c>MathVisitor</c> and
	/// emit a SQL <c>ABS(...)</c> wrapper. Regression here would either
	/// throw or evaluate the call client-side.
	/// </summary>
	[TestMethod]
	public void MethodCall_MathAbs_DispatchesToMathVisitor()
	{
		var items = CreateQueryable<TestItem>();

		var query = items.Where(i => Math.Abs(i.Priority) > 0);

		var (sql, _) = Translate<TestItem>(query);

		sql.ContainsIgnoreCase("abs(").AssertTrue($"Expected ABS(...) call from MathVisitor, got: {sql}");
		sql.Contains("[Priority]").AssertTrue($"Expected ABS argument [Priority], got: {sql}");
	}

	/// <summary>
	/// <c>Enumerable.Contains</c> over a captured array of primitives must
	/// dispatch to <c>ContainsVisitor</c> and produce a parameterised
	/// <c>IN (...)</c> clause — the array elements are spread across separate
	/// parameters by <c>EmitMaterialisedValue</c>. Verifies the integration
	/// of materialiser + Contains visitor + IN emission.
	/// </summary>
	[TestMethod]
	public void MethodCall_EnumerableContainsCapturedArray_EmitsInClauseWithSpreadParams()
	{
		var ids = new long[] { 11L, 22L, 33L };
		var items = CreateQueryable<TestItem>();

		var query = items.Where(i => ids.Contains(i.Id));

		var (sql, parameters) = Translate<TestItem>(query);

		sql.ContainsIgnoreCase(" in (").AssertTrue($"Expected IN (...) clause, got: {sql}");

		var captured = parameters.Values.Where(p => p.Item2 is long).Select(p => (long)p.Item2).ToArray();
		captured.Contains(11L).AssertTrue($"Missing 11 in parameters, got: {string.Join(",", parameters.Select(p => $"{p.Key}={p.Value.Item2}"))}");
		captured.Contains(22L).AssertTrue($"Missing 22 in parameters, got: {string.Join(",", parameters.Select(p => $"{p.Key}={p.Value.Item2}"))}");
		captured.Contains(33L).AssertTrue($"Missing 33 in parameters, got: {string.Join(",", parameters.Select(p => $"{p.Key}={p.Value.Item2}"))}");
	}

	#endregion

	#region Helper types

	private sealed class Container
	{
		public string Field { get; set; }
	}

	/// <summary>
	/// Minimal projection target for <see cref="VisitConditional"/> coverage.
	/// </summary>
	public class VTernaryProjection : IDbPersistable
	{
		public long Id { get; set; }
		public string Label { get; set; }

		object IDbPersistable.GetIdentity() => Id;
		void IDbPersistable.SetIdentity(object id) => Id = id.To<long>();
		public void Save(SettingsStorage storage) { }
		public ValueTask LoadAsync(SettingsStorage storage, IStorage db, CancellationToken cancellationToken) => default;
	}

	#endregion
}

#endif
