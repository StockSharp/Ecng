namespace Ecng.Compilation;

using System.IO;

using Ecng.Common;
using Ecng.Serialization;

/// <summary>
/// The reference to the .NET assembly.
/// </summary>
public class AssemblyReference : ICodeReference
{
	/// <summary>
	/// Initializes a new instance of the <see cref="AssemblyReference"/>.
	/// </summary>
	public AssemblyReference()
	{
	}

	/// <summary>
	/// The assembly name.
	/// </summary>
	public string Name => Path.GetFileNameWithoutExtension(FileName);

	/// <summary>
	/// The path to the assembly.
	/// </summary>
	public string FileName { get; set; }

	/// <summary>
	/// <see cref="Location"/>.
	/// </summary>
	public string Location
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

	/// <summary>
	/// Is valid.
	/// </summary>
	public bool IsValid => File.Exists(Location);

	/// <summary>
	/// Load settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	public void Load(SettingsStorage storage)
	{
		FileName = storage.GetValue<string>(nameof(FileName)) ?? storage.GetValue<string>(nameof(Location));
	}

	/// <summary>
	/// Save settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	public void Save(SettingsStorage storage)
	{
		storage.SetValue(nameof(FileName), FileName);
	}

	/// <inheritdoc />
	public override string ToString() => Location;
}