namespace Ecng.Compilation;

using Ecng.Serialization;

public interface ICodeReference : IPersistable
{
	string Name { get; }
	string Location { get; }
	bool IsValid { get; }
}