namespace Ecng.Markdown;

/// <summary>
/// A Designer schema the author referenced, resolved to the address it is fetched from.
/// </summary>
/// <param name="source">Address the schema JSON is fetched from.</param>
/// <param name="inlineSchema">Schema JSON written straight into the text, empty when referenced by address.</param>
public sealed class MdDiagram(string source, string inlineSchema) : MdBlock
{
	/// <summary>
	/// Address the schema JSON is fetched from.
	/// </summary>
	public string Source { get; } = source;

	/// <summary>
	/// Schema JSON written straight into the text.
	/// </summary>
	public string InlineSchema { get; } = inlineSchema;
}
