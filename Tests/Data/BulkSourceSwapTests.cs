#if NET10_0_OR_GREATER

namespace Ecng.Tests.Data;

using System.Linq.Expressions;

using Ecng.Linq;
using Ecng.Serialization;

/// <summary>
/// Running a composed query over held copies of its tables instead of the tables themselves.
///
/// A query names more than one source as soon as it joins or nests. Swapping only the one it is composed
/// over leaves the others naming database tables, and LINQ-to-Objects then reaches them the only way it
/// can — synchronously, parking the calling thread for the whole round-trip. Under load those parked
/// threads outnumber the pool, which then grows by one thread per second and never catches up.
///
/// So the contract these tests hold is: every source the query names is found, and every one of them is
/// replaced.
/// </summary>
[TestClass]
public class BulkSourceSwapTests : BaseTestClass
{
	private sealed class Left
	{
		public int Id { get; set; }
	}

	private sealed class Right
	{
		public int LeftId { get; set; }
		public string Name { get; set; }
	}

	private static IQueryable<Left> Lefts() => new[] { new Left { Id = 1 } }.AsQueryable();
	private static IQueryable<Right> Rights() => new[] { new Right { LeftId = 1, Name = "one" } }.AsQueryable();

	[TestMethod]
	public void APlainQueryNamesItsOnlySource()
	{
		var source = Lefts();
		var query = source.Where(x => x.Id > 0);

		var found = query.Expression.EnumerateSources();

		AreEqual(1, found.Length);
		AreSame(source, found[0]);
	}

	[TestMethod]
	public void AJoinNamesBothSides()
	{
		var left = Lefts();
		var right = Rights();

		var query = left.Join(right, l => l.Id, r => r.LeftId, (l, r) => r.Name);

		var found = query.Expression.EnumerateSources();

		AreEqual(2, found.Length);
		IsTrue(found.Contains(left), "the source the query is composed over was not found");
		IsTrue(found.Contains(right), "the source the join brings in was not found");
	}

	[TestMethod]
	public void ASubQueryNamesItsSource()
	{
		var left = Lefts();
		var right = Rights();

		var query = left.Where(l => right.Any(r => r.LeftId == l.Id));

		var found = query.Expression.EnumerateSources();

		IsTrue(found.Contains(right), "the source a sub-query names was not found");
	}

	/// <summary>The same source named twice is one source, not two: it is replaced once, by one copy.</summary>
	[TestMethod]
	public void ASourceNamedTwiceIsListedOnce()
	{
		var left = Lefts();

		var query = left.Where(l => left.Any(o => o.Id == l.Id));

		AreEqual(1, query.Expression.EnumerateSources().Length);
	}

	[TestMethod]
	public void EveryNamedSourceIsReplaced()
	{
		var left = Lefts();
		var right = Rights();

		var heldLeft = new[] { new Left { Id = 1 }, new Left { Id = 2 } }.AsQueryable();
		var heldRight = new[] { new Right { LeftId = 2, Name = "held" } }.AsQueryable();

		var query = left.Join(right, l => l.Id, r => r.LeftId, (l, r) => r.Name);

		var swapped = query.Expression.ReplaceSources(new Dictionary<object, IQueryable>(ReferenceEqualityComparer.Instance)
		{
			{ left, heldLeft },
			{ right, heldRight },
		});

		var found = swapped.EnumerateSources();

		AreEqual(2, found.Length);
		IsFalse(found.Contains(left), "a source was left pointing at the original");
		IsFalse(found.Contains(right), "a source was left pointing at the original");

		// Running it proves the swap is not cosmetic: the answer comes from the held copies, which say
		// something the originals do not.
		var result = (IQueryable<string>)heldLeft.Provider.CreateQuery(swapped);
		AreEqual("held", result.Single());
	}

	// Built anew on every read, so finding it and replacing it meet two different objects.
	private static IQueryable<Right> RightsProperty => Rights();

