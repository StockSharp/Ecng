namespace Ecng.Tests.UnitTesting;

/// <summary>
/// The <see cref="ICollection"/> assertion overloads <see cref="BaseTestClass"/> exposes.
/// </summary>
/// <remarks>
/// Arguments are cast to <see cref="ICollection"/> because for a typed argument such as <c>int[]</c>
/// the generic <c>AreEqual&lt;T&gt;(T, T)</c> binds by identity conversion and wins overload
/// resolution; with both arguments typed as <see cref="ICollection"/> the candidates have identical
/// parameter types and the non-generic overload is the better member.
/// </remarks>
[TestClass]
public class BaseTestClassCollectionTests : BaseTestClass
{
	private class Base;
	private sealed class Derived : Base;

	[TestMethod]
	public void AreEqual_ComparesElementwise()
	{
		AreEqual((ICollection)new[] { 1, 2, 3 }, (ICollection)new[] { 1, 2, 3 });
		// The container type is not part of the comparison, only the elements are.
		AreEqual((ICollection)new List<string> { "a", "b" }, (ICollection)new[] { "a", "b" });

		Throws<AssertFailedException>(() => AreEqual((ICollection)new[] { 1, 2 }, (ICollection)new[] { 1, 3 }));
		// A common prefix is not equality: the extra trailing element counts.
		Throws<AssertFailedException>(() => AreEqual((ICollection)new[] { 1, 2 }, (ICollection)new[] { 1, 2, 3 }));
	}

	[TestMethod]
	public void AreEqual_EmptyMatchesEmptyOnly()
	{
		AreEqual((ICollection)Array.Empty<int>(), (ICollection)new List<int>());
		// One element is the boundary between empty and non-empty.
		Throws<AssertFailedException>(() => AreEqual((ICollection)Array.Empty<int>(), (ICollection)new[] { 1 }));
	}

	[TestMethod]
	public void AreEqual_NullMatchesNullOnly()
	{
		AreEqual((ICollection)null, (ICollection)null);
		// A missing collection is not an empty one, and not a populated one either.
		Throws<AssertFailedException>(() => AreEqual((ICollection)null, (ICollection)Array.Empty<int>()));
		Throws<AssertFailedException>(() => AreEqual((ICollection)null, (ICollection)new[] { 1 }));
		Throws<AssertFailedException>(() => AreEqual((ICollection)new[] { 1 }, (ICollection)null));
	}

	[TestMethod]
	public void AreEqual_NullElements_ComparedByValue()
	{
		AreEqual((ICollection)new string[] { null, "a" }, (ICollection)new string[] { null, "a" });
		Throws<AssertFailedException>(() => AreEqual((ICollection)new string[] { null }, (ICollection)new[] { "a" }));
	}

	[TestMethod]
	public void AreNotEqual_RequiresAnElementwiseDifference()
	{
		AreNotEqual((ICollection)new[] { 1, 2 }, (ICollection)new[] { 1, 3 });
		AreNotEqual((ICollection)new[] { 1 }, (ICollection)new[] { 1, 2 });

		// Distinct instances holding equal elements are equal here: the comparison is
		// element-wise, not by reference.
		Throws<AssertFailedException>(() => AreNotEqual((ICollection)new[] { 1, 2 }, (ICollection)new[] { 1, 2 }));
		Throws<AssertFailedException>(() => AreNotEqual((ICollection)Array.Empty<int>(), (ICollection)new List<int>()));
	}

	[TestMethod]
	public void AreNotEqual_NullDiffersFromEverythingButNull()
	{
		AreNotEqual((ICollection)null, (ICollection)new[] { 1 });
		AreNotEqual((ICollection)new[] { 1 }, (ICollection)null);
		AreNotEqual((ICollection)null, (ICollection)Array.Empty<int>());

		Throws<AssertFailedException>(() => AreNotEqual((ICollection)null, (ICollection)null));
	}

