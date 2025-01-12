namespace Ecng.Compilation;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Ecng.Common;
using Ecng.ComponentModel;

public interface IAssembly
{
	byte[] AsBytes { get; }
	IEnumerable<IType> GetExportTypes(object context);
}

class AssemblyImpl(byte[] body) : IAssembly
{
	private class TypeImpl(Type real) : IType
	{
		private readonly Type _real = real ?? throw new ArgumentNullException(nameof(real));

		string IType.Name => _real.FullName;
		string IType.DisplayName => _real.GetDisplayName();
		string IType.Description => _real.GetDescription();
		string IType.DocUrl => _real.GetDocUrl();
		Uri IType.IconUri => _real.GetIconUrl();

		object IType.CreateInstance(object[] args) => _real.CreateInstance(args);
		bool IType.Is(Type type) => _real.Is(type, false);
	}

	private readonly byte[] _body = body ?? throw new ArgumentNullException(nameof(body));
	private Assembly _assembly;

	byte[] IAssembly.AsBytes => _body;

	IEnumerable<IType> IAssembly.GetExportTypes(object context)
	{
		if (context is null)
			throw new ArgumentNullException(nameof(context));

		var asm = _body ?? throw new InvalidOperationException("Assembly is not set.");

		Assembly load()
		{
#if NETCOREAPP
				if (context is AssemblyLoadContextTracker tracker)
					return tracker.LoadFromStream(asm);
				else if (context is System.Runtime.Loader.AssemblyLoadContext alc)
					return alc.LoadFromStream(asm);
#endif

			throw new NotSupportedException(context.To<string>());
		}

		_assembly ??= load();
		return _assembly.GetTypes().Select(t => new TypeImpl(t)).ToArray();
	}
}