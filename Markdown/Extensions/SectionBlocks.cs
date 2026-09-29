namespace Ecng.Markdown.Extensions;

using Markdig.Extensions.CustomContainers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

/// <summary>
/// Helpers shared by the built-in ::: sections and the ones a host registers through
/// <see cref="SectionContainerRenderer.Register"/>.
/// </summary>
public static class SectionBlocks
{
	/// <summary>
	/// Writes an opening tag carrying the given classes plus the author's {#id .class} attributes. Of the
	/// author's other properties only data-*, aria-*, role and title are written, so event handlers and
	/// inline styles never reach the page.
	/// </summary>
	/// <param name="renderer">The HTML renderer.</param>
	/// <param name="tag">The element name.</param>
	/// <param name="obj">The markdown object whose attributes are written.</param>
	/// <param name="classes">The classes the section itself needs; may be empty.</param>
	public static void WriteOpenTag(HtmlRenderer renderer, string tag, MarkdownObject obj, string classes)
	{
		ArgumentNullException.ThrowIfNull(renderer);
		ArgumentNullException.ThrowIfNull(obj);

		if (tag.IsEmpty())
			throw new ArgumentNullException(nameof(tag));

		var attributes = obj.TryGetAttributes();

		renderer.Write('<').Write(tag);

		if (!(attributes?.Id).IsEmpty())
		{
			renderer.Write(" id=\"");
			renderer.WriteEscape(attributes.Id);
			renderer.Write('"');
		}

		// Markdig adds a fence's info to its classes ("cards", "language-diagram"); the caller decides itself
		// whether it wants it.
		var info = (obj as IFencedBlock)?.Info?.Trim();

		var allClasses = new[] { classes }
			.Concat((attributes?.Classes ?? []).Where(c => info.IsEmpty() || (c != info && c != $"language-{info}")))
			.Where(c => !c.IsEmptyOrWhiteSpace())
			.Select(c => c.Trim())
			.JoinSpace();

		if (!allClasses.IsEmpty())
		{
			renderer.Write(" class=\"");
			renderer.WriteEscape(allClasses);
			renderer.Write('"');
		}

		foreach (var property in attributes?.Properties ?? [])
		{
			if (!IsSafeProperty(property.Key))
				continue;

			renderer.Write(' ').Write(property.Key);

			if (property.Value is not null)
			{
				renderer.Write("=\"");
				renderer.WriteEscape(property.Value);
				renderer.Write('"');
			}
		}

		renderer.Write('>');
	}

	private static bool IsSafeProperty(string name)
		=> !name.IsEmpty()
			&& (name.StartsWithIgnoreCase("data-") || name.StartsWithIgnoreCase("aria-") || name.EqualsIgnoreCase("role") || name.EqualsIgnoreCase("title"))
			&& name.All(c => c is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '-' or '_' or '.' or ':');

	/// <summary>
	/// Tells whether the container's arguments (the words after its name) include the given one.
	/// </summary>
	/// <param name="block">The container.</param>
	/// <param name="name">The argument to look for, compared case-insensitively.</param>
	/// <returns><see langword="true"/> when the argument is present.</returns>
	public static bool HasArgument(CustomContainer block, string name)
	{
		ArgumentNullException.ThrowIfNull(block);

		var args = block.Arguments;

		return !args.IsEmpty() && args.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries).Any(a => a.EqualsIgnoreCase(name));
	}

	/// <summary>
	/// Flattens an inline tree to plain text, keeping the authored line breaks and the tokens of the
	/// placeholders (counters, entity references, diagrams) that are resolved after rendering.
	/// </summary>
	/// <param name="container">The inline tree.</param>
	/// <returns>The text.</returns>
	public static string GetText(ContainerInline container)
	{
		ArgumentNullException.ThrowIfNull(container);

		var builder = new StringBuilder();

		Append(container);

		return builder.ToString();

		void Append(ContainerInline inlines)
		{
			foreach (var inline in inlines)
			{
				switch (inline)
				{
					case LiteralInline literal:
						builder.Append(literal.Content.ToString());
						break;
					case LineBreakInline:
						builder.AppendLine();
						break;
					// Keeping the token is what lets "@connector_count | connectors" read as a live number.
					case IPlaceholderInline placeholder:
						builder.Append(placeholder.Token);
						break;
					case ContainerInline nested:
						Append(nested);
						break;
				}
			}
		}
	}
}
