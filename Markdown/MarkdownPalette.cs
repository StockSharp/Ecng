namespace Ecng.Markdown;

/// <summary>
/// Decides what colour a card's text has to be to be readable on the background it landed on.
/// </summary>
/// <remarks>
/// Shared because it is a judgement about contrast, not a drawing decision, and because the two hosts
/// answering it separately is how one of them ends up with black prose on a dark card. Everything the
/// renderers paint themselves is already translucent grey, which adapts on its own; text cannot, because it
/// has to stay opaque to be read.
/// </remarks>
public static class MarkdownPalette
{
	/// <summary>
	/// Text on a dark background - the warm off-white a page uses rather than pure white, which glares.
	/// </summary>
	public static (byte R, byte G, byte B) OnDark { get; } = (0xE8, 0xE6, 0xE3);

	/// <summary>
	/// Text on a light background.
	/// </summary>
	public static (byte R, byte G, byte B) OnLight { get; } = (0x1A, 0x1A, 0x1A);

	/// <summary>
	/// Whether a background of this colour needs light text.
	/// </summary>
	/// <param name="r">Red.</param>
	/// <param name="g">Green.</param>
	/// <param name="b">Blue.</param>
	/// <returns><see langword="true"/> when the background is dark.</returns>
	/// <remarks>
	/// Weighted by how much each channel contributes to what the eye reads as brightness, so a saturated
	/// blue counts as dark and a saturated yellow as light - which is what a reader sees, and what a plain
	/// average of the three would get wrong.
	/// </remarks>
	public static bool IsDark(byte r, byte g, byte b)
		=> (0.299 * r + 0.587 * g + 0.114 * b) < 140;

	/// <summary>
	/// The text colour for a background of this colour.
	/// </summary>
	/// <param name="r">Red.</param>
	/// <param name="g">Green.</param>
	/// <param name="b">Blue.</param>
	/// <returns>Colour the prose is set in.</returns>
	public static (byte R, byte G, byte B) TextFor(byte r, byte g, byte b)
		=> IsDark(r, g, b) ? OnDark : OnLight;
}
