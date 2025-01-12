namespace Ecng.Compilation;

using System;
using System.Linq;

using Ecng.Common;
using Ecng.ComponentModel;

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

public class TypeImpl(Type real) : IType
{
	private readonly Type _real = real ?? throw new ArgumentNullException(nameof(real));

	string IType.Name => _real.FullName;
	string IType.DisplayName => _real.GetDisplayName();
	string IType.Description => _real.GetDescription();
	string IType.DocUrl => _real.GetDocUrl();
	Uri IType.IconUri => _real.GetIconUrl();

	bool IType.IsAbstract => _real.IsAbstract;
	bool IType.IsPublic => _real.IsPublic;
	bool IType.IsGenericTypeDefinition => _real.IsGenericTypeDefinition;
	object IType.GetConstructor(IType[] value) => _real.GetConstructor(value.Select(t => ((TypeImpl)t)._real).ToArray());

	object IType.CreateInstance(object[] args) => _real.CreateInstance(args);
	bool IType.Is(Type type) => _real.Is(type, false);
}