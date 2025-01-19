namespace Ecng.Compilation;

using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;
using Ecng.ComponentModel;

public interface IType : IMember
{
	string DocUrl { get; }
	Uri IconUri { get; }

	bool Is(Type type);
	object CreateInstance(params object[] args);
	object GetConstructor(IType[] value);

	IEnumerable<IProperty> GetProperties();

	Type ToType();

	string GetTypeName(bool isAssemblyQualifiedName);
}

class TypeImpl(Type real) : IType
{
	private readonly Type _real = real ?? throw new ArgumentNullException(nameof(real));

	public string Name => _real.FullName;
	string IMember.DisplayName => _real.GetDisplayName();
	string IMember.Description => _real.GetDescription();
	string IType.DocUrl => _real.GetDocUrl();
	Uri IType.IconUri => _real.GetIconUrl();

	bool IMember.IsAbstract => _real.IsAbstract;
	bool IMember.IsPublic => _real.IsPublic;
	bool IMember.IsGenericDefinition => _real.IsGenericTypeDefinition;
	object IType.GetConstructor(IType[] value) => _real.GetConstructor(value.Select(t => ((TypeImpl)t)._real).ToArray());

	object IType.CreateInstance(object[] args) => _real.CreateInstance(args);
	bool IType.Is(Type type) => _real.Is(type, false);
	IEnumerable<IProperty> IType.GetProperties() => _real.GetProperties().Select(p => new PropertyImpl(p));

	T IMember.GetAttribute<T>() => _real.GetAttribute<T>();
	Type IType.ToType() => _real;
	string IType.GetTypeName(bool isAssemblyQualifiedName) => _real.GetTypeName(isAssemblyQualifiedName);

	public override string ToString() => _real.ToString();
}