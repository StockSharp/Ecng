namespace Ecng.Common;

using System;

/// <summary>
/// File extensions.
/// </summary>
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
	/// Excel file extension.
	/// </summary>
	public const string Xls = ".xls";

	/// <summary>
	/// Excel file extension.
	/// </summary>
	public const string Xlsx = ".xlsx";

	/// <summary>
	/// Convert the extension into display name format.
	/// </summary>
	/// <param name="fileExt">The extension.</param>
	/// <returns>The display name.</returns>
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