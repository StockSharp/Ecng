namespace Ecng.Compilation
{
	using System;
	using System.IO;
	using System.Reflection;
#if NETCOREAPP
	using System.Runtime.Loader;
#else
	using Ecng.Common;
#endif

	public class AssemblyLoadContextVisitor
	{
#if NETCOREAPP
		private readonly AssemblyLoadContext _context;

		public AssemblyLoadContextVisitor()
			: this(AssemblyLoadContext.CurrentContextualReflectionContext ?? AssemblyLoadContext.Default)
		{
		}

		public AssemblyLoadContextVisitor(AssemblyLoadContext context)
		{
			_context = context ?? throw new ArgumentNullException(nameof(context));
		}

		public Assembly LoadFromAssemblyPath(string pathToAssembly) => _context.LoadFromAssemblyPath(pathToAssembly);
		public Assembly LoadFromStream(Stream stream) => _context.LoadFromStream(stream);
#else
		public AssemblyLoadContextVisitor()
		{
		}

		public Assembly LoadFromAssemblyPath(string pathToAssembly) => Assembly.LoadFile(pathToAssembly);
		public Assembly LoadFromStream(Stream stream) => Assembly.Load(stream.To<byte[]>());
#endif
	}
}