namespace Ecng.Compilation;

using System;

public interface IType
{
	string Name { get; }
	Type DotNet { get; }
	object Native { get; }
	bool Is(Type type);
	object CreateInstance(params object[] args);
}