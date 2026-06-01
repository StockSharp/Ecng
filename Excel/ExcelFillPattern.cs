namespace Ecng.Excel;

/// <summary>
/// Fill pattern applied behind a cell or by a conditional-formatting rule. Mirrors the
/// OOXML pattern set. <see cref="Solid"/> fills with a single colour; every other value
/// is a two-colour pattern whose lines/dots take the pattern colour and whose background
/// takes the fill colour.
/// </summary>
public enum ExcelFillPattern
{
	/// <summary>No fill.</summary>
	None,

	/// <summary>Solid single-colour fill (the default).</summary>
	Solid,

	/// <summary>50% gray.</summary>
	MediumGray,

	/// <summary>75% gray.</summary>
	DarkGray,

	/// <summary>25% gray.</summary>
	LightGray,

	/// <summary>12.5% gray.</summary>
	Gray125,

	/// <summary>6.25% gray.</summary>
	Gray0625,

	/// <summary>Dark horizontal stripes.</summary>
	DarkHorizontal,

	/// <summary>Dark vertical stripes.</summary>
	DarkVertical,

	/// <summary>Dark diagonal stripes going down.</summary>
	DarkDown,

	/// <summary>Dark diagonal stripes going up.</summary>
	DarkUp,

	/// <summary>Dark grid.</summary>
	DarkGrid,

	/// <summary>Dark trellis.</summary>
	DarkTrellis,

	/// <summary>Light horizontal stripes.</summary>
	LightHorizontal,

	/// <summary>Light vertical stripes.</summary>
	LightVertical,

	/// <summary>Light diagonal stripes going down.</summary>
	LightDown,

	/// <summary>Light diagonal stripes going up.</summary>
	LightUp,

	/// <summary>Light grid.</summary>
	LightGrid,

	/// <summary>Light trellis.</summary>
	LightTrellis,
}
