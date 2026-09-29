namespace Ecng.Markdown.Extensions;

using Markdig.Extensions.CustomContainers;
using Markdig.Renderers;

/// <summary>
/// A host-defined ::: section rendered by <see cref="SectionContainerRenderer"/> when the container's info
/// matches <see cref="Name"/>.
/// </summary>
public interface ISectionBlock
{
	/// <summary>
	/// The container info this block answers to, e.g. "hero" for ":::hero". Compared case-insensitively.
	/// </summary>
	string Name { get; }

	/// <summary>
	/// Writes the whole section, including its outer element and children.
	/// </summary>
	/// <param name="renderer">The HTML renderer.</param>
	/// <param name="block">The container being rendered.</param>
	void Write(HtmlRenderer renderer, CustomContainer block);
}
