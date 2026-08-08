namespace Ecng.Tests;

/// <summary>
/// The boolean, nullity, identity and type-check part of the <see cref="BaseTestClass"/> assertion surface.
/// Each test drives one helper in both directions - what it must accept and what it must reject - because a
/// helper whose body was emptied still satisfies every accepting call on its own.
/// </summary>
[TestClass]
public class BaseTestClassBooleanTests : BaseTestClass
{
	private interface IMarker
	{
	}

	private class Animal
	{
	}

	private class Dog : Animal, IMarker
	{
	}

	[TestMethod]
	public void Fail_AlwaysThrowsAndKeepsMessage()
	{
		Throws<AssertFailedException>(() => Fail());

		var ex = Throws<AssertFailedException>(() => Fail("custom failure text"));
		Contains("custom failure text", ex.Message);
	}

	[TestMethod]
	public void Inconclusive_ThrowsInconclusiveNotFailed()
	{
		Throws<AssertInconclusiveException>(() => Inconclusive());

		var ex = Throws<AssertInconclusiveException>(() => Inconclusive("skipped"));
		Contains("skipped", ex.Message);

		// Callers routinely write Throws<AssertFailedException>; if the two exceptions were
		// related by inheritance an inconclusive result would silently read as a failure.
		IsNotInstanceOfType<AssertFailedException>(ex);
	}

	[TestMethod]
	public void IsTrue_AcceptsTrueRejectsFalse()
	{
		// Built at run time so the condition survives constant folding: a folded literal
		// would test the compiler rather than the helper.
		var length = new string('a', 2).Length;

		IsTrue(true);
		IsTrue(length == 2);

		Throws<AssertFailedException>(() => IsTrue(false));
		Throws<AssertFailedException>(() => IsTrue(length == 3));

		var ex = Throws<AssertFailedException>(() => IsTrue(false, "condition text"));
		Contains("condition text", ex.Message);
	}

	[TestMethod]
	public void IsFalse_AcceptsFalseRejectsTrue()
	{
		var length = new string('a', 2).Length;

		IsFalse(false);
		IsFalse(length == 3);

		Throws<AssertFailedException>(() => IsFalse(true));
		Throws<AssertFailedException>(() => IsFalse(length == 2));

		var ex = Throws<AssertFailedException>(() => IsFalse(true, "condition text"));
		Contains("condition text", ex.Message);
	}

	[TestMethod]
	public void IsNull_AcceptsNullsRejectsValues()
	{
		IsNull(null);
		IsNull((string)null);
		IsNull((Animal)null);

		Throws<AssertFailedException>(() => IsNull(new object()));

		// The usual traps: an empty string and a zero are values, not absence of one.
		Throws<AssertFailedException>(() => IsNull(string.Empty));
		Throws<AssertFailedException>(() => IsNull(0));

		var ex = Throws<AssertFailedException>(() => IsNull(new object(), "null text"));
		Contains("null text", ex.Message);
	}

	[TestMethod]
	public void IsNotNull_AcceptsValuesRejectsNulls()
	{
		IsNotNull(new object());
		IsNotNull(string.Empty);
		IsNotNull(0);
		IsNotNull(Array.Empty<int>());

		Throws<AssertFailedException>(() => IsNotNull(null));
		Throws<AssertFailedException>(() => IsNotNull((string)null));

		var ex = Throws<AssertFailedException>(() => IsNotNull(null, "not null text"));
		Contains("not null text", ex.Message);
	}

	[TestMethod]
	public void AreSame_AcceptsSameInstanceRejectsDistinctOnes()
	{
		var instance = new Animal();
		AreSame(instance, instance);
		AreSame(null, null);

		// Built at run time so the compiler cannot intern them into one instance:
		// equal by value, different references, and AreSame is about references.
		var left = new string('a', 2);
		var right = new string('a', 2);
		AreEqual(left, right);
		Throws<AssertFailedException>(() => AreSame(left, right));

		Throws<AssertFailedException>(() => AreSame(new Animal(), new Animal()));
		Throws<AssertFailedException>(() => AreSame(new Animal(), null));
		Throws<AssertFailedException>(() => AreSame(null, new Animal()));

		// Numbers reach the helper as two separate boxes, so identity never holds for them -
		// the trap a caller falls into by reaching for AreSame when they mean AreEqual.
		Throws<AssertFailedException>(() => AreSame(1, 1));

		var ex = Throws<AssertFailedException>(() => AreSame(new Animal(), new Animal(), "same text"));
		Contains("same text", ex.Message);
	}

	[TestMethod]
	public void AreNotSame_AcceptsDistinctInstancesRejectsSameOne()
	{
		AreNotSame(new Animal(), new Animal());
		AreNotSame(new string('a', 2), new string('a', 2));
		AreNotSame(new Animal(), null);
		AreNotSame(null, new Animal());

		var instance = new Animal();
		Throws<AssertFailedException>(() => AreNotSame(instance, instance));

		// Two nulls are the same absent reference.
		Throws<AssertFailedException>(() => AreNotSame(null, null));

		var ex = Throws<AssertFailedException>(() => AreNotSame(instance, instance, "not same text"));
		Contains("not same text", ex.Message);
	}