	[TestMethod]
	public void AreEqual_IsOrderSensitive_AreEquivalent_IsNot()
	{
		var forward = (ICollection)new[] { 1, 2, 3 };
		var reversed = (ICollection)new[] { 3, 2, 1 };

		Throws<AssertFailedException>(() => AreEqual(forward, reversed));
		AreEquivalent(forward, reversed);

		// Order-insensitive is not content-insensitive: one differing element still fails.
		Throws<AssertFailedException>(() => AreEquivalent(forward, (ICollection)new[] { 1, 2, 4 }));
		AreEquivalent((ICollection)new List<int> { 1, 2, 3 }, reversed);
	}

	[TestMethod]
	public void AreEquivalent_CountsMultiplicities()
	{
		// Same distinct elements and the same size, different multiplicities.
		Throws<AssertFailedException>(() => AreEquivalent((ICollection)new[] { 1, 1, 2 }, (ICollection)new[] { 1, 2, 2 }));
		AreEquivalent((ICollection)new[] { 1, 1, 2 }, (ICollection)new[] { 2, 1, 1 });
	}

	[TestMethod]
	public void AreEquivalent_EmptyAndNull()
	{
		AreEquivalent((ICollection)Array.Empty<int>(), (ICollection)new List<int>());
		AreEquivalent((ICollection)null, (ICollection)null);
		Throws<AssertFailedException>(() => AreEquivalent((ICollection)null, (ICollection)Array.Empty<int>()));
		Throws<AssertFailedException>(() => AreEquivalent((ICollection)Array.Empty<int>(), (ICollection)new[] { 1 }));
	}

	[TestMethod]
	public void AreNotEquivalent_RequiresDifferentContents()
	{
		AreNotEquivalent((ICollection)new[] { 1, 2 }, (ICollection)new[] { 1, 3 });
		AreNotEquivalent((ICollection)new[] { 1, 1, 2 }, (ICollection)new[] { 1, 2, 2 });
		AreNotEquivalent((ICollection)Array.Empty<int>(), (ICollection)new[] { 1 });

		// Reordering does not make two collections non-equivalent.
		Throws<AssertFailedException>(() => AreNotEquivalent((ICollection)new[] { 1, 2, 3 }, (ICollection)new[] { 3, 2, 1 }));
		Throws<AssertFailedException>(() => AreNotEquivalent((ICollection)Array.Empty<int>(), (ICollection)new List<int>()));
		Throws<AssertFailedException>(() => AreNotEquivalent((ICollection)null, (ICollection)null));
	}

	[TestMethod]
	public void HasCount_MatchesSizeExactly()
	{
		HasCount(3, (ICollection)new[] { 1, 2, 3 });
		Throws<AssertFailedException>(() => HasCount(2, (ICollection)new[] { 1, 2, 3 }));
		Throws<AssertFailedException>(() => HasCount(4, (ICollection)new[] { 1, 2, 3 }));

		HasCount(0, (ICollection)Array.Empty<int>());
		Throws<AssertFailedException>(() => HasCount(1, (ICollection)Array.Empty<int>()));

		// The count is the size, not the number of distinct values.
		HasCount(3, (ICollection)new[] { 1, 1, 1 });
		Throws<AssertFailedException>(() => HasCount(1, (ICollection)new[] { 1, 1, 1 }));
	}

	[TestMethod]
	public void HasCount_NullIsNotAnEmptyCollection()
	{
		HasCount(0, (ICollection)Array.Empty<int>());

		// A missing collection has no count to match, not even zero.
		Throws<AssertFailedException>(() => HasCount(0, (ICollection)null));
	}

	[TestMethod]
	public void AllItemsAreNotNull_RejectsAnyNull()
	{
		AllItemsAreNotNull((ICollection)new[] { "a", "b" });
		AllItemsAreNotNull((ICollection)Array.Empty<string>());

		// One null among valid elements is enough to fail.
		Throws<AssertFailedException>(() => AllItemsAreNotNull((ICollection)new string[] { "a", null }));
		Throws<AssertFailedException>(() => AllItemsAreNotNull((ICollection)new string[] { null }));
	}

