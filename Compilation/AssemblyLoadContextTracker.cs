namespace Ecng.Compilation;

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

using Ecng.Common;

/// <summary>
/// Provides tracking functionality for AssemblyLoadContext instances when loading assemblies dynamically.
/// Implements ICompilerContext and manages unloading of previous contexts when loading a new assembly.
/// </summary>
public class AssemblyLoadContextTracker(Action<Exception> uploadingError = default) : Disposable, ICompilerContext
{
	private readonly SyncObject _lock = new();
	private readonly Action<Exception> _uploadingError = uploadingError;
	private AssemblyLoadContext _context;
	private byte[] _assembly;

	/// <summary>
	/// Loads an assembly from the provided binary data.
	/// If a different assembly is passed than the one previously loaded, the previous context is unloaded.
	/// </summary>
	/// <param name="assembly">The binary representation of the assembly to load.</param>
	/// <returns>The loaded <see cref="Assembly"/> instance.</returns>
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

	/// <summary>
	/// Unloads the current AssemblyLoadContext and resets the internal state.
	/// </summary>
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

	/// <inheritdoc />
	protected override void DisposeManaged()
	{
		base.DisposeManaged();
		Unload();
	}
}