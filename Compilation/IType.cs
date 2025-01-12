namespace Ecng.Compilation;

using System;

public interface IType
{
	string Name { get; }
	string DisplayName { get; }
	string Description { get; }
	string DocUrl { get; }
	Uri IconUri { get; }
	bool IsAbstract { get; }
	bool IsPublic { get; }
	bool IsGenericTypeDefinition { get; }

	bool Is(Type type);
	object CreateInstance(params object[] args);
	object GetConstructor(IType[] value);
}