namespace Ecng.Markdown;

/// <summary>
/// A piece of rendered HTML cut at the widget markers (see <see cref="MarkdownWidget.Split"/>): either markup to
/// write as is, or the widget that stands in its place.
/// </summary>
/// <param name="Html">The markup, or <see langword="null"/> for a widget.</param>
/// <param name="Widget">The widget, or <see langword="null"/> for markup.</param>
public record MarkdownWidgetPart(string Html, MarkdownWidget Widget);
