namespace Ecng.Data.Sql.Model;

/// <summary>
/// Splits a filter over a grouped projection into the part SQL applies to the rows before
/// grouping (WHERE) and the part that reads aggregates and so applies to the groups (HAVING).
/// </summary>
/// <remarks>
/// A grouped projection is <c>GroupBy(...).Select(g =&gt; new T { ... })</c>, optionally followed by
/// other filters, ordering and projections of it. A member of <c>T</c> is an aggregate when its value
/// reads the rows of the group, that is uses <c>g</c> other than through <c>g.Key</c>; a member of a
/// further projection is an aggregate when its value reads an aggregate member of its source. The
/// condition is split at its top-level <c>&amp;&amp;</c> only: a conjunct that reads any aggregate
/// goes to HAVING as a whole.
/// </remarks>
static class GroupedFilter
{
	private abstract class Finder : ExpressionVisitor
	{
		protected bool IsFound { get; set; }

		public bool Find(Expression expression)
		{
			Visit(expression);
			return IsFound;
		}
	}

	private sealed class GroupRowsFinder(ParameterExpression grouping) : Finder
	{
		protected override Expression VisitMember(MemberExpression node)
		{
			// The key has one value per group, so reading it is not an aggregate.
			if (node.Expression == grouping && node.Member.Name == nameof(IGrouping<int, int>.Key))
				return node;

			return base.VisitMember(node);
		}

		protected override Expression VisitParameter(ParameterExpression node)
		{
			if (node == grouping)
				IsFound = true;

			return node;
		}
	}

	private sealed class MemberReadFinder(ParameterExpression parameter, ISet<string> members) : Finder
	{
		protected override Expression VisitMember(MemberExpression node)
		{
			if (node.Expression == parameter && members.Contains(node.Member.Name))
				IsFound = true;

			return base.VisitMember(node);
		}
	}

	/// <summary>
	/// Splits the condition of a filter applied to <paramref name="source"/>.
	/// </summary>
	/// <param name="source">The sequence the filter is applied to.</param>
	/// <param name="predicate">The filter condition.</param>
	/// <returns>
	/// The conjuncts that filter rows and those that filter groups, each <see langword="null"/> when
	/// there is none. When <paramref name="source"/> is not a grouped projection, the whole condition
	/// filters rows.
	/// </returns>
	public static (Expression rows, Expression groups) Split(Expression source, LambdaExpression predicate)
	{
		ArgumentNullException.ThrowIfNull(source);
		ArgumentNullException.ThrowIfNull(predicate);

		var aggregates = GetAggregateMembers(source);

		if (aggregates.Count == 0 || predicate.Parameters.Count != 1)
			return (predicate.Body, null);

		var parameter = predicate.Parameters[0];

		Expression rows = null;
		Expression groups = null;

		foreach (var conjunct in GetConjuncts(predicate.Body))
		{
			if (new MemberReadFinder(parameter, aggregates).Find(conjunct))
				groups = groups is null ? conjunct : Expression.AndAlso(groups, conjunct);
			else
				rows = rows is null ? conjunct : Expression.AndAlso(rows, conjunct);
		}

		return (rows, groups);
	}

	private static ISet<string> GetAggregateMembers(Expression source)
	{
		while (source is MethodCallExpression call && (call.Method.DeclaringType == typeof(Queryable) || call.Method.DeclaringType == typeof(Enumerable)))
		{
			switch (call.Method.Name)
			{
				case nameof(Queryable.Where):
				case nameof(Queryable.OrderBy):
				case nameof(Queryable.OrderByDescending):
				case nameof(Queryable.ThenBy):
				case nameof(Queryable.ThenByDescending):
					source = call.Arguments[0];
					break;

				case nameof(Queryable.Select):
					return GetAggregateMembers(call.Arguments[0], (LambdaExpression)call.Arguments[1].StripQuotes());

				default:
					return new HashSet<string>();
			}
		}

		return new HashSet<string>();
	}

	private static ISet<string> GetAggregateMembers(Expression source, LambdaExpression selector)
	{
		var members = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		if (selector.Parameters.Count != 1)
			return members;

		var parameter = selector.Parameters[0];
		Func<Finder> createFinder;

		if (parameter.Type.GetGenericType(typeof(IGrouping<,>)) is not null)
			createFinder = () => new GroupRowsFinder(parameter);
		else
		{
			// A projection of a grouped projection carries the aggregates of its source along.
			var sourceAggregates = GetAggregateMembers(source);

			if (sourceAggregates.Count == 0)
				return members;

			createFinder = () => new MemberReadFinder(parameter, sourceAggregates);
		}

		foreach (var (name, value) in GetBindings(selector.Body))
		{
			if (createFinder().Find(value))
				members.Add(name);
		}

		return members;
	}

	private static IEnumerable<(string name, Expression value)> GetBindings(Expression body)
	{
		switch (body)
		{
			case MemberInitExpression init:
				foreach (var binding in init.Bindings.OfType<MemberAssignment>())
					yield return (binding.Member.Name, binding.Expression);

				break;

			case NewExpression n:
				// A positional constructor has no members; its parameters are named after them.
				var parameters = n.Constructor?.GetParameters();

				for (var i = 0; i < n.Arguments.Count; i++)
				{
					var name = n.Members?[i].Name ?? parameters?[i].Name;

					if (name is not null)
						yield return (name, n.Arguments[i]);
				}

				break;
		}
	}

	private static IEnumerable<Expression> GetConjuncts(Expression body)
	{
		if (body is BinaryExpression { NodeType: ExpressionType.AndAlso, Method: null } and)
		{
			foreach (var left in GetConjuncts(and.Left))
				yield return left;

			foreach (var right in GetConjuncts(and.Right))
				yield return right;
		}
		else
			yield return body;
	}
}
