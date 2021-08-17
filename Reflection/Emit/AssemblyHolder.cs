namespace Ecng.Reflection.Emit
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Reflection;
	using System.Reflection.Emit;

	using Ecng.Common;

	public class AssemblyHolderSettings
	{
		public string AssemblyCachePath { get; set; }
		public int CompiledTypeLimit { get; set; }
	}

	public static class AssemblyHolder
	{
		private static AssemblyGenerator _assembly;
		private static readonly SyncObject _initializeSync = new();

		private static AssemblyHolderSettings _settings;

		public static AssemblyHolderSettings Settings
		{
			get => Scope<AssemblyHolderSettings>.Current?.Value ?? _settings;
			set => _settings = value;
		}

		public static int CompiledTypeCount { get; private set; }

		public static IList<Type> CachedTypes { get; } = new List<Type>();

		public static TypeGenerator CreateType(string typeName, TypeAttributes attrs, params Type[] baseTypes)
		{
			lock (_initializeSync)
			{
				if (_assembly is null)
					_assembly = new AssemblyGenerator(new AssemblyName(Guid.NewGuid() + ".dll"), AssemblyBuilderAccess.Run);

				return _assembly.CreateType(typeName, attrs, baseTypes, type =>
				{
					lock (_initializeSync)
					{
						CachedTypes.Add(type);

						var settings = Settings;

						if (settings != null)
						{
							CompiledTypeCount++;

							if (CompiledTypeCount > settings.CompiledTypeLimit)
							{
								var asm = (AssemblyBuilder)_assembly.Builder.Assembly;

								var bytes = new Lokad.ILPack.AssemblyGenerator().GenerateAssemblyBytes(asm);

								var cachePath = settings.AssemblyCachePath;
								Directory.CreateDirectory(cachePath);
								File.WriteAllBytes(Path.Combine(cachePath, asm.GetName(false).Name), bytes);

								_assembly = null;
								CompiledTypeCount = 0;
							}
						}
					}
				});
			}
		}
	}
}