	[TestMethod]
	public void AllItemsAreUnique_RejectsValueEqualDuplicates()
	{
		AllItemsAreUnique((ICollection)new[] { 1, 2, 3 });
		AllItemsAreUnique((ICollection)Array.Empty<int>());
		Throws<AssertFailedException>(() => AllItemsAreUnique((ICollection)new[] { 1, 2, 1 }));

		// One null is a unique item; a second null is a duplicate.
		AllItemsAreUnique((ICollection)new string[] { null, "a" });
		Throws<AssertFailedException>(() => AllItemsAreUnique((ICollection)new string[] { null, null }));

		// Duplicates are by value, not by reference: two distinct equal strings collide.
		Throws<AssertFailedException>(() => AllItemsAreUnique((ICollection)new[] { "a", new string(['a']) }));
	}

	[TestMethod]
	public void AllItemsAreInstancesOfType_IsAssignabilityInOneDirection()
	{
		AllItemsAreInstancesOfType((ICollection)new[] { "a", "b" }, typeof(string));
		AllItemsAreInstancesOfType((ICollection)Array.Empty<string>(), typeof(string));
		Throws<AssertFailedException>(() => AllItemsAreInstancesOfType((ICollection)new object[] { "a", 1 }, typeof(string)));

		// A derived instance is an instance of the base type, but not the other way round.
		AllItemsAreInstancesOfType((ICollection)new object[] { new Derived(), new Derived() }, typeof(Base));
		Throws<AssertFailedException>(() => AllItemsAreInstancesOfType((ICollection)new object[] { new Base() }, typeof(Derived)));

		// null is an instance of nothing, so it fails the type assertion.
		Throws<AssertFailedException>(() => AllItemsAreInstancesOfType((ICollection)new string[] { "a", null }, typeof(string)));
	}

	[TestMethod]
	public void Contains_SearchesByValue()
	{
		Contains((ICollection)new[] { 1, 2, 3 }, 2);
		Throws<AssertFailedException>(() => Contains((ICollection)new[] { 1, 2, 3 }, 4));
		Throws<AssertFailedException>(() => Contains((ICollection)Array.Empty<int>(), 1));

		// Equal by value: the searched instance need not be the stored one.
		Contains((ICollection)new[] { "a", "b" }, new string(['a']));

		// null is an ordinary element to search for: found when stored, missing otherwise.
		Contains((ICollection)new string[] { "a", null }, null);
		Throws<AssertFailedException>(() => Contains((ICollection)new[] { "a" }, null));
	}

	[TestMethod]
	public void DoesNotContain_SearchesByValue()
	{
		DoesNotContain((ICollection)new[] { 1, 2, 3 }, 4);
		DoesNotContain((ICollection)Array.Empty<int>(), 1);
		Throws<AssertFailedException>(() => DoesNotContain((ICollection)new[] { 1, 2, 3 }, 2));

		DoesNotContain((ICollection)new[] { "a" }, null);
		Throws<AssertFailedException>(() => DoesNotContain((ICollection)new string[] { "a", null }, null));
	}

	[TestMethod]
	public void IsSubsetOf_IsMultisetContainment()
	{
		IsSubsetOf((ICollection)new[] { 1, 2 }, (ICollection)new[] { 1, 2, 3 });
		// Sharing an element is not containment.
		Throws<AssertFailedException>(() => IsSubsetOf((ICollection)new[] { 1, 4 }, (ICollection)new[] { 1, 2, 3 }));

		// The empty collection is a subset of everything, including itself; nothing is a
		// subset of it but itself.
		IsSubsetOf((ICollection)Array.Empty<int>(), (ICollection)new[] { 1 });
		IsSubsetOf((ICollection)Array.Empty<int>(), (ICollection)Array.Empty<int>());
		Throws<AssertFailedException>(() => IsSubsetOf((ICollection)new[] { 1 }, (ICollection)Array.Empty<int>()));

		// A collection is a subset of itself: the relation is not proper containment.
		IsSubsetOf((ICollection)new[] { 1, 2 }, (ICollection)new[] { 2, 1 });

		// Multiplicity counts: two occurrences need two on the superset side.
		IsSubsetOf((ICollection)new[] { 1, 1 }, (ICollection)new[] { 1, 1, 2 });
		Throws<AssertFailedException>(() => IsSubsetOf((ICollection)new[] { 1, 1 }, (ICollection)new[] { 1, 2 }));
	}

