#if NETCOREAPP
namespace Ecng.Compilation;

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

using Ecng.Common;

public class AssemblyLoadContextTracker
{
	private readonly SyncObject _lock = new();
	private readonly Action<Exception> _uploadingError;
	private AssemblyLoadContext _context;
	private byte[] _assembly;

    public AssemblyLoadContextTracker(Action<Exception> uploadingError = default)
    {
		_uploadingError = uploadingError;
	}

    public Assembly LoadFromStream(byte[] assembly)
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
		
		return _context.LoadFromStream(assembly);
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
}
#endif