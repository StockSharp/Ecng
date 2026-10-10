namespace Ecng.Drawing;

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;

/// <summary>
/// Minimal managed TrueType 'glyf' outline reader and quadratic rasterizer.
/// Accepts .ttf fonts with Unicode cmap format 4/12 and simple/composite outlines.
/// No platform text engine or managed font package is used.
/// </summary>
internal sealed class TrueTypeFont
{
	private readonly byte[] _file;
	private readonly Dictionary<string, (int start, int length)> _tables = new();
	private readonly int _cmap, _glyf, _loca, _hmtx, _metricsCount, _numGlyphs, _locaFormat;
	private readonly int _unitsPerEm, _ascent, _descent;
	private readonly Dictionary<int, Glyph> _cache = new();

	// The most points one glyph may have, whether its own or gathered from the glyphs it is made of.
	private const int _maxPoints = 30000;

	// With fewer pixels to the em a line of text is a smear.
	private const double _smallestEm = 6;

	private readonly record struct Point(double X, double Y, bool OnCurve);
	private sealed class Glyph
	{
		public int Advance;
		public int Points;
		public List<List<Point>> Contours = [];
	}

	private readonly record struct Line(double X1, double Y1, double X2, double Y2);

	/// <summary>
	/// Opens the font of a file.
	/// </summary>
	public static TrueTypeFont FromFile(string path)
	{
		if (!File.Exists(path))
			throw new FileNotFoundException("TrueType font file was not found.", path);

		return new(File.ReadAllBytes(path));
	}

	/// <summary>
	/// Opens the font the installed ones offer for a family, and the built-in font if they offer none.
	/// </summary>
	public static TrueTypeFont OfFamily(FontCatalog installed, string family)
		=> new(installed.Resolve(family) is { } path ? File.ReadAllBytes(path) : BuiltInFont.Data);

	/// <summary>
	/// Reads the tables of a font file. The bytes are kept and read from as glyphs are asked for.
	/// </summary>
	public TrueTypeFont(byte[] file)
	{
		_file = file;
		if (_file.Length > 30_000_000 || _file.Length < 12)
			throw new InvalidDataException("TrueType font has an invalid length.");

		var signature = U32(0);
		if (signature != 0x00010000 && signature != 0x74727565)
			throw new NotSupportedException("Only outline-based TrueType (.ttf), not OpenType CFF or font collections, is supported.");

		var count = U16(4);
		Require(12, count * 16);
		for (var i = 0; i < count; i++)
		{
			var pos = 12 + i * 16;
			var tag = Encoding.ASCII.GetString(_file, pos, 4);
			var offset = SafeU32(U32(pos + 8));
			var length = SafeU32(U32(pos + 12));
			Require(offset, length);
			_tables[tag] = (offset, length);
		}

		var head = Table("head", 54);
		var hhea = Table("hhea", 36);
		var maxp = Table("maxp", 6);
		var cmap = Table("cmap", 4);
		_loca = Table("loca", 2);
		_glyf = Table("glyf", 0);
		_hmtx = Table("hmtx", 4);

		_unitsPerEm = U16(head + 18);
		_ascent = I16(hhea + 4);
		_descent = I16(hhea + 6);
		_locaFormat = I16(head + 50);
		_metricsCount = U16(hhea + 34);
		_numGlyphs = U16(maxp + 4);

		if (_unitsPerEm == 0 || _metricsCount < 1 || _metricsCount > _numGlyphs ||
			_locaFormat is not (0 or 1) || _numGlyphs == 0)
			throw new InvalidDataException("TrueType font metrics are invalid.");

		Require(_hmtx, _metricsCount * 4 + (_numGlyphs - _metricsCount) * 2);
		Require(_loca, (_numGlyphs + 1) * (_locaFormat == 0 ? 2 : 4));
		_cmap = SelectUnicodeCmap(cmap);
	}

