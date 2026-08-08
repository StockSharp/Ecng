namespace Ecng.Tests;

using System.Collections.Generic;

using Ecng.UnitTesting;

/// <summary>
/// The assertion surface <see cref="BaseTestClass"/> exposes to every test in the workspace.
/// </summary>
[TestClass]
public class BaseTestClassAssertionTests : BaseTestClass
{
	[TestMethod]
	public void AreEqual_Arrays_ComparesElements()
	{
		// The trap this guards: BaseTestClass has an AreEqual(ICollection, ICollection)
		// overload, but it is unreachable for a typed argument - AreEqual<T>(T, T) binds
		// string[] by identity conversion and wins resolution outright. Left to the generic
		// overload the comparison is Assert.AreEqual, i.e. reference equality, so two equal
		// arrays report "expected: [\"a\"] actual: [\"a\"]" and fail.
		AreEqual(new[] { "a", "b" }, new[] { "a", "b" });
		AreEqual(new[] { 1, 2, 3 }, new[] { 1, 2, 3 });
	}

	[TestMethod]
	public void AreEqual_Lists_ComparesElements()
		=> AreEqual(new List<int> { 1, 2 }, new List<int> { 1, 2 });

	[TestMethod]
	public void AreEqual_DifferentElements_Fails()
	{
		Throws<AssertFailedException>(() => AreEqual(new[] { "a" }, new[] { "b" }));
		Throws<AssertFailedException>(() => AreEqual(new[] { "a" }, new[] { "a", "b" }));
	}

	[TestMethod]
	public void AreEqual_OrderMatters()
		=> Throws<AssertFailedException>(() => AreEqual(new[] { "a", "b" }, new[] { "b", "a" }));

	[TestMethod]
	public void AreNotEqual_Arrays_ComparesElements()
	{
		AreNotEqual(new[] { "a" }, new[] { "b" });
		// Equal by element: two distinct instances must NOT count as different just
		// because they are different objects.
		Throws<AssertFailedException>(() => AreNotEqual(new[] { "a" }, new[] { "a" }));
	}

	[TestMethod]
	public void AreEqual_Strings_StillCompareByValue()
	{
		// A string is IEnumerable but not ICollection, so it must never fall into the
		// element-wise path and start reporting a char sequence.
		AreEqual("abc", "abc");
		Throws<AssertFailedException>(() => AreEqual("abc", "abd"));
	}

	[TestMethod]
	public void AreEqual_NullAgainstCollection_Fails()
	{
		Throws<AssertFailedException>(() => AreEqual(null, new[] { "a" }));
		Throws<AssertFailedException>(() => AreEqual(new[] { "a" }, null));
	}

	[TestMethod]
	public void AreEqual_BothNull_Passes()
		=> AreEqual<string[]>(null, null);

	[TestMethod]
	public void AreEqual_Scalars_Unaffected()
	{
		AreEqual(1, 1);
		AreEqual(1.5m, 1.5m);
		Throws<AssertFailedException>(() => AreEqual(1, 2));
	}
}
