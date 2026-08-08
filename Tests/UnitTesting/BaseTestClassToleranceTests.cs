namespace Ecng.Tests.UnitTesting;

/// <summary>
/// The tolerance and case-insensitive assertion overloads of <see cref="BaseTestClass"/>.
/// </summary>
/// <remarks>
/// These helpers forward to MSTest, so what is pinned here is the contract every test in the
/// workspace already relies on, including the parts that are surprising: the delta boundary is
/// inclusive, NaN equals NaN, an invalid delta is an <see cref="ArgumentOutOfRangeException"/>
/// rather than an assertion failure, an infinite delta merges any finite distance but still not
/// NaN, and a pair of like-signed infinities satisfies the equal and the not-equal helper alike.
/// Every claim below is bracketed: each method shows both the input the helper must accept and
/// the input it must reject, so none of them would survive an empty helper body.
/// </remarks>
[TestClass]
public class BaseTestClassToleranceTests : BaseTestClass
{
	// AreEqual(string, string, bool ignoreCase)

	[TestMethod]
	public void AreEqualIgnoreCase_True_RelaxesCaseAndNothingElse()
	{
		AreEqual("abc", "ABC", true);
		AreEqual("Mixed CASE 42", "mIXED case 42", true);

		// Case folding is the only thing relaxed: letters, length and whitespace still count.
		Throws<AssertFailedException>(() => AreEqual("abc", "abd", true));
		Throws<AssertFailedException>(() => AreEqual("a b", "ab", true));
		Throws<AssertFailedException>(() => AreEqual("abc", "abcd", true));
	}

	[TestMethod]
	public void AreEqualIgnoreCase_False_RejectsCaseDifference()
	{
		AreEqual("abc", "abc", false);

		Throws<AssertFailedException>(() => AreEqual("abc", "ABC", false));
		Throws<AssertFailedException>(() => AreEqual("a", "A", false));
		Throws<AssertFailedException>(() => AreEqual("abc", "abd", false));
	}

	[TestMethod]
	public void AreEqualIgnoreCase_NullAndEmptyAreDistinct()
	{
		// null and "" are distinct here under either flag, unlike Ecng's IsEmpty() convention.
		AreEqual((string)null, (string)null, true);
		AreEqual((string)null, (string)null, false);
		AreEqual("", "", true);
		AreEqual("", "", false);

		Throws<AssertFailedException>(() => AreEqual(null, "", true));
		Throws<AssertFailedException>(() => AreEqual("", null, true));
		Throws<AssertFailedException>(() => AreEqual(null, "", false));
		Throws<AssertFailedException>(() => AreEqual("", " ", true));
	}

	// AreEqual(double, double, double delta)

	[TestMethod]
	public void AreEqualDouble_DeltaBoundaryIsInclusive()
	{
		AreEqual(1.0, 1.0004, 0.001);
		AreEqual(1.0, 0.9996, 0.001);

		// A difference of exactly delta is accepted, and the comparison is symmetric in
		// expected/actual. 1.5000000000000002 is the next double above 1.5, so the pair below
		// straddles the boundary by one ulp and fixes which side it falls on.
		AreEqual(1.0, 1.5, 0.5);
		AreEqual(1.5, 1.0, 0.5);

		Throws<AssertFailedException>(() => AreEqual(1.0, 1.5000000000000002, 0.5));
		Throws<AssertFailedException>(() => AreEqual(1.5000000000000002, 1.0, 0.5));
		Throws<AssertFailedException>(() => AreEqual(1.0, 1.6, 0.5));
	}

	[TestMethod]
	public void AreEqualDouble_ZeroDelta_DemandsExactEquality()
	{
		AreEqual(1.0, 1.0, 0.0);

		// The same pair either way round the classic binary representation error: absorbed by a
		// delta that covers it, rejected by a zero delta.
		AreEqual(0.1 + 0.2, 0.3, 1e-15);
		Throws<AssertFailedException>(() => AreEqual(0.1 + 0.2, 0.3, 0.0));
	}

	[TestMethod]
	public void AreEqualDouble_InvalidDelta_ThrowsArgumentOutOfRange()
	{
		// A nonsensical delta is a caller bug, not a failed assertion - it does not surface as
		// AssertFailedException even when the values are equal and the assertion would hold.
		AreEqual(1.0, 1.0, 0.0);

		var negative = Throws<ArgumentOutOfRangeException>(() => AreEqual(1.0, 1.0, -1.0));
		AreEqual("delta", negative.ParamName);

		Throws<ArgumentOutOfRangeException>(() => AreEqual(1.0, 2.0, -1.0));

		var nan = Throws<ArgumentOutOfRangeException>(() => AreEqual(1.0, 1.0, double.NaN));
		AreEqual("delta", nan.ParamName);
	}

	[TestMethod]
	public void AreEqualDouble_NaNValues()
	{
		// Contrary to IEEE ==, two NaNs are accepted as equal by this helper, at any delta.
		AreEqual(double.NaN, double.NaN, 1.0);
		AreEqual(double.NaN, double.NaN, 0.0);

		Throws<AssertFailedException>(() => AreEqual(double.NaN, 1.0, 1.0));
		Throws<AssertFailedException>(() => AreEqual(1.0, double.NaN, 1.0));
	}

