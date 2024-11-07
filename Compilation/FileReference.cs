namespace Ecng.Compilation;

using System.IO;

using Ecng.Serialization;

public class FileReference : ICodeReference
{
	public string Name => Path.GetFileNameWithoutExtension(Location);
	public bool IsValid => File.Exists(Location);
	
	public string Location { get; set; }

	public void Load(SettingsStorage storage)
	{
		Location = storage.GetValue<string>(nameof(Location));
	}

	public void Save(SettingsStorage storage)
	{
		storage.SetValue(nameof(Location), Location);
	}

	public override string ToString() => Location;
}