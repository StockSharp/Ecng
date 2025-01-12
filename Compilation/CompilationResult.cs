namespace Ecng.Compilation;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Ecng.Common;

public abstract class CompilationResult(IEnumerable<CompilationError> errors)
{
	public IEnumerable<CompilationError> Errors { get; } = errors.ToArray();

	public abstract IEnumerable<IType> GetExportTypes(object context);
}

public class AssemblyCompilationResult(IEnumerable<CompilationError> errors)
	: CompilationResult(errors)
{
	private class TypeImpl(Type dotNet) : IType
	{
		public string Name => DotNet.Name;
		public object Native => DotNet;

		public Type DotNet { get; } = dotNet ?? throw new ArgumentNullException(nameof(dotNet));

		public object CreateInstance(params object[] args)
			=> DotNet.CreateInstance(args);

		public bool Is(Type type)
			=> DotNet.Is(type, false);
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