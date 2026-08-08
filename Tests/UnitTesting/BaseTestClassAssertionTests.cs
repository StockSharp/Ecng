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
		// Arrays compare element-wise, not by reference, even though the generic
		// AreEqual<T>(T, T) overload is the one overload resolution picks for a typed array.
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
		// Distinct instances with equal elements count as equal, not as different.
		Throws<AssertFailedException>(() => AreNotEqual(new[] { "a" }, new[] { "a" }));
	}

	[TestMethod]
	public void AreEqual_Strings_StillCompareByValue()
	{
		// A string is IEnumerable but not ICollection, so it compares by value rather
		// than as a char sequence.
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
