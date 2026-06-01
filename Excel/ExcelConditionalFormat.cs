namespace Ecng.Excel;

/// <summary>
/// Line style of a conditional-formatting border.
/// </summary>
public enum ExcelBorderStyle
{
	/// <summary>No border.</summary>
	None,

	/// <summary>Hair line.</summary>
	Hair,

	/// <summary>Thin line.</summary>
	Thin,

	/// <summary>Medium line.</summary>
	Medium,

	/// <summary>Thick line.</summary>
	Thick,

	/// <summary>Dashed line.</summary>
	Dashed,

	/// <summary>Dotted line.</summary>
	Dotted,

	/// <summary>Double line.</summary>
	Double,
}

/// <summary>
/// Describes the visual format a conditional-formatting rule applies when its
/// condition is true. Every member is optional: a <see langword="null"/> value
/// (or <see cref="ExcelBorderStyle.None"/> for <see cref="Border"/>) leaves that
/// aspect untouched, so only the attributes you set are written into the rule's
/// differential format. Used by
/// <see cref="IExcelWorker.SetConditionalFormattingFormula(int,int,int,int,string,ExcelConditionalFormat)"/>.
/// </summary>
/// <remarks>
/// Covers fill (solid or patterned), font (color, bold, italic, underline, strikethrough,
/// size, name), number format and a uniform border. Per-side borders, cell alignment and
/// text wrapping are not supported yet.
/// </remarks>
public sealed class ExcelConditionalFormat
{
	/// <summary>Background fill color (hex code or name). For a solid fill this is the fill colour; for a pattern it is the colour behind the pattern.</summary>
	public string BackgroundColor { get; set; }

	/// <summary>Fill pattern. Defaults to <see cref="ExcelFillPattern.Solid"/> (a single-colour fill).</summary>
	public ExcelFillPattern FillPattern { get; set; } = ExcelFillPattern.Solid;

	/// <summary>Pattern (lines/dots) colour for a non-solid <see cref="FillPattern"/> (hex code or name). Defaults to black when a pattern is set and this is empty. Ignored for <see cref="ExcelFillPattern.Solid"/>.</summary>
	public string PatternColor { get; set; }

	/// <summary>Font (text) color (hex code or name).</summary>
	public string FontColor { get; set; }

	/// <summary>Bold font. <see langword="true"/> forces on, <see langword="false"/> forces off, <see langword="null"/> leaves unchanged.</summary>
	public bool? Bold { get; set; }

	/// <summary>Italic font. <see langword="true"/> forces on, <see langword="false"/> forces off, <see langword="null"/> leaves unchanged.</summary>
	public bool? Italic { get; set; }

	/// <summary>Single underline. <see langword="true"/> forces on, <see langword="false"/> forces off, <see langword="null"/> leaves unchanged.</summary>
	public bool? Underline { get; set; }

	/// <summary>Strikethrough. <see langword="true"/> forces on, <see langword="false"/> forces off, <see langword="null"/> leaves unchanged.</summary>
	public bool? Strikethrough { get; set; }

	/// <summary>Font size in points. <see langword="null"/> leaves unchanged.</summary>
	public double? FontSize { get; set; }

	/// <summary>Font family name (e.g. "Calibri"). <see langword="null"/> leaves unchanged.</summary>
	public string FontName { get; set; }

	/// <summary>
	/// Excel number format code (e.g. <c>0.00%</c>, <c>#,##0.00</c>, <c>"$"#,##0</c>).
	/// <see langword="null"/> leaves the cell's number format unchanged.
	/// </summary>
	public string NumberFormat { get; set; }

	/// <summary>
	/// Border line style applied to all four sides. <see cref="ExcelBorderStyle.None"/>
	/// (the default) writes no border.
	/// </summary>
	public ExcelBorderStyle Border { get; set; } = ExcelBorderStyle.None;

	/// <summary>Border color (hex code or name). Defaults to black when <see cref="Border"/> is set and this is empty.</summary>
	public string BorderColor { get; set; }
}