	/// <summary>
	/// Renders a line of text for the bottom right corner of a canvas, as large as asked or as fits.
	/// </summary>
	/// <param name="width">Width of the canvas in pixels.</param>
	/// <param name="height">Height of the canvas in pixels.</param>
	/// <param name="text">The line.</param>
	/// <param name="size">The size asked for, in points.</param>
	/// <param name="margin">The margin asked for, in pixels.</param>
	/// <param name="opacity">How opaque the text is, 0..255.</param>
	/// <param name="mask">The line as it is to be drawn.</param>
	/// <returns><see langword="false"/> if the canvas has no room for the line, or no pixel of the line would show.</returns>
	/// <exception cref="ArgumentException">None of the characters has an outline in this font.</exception>
	public bool TryRender(int width, int height, string text, float size, int margin, byte opacity, out TextMask mask)
	{
		mask = null;

		var glyphs = new List<Glyph>();

		foreach (var rune in text.EnumerateRunes())
			glyphs.Add(LoadGlyph(Map(rune.Value), 0));

		if (glyphs.TrueForAll(x => x.Points == 0))
			throw new ArgumentException("Watermark has no printable characters.", nameof(text));

		// The room the line takes, in font units from its origin on the baseline: the advances of its glyphs
		// by the ascent and descent of the font, widened by every outline that reaches out of that.
		double left = 0, right = 0, top = _ascent, bottom = _descent;
		var advance = 0L;

		foreach (var glyph in glyphs)
		{
			foreach (var contour in glyph.Contours)
				foreach (var point in contour)
				{
					left = Math.Min(left, advance + point.X);
					right = Math.Max(right, advance + point.X);
					top = Math.Max(top, point.Y);
					bottom = Math.Min(bottom, point.Y);
				}

			advance += glyph.Advance;
		}

		right = Math.Max(right, advance);

		// The margin gives way on a small picture: a quarter of the smaller side is the most it takes.
		margin = Math.Min(margin, Math.Min(width, height) / 4);

		var maxWidth = width - 2 * margin;
		var maxHeight = height - 2 * margin;

		// FontSize is points, and 96dpi defines a stable cross-platform point conversion.
		var wanted = size * (96.0 / 72.0) / _unitsPerEm;
		var scale = Math.Min(wanted, Math.Min(maxWidth / Math.Max(1, right - left), maxHeight / Math.Max(1, top - bottom)) * 0.95);

		// The text is made smaller to fit, but not into a smear; a size asked for below that is kept.
		if (scale < Math.Min(wanted, _smallestEm / _unitsPerEm))
			return false;

		var originX = width - margin - right * scale;
		var baseline = height - margin + bottom * scale;

		var boxLeft = Math.Max(0, (int)Math.Floor(originX + left * scale));
		var boxTop = Math.Max(0, (int)Math.Floor(baseline - top * scale));
		var boxWidth = Math.Min(width - 1, (int)Math.Ceiling(originX + right * scale)) - boxLeft + 1;
		var boxHeight = Math.Min(height - 1, (int)Math.Ceiling(baseline - bottom * scale)) - boxTop + 1;

		// A pixel is sampled at four points; a bit for each of them that lies inside an outline.
		var samples = new byte[boxWidth * boxHeight];

		foreach (var glyph in glyphs)
		{
			Fill(Flatten(glyph, originX, baseline, scale), samples, boxLeft, boxTop, boxWidth, boxHeight);
			originX += glyph.Advance * scale;
		}

		var isBlank = true;

		for (var i = 0; i < samples.Length; i++)
		{
			samples[i] = (byte)(opacity * BitOperations.PopCount(samples[i]) / 4);
			isBlank &= samples[i] == 0;
		}

		if (isBlank)
			return false;

		mask = new(boxLeft, boxTop, boxWidth, samples);
		return true;
	}

