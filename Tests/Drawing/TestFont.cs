namespace Ecng.Tests.Drawing;

using System.Buffers.Binary;

/// <summary>
/// Assembles a TrueType font out of outlines given by a test, so that no font file is shipped or looked for.
/// </summary>
internal sealed class TestFont
{
	/// <summary>
	/// A point of an outline, in font units.
	/// </summary>
	public readonly record struct Point(int X, int Y, bool OnCurve = true);

	/// <summary>
	/// A glyph placed into a composite one. The four numbers of the transform are named as the format
	/// names them and are written in this order.
	/// </summary>
	public readonly record struct Component(int Glyph, int Dx, int Dy, double XScale = 1, double Scale01 = 0, double Scale10 = 0, double YScale = 1);

	private sealed class Glyph
	{
		public int Advance;
		public Point[][] Contours = [];
		public Component[] Components = [];
	}

	private const int _argsAreWords = 0x0001;
	private const int _argsAreXyValues = 0x0002;
	private const int _hasScale = 0x0008;
	private const int _moreComponents = 0x0020;
	private const int _hasXyScale = 0x0040;
	private const int _hasTwoByTwo = 0x0080;

	// Glyph 0 is what a font shows for a character it lacks: here, nothing.
	private readonly List<Glyph> _glyphs = [new()];
	private readonly SortedDictionary<int, int> _map = [];

	public int UnitsPerEm { get; init; } = 1000;
	public int Ascent { get; init; } = 800;
	public int Descent { get; init; } = -200;

	/// <summary>
	/// The family name for the name table; the font gets no name table without one.
	/// </summary>
	public string Family { get; init; }

	public string Subfamily { get; init; } = "Regular";

	/// <summary>
	/// Whether the character map is written in format 12, which reaches beyond U+FFFF, instead of format 4.
	/// </summary>
	public bool IsFormat12 { get; init; }

	/// <summary>
	/// A clockwise rectangle.
	/// </summary>
	public static Point[] Rectangle(int left, int bottom, int right, int top)
		=> [new(left, bottom), new(left, top), new(right, top), new(right, bottom)];

	/// <summary>
	/// Adds a glyph made of contours.
	/// </summary>
	/// <returns>The index of the glyph.</returns>
	public int AddGlyph(int advance, params Point[][] contours)
	{
		_glyphs.Add(new() { Advance = advance, Contours = contours });
		return _glyphs.Count - 1;
	}

	/// <summary>
	/// Gives glyph 0, shown for a character the font lacks, a width and an outline.
	/// </summary>
	public TestFont SetMissingGlyph(int advance, params Point[][] contours)
	{
		_glyphs[0] = new() { Advance = advance, Contours = contours };
		return this;
	}

	/// <summary>
	/// Maps 'I' to the glyph most tests draw: a rectangle from (100, 0) to (900, 800) with an advance of 1000.
	/// </summary>
	public TestFont MapLetterI()
		=> Map('I', AddGlyph(1000, Rectangle(100, 0, 900, 800)));

	/// <summary>
	/// Adds a glyph made of other glyphs.
	/// </summary>
	/// <returns>The index of the glyph.</returns>
	public int AddComposite(int advance, params Component[] components)
	{
		_glyphs.Add(new() { Advance = advance, Components = components });
		return _glyphs.Count - 1;
	}

	/// <summary>
	/// Maps a character to a glyph.
	/// </summary>
	public TestFont Map(int codePoint, int glyph)
	{
		_map[codePoint] = glyph;
		return this;
	}

	public byte[] Build()
	{
		var head = new byte[54];
		Put16(head, 18, UnitsPerEm);
		Put16(head, 50, 1); // long offsets in loca

		var hhea = new byte[36];
		Put16(hhea, 4, Ascent);
		Put16(hhea, 6, Descent);
		Put16(hhea, 34, _glyphs.Count);

		var maxp = new byte[6];
		Put32(maxp, 0, 0x00010000);
		Put16(maxp, 4, _glyphs.Count);

		var hmtx = new byte[_glyphs.Count * 4];

		for (var i = 0; i < _glyphs.Count; i++)
			Put16(hmtx, i * 4, _glyphs[i].Advance);

		var glyf = new List<byte>();
		var loca = new byte[(_glyphs.Count + 1) * 4];

		for (var i = 0; i < _glyphs.Count; i++)
		{
			Put32(loca, i * 4, glyf.Count);
			glyf.AddRange(Encode(_glyphs[i]));

			while (glyf.Count % 4 != 0)
				glyf.Add(0);
		}

		Put32(loca, _glyphs.Count * 4, glyf.Count);

		var tables = new List<(string tag, byte[] data)>
		{
			("head", head), ("hhea", hhea), ("maxp", maxp), ("cmap", CharacterMap()),
			("loca", loca), ("glyf", [.. glyf]), ("hmtx", hmtx),
		};

		if (Family is not null)
			tables.Add(("name", Names()));

		var font = new List<byte>(new byte[12 + tables.Count * 16]);
		var directory = new byte[12 + tables.Count * 16];
		Put32(directory, 0, 0x00010000);
		Put16(directory, 4, tables.Count);

		for (var i = 0; i < tables.Count; i++)
		{
			var record = 12 + i * 16;
			Encoding.ASCII.GetBytes(tables[i].tag).CopyTo(directory, record);
			Put32(directory, record + 8, font.Count);
			Put32(directory, record + 12, tables[i].data.Length);

			font.AddRange(tables[i].data);

			while (font.Count % 4 != 0)
				font.Add(0);
		}

		var result = font.ToArray();
		directory.CopyTo(result, 0);
		return result;
	}

