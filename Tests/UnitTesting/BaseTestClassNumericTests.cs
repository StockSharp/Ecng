namespace Ecng.Tests;

/// <summary>
/// The ordering, range and sign assertions <see cref="BaseTestClass"/> exposes.
/// Every method here drives the helpers it names in both directions - it accepts what they must
/// accept and gets <see cref="AssertFailedException"/> on what they must reject - so that none of
/// them can stay green against a helper whose body has been emptied. Where the claim is a boundary,
/// the two calls that bracket it live side by side: one just inside, one just outside.
/// </summary>
[TestClass]
public class BaseTestClassNumericTests : BaseTestClass
{
	private static readonly DateTime _earlier = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
	private static readonly DateTime _later = new(2020, 1, 2, 0, 0, 0, DateTimeKind.Utc);

	// The smallest magnitude a decimal can hold apart from zero, used to bracket the zero and
	// range boundaries as tightly as the type allows.
	private const decimal _tinyDecimal = 0.0000000000000000000000000001m;

	/// <summary>
	/// Reports whether a helper accepted its input. Used only where the direction of the answer -
	/// not the answer itself - is what the test can legitimately claim, so that a culture-dependent
	/// collation order is never baked into an assertion.
	/// </summary>
	private static bool Accepts(Action call)
	{
		try
		{
			call();
			return true;
		}
		catch (AssertFailedException)
		{
			return false;
		}
	}

	[TestMethod]
	public void IsGreater_AcceptsGreater_RejectsEqualOrLess()
	{
		IsGreater(2, 1);
		IsGreater(0, -1);
		IsGreater(0.1, 0.0);
		IsGreater(0.1m, 0m);

		// Equality is the boundary that separates IsGreater from IsGreaterOrEqual, so it is
		// bracketed by the smallest step the type has.
		IsGreater(long.MaxValue, long.MaxValue - 1);
		Throws<AssertFailedException>(() => IsGreater(long.MaxValue, long.MaxValue));

		Throws<AssertFailedException>(() => IsGreater(5, 5));
		Throws<AssertFailedException>(() => IsGreater(4, 5));
		Throws<AssertFailedException>(() => IsGreater(0.0, 0.0));
		Throws<AssertFailedException>(() => IsGreater(0m, 0m));
	}

	[TestMethod]
	public void IsGreaterOrEqual_AcceptsGreaterOrEqual_RejectsLess()
	{
		IsGreaterOrEqual(2, 1);
		IsGreaterOrEqual(0m, 0m);

		// Equality is on the accepting side here - the opposite of IsGreater - so both sides of
		// the boundary are taken at the same point.
		IsGreaterOrEqual(int.MinValue, int.MinValue);
		Throws<AssertFailedException>(() => IsGreaterOrEqual(int.MinValue, int.MinValue + 1));

		Throws<AssertFailedException>(() => IsGreaterOrEqual(4, 5));
		Throws<AssertFailedException>(() => IsGreaterOrEqual(-1, 0));
	}

	[TestMethod]
	public void IsLess_AcceptsLess_RejectsEqualOrGreater()
	{
		IsLess(1, 2);
		IsLess(-1, 0);
		IsLess(0m, 0.1m);

		IsLess(long.MinValue, long.MinValue + 1);
		Throws<AssertFailedException>(() => IsLess(long.MinValue, long.MinValue));

		Throws<AssertFailedException>(() => IsLess(5, 5));
		Throws<AssertFailedException>(() => IsLess(6, 5));
		Throws<AssertFailedException>(() => IsLess(0.0, 0.0));
	}

	[TestMethod]
	public void IsLessOrEqual_AcceptsLessOrEqual_RejectsGreater()
	{
		IsLessOrEqual(1, 2);
		IsLessOrEqual(0m, 0m);

		IsLessOrEqual(int.MaxValue, int.MaxValue);
		Throws<AssertFailedException>(() => IsLessOrEqual(int.MaxValue, int.MaxValue - 1));

		Throws<AssertFailedException>(() => IsLessOrEqual(6, 5));
		Throws<AssertFailedException>(() => IsLessOrEqual(0, -1));
	}

