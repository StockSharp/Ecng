namespace Ecng.Markdown;

using System.Collections.Generic;

/// <summary>
/// Where a section's content sits across the width available to it.
/// </summary>
public enum MdAlignments
{
	/// <summary>
	/// Against the leading edge.
	/// </summary>
	Left,

	/// <summary>
	/// Centred.
	/// </summary>
	Center,

	/// <summary>
	/// Against the trailing edge.
	/// </summary>
	Right,
}

/// <summary>
/// Content the author placed across the width, from a ":::center", ":::left" or ":::right" section.
/// </summary>
/// <param name="alignment">Where the content sits.</param>
/// <param name="children">The content.</param>
public sealed class MdSection(MdAlignments alignment, IReadOnlyList<MdBlock> children) : MdBlock
{
	/// <summary>
	/// Where the content sits.
	/// </summary>
	public MdAlignments Alignment { get; } = alignment;

	/// <summary>
	/// The content.
	/// </summary>
	public IReadOnlyList<MdBlock> Children { get; } = children;
}

/// <summary>
/// Calls to action, from a ":::cta" section - every link in it, the first one the primary one.
/// </summary>
/// <param name="links">The links, in source order.</param>
public sealed class MdCta(IReadOnlyList<MdLink> links) : MdBlock
{
	/// <summary>
	/// The links, in source order.
	/// </summary>
	public IReadOnlyList<MdLink> Links { get; } = links;
}

/// <summary>
/// A testimonial, from a ":::quote" section, with the trailing em-dash line lifted out as its attribution.
/// </summary>
/// <param name="children">What is being quoted.</param>
/// <param name="attribution">Who said it, empty when the author gave no line.</param>
public sealed class MdTestimonial(IReadOnlyList<MdBlock> children, string attribution) : MdBlock
{
	/// <summary>
	/// What is being quoted.
	/// </summary>
	public IReadOnlyList<MdBlock> Children { get; } = children;

	/// <summary>
	/// Who said it, empty when the author gave no line.
	/// </summary>
	public string Attribution { get; } = attribution;
}

/// <summary>
/// Screenshots of one screen meant to be seen together, from a ":::split" section.
/// </summary>
/// <param name="layers">The layers, the first one the base.</param>
/// <remarks>
/// A browser clips the layers diagonally so one frame shows a light and a dark theme at once. Nothing here
/// can clip a control that way, so a host stacks them - which still shows both looks, which is the point.
/// </remarks>
public sealed class MdSplit(IReadOnlyList<IReadOnlyList<MdBlock>> layers) : MdBlock
{
	/// <summary>
	/// The layers, the first one the base.
	/// </summary>
	public IReadOnlyList<IReadOnlyList<MdBlock>> Layers { get; } = layers;
}
