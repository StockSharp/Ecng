namespace Ecng.Linq;

using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

/// <summary>
/// The extensions for <see cref="Expression"/>.
/// </summary>
public static class ExpressionExtensions
{
	/// <summary>
	/// Gets the constant value from the expression.
	/// </summary>
	/// <typeparam name="T">The type of the constant value.</typeparam>
	/// <param name="e">The expression.</param>
	/// <returns>The constant value.</returns>
	public static T GetConstant<T>(this Expression e)
		=> ((ConstantExpression)e).Value.To<T>();

	/// <summary>
	/// Evaluates the expression and returns its value.
	/// </summary>
	/// <param name="e">The expression to evaluate.</param>
	/// <returns>The evaluated value.</returns>
	public static object Evaluate(this Expression e)
	{
		//A little optimization for constant expressions
		if (e.NodeType == ExpressionType.Constant)
			return e.GetConstant<object>();

		return Expression.Lambda(e).Compile().DynamicInvoke();
	}

	/// <summary>
	/// Replaces the source provider in the expression with the specified query provider.
	/// </summary>
	/// <param name="expression">The expression to modify.</param>
	/// <param name="provider">The query provider to set.</param>
	/// <exception cref="ArgumentNullException">Thrown when expression or provider is null.</exception>
	public static void ReplaceSource(this Expression expression, IQueryProvider provider)
	{
		if (expression is null)
			throw new ArgumentNullException(nameof(expression));

		if (provider is null)
			throw new ArgumentNullException(nameof(provider));

		while (expression is MethodCallExpression mce)
			expression = mce.Arguments.Count > 0 ? mce.Arguments[0] : mce.Object;

		if (expression is not ConstantExpression constant)
			throw new ArgumentOutOfRangeException(nameof(expression), expression.NodeType.ToString());

		var field = expression.GetType().GetInstanceFields()
			.FirstOrDefault(f => ReferenceEquals(f.GetValue(expression), constant.Value))
			?? throw new InvalidOperationException("Cannot find constant value field.");

		field.SetValue(expression, provider);
	}

	/// <summary>
	/// Rebuilds <paramref name="expression"/> over another source, so a query composed against one
	/// queryable runs against another.
	/// </summary>
	/// <param name="expression">The composed query, e.g. a Where over a table.</param>
	/// <param name="source">The source to run it over instead, e.g. the same table held in memory.</param>
	/// <returns>The rebuilt expression, or <paramref name="expression"/> when it has no queryable root.</returns>
	/// <remarks>
	/// Only the root is swapped: a join or a sub-query naming another source keeps naming it.
	/// </remarks>
	public static Expression ReplaceRootSource(this Expression expression, IQueryable source)
	{
		if (expression is null)
			throw new ArgumentNullException(nameof(expression));

		if (source is null)
			throw new ArgumentNullException(nameof(source));

		var root = expression;

		while (root is MethodCallExpression mce)
			root = mce.Arguments.Count > 0 ? mce.Arguments[0] : mce.Object;

		if (root is not ConstantExpression constant || constant.Value is not IQueryable)
			return expression;

		return new RootSourceSwapper(constant, Expression.Constant(source)).Visit(expression);
	}

	private sealed class RootSourceSwapper(ConstantExpression from, Expression to) : ExpressionVisitor
	{
		protected override Expression VisitConstant(ConstantExpression node)
			=> ReferenceEquals(node, from) ? to : node;
	}

	/// <summary>
	/// Lists every queryable source <paramref name="expression"/> names: the one it is composed over plus any
	/// a join or a sub-query brings in.
	/// </summary>
	/// <param name="expression">The composed query.</param>
	/// <returns>The distinct sources, in the order they appear.</returns>
	public static IQueryable[] EnumerateSources(this Expression expression)
	{
		if (expression is null)
			throw new ArgumentNullException(nameof(expression));

		var collector = new SourceCollector();
		collector.Visit(expression);
		return [.. collector.Sources];
	}

