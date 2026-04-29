#if NET10_0_OR_GREATER

namespace Ecng.Tests.Data;

using System.Linq.Expressions;

using Ecng.Data;
using Ecng.Data.Sql;
using Ecng.Serialization;

/// <summary>
/// Locks in translator behaviour for entities keyed by <see cref="Guid"/>.
/// All existing test entities use <c>long</c> identity, so support for
/// GUID primary keys was implicit; these tests make it explicit so a
/// regression cannot slip through unnoticed.
/// </summary>
[TestClass]
public class GuidIdentityTranslationTests : BaseTestClass
{
	[Entity(Name = "Ecng_TestGuidEntity")]
	public class TestGuidEntity : IDbPersistable
	{
		public Guid Id { get; set; }
		public string Name { get; set; }

		object IDbPersistable.GetIdentity() => Id;
		void IDbPersistable.SetIdentity(object id) => Id = id.To<Guid>();

		public void Save(SettingsStorage storage)
			=> storage.Set(nameof(Name), Name);

		public ValueTask LoadAsync(SettingsStorage storage, IStorage db, CancellationToken cancellationToken)
		{
			Name = storage.GetValue<string>(nameof(Name));
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
	public void Where_EqualsCapturedGuid_ParametrisesValue()
	{
		var target = Guid.Parse("11111111-2222-3333-4444-555555555555");
		var entities = CreateQueryable<TestGuidEntity>();

		var (sql, parameters) = Translate<TestGuidEntity>(entities.Where(e => e.Id == target));

		sql.ContainsIgnoreCase("[Id]").AssertTrue($"Expected [Id] in predicate, got: {sql}");

		parameters.Values.Any(p => p.Item1 == typeof(Guid) && Equals(p.Item2, target)).AssertTrue(
			$"Expected the captured GUID to land in the parameter list, got: {string.Join(",", parameters.Select(p => p.Value.Item2))}");
	}

	[TestMethod]
	public void Where_GuidArrayContains_ExpandsAllValuesIntoParameters()
	{
		var ids = new[]
		{
			Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
			Guid.Parse("ffffffff-0000-1111-2222-333333333333"),
		};
		var entities = CreateQueryable<TestGuidEntity>();

		var (sql, parameters) = Translate<TestGuidEntity>(entities.Where(e => ids.Contains(e.Id)));

		sql.ContainsIgnoreCase("[Id]").AssertTrue($"Expected [Id] in predicate, got: {sql}");

		var guidParams = parameters.Values.Where(p => p.Item1 == typeof(Guid)).Select(p => (Guid)p.Item2).ToArray();
		guidParams.Contains(ids[0]).AssertTrue($"Missing first GUID in parameters, got: {string.Join(",", guidParams)}");
		guidParams.Contains(ids[1]).AssertTrue($"Missing second GUID in parameters, got: {string.Join(",", guidParams)}");
	}

	[TestMethod]
	public void Select_AllColumns_ReferencesGuidIdentity()
	{
		// Default-shape SELECT must still surface the GUID-id table via its
		// alias and not strip the identity column.
		var entities = CreateQueryable<TestGuidEntity>();

		var (sql, _) = Translate<TestGuidEntity>(entities);

		sql.ContainsIgnoreCase("from [Ecng_TestGuidEntity]").AssertTrue($"Expected FROM Ecng_TestGuidEntity, got: {sql}");
		sql.Contains("[e].*").AssertTrue($"Expected [e].* default projection, got: {sql}");
	}
}

#endif