	[TestMethod]
	public void AreEqualDouble_Infinities()
	{
		AreEqual(double.PositiveInfinity, double.PositiveInfinity, 1.0);
		AreEqual(double.NegativeInfinity, double.NegativeInfinity, 1.0);

		Throws<AssertFailedException>(() => AreEqual(double.PositiveInfinity, double.NegativeInfinity, 1.0));
		Throws<AssertFailedException>(() => AreEqual(double.PositiveInfinity, 1.0, 1.0));
		Throws<AssertFailedException>(() => AreEqual(1.0, double.NegativeInfinity, 1.0));
	}

	[TestMethod]
	public void AreEqualDouble_InfiniteDelta_MergesFiniteDistancesButNotNaN()
	{
		AreEqual(0.0, 1e300, double.PositiveInfinity);
		AreEqual(double.MinValue, double.MaxValue, double.PositiveInfinity);
		AreEqual(double.PositiveInfinity, 0.0, double.PositiveInfinity);

		// The NaN check runs ahead of the delta comparison, so an infinite delta does not
		// swallow a NaN operand the way it swallows any distance between real values.
		Throws<AssertFailedException>(() => AreEqual(double.NaN, 1.0, double.PositiveInfinity));
	}

	// AreEqual(float, float, float delta)

	[TestMethod]
	public void AreEqualFloat_DeltaBoundaryIsInclusive()
	{
		AreEqual(1f, 1.4f, 0.5f);

		// 1.5000001f is the next float above 1.5f, so these two calls sit one ulp either side of
		// the boundary and pin it to the inclusive side.
		AreEqual(1f, 1.5f, 0.5f);
		AreEqual(1.5f, 1f, 0.5f);

		Throws<AssertFailedException>(() => AreEqual(1f, 1.5000001f, 0.5f));
		Throws<AssertFailedException>(() => AreEqual(1.5000001f, 1f, 0.5f));
		Throws<AssertFailedException>(() => AreEqual(1f, 1.6f, 0.5f));
	}

	[TestMethod]
	public void AreEqualFloat_ZeroDelta_DemandsExactEquality()
	{
		AreEqual(1f, 1f, 0f);

		// 1.0000001f is the next representable float above 1f - the smallest difference the
		// helper can be asked to notice.
		Throws<AssertFailedException>(() => AreEqual(1f, 1.0000001f, 0f));
	}

	[TestMethod]
	public void AreEqualFloat_InvalidDelta_ThrowsArgumentOutOfRange()
	{
		AreEqual(1f, 1f, 0f);

		var negative = Throws<ArgumentOutOfRangeException>(() => AreEqual(1f, 1f, -1f));
		AreEqual("delta", negative.ParamName);

		var nan = Throws<ArgumentOutOfRangeException>(() => AreEqual(1f, 1f, float.NaN));
		AreEqual("delta", nan.ParamName);
	}

	[TestMethod]
	public void AreEqualFloat_NaNValues()
	{
		AreEqual(float.NaN, float.NaN, 1f);

		Throws<AssertFailedException>(() => AreEqual(float.NaN, 1f, 1f));
		Throws<AssertFailedException>(() => AreEqual(1f, float.NaN, 1f));
	}

	[TestMethod]
	public void AreEqualFloat_Infinities()
	{
		AreEqual(float.PositiveInfinity, float.PositiveInfinity, 1f);
		AreEqual(float.NegativeInfinity, float.NegativeInfinity, 1f);

		Throws<AssertFailedException>(() => AreEqual(float.PositiveInfinity, float.NegativeInfinity, 1f));
		Throws<AssertFailedException>(() => AreEqual(float.PositiveInfinity, 1f, 1f));
	}

	// AreNotEqual(double, double, double delta)

	[TestMethod]
	public void AreNotEqualDouble_DeltaBoundaryBelongsToEqual()
	{
		AreNotEqual(1.0, 2.0, 0.5);
		AreNotEqual(2.0, 1.0, 0.5);

		// One ulp past delta already counts as differing, while exactly delta does not, so the
		// boundary belongs to AreEqual. Both boundary pairs are run through both helpers, which
		// is what shows they divide the pairs instead of both accepting one.
		AreNotEqual(1.0, 1.5000000000000002, 0.5);
		Throws<AssertFailedException>(() => AreEqual(1.0, 1.5000000000000002, 0.5));

		Throws<AssertFailedException>(() => AreNotEqual(1.0, 1.5, 0.5));
		Throws<AssertFailedException>(() => AreNotEqual(1.5, 1.0, 0.5));
		AreEqual(1.0, 1.5, 0.5);
		AreEqual(1.5, 1.0, 0.5);
	}

	[TestMethod]
	public void AreNotEqualDouble_ZeroDelta_SeparatesAnyDifference()
	{
		AreNotEqual(0.1 + 0.2, 0.3, 0.0);

		Throws<AssertFailedException>(() => AreNotEqual(1.0, 1.0, 0.0));
	}