	/// <summary>
	/// Rebuilds <paramref name="expression"/> so that every source it names is held in place, and reports the
	/// sources found.
	/// </summary>
	/// <param name="expression">The composed query.</param>
	/// <param name="sources">The distinct sources, in the order they appear.</param>
	/// <returns>The rebuilt expression, naming each source directly.</returns>
	/// <remarks>
	/// A source read through a property is built anew on each read, so finding it and replacing it would meet
	/// two different objects. Each is read once and put into the expression, so both steps mean the same one.
	/// </remarks>
	public static Expression PinSources(this Expression expression, out IQueryable[] sources)
	{
		if (expression is null)
			throw new ArgumentNullException(nameof(expression));

		var pinner = new SourcePinner();
		var pinned = pinner.Visit(expression);

		sources = [.. pinner.Sources];
		return pinned;
	}

	/// <summary>
	/// Rebuilds <paramref name="expression"/> with each source it names replaced by the one mapped to it, so a
	/// query composed over database tables runs over held copies of them instead.
	/// </summary>
	/// <param name="expression">The composed query.</param>
	/// <param name="sources">What to put in place of each source.</param>
	/// <returns>The rebuilt expression.</returns>
	/// <remarks>
	/// Every source, not just the one the query is composed over: one left naming a table is read
	/// synchronously, blocking the caller for the round-trip.
	/// </remarks>
	public static Expression ReplaceSources(this Expression expression, IReadOnlyDictionary<object, IQueryable> sources)
	{
		if (expression is null)
			throw new ArgumentNullException(nameof(expression));

		if (sources is null)
			throw new ArgumentNullException(nameof(sources));

		return new SourceSwapper(sources).Visit(expression);
	}

	// A sub-query names its source through the compiler's closure, which arrives as a field read - so looking
	// for constants alone would miss exactly the source a join or a sub-query brings in.
	private static object ReadValue(Expression node)
	{
		switch (node)
		{
			case ConstantExpression constant:
				return constant.Value;

			case MemberExpression member:
			{
				var owner = member.Expression is null ? null : ReadValue(member.Expression);

				if (owner is null && member.Expression is not null)
					return null;

				return member.Member switch
				{
					FieldInfo field => field.GetValue(owner),
					PropertyInfo property => property.GetValue(owner),
					_ => null,
				};
			}

			default:
				return null;
		}
	}

	private sealed class SourceCollector : ExpressionVisitor
	{
		private readonly HashSet<object> _seen = new(ReferenceEqualityComparer.Instance);

		public List<IQueryable> Sources { get; } = [];

		protected override Expression VisitConstant(ConstantExpression node)
		{
			Add(node.Value);
			return node;
		}

		protected override Expression VisitMember(MemberExpression node)
		{
			if (ReadValue(node) is IQueryable source)
			{
				Add(source);
				return node;
			}

			return base.VisitMember(node);
		}

		private void Add(object value)
		{
			if (value is IQueryable source && _seen.Add(source))
				Sources.Add(source);
		}
	}

	private sealed class SourcePinner : ExpressionVisitor
	{
		private readonly HashSet<object> _seen = new(ReferenceEqualityComparer.Instance);

		public List<IQueryable> Sources { get; } = [];

		protected override Expression VisitConstant(ConstantExpression node)
		{
			Add(node.Value);
			return node;
		}

		public override Expression Visit(Expression node)
		{
			if (!IsSource(node))
				return base.Visit(node);

			if (Evaluate(node) is not IQueryable source)
				return base.Visit(node);

			Add(source);

			// What was read replaces the read itself, so nobody reads it a second time and gets something else.
			return Expression.Constant(source);
		}

		// A step of the query is named by the same interface as a source, and evaluating one would run the very
		// query being looked at.
		private static bool IsSource(Expression node)
		{
			if (node is null || node is ConstantExpression || !typeof(IQueryable).IsAssignableFrom(node.Type))
				return false;

			if (node is MethodCallExpression call &&
				(call.Method.DeclaringType == typeof(Queryable) || call.Method.DeclaringType == typeof(Enumerable)))
				return false;

			return node is MemberExpression or MethodCallExpression && IsClosed(node);
		}

		// Anything reaching for the value a lambda is called with has no value yet.
		private static bool IsClosed(Expression node)
		{
			var finder = new ParameterFinder();
			finder.Visit(node);
			return !finder.Found;
		}

		private static object Evaluate(Expression node)
		{
			try
			{
				return Expression.Lambda(node).Compile().DynamicInvoke();
			}
			catch (Exception)
			{
				return null;
			}
		}