	[TestMethod]
	public void Comparisons_NonNumericComparables_Ordered()
	{
		// The helpers are constrained to IComparable<T>, not to numbers - a regression that
		// made them numeric-only would break every date and string ordering assertion.
		IsGreater("b", "a");
		IsLess("a", "b");
		IsGreaterOrEqual("a", "a");
		IsLessOrEqual("a", "a");

		IsGreater(_later, _earlier);
		IsLess(_earlier, _later);
		IsGreaterOrEqual(_earlier, _earlier);
		IsLessOrEqual(_later, _later);

		IsGreater(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(1));
		IsLess(TimeSpan.Zero, TimeSpan.FromTicks(1));

		Throws<AssertFailedException>(() => IsGreater("a", "b"));
		Throws<AssertFailedException>(() => IsLess(_later, _earlier));
		Throws<AssertFailedException>(() => IsGreaterOrEqual(_earlier, _later));
		Throws<AssertFailedException>(() => IsLessOrEqual(TimeSpan.FromSeconds(2), TimeSpan.Zero));
	}

	[TestMethod]
	public void Comparisons_TreatStringCaseAsADifference()
	{
		// No ignoreCase flag exists on the ordering helpers, so "a" and "A" must not compare
		// equal: exactly one strict direction has to be accepted. Which one it is depends on the
		// current culture's collation rather than on the helper, so the order itself is not
		// asserted - pinning it would make this test fail under ordinal or invariant comparison.
		var lowerIsLess = Accepts(() => IsLess("a", "A"));
		var lowerIsGreater = Accepts(() => IsGreater("a", "A"));
		IsTrue(lowerIsLess != lowerIsGreater, "\"a\" and \"A\" compared equal, so casing was ignored.");

		var lowerIsAtLeast = Accepts(() => IsGreaterOrEqual("a", "A"));
		var lowerIsAtMost = Accepts(() => IsLessOrEqual("a", "A"));
		IsTrue(lowerIsAtLeast != lowerIsAtMost, "The or-equal helpers accepted both directions, so casing was ignored.");
	}

	[TestMethod]
	public void Comparisons_NullActual_ThrowsNullReference()
	{
		// The helpers dereference actual to call CompareTo, so a null actual crashes instead of
		// being reported as an assertion failure. Pinned exactly, not as "some exception": the
		// property worth guarding is that null cannot pass silently, and the day the helpers turn
		// this into an AssertFailedException that change has to be visible here.
		ThrowsExactly<NullReferenceException>(() => IsGreater<string>(null, "a"));
		ThrowsExactly<NullReferenceException>(() => IsGreaterOrEqual<string>(null, "a"));
		ThrowsExactly<NullReferenceException>(() => IsLess<string>(null, "a"));
		ThrowsExactly<NullReferenceException>(() => IsLessOrEqual<string>(null, "a"));
		ThrowsExactly<NullReferenceException>(() => IsInRange<string>(null, "a", "z"));
		ThrowsExactly<NullReferenceException>(() => IsNotInRange<string>(null, "a", "z"));

		// The same six helpers answer normally in both directions once actual is not null, so the
		// crashes above are the doing of the null operand and not of a helper that throws whatever
		// it is handed.
		IsGreater("b", "a");
		Throws<AssertFailedException>(() => IsGreater("a", "b"));
		IsGreaterOrEqual("a", "a");
		Throws<AssertFailedException>(() => IsGreaterOrEqual("a", "b"));
		IsLess("a", "b");
		Throws<AssertFailedException>(() => IsLess("b", "a"));
		IsLessOrEqual("a", "a");
		Throws<AssertFailedException>(() => IsLessOrEqual("b", "a"));
		IsInRange("b", "a", "c");
		Throws<AssertFailedException>(() => IsInRange("d", "a", "c"));
		IsNotInRange("d", "a", "c");
		Throws<AssertFailedException>(() => IsNotInRange("b", "a", "c"));
	}

	[TestMethod]
	public void Comparisons_NullComparand_SortsBeforeAnyValue()
	{
		// String.CompareTo(null) is positive by contract, in every culture: null sorts before any
		// instance. A null bound therefore reaches the comparison instead of throwing.
		IsGreater("a", null);
		IsGreaterOrEqual("a", null);
		IsInRange("a", null, "z");

		Throws<AssertFailedException>(() => IsLess("a", null));
		Throws<AssertFailedException>(() => IsLessOrEqual("a", null));
		Throws<AssertFailedException>(() => IsNotInRange("a", null, "z"));
	}

