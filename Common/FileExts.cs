namespace Ecng.Common;

using System;

public static class FileExts
{
	/// <summary>
	/// Backup extension for settings file.
	/// </summary>
	public const string Backup = ".bak";

	/// <summary>
	/// C# file extension.
	/// </summary>
	public const string CSharp = ".cs";

	/// <summary>
	/// F# file extension.
	/// </summary>
	public const string FSharp = ".fs";

	/// <summary>
	/// Visual Basic file extension.
	/// </summary>
	public const string VisualBasic = ".vb";

	/// <summary>
	/// Python file extension.
	/// </summary>
	public const string Python = ".py";

	/// <summary>
	/// Assembly file extension.
	/// </summary>
	public const string Dll = ".dll";

	/// <summary>
	/// JSON file extension.
	/// </summary>
	public const string Json = ".json";

	/// <summary>
	/// XML file extension.
	/// </summary>
	public const string Xml = ".xml";

	/// <summary>
	/// CSV file extension.
	/// </summary>
	public const string Csv = ".csv";

	/// <summary>
	/// Text file extension.
	/// </summary>
	public const string Txt = ".txt";

	/// <summary>
	/// Binary file extension.
	/// </summary>
	public const string Bin = ".bin";

	/// <summary>
	/// Xsl file extension.
	/// </summary>
	public const string Xls = ".xsl";

	/// <summary>
	/// Xslx file extension.
	/// </summary>
	public const string Xlsx = ".xslx";

	public static string ToDisplayName(this string fileExt)
		=> (fileExt?.ToLowerInvariant()) switch
		{
			CSharp => "C#",
			FSharp => "F#",
			Python => "Python",
			VisualBasic => "VB",
			Txt => "TXT",
			Csv => "CSV",
			Xml => "XML",
			Xls or Xlsx => "Excel",
			Dll => "DLL",
			Json => "JSON",
			_ => throw new ArgumentOutOfRangeException(nameof(fileExt), fileExt, "Invalid value."),
		};
}