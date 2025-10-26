namespace Ecng.Tests.Linq;

using System.Collections;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Linq;

[TestClass]
public class QueryableExtensionsTests
{
	// Minimal in-memory IQueryable provider that understands our QueryableExtensions methods
	private interface ITestAsyncQueryable
	{
		IEnumerable Enumerate();
	}

	private class TestAsyncQueryable<T> : IQueryable<T>, IEnumerable<T>, ITestAsyncQueryable
	{
		private readonly IEnumerable<T> _enumerable;
		private readonly IQueryProvider _provider;

		public TestAsyncQueryable(IEnumerable<T> enumerable)
		{
			_enumerable = enumerable ?? throw new ArgumentNullException(nameof(enumerable));
			_provider = new TestQueryProvider();
			Expression = Expression.Constant(this);
		}

		public IEnumerator<T> GetEnumerator() => _enumerable.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public Type ElementType => typeof(T);
		public Expression Expression { get; }
		public IQueryProvider Provider => _provider;

		IEnumerable ITestAsyncQueryable.Enumerate() => _enumerable;
	}

	private class TestQueryProvider : IQueryProvider
	{
		public IQueryable CreateQuery(Expression expression)
		{
			var elementType = expression.Type.GetGenericArguments().First();
			var method = typeof(TestQueryProvider).GetMethod(nameof(CreateQueryGeneric), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
				.MakeGenericMethod(elementType);
			return (IQueryable)method.Invoke(this, [expression])!;
		}

		public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
			=> CreateQueryGeneric<TElement>(expression);

		private IQueryable<TElement> CreateQueryGeneric<TElement>(Expression expression)
		{
			if (expression is MethodCallExpression mc && mc.Method.DeclaringType == typeof(QueryableExtensions) && mc.Method.Name == nameof(QueryableExtensions.SkipLong))
			{
				var srcEnum = Enumerate(mc.Arguments[0]).Cast<TElement>().ToList();
				var count = (long)Evaluate(mc.Arguments[1]);
				IEnumerable<TElement> result = srcEnum;
				if (count > 0)
				{
					result = count >= srcEnum.Count ? [] : srcEnum.Skip((int)Math.Min(count, int.MaxValue));
				}
				return new TestAsyncQueryable<TElement>(result);
			}

			// Fallback to wrapping the current expression source without transformation
			return new TestAsyncQueryable<TElement>(Enumerate(expression).Cast<TElement>());
		}

		public object Execute(Expression expression)
		{
			var method = typeof(TestQueryProvider).GetMethod(nameof(ExecuteGeneric), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
				.MakeGenericMethod(expression.Type);
			return method.Invoke(this, [expression])!;
		}

		public TResult Execute<TResult>(Expression expression)
			=> ExecuteGeneric<TResult>(expression);

		private TRes ExecuteGeneric<TRes>(Expression expression)
		{
			if (expression is MethodCallExpression mc && mc.Method.DeclaringType == typeof(QueryableExtensions))
			{
				var methodName = mc.Method.Name;
				if (methodName == nameof(QueryableExtensions.FirstOrDefaultAsync))
				{
					var srcType = mc.Method.GetGenericArguments()[0];
					var src = Enumerate(mc.Arguments[0]);
					var firstOrDefault = typeof(Enumerable).GetMethods()
						.Single(m => m.Name == nameof(Enumerable.FirstOrDefault) && m.GetParameters().Length == 1)
						.MakeGenericMethod(srcType)
						.Invoke(null, [src]);

					var vt = Activator.CreateInstance(typeof(ValueTask<>).MakeGenericType(srcType), firstOrDefault)!;
					return (TRes)vt;
				}
				if (methodName == nameof(QueryableExtensions.CountAsync))
				{
					var srcType = mc.Method.GetGenericArguments()[0];
					var src = Enumerate(mc.Arguments[0]);
					var longCount = typeof(Enumerable).GetMethods()
						.Single(m => m.Name == nameof(Enumerable.LongCount) && m.GetParameters().Length == 1)
						.MakeGenericMethod(srcType)
						.Invoke(null, [src]);

					var vt = new ValueTask<long>((long)longCount!);
					return (TRes)(object)vt;
				}
				if (methodName == nameof(QueryableExtensions.ToArrayAsync))
				{
					var srcType = mc.Method.GetGenericArguments()[0];
					var src = Enumerate(mc.Arguments[0]);
					var toArray = typeof(Enumerable).GetMethods()
						.Single(m => m.Name == nameof(Enumerable.ToArray) && m.GetParameters().Length == 1)
						.MakeGenericMethod(srcType)
						.Invoke(null, [src]);

					var vt = Activator.CreateInstance(typeof(ValueTask<>).MakeGenericType(srcType.MakeArrayType()), toArray)!;
					return (TRes)vt;
				}
			}

			// As a last resort try to compile and execute the expression
			var lambda = Expression.Lambda<Func<TRes>>(expression);
			return lambda.Compile().Invoke();
		}

		private static IEnumerable Enumerate(Expression expr)
		{
			var value = Evaluate(expr);
			if (value is ITestAsyncQueryable q)
				return q.Enumerate();
			if (value is IEnumerable enumerable)
				return enumerable;
			throw new InvalidOperationException("Unsupported source expression.");
		}

		private static object Evaluate(Expression expr)
			=> Expression.Lambda(expr).Compile().DynamicInvoke()!;
	}

	private static IQueryable<T> AsTestQueryable<T>(IEnumerable<T> source) => new TestAsyncQueryable<T>(source);

	[TestMethod]
	public async Task FirstOrDefaultAsync_Empty_ReturnsDefault()
	{
		var query = AsTestQueryable(Array.Empty<int>());
		var val = await query.FirstOrDefaultAsync(CancellationToken.None);
		val.AssertEqual(default);
	}

	[TestMethod]
	public async Task CountAsync_ReturnsCorrectValue()
	{
		var query = AsTestQueryable([1, 2, 3]);
		var count = await query.CountAsync(CancellationToken.None);
		count.AssertEqual(3L);
	}

	[TestMethod]
	public async Task ToArrayAsync_ReturnsArray()
	{
		var query = AsTestQueryable([1, 2, 3]);
		var arr = await query.ToArrayAsync(CancellationToken.None);
		arr.SequenceEqual([1, 2, 3]).AssertTrue();
	}

	// TODO
	//[TestMethod]
	//public void SkipLong_SkipsElements()
	//{
	//	var query = AsTestQueryable([1, 2, 3, 4]);
	//	var skipped = query.SkipLong(2).Cast<int>().ToArray();
	//	skipped.SequenceEqual([3, 4]).AssertTrue();
	//}

	[TestMethod]
	public async Task AnyAsync_NonEmptyWithDefaultValue()
	{
		var query = AsTestQueryable([0]);
		var hasAny = await query.AnyAsync(CancellationToken.None);
		// Expected: true (sequence has an element even if it's default(T))
		hasAny.AssertTrue();
	}

	private class RefItem
	{
		public int Id { get; set; }
		public string Name { get; set; }
	}

	[TestMethod]
	public async Task FirstOrDefaultAsync_RefType_ReturnsFirst()
	{
		var items = new[] { new RefItem { Id = 1, Name = "a" }, new RefItem { Id = 2, Name = "b" } };
		var query = AsTestQueryable(items);
		var first = await query.FirstOrDefaultAsync(CancellationToken.None);
		(first?.Id).AssertEqual(1);
		(first?.Name).AssertEqual("a");
	}

	[TestMethod]
	public async Task FirstOrDefaultAsync_RefType_Empty_ReturnsNull()
	{
		var query = AsTestQueryable(Array.Empty<RefItem>());
		var first = await query.FirstOrDefaultAsync(CancellationToken.None);
		(first is null).AssertTrue();
	}

	[TestMethod]
	public async Task CountAsync_RefType()
	{
		var items = new[] { new RefItem(), new RefItem(), new RefItem() };
		var query = AsTestQueryable(items);
		var count = await query.CountAsync(CancellationToken.None);
		count.AssertEqual(3L);
	}

	[TestMethod]
	public async Task ToArrayAsync_RefType()
	{
		var items = new[] { new RefItem { Id = 10 }, new RefItem { Id = 11 } };
		var query = AsTestQueryable(items);
		var arr = await query.ToArrayAsync(CancellationToken.None);
		arr.Length.AssertEqual(2);
		arr[0].Id.AssertEqual(10);
	}

	[TestMethod]
	public async Task AnyAsync_RefType_WithNullElement()
	{
		var items = new RefItem[] { null };
		var query = AsTestQueryable(items);
		var hasAny = await query.AnyAsync(CancellationToken.None);
		hasAny.AssertTrue();
	}
}