	// Turns the outline of a glyph into straight segments in the pixels of the canvas.
	private static List<Line> Flatten(Glyph glyph, double originX, double baseline, double scale)
	{
		var segments = new List<Line>();

		foreach (var contour in glyph.Contours)
		{
			if (contour.Count == 0) continue;

			Point start;
			var first = contour[0];
			var last = contour[^1];
			if (first.OnCurve) start = first;
			else if (last.OnCurve) start = last;
			else start = Mid(first, last);

			var current = start;
			for (var i = 0; i < contour.Count;)
			{
				var node = contour[i];
				if (node.OnCurve)
				{
					AddSegment(current, node);
					current = node;
					i++;
				}
				else
				{
					var next = contour[(i + 1) % contour.Count];
					var target = next.OnCurve ? next : Mid(node, next);
					var flatness = (Math.Abs(current.X - node.X) + Math.Abs(current.Y - node.Y) +
						Math.Abs(node.X - target.X) + Math.Abs(node.Y - target.Y)) * scale;
					var subdivisions = Math.Clamp((int)Math.Ceiling(flatness / 3), 4, 64);
					var from = current;

					for (var t = 1; t <= subdivisions; t++)
					{
						var s = (double)t / subdivisions;
						var reverse = 1 - s;
						var to = new Point(
							reverse * reverse * current.X + 2 * reverse * s * node.X + s * s * target.X,
							reverse * reverse * current.Y + 2 * reverse * s * node.Y + s * s * target.Y, true);
						AddSegment(from, to);
						from = to;
					}

					current = target;
					i += next.OnCurve ? 2 : 1;
				}
			}
			AddSegment(current, start);

			void AddSegment(Point a, Point b)
			{
				var x1 = originX + a.X * scale;
				var y1 = baseline - a.Y * scale;
				var x2 = originX + b.X * scale;
				var y2 = baseline - b.Y * scale;
				// A horizontal segment is crossed by no scanline; every other one closes the outline.
				if (y1 != y2)
					segments.Add(new Line(x1, y1, x2, y2));
			}
		}

		return segments;
	}

	// Marks the sampling points of a box of pixels that lie inside an outline. A point is inside if
	// the outlines wind around it: contours of one direction add up where they overlap, a contour of
	// the other direction cuts a hole.
	private static void Fill(List<Line> segments, byte[] samples, int boxLeft, int boxTop, int boxWidth, int boxHeight)
	{
		if (segments.Count == 0)
			return;

		var left = Math.Max(boxLeft, (int)Math.Floor(segments.Min(s => Math.Min(s.X1, s.X2))));
		var right = Math.Min(boxLeft + boxWidth - 1, (int)Math.Ceiling(segments.Max(s => Math.Max(s.X1, s.X2))));
		var top = Math.Max(boxTop, (int)Math.Floor(segments.Min(s => Math.Min(s.Y1, s.Y2))));
		var bottom = Math.Min(boxTop + boxHeight - 1, (int)Math.Ceiling(segments.Max(s => Math.Max(s.Y1, s.Y2))));

		// Where the segments cross the row being sampled, and which way each of them goes there.
		var crossings = new double[segments.Count];
		var turns = new int[segments.Count];

		for (var y = top; y <= bottom; y++)
			for (var sampleY = 0; sampleY < 2; sampleY++)
			{
				var yy = y + (sampleY + 0.5) / 2;
				var count = 0;
				var winding = 0;

				foreach (var s in segments)
				{
					if ((s.Y1 > yy) == (s.Y2 > yy))
						continue;

					crossings[count] = (s.X2 - s.X1) * (yy - s.Y1) / (s.Y2 - s.Y1) + s.X1;
					turns[count] = s.Y2 > s.Y1 ? 1 : -1;
					winding += turns[count++];
				}

				if (count == 0)
					continue;

				Array.Sort(crossings, turns, 0, count);

				// The winding of a point is the sum of the crossings to the right of it:
				// going right along the row, a point leaves crossings behind.
				var passed = 0;

				for (var x = left; x <= right; x++)
					for (var sampleX = 0; sampleX < 2; sampleX++)
					{
						var xx = x + (sampleX + 0.5) / 2;

						while (passed < count && !(xx < crossings[passed]))
							winding -= turns[passed++];

						if (winding != 0)
							samples[(y - boxTop) * boxWidth + x - boxLeft] |= (byte)(1 << (sampleY * 2 + sampleX));
					}
			}
	}