	[TestMethod]
	public void Comparisons_FailureMessage_CarriesContext()
	{
		// A caller-supplied message has to reach the report of every helper this file covers - the
		// six generic ordering and range ones below, and the sixteen numeric sign overloads further
		// down. One that formats its own text regardless would discard the only sentence explaining
		// what the caller expected.
		Contains("custom greater", Throws<AssertFailedException>(() => IsGreater(1, 2, "custom greater")).Message);
		Contains("custom greater or equal", Throws<AssertFailedException>(() => IsGreaterOrEqual(1, 2, "custom greater or equal")).Message);
		Contains("custom less", Throws<AssertFailedException>(() => IsLess(2, 1, "custom less")).Message);
		Contains("custom less or equal", Throws<AssertFailedException>(() => IsLessOrEqual(2, 1, "custom less or equal")).Message);
		Contains("custom in range", Throws<AssertFailedException>(() => IsInRange(99, 1, 10, "custom in range")).Message);
		Contains("custom not in range", Throws<AssertFailedException>(() => IsNotInRange(5, 1, 10, "custom not in range")).Message);

		// Each sign helper is overloaded per numeric type, and the message is threaded through the
		// body of each overload separately, so one of them can lose it while the others keep it.
		Contains("custom positive int", Throws<AssertFailedException>(() => IsPositive(0, "custom positive int")).Message);
		Contains("custom positive long", Throws<AssertFailedException>(() => IsPositive(0L, "custom positive long")).Message);
		Contains("custom positive double", Throws<AssertFailedException>(() => IsPositive(0d, "custom positive double")).Message);
		Contains("custom positive decimal", Throws<AssertFailedException>(() => IsPositive(0m, "custom positive decimal")).Message);

		Contains("custom negative int", Throws<AssertFailedException>(() => IsNegative(0, "custom negative int")).Message);
		Contains("custom negative long", Throws<AssertFailedException>(() => IsNegative(0L, "custom negative long")).Message);
		Contains("custom negative double", Throws<AssertFailedException>(() => IsNegative(0d, "custom negative double")).Message);
		Contains("custom negative decimal", Throws<AssertFailedException>(() => IsNegative(0m, "custom negative decimal")).Message);

		Contains("custom zero int", Throws<AssertFailedException>(() => IsZero(1, "custom zero int")).Message);
		Contains("custom zero long", Throws<AssertFailedException>(() => IsZero(1L, "custom zero long")).Message);
		Contains("custom zero double", Throws<AssertFailedException>(() => IsZero(1d, "custom zero double")).Message);
		Contains("custom zero decimal", Throws<AssertFailedException>(() => IsZero(1m, "custom zero decimal")).Message);

		Contains("custom not zero int", Throws<AssertFailedException>(() => IsNotZero(0, "custom not zero int")).Message);
		Contains("custom not zero long", Throws<AssertFailedException>(() => IsNotZero(0L, "custom not zero long")).Message);
		Contains("custom not zero double", Throws<AssertFailedException>(() => IsNotZero(0d, "custom not zero double")).Message);
		Contains("custom not zero decimal", Throws<AssertFailedException>(() => IsNotZero(0m, "custom not zero decimal")).Message);

		// The message is text for the failure path alone: supplying one must not turn a call the
		// helper would otherwise accept into a failure.
		IsGreater(2, 1, "unused");
		IsGreaterOrEqual(1, 1, "unused");
		IsLess(1, 2, "unused");
		IsLessOrEqual(1, 1, "unused");
		IsInRange(5, 1, 10, "unused");
		IsNotInRange(99, 1, 10, "unused");
		IsPositive(1, "unused");
		IsNegative(-1, "unused");
		IsZero(0, "unused");
		IsNotZero(1, "unused");

		// With no message supplied the values themselves have to reach the report, otherwise a
		// failure says nothing about what was compared. The operands are four digits long so that
		// they cannot be matched by incidental text such as a line or frame number, and each is
		// matched in one span together with the relation between them: separate per-operand checks
		// would pass just as well on a report that named the comparand as the actual value.
		var generated = Throws<AssertFailedException>(() => IsGreater(1234, 5678)).Message;
		Contains("1234 to be greater than 5678", generated);

		var range = Throws<AssertFailedException>(() => IsInRange(9999, 1234, 5678)).Message;
		Contains("9999", range);
		Contains("[1234, 5678]", range);
	}

