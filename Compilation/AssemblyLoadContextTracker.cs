#if NETCOREAPP
namespace Ecng.Compilation;

using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

using Ecng.Common;

public class AssemblyLoadContextTracker
{
	private readonly SyncObject _lock = new();
	private AssemblyLoadContext _context;
	private byte[] _assembly;

	public Assembly LoadFromStream(byte[] assembly)
	{
		void init()
		{
			_context = new(default, true);
			_assembly = assembly;
		}

		lock (_lock)
		{
			if (_assembly is null)
			{
				init();
			}
			else if (!_assembly.SequenceEqual(assembly))
			{
				_context.Unload();
				init();
			}
		}
		
		return _context.LoadFromStream(assembly);
	}
}
#endif