	[TestMethod]
	public void ASourceReadThroughAPropertyIsReplaced()
	{
		var left = Lefts();
		var heldRight = new[] { new Right { LeftId = 1, Name = "held" } }.AsQueryable();

		var query = left.Where(l => RightsProperty.Any(r => r.LeftId == l.Id));

		var pinned = query.Expression.PinSources(out var sources);
		var right = sources.Single(s => s is IQueryable<Right>);

		var swapped = pinned.ReplaceSources(new Dictionary<object, IQueryable>(ReferenceEqualityComparer.Instance)
		{
			{ right, heldRight },
		});

		IsTrue(swapped.EnumerateSources().Contains(heldRight), "the source read through a property was not replaced");
		IsFalse(swapped.EnumerateSources().Contains(right), "a source was left pointing at the original");
	}

	[TestMethod]
	public void ASourceWithNoReplacementIsLeftAlone()
	{
		var left = Lefts();
		var query = left.Where(x => x.Id > 0);

		var swapped = query.Expression.ReplaceSources(new Dictionary<object, IQueryable>(ReferenceEqualityComparer.Instance));

		AreSame(left, swapped.EnumerateSources().Single());
	}

	// A table met by a query somebody else composed. It can be read asynchronously; being asked for a plain
	// enumerator is the mistake under test, so that is what it refuses.
	private sealed class Table : IQueryable<Right>, IAsyncEnumerable<Right>, IQueryProvider, IDefaultQueryProvider
	{
		private readonly Right[] _items;
		private readonly ConstantExpression _expression;

		public Table(Right[] items)
		{
			_items = items;
			_expression = System.Linq.Expressions.Expression.Constant(this);
		}

		Type IQueryable.ElementType => typeof(Right);
		Expression IQueryable.Expression => _expression;
		IQueryProvider IQueryable.Provider => this;

		IEnumerator<Right> IEnumerable<Right>.GetEnumerator() => throw new NotSupportedException("read synchronously");
		IEnumerator IEnumerable.GetEnumerator() => throw new NotSupportedException("read synchronously");

		async IAsyncEnumerator<Right> IAsyncEnumerable<Right>.GetAsyncEnumerator(CancellationToken cancellationToken)
		{
			await Task.Yield();

			foreach (var item in _items)
				yield return item;
		}

		IQueryable IQueryProvider.CreateQuery(Expression expression) => throw new NotSupportedException();
		IQueryable<TElement> IQueryProvider.CreateQuery<TElement>(Expression expression) => throw new NotSupportedException();
		object IQueryProvider.Execute(Expression expression) => throw new NotSupportedException();
		TResult IQueryProvider.Execute<TResult>(Expression expression) => throw new NotSupportedException();

		ValueTask<IQueryable> IDefaultQueryProvider.TryInitBulkLoad(CancellationToken cancellationToken) => default;

		ValueTask<IQueryable> IDefaultQueryProvider.ReadAllAsync(IQueryable source, CancellationToken cancellationToken)
			=> new(_items.AsQueryable());
	}

	/// <summary>
	/// A query composed by somebody else still names tables, and those must not be read synchronously.
	/// </summary>
	[TestMethod]
	public async Task ATableNamedByAnOutsideQueryIsNotReadSynchronously()
	{
		var table = new Table([new() { LeftId = 1, Name = "one" }]);

		var query = Lefts().Join(table, l => l.Id, r => r.LeftId, (l, r) => r.Name);

		var result = await query.ToArrayAsyncEx(CancellationToken);

		AreEqual("one", result.Single());
	}

	[TestMethod]
	public void NullArgumentsAreRefused()
	{
		Expression expression = null;

		Throws<ArgumentNullException>(() => expression.EnumerateSources());
		Throws<ArgumentNullException>(() => Lefts().Expression.ReplaceSources(null));
	}
}

#endif