	[TestMethod]
	public void IsNotSubsetOf_IsTheComplementOfIsSubsetOf()
	{
		IsNotSubsetOf((ICollection)new[] { 1, 4 }, (ICollection)new[] { 1, 2, 3 });
		Throws<AssertFailedException>(() => IsNotSubsetOf((ICollection)new[] { 1, 2 }, (ICollection)new[] { 1, 2, 3 }));

		IsNotSubsetOf((ICollection)new[] { 1 }, (ICollection)Array.Empty<int>());
		Throws<AssertFailedException>(() => IsNotSubsetOf((ICollection)Array.Empty<int>(), (ICollection)new[] { 1 }));

		IsNotSubsetOf((ICollection)new[] { 1, 1 }, (ICollection)new[] { 1, 2 });
		Throws<AssertFailedException>(() => IsNotSubsetOf((ICollection)new[] { 1, 1 }, (ICollection)new[] { 1, 1, 2 }));
	}

	[TestMethod]
	public void IsEmpty_AcceptsSizeZeroOnly()
	{
		IsEmpty((ICollection)Array.Empty<int>());
		IsEmpty((ICollection)new List<string>());

		// A single element is the boundary; a collection that is not there is not an empty one.
		Throws<AssertFailedException>(() => IsEmpty((ICollection)new[] { 1 }));
		Throws<AssertFailedException>(() => IsEmpty((ICollection)null));
	}

	[TestMethod]
	public void IsNotEmpty_RejectsSizeZeroAndNull()
	{
		IsNotEmpty((ICollection)new[] { 1 });
		// A null element still occupies a slot, so the collection is not empty.
		IsNotEmpty((ICollection)new List<string> { null });

		Throws<AssertFailedException>(() => IsNotEmpty((ICollection)Array.Empty<int>()));
		Throws<AssertFailedException>(() => IsNotEmpty((ICollection)null));
	}

	[TestMethod]
	public void MissingCollectionOrType_IsAnAssertionFailureNotAnArgumentException()
	{
		// Each helper is exercised with valid input first, so only the null argument fails below.
		AllItemsAreNotNull((ICollection)new[] { "a" });
		Throws<AssertFailedException>(() => AllItemsAreNotNull(null));

		AllItemsAreUnique((ICollection)new[] { 1 });
		Throws<AssertFailedException>(() => AllItemsAreUnique(null));

		AllItemsAreInstancesOfType((ICollection)new[] { "a" }, typeof(string));
		Throws<AssertFailedException>(() => AllItemsAreInstancesOfType(null, typeof(string)));
		Throws<AssertFailedException>(() => AllItemsAreInstancesOfType((ICollection)new[] { "a" }, null));

		Contains((ICollection)new[] { 1 }, 1);
		Throws<AssertFailedException>(() => Contains(null, 1));

		DoesNotContain((ICollection)new[] { 1 }, 2);
		Throws<AssertFailedException>(() => DoesNotContain(null, 2));

		IsSubsetOf((ICollection)new[] { 1 }, (ICollection)new[] { 1, 2 });
		Throws<AssertFailedException>(() => IsSubsetOf(null, (ICollection)new[] { 1 }));
		Throws<AssertFailedException>(() => IsSubsetOf((ICollection)new[] { 1 }, null));

		IsNotSubsetOf((ICollection)new[] { 3 }, (ICollection)new[] { 1, 2 });
		Throws<AssertFailedException>(() => IsNotSubsetOf(null, (ICollection)new[] { 1 }));
		Throws<AssertFailedException>(() => IsNotSubsetOf((ICollection)new[] { 1 }, null));
	}
}
