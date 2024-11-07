namespace Ecng.Compilation;

using System.IO;

using Ecng.Common;
using Ecng.Serialization;

/// <summary>
/// The reference to the .NET assembly.
/// </summary>
public class AssemblyReference : BaseFileReference
{
	/// <summary>
	/// Initializes a new instance of the <see cref="AssemblyReference"/>.
	/// </summary>
	public AssemblyReference()
	{
	}

	/// <summary>
	/// <see cref="Location"/>.
	/// </summary>
	public override string Location
	{
		get
		{
			var fileName = FileName;

			if (fileName.IsEmpty())
				return string.Empty;

			fileName = Path.GetFileName(fileName);

			if (fileName.EqualsIgnoreCase(FileName))
			{
				var tmp = Path.Combine(ICompilerExtensions.RuntimePath, fileName);

				if (File.Exists(tmp))
					return tmp;
			}

			return fileName;
		}
	}

	// TODO 2024-11-07 Remove few years later
	public override void Load(SettingsStorage storage)
	{
		FileName = storage.GetValue<string>(nameof(FileName)) ?? storage.GetValue<string>(nameof(Location));
	}
}