	[TestMethod]
	public void IsInstanceOfType_AcceptsExactDerivedAndInterfaceRejectsTheRest()
	{
		IsInstanceOfType("abc", typeof(string));
		IsInstanceOfType(new Animal(), typeof(Animal));
		IsInstanceOfType(42, typeof(int));

		IsInstanceOfType(new Dog(), typeof(Animal));
		IsInstanceOfType(new Dog(), typeof(object));
		IsInstanceOfType(new ArgumentNullException(), typeof(ArgumentException));

		IsInstanceOfType(new Dog(), typeof(IMarker));
		IsInstanceOfType(new List<int>(), typeof(IEnumerable));
		IsInstanceOfType(new List<int>(), typeof(IList<int>));

		Throws<AssertFailedException>(() => IsInstanceOfType(42, typeof(string)));
		Throws<AssertFailedException>(() => IsInstanceOfType("abc", typeof(int)));

		// The other side of the derived/interface cases above: the relation is one-way,
		// so a base instance is not an instance of the derived type.
		Throws<AssertFailedException>(() => IsInstanceOfType(new Animal(), typeof(Dog)));
		Throws<AssertFailedException>(() => IsInstanceOfType(new Animal(), typeof(IMarker)));

		Throws<AssertFailedException>(() => IsInstanceOfType(null, typeof(string)));
		Throws<AssertFailedException>(() => IsInstanceOfType(null, typeof(object)));

		var ex = Throws<AssertFailedException>(() => IsInstanceOfType(42, typeof(string), "type text"));
		Contains("type text", ex.Message);
	}

	[TestMethod]
	public void IsNotInstanceOfType_AcceptsUnrelatedAndNullRejectsMatches()
	{
		IsNotInstanceOfType(42, typeof(string));
		IsNotInstanceOfType("abc", typeof(int));
		IsNotInstanceOfType(new Animal(), typeof(Dog));
		IsNotInstanceOfType(new Animal(), typeof(IMarker));

		// Null is an instance of nothing, so the negative check holds for any type.
		IsNotInstanceOfType(null, typeof(string));
		IsNotInstanceOfType(null, typeof(object));

		Throws<AssertFailedException>(() => IsNotInstanceOfType("abc", typeof(string)));
		Throws<AssertFailedException>(() => IsNotInstanceOfType(new Dog(), typeof(Animal)));
		Throws<AssertFailedException>(() => IsNotInstanceOfType(new Dog(), typeof(IMarker)));
		Throws<AssertFailedException>(() => IsNotInstanceOfType(new List<int>(), typeof(IEnumerable)));

		var ex = Throws<AssertFailedException>(() => IsNotInstanceOfType("abc", typeof(string), "type text"));
		Contains("type text", ex.Message);
	}

	[TestMethod]
	public void IsInstanceOfTypeGeneric_AcceptsExactDerivedAndInterfaceRejectsTheRest()
	{
		IsInstanceOfType<string>("abc");
		IsInstanceOfType<int>(42);
		IsInstanceOfType<Animal>(new Dog());
		IsInstanceOfType<IMarker>(new Dog());
		IsInstanceOfType<IEnumerable>(new List<int>());
		IsInstanceOfType<ArgumentException>(new ArgumentNullException());

		Throws<AssertFailedException>(() => IsInstanceOfType<string>(42));

		// Against the accepted IsInstanceOfType<int>(42) above: boxed numbers keep their exact
		// type, so a widening conversion that the compiler would allow does not apply here.
		Throws<AssertFailedException>(() => IsInstanceOfType<long>(42));

		Throws<AssertFailedException>(() => IsInstanceOfType<Dog>(new Animal()));
		Throws<AssertFailedException>(() => IsInstanceOfType<string>(null));
		Throws<AssertFailedException>(() => IsInstanceOfType<object>(null));

		var ex = Throws<AssertFailedException>(() => IsInstanceOfType<string>(42, "type text"));
		Contains("type text", ex.Message);
	}

	[TestMethod]
	public void IsNotInstanceOfTypeGeneric_AcceptsUnrelatedAndNullRejectsMatches()
	{
		IsNotInstanceOfType<string>(42);
		IsNotInstanceOfType<Dog>(new Animal());
		IsNotInstanceOfType<IMarker>(new Animal());
		IsNotInstanceOfType<string>(null);
		IsNotInstanceOfType<object>(null);

		Throws<AssertFailedException>(() => IsNotInstanceOfType<string>("abc"));
		Throws<AssertFailedException>(() => IsNotInstanceOfType<Animal>(new Dog()));
		Throws<AssertFailedException>(() => IsNotInstanceOfType<IMarker>(new Dog()));
		Throws<AssertFailedException>(() => IsNotInstanceOfType<IEnumerable>(new List<int>()));

		var ex = Throws<AssertFailedException>(() => IsNotInstanceOfType<string>("abc", "type text"));
		Contains("type text", ex.Message);
	}
}
