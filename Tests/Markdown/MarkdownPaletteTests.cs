namespace Ecng.Tests.Markdown;

using Ecng.Markdown;


[TestClass]
public class MarkdownPaletteTests : BaseTestClass
{
	[TestMethod]
	public void Text_IsLightOnADarkPanelAndDarkOnALightOne()
	{
		// The card takes the colour of whatever it landed on. Getting this backwards is not a shade off - it
		// is prose nobody can read.
		IsTrue(MarkdownPalette.IsDark(0x1E, 0x1E, 0x1E));
		IsFalse(MarkdownPalette.IsDark(0xFA, 0xFA, 0xFA));

		AreEqual(MarkdownPalette.OnDark, MarkdownPalette.TextFor(0x25, 0x25, 0x26));
		AreEqual(MarkdownPalette.OnLight, MarkdownPalette.TextFor(0xFF, 0xFF, 0xFF));
	}

	[TestMethod]
	public void Brightness_IsWeightedTheWayTheEyeReadsIt()
	{
		// A plain average calls saturated blue light and saturated yellow dark; both are wrong, and both
		// turn up in themes.
		IsTrue(MarkdownPalette.IsDark(0x00, 0x00, 0xFF));
		IsFalse(MarkdownPalette.IsDark(0xFF, 0xFF, 0x00));
	}
}
