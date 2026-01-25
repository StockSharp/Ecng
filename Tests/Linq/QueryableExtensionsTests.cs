namespace Ecng.Tests.Linq;

using System.Collections;
using System.Linq.Expressions;

using Ecng.Linq;

[TestClass]
public class QueryableExtensionsTests : BaseTestClass
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
			: this(enumerable, null)
		{
		}

		public TestAsyncQueryable(IEnumerable<T> enumerable, Expression expression)
		{
			_enumerable = enumerable ?? throw new ArgumentNullException(nameof(enumerable));
			_provider = new TestQueryProvider();
			Expression = expression ?? Expression.Constant(this);
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
			if (expression is MethodCallExpression mc)
			{
				// Handle SkipLong from QueryableExtensions - supports long count
				if (mc.Method.DeclaringType == typeof(QueryableExtensions) && mc.Method.Name == nameof(QueryableExtensions.SkipLong))
				{
					var srcEnum = Enumerate(mc.Arguments[0]).Cast<TElement>().ToList();
					var count = (long)Evaluate(mc.Arguments[1]);
					// For in-memory: if count >= collection size, return empty
					IEnumerable<TElement> result = count >= srcEnum.Count ? [] : srcEnum.Skip((int)count);
					// Preserve expression so tests can verify the Expression tree structure
					return new TestAsyncQueryable<TElement>(result, expression);
				}

				// Handle standard Queryable.Skip
				if (mc.Method.DeclaringType == typeof(Queryable) && mc.Method.Name == nameof(Queryable.Skip))
				{
					var srcEnum = Enumerate(mc.Arguments[0]).Cast<TElement>().ToList();
					var count = (int)Evaluate(mc.Arguments[1]);
					IEnumerable<TElement> result = count >= srcEnum.Count ? [] : srcEnum.Skip(count);
					return new TestAsyncQueryable<TElement>(result);
				}

				// Handle Queryable.Cast - just pass through since we Cast in Enumerate anyway
				if (mc.Method.DeclaringType == typeof(Queryable) && mc.Method.Name == nameof(Queryable.Cast))
				{
					return new TestAsyncQueryable<TElement>(Enumerate(mc.Arguments[0]).Cast<TElement>());
				}

				// Fallback for other method calls - try to extract source from first argument
				if (mc.Arguments.Count > 0)
				{
					return new TestAsyncQueryable<TElement>(Enumerate(mc.Arguments[0]).Cast<TElement>());
				}
			}

			// Fallback for constant expressions (the original source)
			if (expression is ConstantExpression ce && ce.Value is ITestAsyncQueryable q)
			{
				return new TestAsyncQueryable<TElement>(q.Enumerate().Cast<TElement>());
			}

			// Last resort - compile and execute
			return new TestAsyncQueryable<TElement>(((IEnumerable)Expression.Lambda(expression).Compile().DynamicInvoke()!).Cast<TElement>());
		}

		public object Execute(Expression expression)
		{
			var method = typeof(TestQueryProvider).GetMethod(nameof(ExecuteGeneric), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
				.MakeGenericMethod(expression.Type);
			return method.Invoke(this, [expression])!;
		}

		public TResult Execute<TResult>(Expression expression)
			=> ExecuteGeneric<TResult>(expression);

		private static object CreateValueTaskObject(Type resultType, object result)
		{
			var vtType = typeof(ValueTask<>).MakeGenericType(resultType);
			// Prefer the constructor ValueTask<T>(T)
			var ctor = vtType.GetConstructor([resultType]) ?? throw new InvalidOperationException("Expected ValueTask<T>(T) constructor not found.");
			return ctor.Invoke([result]);
		}

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

					var vtObj = CreateValueTaskObject(srcType, firstOrDefault);
					return (TRes)vtObj;
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
			// Handle MethodCallExpression directly to avoid infinite recursion
			// when expression contains QueryableExtensions methods
			if (expr is MethodCallExpression mc)
			{
				// Handle SkipLong
				if (mc.Method.DeclaringType == typeof(QueryableExtensions) && mc.Method.Name == nameof(QueryableExtensions.SkipLong))
				{
					var srcEnum = Enumerate(mc.Arguments[0]).Cast<object>().ToList();
					var count = (long)Evaluate(mc.Arguments[1]);
					return count >= srcEnum.Count ? Array.Empty<object>() : srcEnum.Skip((int)count);
				}

				// Handle Queryable.Skip
				if (mc.Method.DeclaringType == typeof(Queryable) && mc.Method.Name == nameof(Queryable.Skip))
				{
					var srcEnum = Enumerate(mc.Arguments[0]).Cast<object>().ToList();
					var count = (int)Evaluate(mc.Arguments[1]);
					return count >= srcEnum.Count ? Array.Empty<object>() : srcEnum.Skip(count);
				}

				// Handle Queryable.Cast
				if (mc.Method.DeclaringType == typeof(Queryable) && mc.Method.Name == nameof(Queryable.Cast))
				{
					return Enumerate(mc.Arguments[0]);
				}
			}

			var value = Evaluate(expr);
			if (value is ITestAsyncQueryable q)
				return q.Enumerate();
			if (value is IEnumerable enumerable)
				return enumerable;
			throw new InvalidOperationException("Unsupported source expression.");
		}

		private static object Evaluate(Expression expr)
		{
			// For ConstantExpression, return value directly
			if (expr is ConstantExpression ce)
				return ce.Value!;
			return Expression.Lambda(expr).Compile().DynamicInvoke()!;
		}
	}

	private static IQueryable<T> AsTestQueryable<T>(IEnumerable<T> source) => new TestAsyncQueryable<T>(source);

	[TestMethod]
	public async Task FirstOrDefaultAsync_Empty_ReturnsDefault()
	{
		var query = AsTestQueryable(Array.Empty<int>());
		var val = await query.FirstOrDefaultAsync(CancellationToken);
		val.AssertEqual(default);
	}

	[TestMethod]
	public async Task CountAsync_ReturnsCorrectValue()
	{
		var query = AsTestQueryable([1, 2, 3]);
		var count = await query.CountAsync(CancellationToken);
		count.AssertEqual(3L);
	}

	[TestMethod]
	public async Task ToArrayAsync_ReturnsArray()
	{
		var query = AsTestQueryable([1, 2, 3]);
		var arr = await query.ToArrayAsync(CancellationToken);
		arr.AssertEqual([1, 2, 3]);
	}

	[TestMethod]
	public void SkipLong_SkipsElements()
	{
		var query = AsTestQueryable([1, 2, 3, 4]);
		var skipped = query.SkipLong(2).Cast<int>().ToArray();
		skipped.AssertEqual([3, 4]);
	}

	[TestMethod]
	public void SkipLong_ZeroCount_ReturnsAll()
	{
		var query = AsTestQueryable([1, 2, 3]);
		var result = query.SkipLong(0).Cast<int>().ToArray();
		result.AssertEqual([1, 2, 3]);
	}

	[TestMethod]
	public void SkipLong_CountExceedsLength_ReturnsEmpty()
	{
		var query = AsTestQueryable([1, 2, 3]);
		var result = query.SkipLong(100).Cast<int>().ToArray();
		result.Length.AssertEqual(0);
	}

	[TestMethod]
	public void SkipLong_CountExceedsIntMaxValue_ReturnsEmpty()
	{
		var query = AsTestQueryable([1, 2, 3, 4, 5]);
		// Value greater than int.MaxValue - tests the chained Skip logic
		var result = query.SkipLong((long)int.MaxValue + 100).Cast<int>().ToArray();
		result.Length.AssertEqual(0, "Skipping more than int.MaxValue elements should return empty");
	}

	[TestMethod]
	public void SkipLong_ExactlyIntMaxValue_ReturnsEmpty()
	{
		var query = AsTestQueryable([1, 2, 3]);
		var result = query.SkipLong(int.MaxValue).Cast<int>().ToArray();
		result.Length.AssertEqual(0, "Skipping int.MaxValue elements from small collection should return empty");
	}

	/// <summary>
	/// Verifies that SkipLong creates a proper SkipLong Expression (not chained Skip calls).
	/// This ensures query providers receive a single SkipLong node with long parameter
	/// that can be translated to SQL OFFSET with bigint support.
	/// </summary>
	[TestMethod]
	public void SkipLong_CreatesSkipLongExpression_NotChainedSkip()
	{
		var query = AsTestQueryable([1, 2, 3, 4, 5]);
		var skipped = query.SkipLong(100);

		// The expression should be a MethodCallExpression for SkipLong
		var expression = skipped.Expression;
		(expression is MethodCallExpression).AssertTrue("Expression should be MethodCallExpression");

		var mc = (MethodCallExpression)expression;
		mc.Method.Name.AssertEqual("SkipLong", "Should create SkipLong expression, not Skip");
		mc.Method.DeclaringType.AssertEqual(typeof(QueryableExtensions));

		// The count parameter should be long
		var countArg = mc.Arguments[1];
		(countArg is ConstantExpression).AssertTrue("Count argument should be ConstantExpression");
		var countValue = ((ConstantExpression)countArg).Value;
		(countValue is long).AssertTrue("Count should be long type");
		((long)countValue).AssertEqual(100L);
	}

	/// <summary>
	/// Verifies that SkipLong with value > int.MaxValue still creates a single SkipLong Expression.
	/// Old workaround would chain multiple Skip(int.MaxValue) calls.
	/// </summary>
	[TestMethod]
	public void SkipLong_LargeValue_CreatesSingleExpression()
	{
		var query = AsTestQueryable([1, 2, 3]);
		long largeCount = (long)int.MaxValue + 1000;
		var skipped = query.SkipLong(largeCount);

		var mc = (MethodCallExpression)skipped.Expression;

		// Should be single SkipLong, not chained Skip calls
		mc.Method.Name.AssertEqual("SkipLong", "Large values should still use single SkipLong expression");

		// Verify the count is preserved as-is
		var countValue = (long)((ConstantExpression)mc.Arguments[1]).Value;
		countValue.AssertEqual(largeCount, "Count value should be preserved exactly");
	}

	[TestMethod]
	public async Task AnyAsync_NonEmptyWithDefaultValue()
	{
		var query = AsTestQueryable([0]);
		var hasAny = await query.AnyAsync(CancellationToken);
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
		var first = await query.FirstOrDefaultAsync(CancellationToken);
		(first?.Id).AssertEqual(1);
		(first?.Name).AssertEqual("a");
	}

	[TestMethod]
	public async Task FirstOrDefaultAsync_RefType_Empty_ReturnsNull()
	{
		var query = AsTestQueryable(Array.Empty<RefItem>());
		var first = await query.FirstOrDefaultAsync(CancellationToken);
		(first is null).AssertTrue();
	}

	[TestMethod]
	public async Task CountAsync_RefType()
	{
		var items = new[] { new RefItem(), new RefItem(), new RefItem() };
		var query = AsTestQueryable(items);
		var count = await query.CountAsync(CancellationToken);
		count.AssertEqual(3L);
	}

	[TestMethod]
	public async Task ToArrayAsync_RefType()
	{
		var items = new[] { new RefItem { Id = 10 }, new RefItem { Id = 11 } };
		var query = AsTestQueryable(items);
		var arr = await query.ToArrayAsync(CancellationToken);
		arr.Length.AssertEqual(2);
		arr[0].Id.AssertEqual(10);
	}

	[TestMethod]
	public async Task AnyAsync_RefType_WithNullElement()
	{
		var items = new RefItem[] { null };
		var query = AsTestQueryable(items);
		var hasAny = await query.AnyAsync(CancellationToken);
		hasAny.AssertTrue();
	}
}