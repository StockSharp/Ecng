namespace Ecng.Compilation;

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;
using Ecng.Serialization;

/// <summary>
/// The reference to the .NET assembly.
/// </summary>
public class AssemblyReference : BaseCodeReference
{
	/// <summary>
	/// Initializes a new instance of the <see cref="AssemblyReference"/>.
	/// </summary>
	public AssemblyReference()
	{
	}

	/// <summary>
	/// Gets the display name of the assembly reference.
	/// </summary>
	public override string Name => Path.GetFileNameWithoutExtension(FileName);

	/// <summary>
	/// Determines whether the assembly reference is valid.
	/// </summary>
	public override bool IsValid => File.Exists(Location);

	/// <summary>
	/// Gets or sets the file name of the assembly reference.
	/// </summary>
	public string FileName { get; set; }

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

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		FileName = storage.GetValue<string>(nameof(FileName)) ?? storage.GetValue<string>(nameof(Location));
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		storage.SetValue(nameof(FileName), FileName);
	}

	/// <inheritdoc />
	public override ValueTask<IEnumerable<(string name, byte[] body)>> GetImages(CancellationToken cancellationToken)
	{
		return new([Location.ToRef()]);
	}
}