namespace Ecng.Reflection.Emit
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Reflection;
	using System.Reflection.Emit;

	using Ecng.Common;

	public static class AssemblyHolder
	{
		private static AssemblyGenerator _assembly;
		private static readonly SyncObject _initializeSync = new();

		public static bool NeedCache { get; set; }

		private static string _assemblyCachePath = string.Empty;

		public static string AssemblyCachePath
		{
			get => _assemblyCachePath;
			set
			{
				if (value is null)
					value = string.Empty;

				_assemblyCachePath = value;
			}
		}

		public static int CompiledTypeLimit { get; set; }
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

						if (NeedCache)
						{
							CompiledTypeCount++;

							if (CompiledTypeCount > CompiledTypeLimit)
							{
								var asm = (AssemblyBuilder)_assembly.Builder.Assembly;

								var bytes = new Lokad.ILPack.AssemblyGenerator().GenerateAssemblyBytes(asm);
								File.WriteAllBytes(Path.Combine(AssemblyCachePath, asm.GetName(false).Name), bytes);

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