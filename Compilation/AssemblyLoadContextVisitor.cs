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
	using Ecng.Collections;

	public class AssemblyLoadContextVisitor
	{
#if NETCOREAPP
		private readonly AssemblyLoadContext _context;
		private readonly SynchronizedDictionary<Assembly, int> _refs;
		private bool _unloaded;

		public AssemblyLoadContextVisitor()
			: this(AssemblyLoadContext.CurrentContextualReflectionContext ?? AssemblyLoadContext.Default)
		{
		}

		public AssemblyLoadContextVisitor(bool isCollectible)
			: this(new AssemblyLoadContext(null, isCollectible))
		{
		}

		public AssemblyLoadContextVisitor(AssemblyLoadContext context)
		{
			_context = context ?? throw new ArgumentNullException(nameof(context));

			if (context.IsCollectible)
				_refs = new();
		}

		public bool IsCollectible => _context.IsCollectible;

		public Assembly LoadFromStream(Stream stream)
		{
			var asm = _context.LoadFromStream(stream);

			if (_refs is not null)
				AddRef(asm);

			return asm;
		}

		private void Validate(Assembly asm)
		{
			if (asm is null)
				throw new ArgumentNullException(nameof(asm));

			if (!IsCollectible)
				throw new InvalidOperationException("IsCollectible=false");

			if (_unloaded)
				throw new InvalidOperationException("unloaded");
		}

		public void AddRef(Assembly asm)
		{
			Validate(asm);

			lock (_refs.SyncRoot)
				_refs[asm] = _refs.TryGetValue(asm) + 1;
		}

		public void RemoveRef(Assembly asm)
		{
			Validate(asm);

			lock (_refs.SyncRoot)
			{
				if (_refs.TryGetValue(asm, out var counter))
				{
					if (counter > 2)
						_refs[asm] = counter - 1;
					else
					{
						_refs.Remove(asm);

						if (_refs.Count == 0)
						{
							_context.Unload();
							_unloaded = true;
						}
					}
				}
			}
		}
#else
		public AssemblyLoadContextVisitor()
		{
		}

		public Assembly LoadFromStream(Stream stream) => Assembly.Load(stream.To<byte[]>());
#endif
	}
}