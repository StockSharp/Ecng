namespace Ecng.Markdown;

using System.Collections.Generic;

/// <summary>
/// Emphasised fragment.
/// </summary>
/// <param name="kind">How the fragment is emphasised.</param>
/// <param name="children">Nested inlines.</param>
public sealed class MdEmphasis(MdEmphasisKinds kind, IReadOnlyList<MdInline> children) : MdInline
{
	/// <summary>
	/// How the fragment is emphasised.
	/// </summary>
	public MdEmphasisKinds Kind { get; } = kind;

	/// <summary>
	/// Nested inlines.
	/// </summary>
	public IReadOnlyList<MdInline> Children { get; } = children;
}
