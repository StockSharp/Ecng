namespace Ecng.Markdown.Extensions;

/// <summary>
/// An inline whose real content is not known while the document is being parsed -- a counter, an entity
/// reference, a diagram -- and which therefore renders as a token the fetch phase replaces later
/// (see Md2HtmlFormatter).
/// </summary>
/// <remarks>
/// The token belongs to the inline rather than to its renderer because the renderer is not the only reader:
/// a section that flattens its content to plain text (":::stats", ":::quote") has to keep the token as well,
/// or the value quietly disappears -- which is what happened to "@connector_count" inside a stats row.
/// </remarks>
public interface IPlaceholderInline
{
	/// <summary>The token standing in for the content until it is resolved.</summary>
	string Token { get; }
}
