namespace Ecng.Compilation;

using System;

public interface IMember
{
	string Name { get; }
	string DisplayName { get; }
	string Description { get; }
	bool IsPublic { get; }
	bool IsAbstract { get; }
	bool IsGenericDefinition { get; }

	T GetAttribute<T>()
		where T : Attribute;
}