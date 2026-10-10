namespace Ecng.Drawing;

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>
/// The TrueType fonts of a set of directories, found by the family names the fonts carry.
/// </summary>
internal sealed class FontCatalog
{
	// A font file and what its name table says.
	private sealed class Entry
	{
		public string Path;
		public string Family;
		public bool IsRegular;
	}

	private const int _maxTables = 256;
	private const int _maxNameTableLength = 1 << 20;

	private const int _familyName = 1;
	private const int _subfamilyName = 2;
	private const int _windowsPlatform = 3;
	private const int _macintoshPlatform = 1;
	private const int _english = 0x0409;

	// What a family that is not installed is replaced with, most wanted first. A font that carries no
	// readable name is still found by the name of its file. If none of these is installed either,
	// the built-in font is what is left.
	private static readonly string[] _fallbacks =
	[
		"Verdana", "DejaVu Sans", "DejaVuSans", "Arial", "Liberation Sans", "LiberationSans-Regular",
		"Noto Sans", "NotoSans-Regular", "FreeSans", "Helvetica",
	];

	// The fonts of a machine are listed once: a server draws many watermarks and installs fonts seldom.
	private static readonly Lazy<FontCatalog> _installed = new(() => Scan(InstalledDirectories()));

	private readonly List<Entry> _entries;

	private FontCatalog(List<Entry> entries)
	{
		_entries = entries;
	}

	/// <summary>
	/// The fonts installed on this machine.
	/// </summary>
	public static FontCatalog Installed => _installed.Value;

	/// <summary>
	/// Lists the TrueType outline fonts of the given directories and of everything below them.
	/// </summary>
	public static FontCatalog Scan(IEnumerable<string> directories)
	{
		var entries = new List<Entry>();
		var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var options = new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true, MatchCasing = MatchCasing.CaseInsensitive };

		foreach (var directory in directories)
		{
			if (!Directory.Exists(directory))
				continue;

			try
			{
				foreach (var path in Directory.EnumerateFiles(directory, "*.ttf", options))
				{
					if (seen.Add(path) && TryRead(path) is { } entry)
						entries.Add(entry);
				}
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
				// A directory that cannot be read has no fonts to offer.
			}
		}