	private Glyph LoadGlyph(int glyphId, int depth)
	{
		if (depth > 12)
			throw new InvalidDataException("TrueType composite glyph nesting is too deep.");
		if (_cache.TryGetValue(glyphId, out var cached)) return cached;

		if (glyphId < 0 || glyphId >= _numGlyphs)
			throw new InvalidDataException("Invalid TrueType glyph index.");

		var glyph = new Glyph { Advance = U16(_hmtx + Math.Min(glyphId, _metricsCount - 1) * 4) };
		var offset = _loca + glyphId * (_locaFormat == 0 ? 2 : 4);
		var start = _locaFormat == 0 ? U16(offset) * 2 : SafeU32(U32(offset));
		var end = _locaFormat == 0 ? U16(offset + 2) * 2 : SafeU32(U32(offset + 4));

		if (start == end) { _cache[glyphId] = glyph; return glyph; }
		if (end < start || end > _tables["glyf"].length)
			throw new InvalidDataException("Invalid TrueType glyph location.");

		var pos = _glyf + start;
		Require(pos, 10);
		var contourCount = I16(pos);
		pos += 10;

		if (contourCount >= 0)
		{
			Require(pos, contourCount * 2 + 2);
			var ends = new int[contourCount];
			for (var i = 0; i < contourCount; i++) { ends[i] = U16(pos); pos += 2; }

			var points = contourCount == 0 ? 0 : ends[^1] + 1;
			if (points > _maxPoints)
				throw new InvalidDataException("TrueType glyph is too complex.");

			glyph.Points = points;

			var instructionLength = U16(pos); pos += 2;
			Require(pos, instructionLength);
			pos += instructionLength;

			var flags = new byte[points];
			for (var i = 0; i < points; i++)
			{
				Require(pos, 1);
				var flag = _file[pos++];
				flags[i] = flag;
				if ((flag & 8) != 0)
				{
					Require(pos, 1);
					var repetitions = _file[pos++];
					if (i + repetitions >= points) throw new InvalidDataException("Bad TrueType flag run.");
					for (var j = 0; j < repetitions; j++) flags[++i] = flag;
				}
			}

			var xx = new int[points];
			var yy = new int[points];
			var coordinate = 0;
			for (var i = 0; i < points; i++)
			{
				var flag = flags[i];
				if ((flag & 2) != 0) { Require(pos, 1); var delta = _file[pos++]; coordinate += (flag & 16) != 0 ? delta : -delta; }
				else if ((flag & 16) == 0) { Require(pos, 2); coordinate += I16(pos); pos += 2; }
				xx[i] = coordinate;
			}

			coordinate = 0;
			for (var i = 0; i < points; i++)
			{
				var flag = flags[i];
				if ((flag & 4) != 0) { Require(pos, 1); var delta = _file[pos++]; coordinate += (flag & 32) != 0 ? delta : -delta; }
				else if ((flag & 32) == 0) { Require(pos, 2); coordinate += I16(pos); pos += 2; }
				yy[i] = coordinate;
			}

			var from = 0;
			foreach (var to in ends)
			{
				if (to < from || to >= points)
					throw new InvalidDataException("Bad TrueType contour indices.");

				var contour = new List<Point>(to - from + 1);
				for (var i = from; i <= to; i++)
					contour.Add(new Point(xx[i], yy[i], (flags[i] & 1) != 0));

				glyph.Contours.Add(contour);
				from = to + 1;
			}
		}
		else
		{
			var more = true;
			while (more)
			{
				Require(pos, 4);
				var flags = U16(pos); pos += 2;
				var childId = U16(pos); pos += 2;
				if ((flags & 2) == 0)
					throw new NotSupportedException("TrueType composite point attachments are not supported.");

				int dx, dy;
				if ((flags & 1) != 0)
				{
					Require(pos, 4);
					dx = I16(pos); dy = I16(pos + 2); pos += 4;
				}
				else
				{
					Require(pos, 2);
					dx = (sbyte)_file[pos++]; dy = (sbyte)_file[pos++];
				}

				double a = 1, b = 0, c = 0, d = 1;
				if ((flags & 8) != 0)
				{
					Require(pos, 2);
					a = d = I16(pos) / 16384.0;
					pos += 2;
				}
				else if ((flags & 64) != 0)
				{
					Require(pos, 4);
					a = I16(pos) / 16384.0;
					d = I16(pos + 2) / 16384.0;
					pos += 4;
				}
				else if ((flags & 128) != 0)
				{
					Require(pos, 8);
					a = I16(pos) / 16384.0;
					b = I16(pos + 2) / 16384.0;
					c = I16(pos + 4) / 16384.0;
					d = I16(pos + 6) / 16384.0;
					pos += 8;
				}

				var child = LoadGlyph(childId, depth + 1);

				// Components may be composite themselves: without a bound a few of them multiply into more outlines than there is memory.
				glyph.Points += child.Points;
				if (glyph.Points > _maxPoints)
					throw new InvalidDataException("TrueType glyph is too complex.");

				// The four numbers are stored as xscale, scale01, scale10, yscale: x' = xscale * x + scale10 * y.
				foreach (var contour in child.Contours)
					glyph.Contours.Add(contour.Select(p => new Point(a * p.X + c * p.Y + dx, b * p.X + d * p.Y + dy, p.OnCurve)).ToList());

				more = (flags & 32) != 0;
			}
		}

		_cache[glyphId] = glyph;
		return glyph;
	}

