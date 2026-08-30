namespace Ecng.Data.Sql.Model;

/// <summary>
/// Walks a <see cref="MemberExpression"/> chain that ultimately roots at a
/// <see cref="ConstantExpression"/> and dereferences it via reflection,
/// turning closure captures (compiler-generated <c>DisplayClass</c> fields)
/// of any depth into a single materialised value.
///
/// Replaces three independent inline branches that previously lived in the
/// translator's <c>VisitMember</c> — one for direct DisplayClass fields,
/// one for two-level captures, and one for nested DisplayClass chains —
/// with a single unified path. Callers feed the result into the
/// translator's emission helper so the captured value becomes a SQL
/// parameter (or, for <see cref="IQueryable"/> captures, a re-visited
/// subquery) instead of leaking the local-variable name as a raw column
/// reference.
/// </summary>
public static class ClosureMaterializer
{
	/// <summary>
	/// Tries to materialise <paramref name="member"/> as a constant value.
	/// </summary>
	/// <param name="member">Leaf <see cref="MemberExpression"/>.</param>
	/// <param name="value">
	/// Materialised value when the chain is rooted at a constant. May be
	/// <see langword="null"/> when the captured value itself is null —
	/// inspect the return value, not the out parameter, to distinguish
	/// "couldn't materialise" from "materialised to null".
	/// </param>
	/// <returns>
	/// <see langword="true"/> when the entire chain dereferenced cleanly.
	/// <see langword="false"/> for any unsupported root (parameter, method
	/// call, etc.) — callers must fall through to the column-emission
	/// branches in that case.
	/// </returns>
	public static bool TryEvaluate(MemberExpression member, out object value)
	{
		value = null;

		if (member is null)
			return false;

		var stack = new Stack<MemberInfo>();
		Expression cur = member;

		while (cur is MemberExpression me)
		{
			stack.Push(me.Member);
			cur = me.Expression;
		}

		// A chain either starts from a captured value or from a static member, which opens it without an
		// instance -- a registry of tables offered as static properties reaches here that way.
		object instance;

		if (cur is ConstantExpression ce)
			instance = ce.Value;
		else if (cur is null)
			instance = null;
		else
			return false;

		while (stack.Count > 0)
		{
			var hop = stack.Pop();

			if (instance is null && !hop.IsStatic())
				return false;

			instance = hop.GetMemberValue(instance);
		}

		value = instance;
		return true;
	}
}