	[TestMethod]
	public void IsInRange_AcceptsInsideAndBounds_RejectsOutside()
	{
		IsInRange(5, 1, 10);
		IsInRange(0, -1, 1);

		// The documented contract is inclusive, and only a pair of calls one step apart can show
		// which side of each bound the line falls on. An exclusive implementation would reject the
		// first call of each pair and silently narrow every range assertion in the workspace.
		IsInRange(1, 1, 10);
		Throws<AssertFailedException>(() => IsInRange(0, 1, 10));
		IsInRange(10, 1, 10);
		Throws<AssertFailedException>(() => IsInRange(11, 1, 10));

		IsInRange(0.0, 0.0, 1.0);
		Throws<AssertFailedException>(() => IsInRange(Math.BitDecrement(0.0), 0.0, 1.0));
		IsInRange(1.0, 0.0, 1.0);
		Throws<AssertFailedException>(() => IsInRange(Math.BitIncrement(1.0), 0.0, 1.0));

		IsInRange(0m, 0m, 1m);
		Throws<AssertFailedException>(() => IsInRange(-_tinyDecimal, 0m, 1m));
		IsInRange(1m, 0m, 1m);
		Throws<AssertFailedException>(() => IsInRange(1m + _tinyDecimal, 0m, 1m));
	}

	[TestMethod]
	public void IsInRange_DegenerateAndInvertedBounds()
	{
		// A single-point range accepts that point and nothing on either side of it.
		IsInRange(5, 5, 5);
		Throws<AssertFailedException>(() => IsInRange(6, 5, 5));
		Throws<AssertFailedException>(() => IsInRange(4, 5, 5));

		// min above max describes an empty range: no input is inside it, including one that sits
		// between the swapped bounds and would be accepted were they the right way round.
		IsInRange(5, 0, 10);
		Throws<AssertFailedException>(() => IsInRange(5, 10, 0));
		Throws<AssertFailedException>(() => IsInRange(-1, 10, 0));
		Throws<AssertFailedException>(() => IsInRange(11, 10, 0));
	}

	[TestMethod]
	public void IsInRange_NonNumericComparables()
	{
		IsInRange(_earlier.AddHours(1), _earlier, _later);
		IsInRange("b", "a", "c");

		// Both date bounds are bracketed by a single tick, the finest step DateTime has.
		IsInRange(_earlier, _earlier, _later);
		Throws<AssertFailedException>(() => IsInRange(_earlier.AddTicks(-1), _earlier, _later));
		IsInRange(_later, _earlier, _later);
		Throws<AssertFailedException>(() => IsInRange(_later.AddTicks(1), _earlier, _later));

		Throws<AssertFailedException>(() => IsInRange("d", "a", "c"));
	}

	[TestMethod]
	public void IsNotInRange_AcceptsOutside_RejectsInsideAndBounds()
	{
		Throws<AssertFailedException>(() => IsNotInRange(5, 1, 10));

		// The bounds belong to the range, so they are not outside it either. Each is bracketed by
		// the value one step beyond it, which must be accepted.
		Throws<AssertFailedException>(() => IsNotInRange(1, 1, 10));
		IsNotInRange(0, 1, 10);
		Throws<AssertFailedException>(() => IsNotInRange(10, 1, 10));
		IsNotInRange(11, 1, 10);

		Throws<AssertFailedException>(() => IsNotInRange(0m, 0m, 1m));
		IsNotInRange(-_tinyDecimal, 0m, 1m);

		Throws<AssertFailedException>(() => IsNotInRange(_later, _earlier, _later));
		IsNotInRange(_later.AddTicks(1), _earlier, _later);

		IsNotInRange("d", "a", "c");
	}