	[TestMethod]
	public void AreNotEqualDouble_InvalidDelta_ThrowsArgumentOutOfRange()
	{
		AreNotEqual(1.0, 5.0, 0.5);

		var negative = Throws<ArgumentOutOfRangeException>(() => AreNotEqual(1.0, 5.0, -1.0));
		AreEqual("delta", negative.ParamName);

		var nan = Throws<ArgumentOutOfRangeException>(() => AreNotEqual(1.0, 5.0, double.NaN));
		AreEqual("delta", nan.ParamName);
	}

	[TestMethod]
	public void AreNotEqualDouble_NaNValues()
	{
		AreNotEqual(double.NaN, 1.0, 1.0);
		AreNotEqual(1.0, double.NaN, 1.0);

		// Mirrors AreEqual: a pair of NaNs counts as equal, so demanding a difference fails.
		Throws<AssertFailedException>(() => AreNotEqual(double.NaN, double.NaN, 1.0));
	}

	[TestMethod]
	public void AreNotEqualDouble_LikeSignedInfinitiesSatisfyBothHelpers()
	{
		// Controls for the anomaly below: on one and the same huge but finite pair the helpers
		// still divide the verdict, and AreEqual is live in its rejecting direction too.
		Throws<AssertFailedException>(() => AreNotEqual(1e300, 1e300, 1.0));
		AreEqual(1e300, 1e300, 1.0);
		Throws<AssertFailedException>(() => AreEqual(1e300, -1e300, 1.0));

		// Yet the same shape with like-signed infinities passes both: Inf - Inf is NaN, which
		// compares false against the delta, so AreNotEqual reports them as differing while
		// AreEqual reports them as equal. Only AreEqual can be trusted on infinite values.
		AreNotEqual(double.PositiveInfinity, double.PositiveInfinity, 1.0);
		AreNotEqual(double.NegativeInfinity, double.NegativeInfinity, 1.0);
		AreEqual(double.PositiveInfinity, double.PositiveInfinity, 1.0);
		AreEqual(double.NegativeInfinity, double.NegativeInfinity, 1.0);

		AreNotEqual(double.PositiveInfinity, double.NegativeInfinity, 1.0);
		AreNotEqual(double.PositiveInfinity, 1.0, 1.0);
	}

	[TestMethod]
	public void AreNotEqualDouble_InfiniteDelta_MergesFiniteDistances()
	{
		Throws<AssertFailedException>(() => AreNotEqual(0.0, 1e300, double.PositiveInfinity));
		Throws<AssertFailedException>(() => AreNotEqual(double.MinValue, double.MaxValue, double.PositiveInfinity));

		// A NaN operand is the one thing an infinite delta cannot merge.
		AreNotEqual(double.NaN, 1.0, double.PositiveInfinity);
	}

	// AreNotEqual(float, float, float delta)

	[TestMethod]
	public void AreNotEqualFloat_DeltaBoundaryBelongsToEqual()
	{
		AreNotEqual(1f, 2f, 0.5f);

		// One ulp past delta separates; exactly delta does not. As in the double overload, both
		// boundary pairs go through both helpers, so the side the boundary falls on is pinned by
		// a disagreement rather than by AreNotEqual alone.
		AreNotEqual(1f, 1.5000001f, 0.5f);
		Throws<AssertFailedException>(() => AreEqual(1f, 1.5000001f, 0.5f));

		Throws<AssertFailedException>(() => AreNotEqual(1f, 1.5f, 0.5f));
		Throws<AssertFailedException>(() => AreNotEqual(1f, 1.4f, 0.5f));
		AreEqual(1f, 1.5f, 0.5f);
	}

	[TestMethod]
	public void AreNotEqualFloat_ZeroDelta_SeparatesAdjacentFloats()
	{
		AreNotEqual(1f, 1.0000001f, 0f);

		Throws<AssertFailedException>(() => AreNotEqual(1f, 1f, 0f));
	}

	[TestMethod]
	public void AreNotEqualFloat_InvalidDelta_ThrowsArgumentOutOfRange()
	{
		AreNotEqual(1f, 5f, 0.5f);

		var negative = Throws<ArgumentOutOfRangeException>(() => AreNotEqual(1f, 5f, -0.5f));
		AreEqual("delta", negative.ParamName);

		var nan = Throws<ArgumentOutOfRangeException>(() => AreNotEqual(1f, 5f, float.NaN));
		AreEqual("delta", nan.ParamName);
	}

	[TestMethod]
	public void AreNotEqualFloat_NaNAndInfinities()
	{
		AreNotEqual(float.NaN, 1f, 1f);
		Throws<AssertFailedException>(() => AreNotEqual(float.NaN, float.NaN, 1f));

		// Same infinity behaviour as the double overload, with a finite control to show the
		// helper does reject identical operands.
		Throws<AssertFailedException>(() => AreNotEqual(1e30f, 1e30f, 1f));
		AreNotEqual(float.PositiveInfinity, float.PositiveInfinity, 1f);
		AreNotEqual(float.PositiveInfinity, float.NegativeInfinity, 1f);
	}
}
