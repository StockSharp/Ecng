namespace Ecng.Compilation;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Serialization;

public interface ICodeReference : IPersistable
{
	string Id { get; }
	string Name { get; }
	string Location { get; }
	bool IsValid { get; }

	ValueTask<IEnumerable<(string name, byte[] body)>> GetImages(CancellationToken cancellationToken);
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

	public abstract ValueTask<IEnumerable<(string name, byte[] body)>> GetImages(CancellationToken cancellationToken);
}