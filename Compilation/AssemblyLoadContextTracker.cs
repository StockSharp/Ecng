namespace Ecng.Compilation;

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

using Ecng.Common;

public class AssemblyLoadContextTracker(Action<Exception> uploadingError = default) : Disposable, ICompilerContext
{
	private readonly SyncObject _lock = new();
	private readonly Action<Exception> _uploadingError = uploadingError;
	private AssemblyLoadContext _context;
	private byte[] _assembly;

	public Assembly LoadFromBinary(byte[] assembly)
	{
		void init()
		{
			_context = new(default, true);
			_assembly = assembly;
		}

		Exception error = null;

		lock (_lock)
		{
			if (_assembly is null)
			{
				init();
			}
			else if (!_assembly.SequenceEqual(assembly))
			{
				try
				{
					_context.Unload();
				}
				catch (Exception ex)
				{
					error = ex;
				}

				init();
			}
		}

		if (error is not null && _uploadingError is not null)
			_uploadingError(error);
		
		return _context.LoadFromBinary(assembly);
	}

	public void Unload()
	{
		lock (_lock)
		{
			if (_context is null)
				return;

			_context.Unload();
			_context = null;
			_assembly = null;
		}
	}

	protected override void DisposeManaged()
	{
		base.DisposeManaged();
		Unload();
	}
}