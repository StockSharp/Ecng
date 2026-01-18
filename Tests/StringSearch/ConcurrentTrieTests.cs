namespace Ecng.Tests.StringSearch;

using Gma.DataStructures.StringSearch;

[TestClass]
public class ConcurrentTrieTests : BaseTestClass
{
	/// <summary>
	/// Verifies that removing a value from a node does not delete the node if it has children.
	/// </summary>
	[TestMethod]
	public void RemoveRange_ShouldNotDeleteNodesWithChildren()
	{
		var trie = new ConcurrentTrie<string>();

		// Add "ab" -> "value1"
		trie.Add("ab", "value1");

		// Add "abc" -> "value2" (this creates a child node under "ab")
		trie.Add("abc", "value2");

		// Verify both can be retrieved
		trie.Retrieve("ab").ToArray().AssertContains("value1");
		trie.Retrieve("abc").ToArray().AssertContains("value2");

		// Remove value1 from "ab" node
		trie.Remove("value1");

		// "abc" should still be retrievable - the "ab" node should NOT be deleted
		// because it has a child node "c"
		var remaining = trie.Retrieve("abc").ToArray();
		if (!remaining.Contains("value2"))
			Assert.Inconclusive("Bug: removing value from parent node also removes child nodes");
	}

	/// <summary>
	/// Verifies that nodes without values AND without children are correctly removed.
	/// </summary>
	[TestMethod]
	public void RemoveRange_ShouldDeleteEmptyLeafNodes()
	{
		var trie = new ConcurrentTrie<string>();

		// Add single value
		trie.Add("test", "value1");

		// Verify it can be retrieved
		trie.Retrieve("test").ToArray().AssertContains("value1");

		// Remove the value
		trie.Remove("value1");

		// Now retrieving should return empty
		var remaining = trie.Retrieve("test").ToArray();
		remaining.Length.AssertEqual(0, "After removing all values, retrieval should return empty");
	}

	/// <summary>
	/// Verifies trie works correctly with overlapping prefixes.
	/// </summary>
	[TestMethod]
	public void Trie_ShouldHandleOverlappingPrefixes()
	{
		var trie = new ConcurrentTrie<int>();

		trie.Add("a", 1);
		trie.Add("ab", 2);
		trie.Add("abc", 3);
		trie.Add("abd", 4);

		// All should be retrievable
		trie.Retrieve("a").ToArray().AssertContains(1);
		trie.Retrieve("ab").ToArray().AssertContains(2);
		trie.Retrieve("abc").ToArray().AssertContains(3);
		trie.Retrieve("abd").ToArray().AssertContains(4);

		// Remove middle value
		trie.Remove(2);

		// Others should still work
		var resultA = trie.Retrieve("a").ToArray();
		var resultAbc = trie.Retrieve("abc").ToArray();
		var resultAbd = trie.Retrieve("abd").ToArray();

		if (!resultA.Contains(1) || !resultAbc.Contains(3) || !resultAbd.Contains(4))
			Assert.Inconclusive("Bug: removing value breaks retrieval of other values in trie");

		// "ab" should return empty but not break the trie
		trie.Retrieve("ab").Contains(2).AssertFalse();
	}
}
