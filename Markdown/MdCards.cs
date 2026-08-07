namespace Ecng.Markdown;

using System.Collections.Generic;

/// <summary>
/// One entry of a <see cref="MdCards"/> or <see cref="MdSteps"/> section.
/// </summary>
/// <param name="title">Heading the entry was introduced by, empty when it had none.</param>
/// <param name="children">Blocks that followed the heading.</param>
public sealed class MdCard(IReadOnlyList<MdInline> title, IReadOnlyList<MdBlock> children)
{
	/// <summary>
	/// Heading the entry was introduced by, empty when it had none.
	/// </summary>
	public IReadOnlyList<MdInline> Title { get; } = title;

	/// <summary>
	/// Blocks that followed the heading.
	/// </summary>
	public IReadOnlyList<MdBlock> Children { get; } = children;
}

/// <summary>
/// A set of cards, one per heading inside a ":::cards" section.
/// </summary>
/// <param name="cards">The cards, in source order.</param>
public sealed class MdCards(IReadOnlyList<MdCard> cards) : MdBlock
{
	/// <summary>
	/// The cards, in source order.
	/// </summary>
	public IReadOnlyList<MdCard> Cards { get; } = cards;
}

/// <summary>
/// A numbered walkthrough, one step per heading inside a ":::steps" section.
/// </summary>
/// <param name="steps">The steps, in the order they are to be followed.</param>
/// <remarks>
/// Grouped the same way as <see cref="MdCards"/>, and kept a separate node because the ordinal is part of
/// what the section means: a set of cards may be read in any order and a set of steps may not.
/// </remarks>
public sealed class MdSteps(IReadOnlyList<MdCard> steps) : MdBlock
{
	/// <summary>
	/// The steps, in the order they are to be followed.
	/// </summary>
	public IReadOnlyList<MdCard> Steps { get; } = steps;
}
