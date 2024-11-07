namespace Ecng.Compilation;

using System.IO;

using Ecng.Serialization;

public interface ICodeReference : IPersistable
{
	string Id { get; }
	string Name { get; }
	string Location { get; }
	bool IsValid { get; }
}

public abstract class BaseCodeReference : ICodeReference
{
	public virtual string Id => Location;

	public abstract string Name { get; }
	public abstract string Location { get; }
	public abstract bool IsValid { get; }

	public abstract void Load(SettingsStorage storage);
	public abstract void Save(SettingsStorage storage);

	public override string ToString() => Location;
}

public abstract class BaseFileReference : BaseCodeReference
{
	public override string Name => Path.GetFileNameWithoutExtension(FileName);
	public override bool IsValid => File.Exists(FileName);
	public override string Location => FileName;

	public string FileName { get; set; }

	public override void Load(SettingsStorage storage)
	{
		FileName = storage.GetValue<string>(nameof(FileName));
	}

	public override void Save(SettingsStorage storage)
	{
		storage.SetValue(nameof(FileName), FileName);
	}
}