	[TestMethod]
	public void IsNotInRange_InvertedBounds_AcceptEverything()
	{
		// With min above max the inside test (>= min && <= max) can never hold, so IsNotInRange
		// accepts every input - including the one the caller most likely meant to be inside.
		// This is the single input shape under which the helper cannot fail, so it is pinned
		// against the same value under correctly ordered bounds, which must be rejected.
		IsNotInRange(5, 10, 0);
		IsNotInRange(-1, 10, 0);
		IsNotInRange(11, 10, 0);

		Throws<AssertFailedException>(() => IsNotInRange(5, 0, 10));
	}

	[TestMethod]
	public void IsPositive_AcceptsAboveZero_RejectsZeroAndBelow()
	{
		IsPositive(1);
		IsPositive(int.MaxValue);
		IsPositive(1L);
		IsPositive(long.MaxValue);
		IsPositive(0.5);
		IsPositive(double.MaxValue);
		IsPositive(0.5m);
		IsPositive(decimal.MaxValue);

		// Zero is not positive - the single most likely thing to get wrong here - and the two
		// floating types are bracketed at the smallest magnitude they can represent, so a helper
		// that rounded or truncated near zero would be caught on the accepting side too.
		IsPositive(double.Epsilon);
		Throws<AssertFailedException>(() => IsPositive(0d));
		Throws<AssertFailedException>(() => IsPositive(-double.Epsilon));

		IsPositive(_tinyDecimal);
		Throws<AssertFailedException>(() => IsPositive(0m));
		Throws<AssertFailedException>(() => IsPositive(-_tinyDecimal));

		// Negative zero carries a sign bit but is still zero, and a decimal zero keeps its scale.
		Throws<AssertFailedException>(() => IsPositive(-0d));
		Throws<AssertFailedException>(() => IsPositive(0.00m));

		Throws<AssertFailedException>(() => IsPositive(0));
		Throws<AssertFailedException>(() => IsPositive(-1));
		Throws<AssertFailedException>(() => IsPositive(0L));
		Throws<AssertFailedException>(() => IsPositive(-1L));
	}

	[TestMethod]
	public void IsNegative_AcceptsBelowZero_RejectsZeroAndAbove()
	{
		IsNegative(-1);
		IsNegative(int.MinValue);
		IsNegative(-1L);
		IsNegative(long.MinValue);
		IsNegative(-0.5);
		IsNegative(double.MinValue);
		IsNegative(-0.5m);
		IsNegative(decimal.MinValue);

		IsNegative(-double.Epsilon);
		Throws<AssertFailedException>(() => IsNegative(0d));
		Throws<AssertFailedException>(() => IsNegative(double.Epsilon));

		IsNegative(-_tinyDecimal);
		Throws<AssertFailedException>(() => IsNegative(0m));
		Throws<AssertFailedException>(() => IsNegative(_tinyDecimal));

		// Negative zero is zero, however its sign bit reads.
		Throws<AssertFailedException>(() => IsNegative(-0d));

		Throws<AssertFailedException>(() => IsNegative(0));
		Throws<AssertFailedException>(() => IsNegative(1));
		Throws<AssertFailedException>(() => IsNegative(0L));
		Throws<AssertFailedException>(() => IsNegative(1L));
	}

	[TestMethod]
	public void IsZero_AcceptsZero_RejectsNonZero()
	{
		IsZero(0);
		IsZero(0L);
		IsZero(0d);
		IsZero(-0d);
		IsZero(0m);
		// Trailing scale does not change the value of a decimal zero.
		IsZero(0.00m);

		Throws<AssertFailedException>(() => IsZero(1));
		Throws<AssertFailedException>(() => IsZero(-1));
		Throws<AssertFailedException>(() => IsZero(1L));
		Throws<AssertFailedException>(() => IsZero(0.1));
		Throws<AssertFailedException>(() => IsZero(0.1m));

		// The smallest representable non-zero magnitudes must not be rounded into zero.
		Throws<AssertFailedException>(() => IsZero(double.Epsilon));
		Throws<AssertFailedException>(() => IsZero(-double.Epsilon));
		Throws<AssertFailedException>(() => IsZero(_tinyDecimal));
		Throws<AssertFailedException>(() => IsZero(-_tinyDecimal));
	}

