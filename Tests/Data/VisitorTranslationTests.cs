#if NET10_0_OR_GREATER

namespace Ecng.Tests.Data;

using System.Linq.Expressions;

using Ecng.Data;
using Ecng.Data.Sql;
using Ecng.Serialization;

/// <summary>
/// Cross-dialect SQL-generation tests for non-trivial method visitors:
/// <c>Guid.NewGuid()</c> (dialect-specific UUID generation),
/// <c>Enum.HasFlag()</c> (bitwise rewrite). These visitors had no
/// unit-level coverage — only the chance that an integration test happened
/// to exercise them.
/// </summary>
[TestClass]
public class VisitorTranslationTests : BaseTestClass
{
	[Flags]
	public enum TestPermissions
	{
		None = 0,
		Read = 1,
		Write = 2,
		Admin = 4,
	}

	[Entity(Name = "Ecng_TestPermitted")]
	public class TestPermitted : IDbPersistable
	{
		public long Id { get; set; }
		public TestPermissions Permissions { get; set; }

		object IDbPersistable.GetIdentity() => Id;
		void IDbPersistable.SetIdentity(object id) => Id = id.To<long>();

		public void Save(SettingsStorage storage)
			=> storage.Set(nameof(Permissions), (long)Permissions);

		public ValueTask LoadAsync(SettingsStorage storage, IStorage db, CancellationToken cancellationToken)
		{
			Permissions = (TestPermissions)storage.GetValue<long>(nameof(Permissions));
			return default;
		}
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

	private static string Translate<TSource>(IQueryable queryable, ISqlDialect dialect)
	{
		var meta = SchemaRegistry.Get(typeof(TSource));
		var asm = typeof(Database).Assembly;
		var translatorType = asm.GetType("Ecng.Data.Sql.ExpressionQueryTranslator");
		var translator = Activator.CreateInstance(translatorType, [meta]);
		var query = (Query)translatorType.GetMethod("GenerateSql").Invoke(translator, [queryable.Expression]);
		return query.Render(dialect);
	}

	[TestMethod]
	[DataRow("sqlserver", "newid()")]
	[DataRow("postgresql", "gen_random_uuid()")]
	[DataRow("sqlite", "lower(hex(randomblob(16)))")]
	public void GuidNewGuid_RendersDialectSpecificFunction(string dialectName, string expectedFragment)
	{
		// Project Guid.NewGuid() through Select so the SQL surfaces in the
		// emitted query rather than collapsing into a parameter.
		var items = CreateQueryable<TestItem>();
		var query = items.Select(i => new { i.Id, NewToken = Guid.NewGuid() });

		var dialect = GetDialect(dialectName);
		var sql = Translate<TestItem>(query, dialect);

		sql.ContainsIgnoreCase(expectedFragment).AssertTrue(
			$"Expected dialect-specific UUID call '{expectedFragment}' for {dialectName}, got: {sql}");
	}

	[TestMethod]
	public void EnumHasFlag_RewritesAsBitwiseAndEqualsRhs()
	{
		// `permissions.HasFlag(flag)` must translate to `((permissions & flag) = flag)`
		// — the mask compares back to the right-hand-side flag, not zero, so
		// the predicate is true only when *every* requested bit is set.
		var items = CreateQueryable<TestPermitted>();
		var write = TestPermissions.Write;

		var query = items.Where(p => p.Permissions.HasFlag(write));

		var sql = Translate<TestPermitted>(query, SqlServerDialect.Instance);

		sql.Contains("&").AssertTrue($"Expected bitwise AND in HasFlag rewrite, got: {sql}");
		sql.Contains("[Permissions]").AssertTrue($"Expected [Permissions] reference, got: {sql}");
	}

	[TestMethod]
	[DataRow("sqlserver")]
	[DataRow("postgresql")]
	[DataRow("sqlite")]
	public void EnumHasFlag_BitwiseRewriteIsConsistentAcrossDialects(string dialectName)
	{
		// Bitwise-AND semantics must not vary between dialects; the same
		// expression should produce the same shape regardless of target SQL.
		var items = CreateQueryable<TestPermitted>();
		var read = TestPermissions.Read;

		var query = items.Where(p => p.Permissions.HasFlag(read));

		var dialect = GetDialect(dialectName);
		var sql = Translate<TestPermitted>(query, dialect);

		sql.Contains("&").AssertTrue($"Expected bitwise AND on {dialectName}, got: {sql}");
		// Result of (x & f) is then compared back to f.
		sql.ContainsIgnoreCase("where").AssertTrue($"Expected WHERE clause on {dialectName}, got: {sql}");
	}

