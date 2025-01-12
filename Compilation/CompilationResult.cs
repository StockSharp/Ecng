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

	public abstract IAssembly Assembly { get; }
}

public class AssemblyCompilationResult(IEnumerable<CompilationError> errors, byte[] assemblyBody = null)
	: CompilationResult(errors)
{
	private class AssemblyImpl(byte[] body) : IAssembly
	{
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
				return ((AssemblyLoadContextTracker)context).LoadFromStream(asm);
#else
				throw new NotSupportedException();
#endif
			}

			_assembly ??= load();
			return _assembly.GetTypes().Select(t => new TypeImpl(t)).ToArray();
		}
	}

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

	public byte[] AssemblyBody { get; } = assemblyBody;

	private IAssembly _assembly;
	public override IAssembly Assembly => AssemblyBody is null ? null : _assembly ??= new AssemblyImpl(AssemblyBody);
}