	[TestMethod]
	public void IsNotZero_AcceptsNonZero_RejectsZero()
	{
		IsNotZero(1);
		IsNotZero(-1);
		IsNotZero(1L);
		IsNotZero(long.MinValue);
		IsNotZero(0.1);
		IsNotZero(double.Epsilon);
		IsNotZero(-double.Epsilon);
		IsNotZero(0.1m);
		IsNotZero(_tinyDecimal);

		Throws<AssertFailedException>(() => IsNotZero(0));
		Throws<AssertFailedException>(() => IsNotZero(0L));
		Throws<AssertFailedException>(() => IsNotZero(0d));
		Throws<AssertFailedException>(() => IsNotZero(-0d));
		Throws<AssertFailedException>(() => IsNotZero(0m));
		Throws<AssertFailedException>(() => IsNotZero(0.00m));
	}

	[TestMethod]
	public void Infinities_CarryTheirSign()
	{
		IsPositive(double.PositiveInfinity);
		IsNegative(double.NegativeInfinity);
		IsNotZero(double.PositiveInfinity);
		IsNotZero(double.NegativeInfinity);

		Throws<AssertFailedException>(() => IsNegative(double.PositiveInfinity));
		Throws<AssertFailedException>(() => IsPositive(double.NegativeInfinity));
		Throws<AssertFailedException>(() => IsZero(double.PositiveInfinity));
		Throws<AssertFailedException>(() => IsZero(double.NegativeInfinity));
	}

	[TestMethod]
	public void SignChecks_NaN_HasNoSignAndIsNotZero()
	{
		// NaN has no sign, so it is neither positive nor negative. Every operator comparison
		// against NaN is false, so a guard written as `value <= 0` never trips and lets NaN
		// through both sign checks at once - a NaN slipping past reads as success.
		Throws<AssertFailedException>(() => IsPositive(double.NaN));
		Throws<AssertFailedException>(() => IsNegative(double.NaN));

		Throws<AssertFailedException>(() => IsZero(double.NaN));
		IsNotZero(double.NaN);
	}

	[TestMethod]
	public void Comparisons_NaN_SortsBelowEveryNumber()
	{
		// The ordering helpers go through IComparable, whose total order puts NaN below every
		// number, unlike the operators for which any NaN comparison is false. So the helpers do
		// not treat NaN as unordered: they reject it as greater and accept it as less. The
		// asymmetry is pinned because switching the helpers to operator comparison would flip
		// the accepting half without touching the rejecting one.
		Throws<AssertFailedException>(() => IsGreater(double.NaN, 0d));
		Throws<AssertFailedException>(() => IsGreaterOrEqual(double.NaN, 0d));
		Throws<AssertFailedException>(() => IsInRange(double.NaN, 0d, 1d));

		IsLess(double.NaN, 0d);
		IsLessOrEqual(double.NaN, 0d);
		IsNotInRange(double.NaN, 0d, 1d);

		// The same order seen from the other operand.
		IsGreater(0d, double.NaN);
		Throws<AssertFailedException>(() => IsLess(0d, double.NaN));

		// Below zero is not below everything: an order that mapped NaN onto some very negative
		// finite sentinel would satisfy every call above. The two lowest values a double has are
		// what separate that from NaN sitting under the whole order, and the widest range there
		// is - the entire number line, endpoints included - still has to leave NaN outside it.
		IsLess(double.NaN, double.MinValue);
		IsLess(double.NaN, double.NegativeInfinity);
		IsGreater(double.MinValue, double.NaN);
		IsGreater(double.NegativeInfinity, double.NaN);
		Throws<AssertFailedException>(() => IsGreater(double.NaN, double.NegativeInfinity));
		Throws<AssertFailedException>(() => IsLess(double.NegativeInfinity, double.NaN));

		IsNotInRange(double.NaN, double.NegativeInfinity, double.PositiveInfinity);
		Throws<AssertFailedException>(() => IsInRange(double.NaN, double.NegativeInfinity, double.PositiveInfinity));

		// The one value NaN does not sort below is another NaN: CompareTo calls the two equal,
		// where every operator comparison between them is false.
		IsGreaterOrEqual(double.NaN, double.NaN);
		IsLessOrEqual(double.NaN, double.NaN);
		Throws<AssertFailedException>(() => IsGreater(double.NaN, double.NaN));
		Throws<AssertFailedException>(() => IsLess(double.NaN, double.NaN));
	}
}
