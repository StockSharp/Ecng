namespace Ecng.Markdown;

using System.Collections.Generic;

/// <summary>
/// Bulleted or numbered list.
/// </summary>
/// <param name="isOrdered">Whether the items are numbered.</param>
/// <param name="marker">Character the author numbered by, when they numbered by something unusual.</param>
/// <param name="items">List items.</param>
public sealed class MdList(bool isOrdered, char marker, IReadOnlyList<MdListItem> items) : MdBlock
{
	/// <summary>
	/// Whether the items are numbered.
	/// </summary>
	public bool IsOrdered { get; } = isOrdered;

	/// <summary>
	/// Character the author numbered by: '1' for digits, 'a' or 'A' for letters, 'i' or 'I' for roman.
	/// </summary>
	/// <remarks>
	/// Kept because a list numbered "a, b, c" and one numbered "1, 2, 3" are not the same list. A step that
	/// says "see point b" has nothing to point at once the letters have been turned into digits.
	/// </remarks>
	public char Marker { get; } = marker;

	/// <summary>
	/// List items.
	/// </summary>
	public IReadOnlyList<MdListItem> Items { get; } = items;
}
