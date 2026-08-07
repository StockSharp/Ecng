#if NET10_0_OR_GREATER

namespace Ecng.Tests.Data;

using System.Linq.Expressions;
using System.Text.RegularExpressions;

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
	/// <summary>
	/// A needle carrying every LIKE metacharacter that changes matching:
	/// <c>%</c> (any run), <c>_</c> (any single char) and <c>[</c>, which opens a
	/// character class on SQL Server.
	/// </summary>
	private const string _metaNeedle = "50%_[a-z]";

	/// <summary>
	/// The same needle once its metacharacters are neutralised by
	/// <see cref="SqlLike.EscapeChar"/>.
	/// </summary>
	private const string _escapedNeedle = "50!%!_![a-z]";

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
		=> TranslateWithParams<TSource>(queryable, dialect).sql;

	/// <summary>
	/// Same translation as <see cref="Translate{TSource}"/>, additionally handing
	/// back the bound parameters: a needle reaches the statement as a parameter
	/// value, never as inline SQL text, so the pattern it ends up carrying is only
	/// observable here.
	/// </summary>
	private static (string sql, IDictionary<string, (Type type, object value)> parameters) TranslateWithParams<TSource>(IQueryable queryable, ISqlDialect dialect)
	{
		var meta = SchemaRegistry.Get(typeof(TSource));
		var translator = new ExpressionQueryTranslator(meta);
		var query = translator.GenerateSql(queryable.Expression);
		return (query.Render(dialect), translator.Parameters);
	}

	private static string DescribeParams(IDictionary<string, (Type type, object value)> parameters)
		=> parameters.Select(p => $"{p.Key}={p.Value.value}").JoinComma();

	/// <summary>
	/// One <c>REPLACE(x, from, to)</c> of the escaping chain, as it stands in the
	/// rendered SQL. The dialect's unicode prefix is optional so one expression
	/// reads all three.
	/// </summary>
	private static readonly Regex _replaceStep = new(@",\s*N?'([^']*)',\s*N?'([^']*)'\)", RegexOptions.Compiled);

	/// <summary>
	/// Applies the escaping the rendered statement asks the database for, and
	/// returns what the needle becomes. The steps stand in the SQL innermost-first,
	/// which is the order the database applies them, so replaying them left to right
	/// reproduces the pattern the LIKE will actually see.
	/// </summary>
	private static string ReplayEscaping(string sql, string needle)
	{
		foreach (Match step in _replaceStep.Matches(sql))
			needle = needle.Replace(step.Groups[1].Value, step.Groups[2].Value);

		return needle;
	}

	/// <summary>
	/// Asserts that a translated LIKE predicate matches its needle literally.
	/// </summary>
	/// <param name="dialectName">Dialect the statement was rendered for.</param>
	/// <param name="wildcardBefore">Any text is allowed before the needle.</param>
	/// <param name="wildcardAfter">Any text is allowed after the needle.</param>
	/// <param name="translated">Rendered statement and its bound parameters.</param>
	/// <remarks>
	/// The needle stays a raw bound parameter — it is the caller's text, and may not
	/// even be a constant — so what has to be checked is the statement: it must
	/// declare the escape character, run the needle through an escaping that turns
	/// it into <see cref="_escapedNeedle"/>, and leave its own wildcards outside that
	/// escaping, or they would be matched as text instead of anchoring the match.
	/// Every unmet condition is reported together, so one run says everything the
	/// translation is missing.
	/// </remarks>
	private static void AssertNeedleMatchedLiterally(string dialectName, bool wildcardBefore, bool wildcardAfter, (string sql, IDictionary<string, (Type type, object value)> parameters) translated)
	{
		var (sql, parameters) = translated;
		var dialect = GetDialect(dialectName);
		var unmet = new List<string>();

		var escapeClause = $" escape '{SqlLike.EscapeChar}'";
		var likeAt = sql.IndexOf(" like ", StringComparison.OrdinalIgnoreCase);
		var escapeAt = sql.IndexOf(escapeClause, StringComparison.Ordinal);

		if (likeAt < 0)
			unmet.Add("no LIKE predicate");

		if (escapeAt < 0)
			unmet.Add($"no ESCAPE clause declaring '{SqlLike.EscapeChar}'");

		if (likeAt >= 0 && escapeAt > likeAt)
		{
			var operand = sql[(likeAt + " like ".Length)..escapeAt];

			var wildcard = $"{dialect.UnicodePrefix}'%'";
			var lead = $"{wildcard} {dialect.ConcatOperator} ";
			var trail = $" {dialect.ConcatOperator} {wildcard}";

			if (wildcardBefore != operand.StartsWith(lead))
				unmet.Add($"the leading wildcard is {(wildcardBefore ? "missing from" : "present in")} the pattern '{operand}'");

			if (wildcardAfter != operand.EndsWith(trail))
				unmet.Add($"the trailing wildcard is {(wildcardAfter ? "missing from" : "present in")} the pattern '{operand}'");

			var needleOnly = operand;

			if (wildcardBefore)
				needleOnly = needleOnly[lead.Length..];

			if (wildcardAfter)
				needleOnly = needleOnly[..^trail.Length];

			var escaped = ReplayEscaping(needleOnly, _metaNeedle);

			if (escaped != _escapedNeedle)
				unmet.Add($"the statement escapes the needle to '{escaped}' rather than '{_escapedNeedle}'");
		}

		unmet.Count.AssertEqual(0, $"On {dialectName} the needle is not matched literally - {unmet.JoinCommaSpace()}.{Environment.NewLine}sql: {sql}{Environment.NewLine}params: {DescribeParams(parameters)}");
	}

	[TestMethod]
	[DataRow("sqlserver")]
	[DataRow("postgresql")]
	[DataRow("sqlite")]
	public void StringContains_MatchesNeedleLiterally(string dialectName)
	{
		// A search box passes user-typed text straight into Contains. The text is
		// data, not a pattern: "50%" must find the rows spelling out "50%", not
		// every row, and "[a-z]" must find that literal text, not any single letter.
		var items = CreateQueryable<TestItem>();
		var needle = _metaNeedle;

		var query = items.Where(i => i.Name.Contains(needle));

		AssertNeedleMatchedLiterally(dialectName, wildcardBefore: true, wildcardAfter: true, TranslateWithParams<TestItem>(query, GetDialect(dialectName)));
	}

	[TestMethod]
	[DataRow("sqlserver")]
	[DataRow("postgresql")]
	[DataRow("sqlite")]
	public void StringStartsWith_MatchesNeedleLiterally(string dialectName)
	{
		var items = CreateQueryable<TestItem>();
		var needle = _metaNeedle;

		var query = items.Where(i => i.Name.StartsWith(needle));

		AssertNeedleMatchedLiterally(dialectName, wildcardBefore: false, wildcardAfter: true, TranslateWithParams<TestItem>(query, GetDialect(dialectName)));
	}

	[TestMethod]
	[DataRow("sqlserver")]
	[DataRow("postgresql")]
	[DataRow("sqlite")]
	public void StringEndsWith_MatchesNeedleLiterally(string dialectName)
	{
		var items = CreateQueryable<TestItem>();
		var needle = _metaNeedle;

		var query = items.Where(i => i.Name.EndsWith(needle));

		AssertNeedleMatchedLiterally(dialectName, wildcardBefore: true, wildcardAfter: false, TranslateWithParams<TestItem>(query, GetDialect(dialectName)));
	}

	/// <summary>
	/// The needle is a column here, so no C# code could have escaped it while the
	/// statement was built — the escaping has to be the database's work, and the
	/// same escaping must be emitted.
	/// </summary>
	[TestMethod]
	[DataRow("sqlserver")]
	[DataRow("postgresql")]
	[DataRow("sqlite")]
	public void StringContains_ColumnNeedle_MatchesLiterally(string dialectName)
	{
		var categories = CreateQueryable<TestCategory>();

		var query = categories.Where(c => c.CategoryName.Contains(c.Description));

		var translated = TranslateWithParams<TestCategory>(query, GetDialect(dialectName));

		translated.parameters.Count.AssertEqual(0);
		AssertNeedleMatchedLiterally(dialectName, wildcardBefore: true, wildcardAfter: true, translated);
	}

	[TestMethod]
	[DataRow("sqlserver", "newid()")]
	[DataRow("postgresql", "gen_random_uuid()")]
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
	public void GuidNewGuid_RendersSQLiteDialectNewId()
	{
		var items = CreateQueryable<TestItem>();
		var query = items.Select(i => new { i.Id, NewToken = Guid.NewGuid() });

		var sql = Translate<TestItem>(query, SQLiteDialect.Instance);

		sql.ContainsIgnoreCase(SQLiteDialect.Instance.NewId()).AssertTrue(
			$"Expected SQLite UUID call '{SQLiteDialect.Instance.NewId()}', got: {sql}");
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
	public void LikeEscaped_EmitsEscapeClause(string dialectName)
	{
		// The ESCAPE clause is SQL-standard and renders identically everywhere,
		// which is what makes client-side escaping dialect-independent.
		var items = CreateQueryable<TestItem>();
		var like = "%abc%";

		var query = items.Where(i => i.Name.LikeEscaped(like));

		var sql = Translate<TestItem>(query, GetDialect(dialectName));

		sql.ContainsIgnoreCase(" like ").AssertTrue($"Expected LIKE on {dialectName}, got: {sql}");
		sql.Contains($" escape '{SqlLike.EscapeChar}'").AssertTrue($"Expected ESCAPE clause on {dialectName}, got: {sql}");
		sql.Contains("@like").AssertTrue($"Expected the pattern to stay a bound parameter, got: {sql}");
	}

	[TestMethod]
	[DataRow("sqlserver")]
	[DataRow("postgresql")]
	[DataRow("sqlite")]
	public void Like_LeavesEscapeClauseOff(string dialectName)
	{
		// Counterpart of LikeEscaped_EmitsEscapeClause, which is this test's
		// positive control: the same harness does emit "escape" for the escaped
		// marker, so an empty result here means the plain marker stayed plain
		// rather than the assertion being unable to fail.
		var items = CreateQueryable<TestItem>();
		var like = "%abc%";

		var query = items.Where(i => i.Name.Like(like));

		var sql = Translate<TestItem>(query, GetDialect(dialectName));

		sql.ContainsIgnoreCase(" like ").AssertTrue($"Expected LIKE on {dialectName}, got: {sql}");
		sql.ContainsIgnoreCase("escape").AssertFalse($"Plain Like must not declare an escape character, got: {sql}");
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
