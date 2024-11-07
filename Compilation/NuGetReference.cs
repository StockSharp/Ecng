namespace Ecng.Compilation;

using Ecng.Serialization;

public class NuGetReference : BaseCodeReference
{
    public string PackageId { get; set; }
    public string Version { get; set; }

    public override string Name => $"{PackageId} v{Version}";
	public override string Location => string.Empty;
	public override bool IsValid => true;

	public override void Load(SettingsStorage storage)
	{
		PackageId = storage.GetValue<string>(nameof(PackageId));
		Version = storage.GetValue<string>(nameof(Version));
	}

	public override void Save(SettingsStorage storage)
	{
		storage.Set(nameof(PackageId), PackageId);
		storage.Set(nameof(Version), Version);
	}
}