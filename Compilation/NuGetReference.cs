namespace Ecng.Compilation;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;
using Ecng.Serialization;

/// <summary>
/// Represents a NuGet package reference with package identifier and version.
/// </summary>
public class NuGetReference : BaseCodeReference
{
	/// <summary>
	/// Initializes a new instance of the <see cref="NuGetReference"/>.
	/// </summary>
	public NuGetReference()
		: this(LocalFileSystem.Instance)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="NuGetReference"/> with a custom file system.
	/// </summary>
	public NuGetReference(IFileSystem fileSystem)
		: base(fileSystem)
	{
	}

	/// <summary>
	/// Gets or sets the NuGet package identifier.
	/// </summary>
	public string PackageId { get; set; }

	/// <summary>
	/// Gets or sets the version of the NuGet package.
	/// </summary>
	public string Version { get; set; }

	/// <summary>
	/// Gets the display name of the NuGet package reference.
	/// </summary>
	public override string Name => $"{PackageId} v{Version}";

	/// <summary>
	/// Gets the location of the NuGet package reference (empty for NuGet packages).
	/// </summary>
	public override string Location => string.Empty;

	/// <summary>
	/// Gets a value indicating whether the NuGet package reference is valid.
	/// </summary>
	public override bool IsValid => true;

	/// <inheritdoc />
	public override ValueTask<IEnumerable<(string name, byte[] body)>> GetImages(CancellationToken cancellationToken)
		=> throw new System.NotImplementedException();

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		PackageId = storage.GetValue<string>(nameof(PackageId));
		Version = storage.GetValue<string>(nameof(Version));
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		storage.Set(nameof(PackageId), PackageId);
		storage.Set(nameof(Version), Version);
	}
}