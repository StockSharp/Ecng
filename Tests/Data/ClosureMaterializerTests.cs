#if NET10_0_OR_GREATER

namespace Ecng.Tests.Data;

using System.Linq.Expressions;

using Ecng.Data.Sql.Model;

/// <summary>
/// Tests the pure closure materialiser. Replaces three independent inline
/// branches in VisitMember (m.Expression is ConstantExpression /
/// me.Expression is ConstantExpression / TryEvaluateClosureCapture) with a
/// single function that walks any MemberExpression chain rooted at a
/// ConstantExpression and dereferences it via reflection.
/// </summary>
[TestClass]
public class ClosureMaterializerTests : BaseTestClass
{
	private static MemberExpression CapturedRightOperand<T>(Expression<Func<T, bool>> filter)
	{
		// Right side of a binary "==" or "!=" is the captured value chain.
		var body = (BinaryExpression)filter.Body;

		var right = body.Right;
		if (right is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } u)
			right = u.Operand;

		return (MemberExpression)right;
	}

	[TestMethod]
	public void TryEvaluate_OneHopLocalCapture_ReifiesValue()
	{
		// Compiler wraps `value` into a DisplayClass field; the chain shape is
		// MemberExpression(<DC>.value) → ConstantExpression(<dc-instance>).
		var value = 42L;

		var member = CapturedRightOperand<long>(id => id == value);

		ClosureMaterializer.TryEvaluate(member, out var result).AssertTrue();
		result.AssertEqual<object>(42L);
	}

	[TestMethod]
	public void TryEvaluate_TwoHopOuterFieldThenInnerField_ReifiesValue()
	{
		// Two-hop closure: DC -> container -> Field.
		var container = new Container { Field = "abc" };

		var member = CapturedRightOperand<string>(s => s == container.Field);

		ClosureMaterializer.TryEvaluate(member, out var result).AssertTrue();
		result.AssertEqual<object>("abc");
	}

	[TestMethod]
	public void TryEvaluate_NestedDisplayClass_ReifiesAcrossMultipleHops()
	{
		// Nested if-block locals produce multiple chained DisplayClass
		// captures — exactly the form that previously slipped through into
		// raw column emission and broke a real-world query.
		var outer = "outer-value";
		// ReSharper disable once ConvertToConstant.Local
		var depth = 1;

		MemberExpression member;

		if (depth == 1)
		{
			var inner = outer;

			if (inner.Length > 0)
			{
				var deepest = inner;
				member = CapturedRightOperand<string>(s => s == deepest);
			}
			else
			{
				member = CapturedRightOperand<string>(s => s == inner);
			}
		}
		else
		{
			member = CapturedRightOperand<string>(s => s == outer);
		}

		ClosureMaterializer.TryEvaluate(member, out var result).AssertTrue();
		result.AssertEqual<object>("outer-value");
	}

	[TestMethod]
	public void TryEvaluate_NullCapturedValue_ReturnsTrueWithNull()
	{
		// Null is a legal captured value and must not collapse to "couldn't
		// resolve" — callers distinguish null from absence themselves.
		string captured = null;

		var member = CapturedRightOperand<string>(s => s == captured);

		ClosureMaterializer.TryEvaluate(member, out var result).AssertTrue();
		result.AssertNull();
	}

	[TestMethod]
	public void TryEvaluate_ArrayCapture_ReifiesArrayInstance()
	{
		// Arrays must come back as Array instances; the caller is responsible
		// for expanding them into a comma-separated parameter list.
		var ids = new long[] { 1, 2, 3 };

		// `arr == ids` captures `ids` on a DisplayClass and compares the
		// parameter to the captured reference — the right-hand side of the
		// binary is the bare MemberExpression for `<DC>.ids`.
		Expression<Func<long[], bool>> e = arr => arr == ids;
		var arrayMember = (MemberExpression)((BinaryExpression)e.Body).Right;

		ClosureMaterializer.TryEvaluate(arrayMember, out var result).AssertTrue();
		(result is long[]).AssertTrue();
		((long[])result).Length.AssertEqual(3);
	}

	[TestMethod]
	public void TryEvaluate_ChainNotRootedAtConstant_ReturnsFalse()
	{
		// A chain rooted at a ParameterExpression (e.g. an entity reference)
		// must be rejected — it represents a column path, not a captured value.
		Expression<Func<TestPerson, string>> e = p => p.Name;
		var member = (MemberExpression)e.Body;

		ClosureMaterializer.TryEvaluate(member, out var result).AssertFalse();
		result.AssertNull();
	}

	private sealed class Container
	{
		public string Field { get; set; }
	}
}

#endif
