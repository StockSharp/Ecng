namespace Ecng.Interop;

using System;
using System.IO;
using System.Reflection;

using Ecng.Common;

public class AssemblyResolver : Disposable
{
	private readonly Action<ResolveEventArgs> _notFound;
	private readonly Action<ResolveEventArgs, Exception> _errorHandler;

	public AssemblyResolver(Action<ResolveEventArgs> notFound, Action<ResolveEventArgs, Exception> errorHandler)
	{
		_notFound = notFound ?? throw new ArgumentNullException(nameof(notFound));
		_errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));

		AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
	}

	protected override void DisposeManaged()
	{
		AppDomain.CurrentDomain.AssemblyResolve -= OnAssemblyResolve;

		base.DisposeManaged();
	}

	private Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
	{
		string dllPath = null;

		try
		{
			if (args.RequestingAssembly is null)
				return null;

			var name = new AssemblyName(args.Name).Name;

			var path = args.RequestingAssembly.Location;

			var dir = Path.GetDirectoryName(path);

			dllPath = Path.Combine(dir, $"{name}.dll");

			if (File.Exists(dllPath))
				return Assembly.LoadFile(dllPath);

			var runtimesPath = Path.Combine(dir, Constants.Runtimes);

			if (Directory.Exists(runtimesPath))
			{
				var architecture = Constants.GetArchitecture();
				var os = Constants.GetOS();

				var searchPaths = new[]
				{
					Path.Combine(runtimesPath, $"{os}-{architecture}"),
					Path.Combine(runtimesPath, os),
					Path.Combine(runtimesPath, architecture),
					Path.Combine(runtimesPath, Constants.Aot, Constants.Lib, Constants.NetCore50),
					Path.Combine(runtimesPath, $"{os}-{architecture}", Constants.Lib, Constants.NetStandard20),
					runtimesPath
				};

				foreach (var searchPath in searchPaths)
				{
					var runtimeDllPath = Path.Combine(searchPath, name);

					if (File.Exists(runtimeDllPath))
						return Assembly.LoadFile(runtimeDllPath);

					runtimeDllPath = Path.Combine(searchPath, Constants.Native, name);

					if (File.Exists(runtimeDllPath))
						return Assembly.LoadFile(runtimeDllPath);
				}
			}

			_notFound(args);
			return null;
		}
		catch (Exception ex)
		{
			_errorHandler(args, ex);
		}

		return null;
	}
}