		private void Add(object value)
		{
			if (value is IQueryable source && _seen.Add(source))
				Sources.Add(source);
		}
	}

	private sealed class ParameterFinder : ExpressionVisitor
	{
		public bool Found { get; private set; }

		protected override Expression VisitParameter(ParameterExpression node)
		{
			Found = true;
			return node;
		}
	}

	private sealed class SourceSwapper(IReadOnlyDictionary<object, IQueryable> sources) : ExpressionVisitor
	{
		protected override Expression VisitConstant(ConstantExpression node)
			=> TryReplace(node.Value) ?? node;

		protected override Expression VisitMember(MemberExpression node)
			=> TryReplace(ReadValue(node)) ?? base.VisitMember(node);

		private Expression TryReplace(object value)
			=> value is not null && sources.TryGetValue(value, out var replacement)
				? Expression.Constant(replacement)
				: null;
	}

	private static IEnumerable<FieldInfo> GetInstanceFields(this Type type)
	{
		for (var t = type; t != null; t = t.BaseType)
		{
			foreach (var field in t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly))
				yield return field;
		}
	}

	/// <summary>
	/// Removes quote expressions from the given expression.
	/// </summary>
	/// <param name="e">The expression to process.</param>
	/// <returns>The unquoted expression.</returns>
	public static Expression StripQuotes(this Expression e)
	{
		while (e.NodeType == ExpressionType.Quote)
		{
			e = ((UnaryExpression)e).Operand;
		}

		return e;
	}

	/// <summary>
	/// Gets the value of the specified member from the given instance.
	/// </summary>
	/// <param name="member">The member whose value to retrieve.</param>
	/// <param name="instance">The object instance from which to retrieve the value.</param>
	/// <returns>The value of the member.</returns>
	/// <exception cref="NotSupportedException">Thrown when the member type is not supported.</exception>
	public static object GetMemberValue(this MemberInfo member, object instance)
	{
		if (member is PropertyInfo pi)
			return pi.GetValue(instance);
		else if (member is FieldInfo fi)
			return fi.GetValue(instance);
		else
			throw new NotSupportedException();
	}

	/// <summary>
	/// Retrieves the innermost member in a nested member expression.
	/// </summary>
	/// <param name="exp">The member expression to process.</param>
	/// <returns>The innermost member expression.</returns>
	public static MemberExpression GetInnerMember(this MemberExpression exp)
	{
		if (exp.Expression is MemberExpression d)
			return GetInnerMember(d);

		return exp;
	}

	/// <summary>
	/// Evaluates the expression and returns its value as the specified type.
	/// </summary>
	/// <typeparam name="TValue">The type to convert the evaluated value to.</typeparam>
	/// <param name="exp">The expression to evaluate.</param>
	/// <returns>The evaluated value converted to the specified type.</returns>
	/// <exception cref="ArgumentNullException">Thrown when the expression is null.</exception>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when the expression type is not supported.</exception>
	public static TValue GetValue<TValue>(this Expression exp)
	{
		if (exp is null)
			throw new ArgumentNullException(nameof(exp));

		if (exp is ConstantExpression c)
			return c.Value.To<TValue>();
		else if (exp is MemberExpression me)
			return me.Member.GetMemberValue(me.Expression is null ? null : GetValue<object>(me.Expression)).To<TValue>();

		throw new ArgumentOutOfRangeException(exp.NodeType.ToString());
	}

	/// <summary>
	/// Converts the given expression type to its equivalent <see cref="ComparisonOperator"/>.
	/// </summary>
	/// <param name="type">The expression type.</param>
	/// <returns>The corresponding <see cref="ComparisonOperator"/>.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when the expression type has no corresponding operator.</exception>
	public static ComparisonOperator ToOperator(this ExpressionType type)
	{
		return type switch
		{
			ExpressionType.GreaterThan => ComparisonOperator.Greater,
			ExpressionType.GreaterThanOrEqual => ComparisonOperator.GreaterOrEqual,
			ExpressionType.LessThan => ComparisonOperator.Less,
			ExpressionType.LessThanOrEqual => ComparisonOperator.LessOrEqual,
			ExpressionType.Equal => ComparisonOperator.Equal,
			ExpressionType.NotEqual => ComparisonOperator.NotEqual,
			_ => throw new ArgumentOutOfRangeException(type.To<string>()),
		};
	}
}