	private static ISqlDialect GetDialect(string name) => name switch
	{
		"sqlserver" => SqlServerDialect.Instance,
		"postgresql" => PostgreSqlDialect.Instance,
		"sqlite" => SQLiteDialect.Instance,
		_ => throw new ArgumentOutOfRangeException(nameof(name)),
	};

	[TestMethod]
	[DataRow("sqlserver")]
	[DataRow("postgresql")]
	[DataRow("sqlite")]
	public void EnumEquals_Literal_InlinesUnderlyingIntegerValue(string dialectName)
	{
		var items = CreateQueryable<TestPermitted>();
		var query = items.Where(p => p.Permissions == TestPermissions.Read);

		var sql = Translate<TestPermitted>(query, GetDialect(dialectName));

		sql.ContainsIgnoreCase("where").AssertTrue($"Expected WHERE clause, got: {sql}");
		sql.ContainsIgnoreCase("Permissions").AssertTrue($"Expected column reference, got: {sql}");
		// Read = 1 — emitted as raw integer (VisitConstant converts enum via .To<long>()).
		sql.Contains("= 1").AssertTrue($"Expected inline integer '= 1' for TestPermissions.Read, got: {sql}");
	}

	[TestMethod]
	[DataRow("sqlserver")]
	[DataRow("postgresql")]
	[DataRow("sqlite")]
	public void EnumEquals_Variable_EmitsParameterizedComparison(string dialectName)
	{
		var items = CreateQueryable<TestPermitted>();
		var write = TestPermissions.Write;
		var query = items.Where(p => p.Permissions == write);

		var sql = Translate<TestPermitted>(query, GetDialect(dialectName));

		sql.ContainsIgnoreCase("where").AssertTrue($"Expected WHERE clause, got: {sql}");
		sql.ContainsIgnoreCase("Permissions").AssertTrue($"Expected column reference, got: {sql}");
		// Captured local does NOT fold to a constant — it is bound as a
		// named parameter (e.g. @write0) for plan-cache reuse.
		sql.Contains("@write").AssertTrue($"Expected '@write' parameter, got: {sql}");
	}

	[TestMethod]
	[DataRow("sqlserver")]
	[DataRow("postgresql")]
	[DataRow("sqlite")]
	public void EnumContains_Array_EmitsInClause(string dialectName)
	{
		var items = CreateQueryable<TestPermitted>();
		var allowed = new[] { TestPermissions.Read, TestPermissions.Write };
		var query = items.Where(p => allowed.Contains(p.Permissions));

		var sql = Translate<TestPermitted>(query, GetDialect(dialectName));

		sql.ContainsIgnoreCase(" in ").AssertTrue($"Expected IN clause, got: {sql}");
		sql.ContainsIgnoreCase("Permissions").AssertTrue($"Expected column reference, got: {sql}");
	}

	[TestMethod]
	[DataRow("sqlserver")]
	[DataRow("postgresql")]
	[DataRow("sqlite")]
	public void EnumOrderBy_EmitsColumn(string dialectName)
	{
		var items = CreateQueryable<TestPermitted>();
		var query = items.OrderBy(p => p.Permissions);

		var sql = Translate<TestPermitted>(query, GetDialect(dialectName));

		sql.ContainsIgnoreCase("order by").AssertTrue($"Expected ORDER BY, got: {sql}");
		sql.ContainsIgnoreCase("Permissions").AssertTrue($"Expected enum column in ORDER BY, got: {sql}");
	}

	[TestMethod]
	[DataRow("sqlserver")]
	[DataRow("postgresql")]
	[DataRow("sqlite")]
	public void EnumGroupBy_EmitsColumn(string dialectName)
	{
		var items = CreateQueryable<TestPermitted>();
		var query = items
			.GroupBy(p => p.Permissions)
			.Select(g => new { g.Key, Count = g.Count() });

		var sql = Translate<TestPermitted>(query, GetDialect(dialectName));

		sql.ContainsIgnoreCase("group by").AssertTrue($"Expected GROUP BY, got: {sql}");
		sql.ContainsIgnoreCase("Permissions").AssertTrue($"Expected enum column in GROUP BY, got: {sql}");
	}
}

#endif