	private static byte[] Encode(Glyph glyph)
	{
		if (glyph.Components.Length > 0)
		{
			var composite = new List<byte>();
			Add16(composite, -1);
			composite.AddRange(new byte[8]);

			for (var i = 0; i < glyph.Components.Length; i++)
			{
				var component = glyph.Components[i];
				var flags = _argsAreWords | _argsAreXyValues;

				if (i < glyph.Components.Length - 1)
					flags |= _moreComponents;

				var isDiagonal = component.Scale01 == 0 && component.Scale10 == 0;

				if (!isDiagonal)
					flags |= _hasTwoByTwo;
				else if (component.XScale != component.YScale)
					flags |= _hasXyScale;
				else if (component.XScale != 1)
					flags |= _hasScale;

				Add16(composite, flags);
				Add16(composite, component.Glyph);
				Add16(composite, component.Dx);
				Add16(composite, component.Dy);

				if ((flags & _hasTwoByTwo) != 0)
				{
					AddFraction(composite, component.XScale);
					AddFraction(composite, component.Scale01);
					AddFraction(composite, component.Scale10);
					AddFraction(composite, component.YScale);
				}
				else if ((flags & _hasXyScale) != 0)
				{
					AddFraction(composite, component.XScale);
					AddFraction(composite, component.YScale);
				}
				else if ((flags & _hasScale) != 0)
					AddFraction(composite, component.XScale);
			}

			return [.. composite];
		}

		if (glyph.Contours.Length == 0)
			return [];

		var simple = new List<byte>();
		Add16(simple, glyph.Contours.Length);
		simple.AddRange(new byte[8]);

		var points = glyph.Contours.SelectMany(c => c).ToArray();
		var last = -1;

		foreach (var contour in glyph.Contours)
		{
			last += contour.Length;
			Add16(simple, last);
		}

		// No instructions; a flag byte per point and coordinates as 16-bit steps from the point before.
		Add16(simple, 0);
		simple.AddRange(points.Select(p => (byte)(p.OnCurve ? 1 : 0)));

		for (var i = 0; i < points.Length; i++)
			Add16(simple, points[i].X - (i == 0 ? 0 : points[i - 1].X));

		for (var i = 0; i < points.Length; i++)
			Add16(simple, points[i].Y - (i == 0 ? 0 : points[i - 1].Y));

		return [.. simple];
	}

	private byte[] CharacterMap()
	{
		var map = new List<byte>();
		Add16(map, 0);
		Add16(map, 1);
		Add16(map, 3); // Windows

		if (IsFormat12)
		{
			Add16(map, 10); // full Unicode
			Add32(map, 12);

			Add16(map, 12);
			Add16(map, 0);
			Add32(map, 16 + _map.Count * 12);
			Add32(map, 0);
			Add32(map, _map.Count);

			foreach (var (codePoint, glyph) in _map)
			{
				Add32(map, codePoint);
				Add32(map, codePoint);
				Add32(map, glyph);
			}

			return [.. map];
		}

		Add16(map, 1); // Unicode BMP
		Add32(map, 12);

		// A segment per character and the closing one the format requires.
		var segments = _map.Count + 1;

		Add16(map, 4);
		Add16(map, 16 + segments * 8);
		Add16(map, 0);
		Add16(map, segments * 2);
		Add16(map, 0);
		Add16(map, 0);
		Add16(map, 0);

		foreach (var codePoint in _map.Keys)
			Add16(map, codePoint);

		Add16(map, 0xFFFF);
		Add16(map, 0);

		foreach (var codePoint in _map.Keys)
			Add16(map, codePoint);

		Add16(map, 0xFFFF);

		foreach (var (codePoint, glyph) in _map)
			Add16(map, glyph - codePoint);

		Add16(map, 1);

		for (var i = 0; i < segments; i++)
			Add16(map, 0);

		return [.. map];
	}

	private byte[] Names()
	{
		(int id, string text)[] names = [(1, Family), (2, Subfamily)];
		var strings = new List<byte>();
		var table = new List<byte>();

		Add16(table, 0);
		Add16(table, names.Length);
		Add16(table, 6 + names.Length * 12);

		foreach (var (id, text) in names)
		{
			var bytes = Encoding.BigEndianUnicode.GetBytes(text);

			Add16(table, 3); // Windows
			Add16(table, 1); // Unicode BMP
			Add16(table, 0x0409); // English
			Add16(table, id);
			Add16(table, bytes.Length);
			Add16(table, strings.Count);

			strings.AddRange(bytes);
		}

		return [.. table, .. strings];
	}

	private static void AddFraction(List<byte> data, double value)
		=> Add16(data, (int)Math.Round(value * 16384));

	private static void Add16(List<byte> data, int value)
	{
		data.Add((byte)(value >> 8));
		data.Add((byte)value);
	}

	private static void Add32(List<byte> data, int value)
	{
		Add16(data, value >> 16);
		Add16(data, value);
	}

	private static void Put16(byte[] data, int at, int value)
		=> BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(at), unchecked((ushort)value));

	private static void Put32(byte[] data, int at, int value)
		=> BinaryPrimitives.WriteInt32BigEndian(data.AsSpan(at), value);
}
