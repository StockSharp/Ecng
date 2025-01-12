namespace Ecng.Compilation;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Ecng.Common;
using Ecng.ComponentModel;

public abstract class CompilationResult(IEnumerable<CompilationError> errors)
{
	public IEnumerable<CompilationError> Errors { get; } = errors.ToArray();

	public abstract IEnumerable<IType> GetExportTypes(object context);
}

public class AssemblyCompilationResult(IEnumerable<CompilationError> errors)
	: CompilationResult(errors)
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

	public byte[] Assembly { get; set; }

	private Assembly _assembly;

	public override IEnumerable<IType> GetExportTypes(object context)
	{
		if (context is null)
			throw new ArgumentNullException(nameof(context));

		var asm = Assembly ?? throw new InvalidOperationException("Assembly is not set.");

		Assembly load()
		{
#if NETCOREAPP
			return ((AssemblyLoadContextTracker)context).LoadFromStream(asm);
#else
			throw new NotSupportedException();
#endif
		}

		_assembly ??= load();
		return _assembly.GetTypes().Select(t => new TypeImpl(t)).ToArray();
	}
}