		// Two files may carry one family: which of them is taken must not depend on how the file system lists them.
		entries.Sort((a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.Path, b.Path));
		return new(entries);
	}

	/// <summary>
	/// Finds the file of a family, or of one of the families that usually stand in for it.
	/// </summary>
	/// <param name="family">The family name as fonts state it, or the name of a font file without its extension.</param>
	/// <returns>
	/// The path of a font file; <see langword="null"/> if the built-in font is to be used: neither the
	/// family nor a stand-in for it is here, or the built-in font itself is asked for and is not here.
	/// </returns>
	public string Resolve(string family)
	{
		if (Find(family) is { } path)
			return path;

		// No other family is closer to the built-in font than the built-in font.
		if (string.Equals(family, BuiltInFont.Family, StringComparison.OrdinalIgnoreCase))
			return null;

		// Asked for by the name of its file, it is the family it carries.
		if (string.Equals(family, BuiltInFont.FileName, StringComparison.OrdinalIgnoreCase))
			return Find(BuiltInFont.Family);

		foreach (var fallback in _fallbacks)
		{
			if (Find(fallback) is { } replacement)
				return replacement;
		}

		return null;
	}

	private string Find(string family)
	{
		Entry other = null;

		foreach (var entry in _entries)
		{
			if (!string.Equals(entry.Family, family, StringComparison.OrdinalIgnoreCase))
				continue;

			if (entry.IsRegular)
				return entry.Path;

			other ??= entry;
		}

		if (other is not null)
			return other.Path;

		foreach (var entry in _entries)
		{
			if (string.Equals(Path.GetFileNameWithoutExtension(entry.Path), family, StringComparison.OrdinalIgnoreCase))
				return entry.Path;
		}

		return null;
	}

	private static List<string> InstalledDirectories()
	{
		var directories = new List<string>();

		void Add(params string[] parts)
		{
			// A folder the system does not have comes back as an empty string.
			if (Array.TrueForAll(parts, part => !string.IsNullOrEmpty(part)))
				directories.Add(Path.Combine(parts));
		}

		var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

		Add(Environment.GetFolderPath(Environment.SpecialFolder.Fonts));

		if (OperatingSystem.IsWindows())
			Add(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Windows", "Fonts");
		else if (OperatingSystem.IsMacOS())
		{
			Add("/System/Library/Fonts");
			Add("/Library/Fonts");
			Add(profile, "Library", "Fonts");
		}
		else
		{
			Add("/usr/share/fonts");
			Add("/usr/local/share/fonts");
			Add(profile, ".fonts");
			Add(profile, ".local", "share", "fonts");
		}

		return directories;
	}

	// Reads the table directory and the name table of a font file. Null if the file is not a font with
	// TrueType outlines.
	private static Entry TryRead(string path)
	{
		try
		{
			using var stream = File.OpenRead(path);

			var header = new byte[12];

			if (!TryFill(stream, header))
				return null;

			var signature = BinaryPrimitives.ReadUInt32BigEndian(header);
			var count = BinaryPrimitives.ReadUInt16BigEndian(header.AsSpan(4));

			// 0x00010000 or 'true': outlines in a glyf table.
			if (signature is not (0x00010000 or 0x74727565) || count > _maxTables)
				return null;

			var directory = new byte[count * 16];

			if (!TryFill(stream, directory))
				return null;

			var hasOutlines = false;
			var nameOffset = 0u;
			var nameLength = 0u;

			for (var at = 0; at < directory.Length; at += 16)
			{
				var tag = directory.AsSpan(at, 4);

				if (tag.SequenceEqual("glyf"u8))
					hasOutlines = true;
				else if (tag.SequenceEqual("name"u8))
				{
					nameOffset = BinaryPrimitives.ReadUInt32BigEndian(directory.AsSpan(at + 8));
					nameLength = BinaryPrimitives.ReadUInt32BigEndian(directory.AsSpan(at + 12));
				}
			}

			if (!hasOutlines)
				return null;

			var entry = new Entry { Path = path, IsRegular = true };

			if (nameLength is > 0 and <= _maxNameTableLength && nameOffset < stream.Length)
			{
				var names = new byte[nameLength];
				stream.Position = nameOffset;

				if (TryFill(stream, names))
				{
					entry.Family = ReadName(names, _familyName);

					var subfamily = ReadName(names, _subfamilyName);
					entry.IsRegular = subfamily is null || subfamily.Equals("Regular", StringComparison.OrdinalIgnoreCase) ||
						subfamily.Equals("Normal", StringComparison.OrdinalIgnoreCase) || subfamily.Equals("Book", StringComparison.OrdinalIgnoreCase) ||
						subfamily.Equals("Roman", StringComparison.OrdinalIgnoreCase);
				}
			}

			return entry;
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			// A file that cannot be read is not a font to draw with.
			return null;
		}
	}

	// A name table is a list of records - platform, encoding, language, name, length, offset - and the
	// strings they point at. The English Windows string is taken if there is one.
	private static string ReadName(byte[] table, int nameId)
	{
		if (table.Length < 6)
			return null;

		var count = BinaryPrimitives.ReadUInt16BigEndian(table.AsSpan(2));
		var strings = BinaryPrimitives.ReadUInt16BigEndian(table.AsSpan(4));
		string found = null;
		var rank = 0;

		for (var i = 0; i < count; i++)
		{
			var at = 6 + i * 12;

			if (at + 12 > table.Length)
				break;

			var record = table.AsSpan(at, 12);

			if (BinaryPrimitives.ReadUInt16BigEndian(record[6..]) != nameId)
				continue;

			var platform = BinaryPrimitives.ReadUInt16BigEndian(record);
			var language = BinaryPrimitives.ReadUInt16BigEndian(record[4..]);
			var length = BinaryPrimitives.ReadUInt16BigEndian(record[8..]);
			var offset = strings + BinaryPrimitives.ReadUInt16BigEndian(record[10..]);

			if (offset + length > table.Length)
				continue;

			var recordRank = platform switch
			{
				_windowsPlatform when language == _english => 3,
				_windowsPlatform => 2,
				_macintoshPlatform => 1,
				_ => 0,
			};

			if (recordRank <= rank)
				continue;

			rank = recordRank;
			found = (platform == _windowsPlatform ? Encoding.BigEndianUnicode : Encoding.Latin1).GetString(table, offset, length);
		}

		return found;
	}

	private static bool TryFill(Stream stream, byte[] buffer)
	{
		for (var filled = 0; filled < buffer.Length;)
		{
			var read = stream.Read(buffer, filled, buffer.Length - filled);

			if (read == 0)
				return false;

			filled += read;
		}

		return true;
	}
}