	private int SelectUnicodeCmap(int cmap)
	{
		var tables = U16(cmap + 2);
		Require(cmap + 4, tables * 8);
		var best = -1;
		var score = -1;

		for (var i = 0; i < tables; i++)
		{
			var pos = cmap + 4 + i * 8;
			var platform = U16(pos);
			var encoding = U16(pos + 2);
			var location = SafeU32(U32(pos + 4));
			var candidate = cmap + location;
			if (candidate < 0 || candidate + 2 > _file.Length || !(platform == 0 || (platform == 3 && encoding is 1 or 10)))
				continue;

			var format = U16(candidate);
			var rank = format == 12 ? 3 : format == 4 ? 2 : -1;
			if (rank > score)
			{
				score = rank;
				best = candidate;
			}
		}

		if (best < 0)
			throw new NotSupportedException("Unicode TrueType cmap format 4 or 12 is required.");
		return best;
	}

	private int Map(int codepoint)
	{
		var format = U16(_cmap);
		if (format == 12)
		{
			var count = SafeU32(U32(_cmap + 12));
			if (count > int.MaxValue / 12) throw new InvalidDataException("TrueType cmap group count exceeds supported size.");
			Require(_cmap + 16, count * 12);
			var lo = 0;
			var hi = count - 1;
			while (lo <= hi)
			{
				var i = lo + (hi - lo) / 2;
				var at = _cmap + 16 + 12 * i;
				var first = U32(at);
				var last = U32(at + 4);
				if (codepoint < first) hi = i - 1;
				else if (codepoint > last) lo = i + 1;
				else
				{
					var mapped = (long)U32(at + 8) + codepoint - first;
					if (mapped < 0 || mapped > int.MaxValue)
						throw new InvalidDataException("TrueType cmap glyph index overflows.");
					return (int)mapped;
				}
			}
			return 0;
		}

		if (codepoint > 0xFFFF) return 0;
		var segments = U16(_cmap + 6) / 2;
		var ends = _cmap + 14;
		var starts = ends + 2 * segments + 2;
		var deltas = starts + 2 * segments;
		var offsets = deltas + 2 * segments;
		Require(offsets, 2 * segments);

		for (var i = 0; i < segments; i++)
		{
			var last = U16(ends + i * 2);
			if (codepoint > last) continue;
			var first = U16(starts + i * 2);
			if (codepoint < first) return 0;
			var delta = I16(deltas + i * 2);
			var range = U16(offsets + i * 2);

			if (range == 0) return (codepoint + delta) & 0xFFFF;
			var at = offsets + i * 2 + range + 2 * (codepoint - first);
			Require(at, 2);
			var glyph = U16(at);
			return glyph == 0 ? 0 : (glyph + delta) & 0xFFFF;
		}

		return 0;
	}

	private int Table(string tag, int minimum)
	{
		if (!_tables.TryGetValue(tag, out var entry) || entry.length < minimum)
			throw new InvalidDataException($"TrueType font lacks required table '{tag}'.");
		return entry.start;
	}

	private void Require(int pos, int len)
	{
		if (pos < 0 || len < 0 || pos > _file.Length || len > _file.Length - pos)
			throw new InvalidDataException("Truncated TrueType font data.");
	}

	private static int SafeU32(uint value)
	{
		if (value > int.MaxValue)
			throw new InvalidDataException("TrueType table offsets and counts must fit a signed 32-bit value.");
		return (int)value;
	}

	private ushort U16(int at) { Require(at, 2); return BinaryPrimitives.ReadUInt16BigEndian(_file.AsSpan(at, 2)); }
	private short I16(int at) { Require(at, 2); return BinaryPrimitives.ReadInt16BigEndian(_file.AsSpan(at, 2)); }
	private uint U32(int at) { Require(at, 4); return BinaryPrimitives.ReadUInt32BigEndian(_file.AsSpan(at, 4)); }
	private static Point Mid(Point a, Point b) => new((a.X + b.X) / 2, (a.Y + b.Y) / 2, true);
}
