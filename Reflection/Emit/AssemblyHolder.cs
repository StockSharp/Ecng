namespace Ecng.Reflection.Emit
{
	#region Using Directives

	using System;
	using System.Collections.Generic;
	using System.Reflection;
	using System.Reflection.Emit;

	#endregion

	public static class AssemblyHolder
	{
		#region Private Fields

		private static AssemblyGenerator _assembly;
		private static readonly object _initializeSync = new object();

		#endregion

		#region NeedCache

		public static bool NeedCache { get; set; }

		#endregion

		#region AssemblyCachePath

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

		#endregion

		public static int CompiledTypeLimit { get; set; }
		public static int CompiledTypeCount { get; private set; }

		#region CachedTypes

		private static readonly List<Type> _cachedTypes = new List<Type>();

		public static IList<Type> CachedTypes => _cachedTypes;

		#endregion

		#region CreateType

		public static TypeGenerator CreateType(string typeName, TypeAttributes attrs, params Type[] baseTypes)
		{
			lock (_initializeSync)
			{
				if (_assembly is null)
				{
					var access = 
#if SILVERLIGHT || NETCOREAPP || NETSTANDARD
						AssemblyBuilderAccess.Run;
#else
						NeedCache ? AssemblyBuilderAccess.RunAndSave : AssemblyBuilderAccess.Run;
#endif
					_assembly = new AssemblyGenerator(new AssemblyName(Guid.NewGuid() + ".dll"), access, AssemblyHolder.AssemblyCachePath);
				}

				var type = _assembly.CreateType(typeName, attrs, baseTypes);
				type.TypeCompiled += (sender, e) =>
				{
					lock (_initializeSync)
					{
						CachedTypes.Add(e.Type);

						if (NeedCache)
						{
							CompiledTypeCount++;

							if (CompiledTypeCount > CompiledTypeLimit)
							{
#if NETCOREAPP || NETSTANDARD
								throw new PlatformNotSupportedException();
#else
								var builder = (AssemblyBuilder)_assembly.Builder.Assembly;
								builder.Save(builder.GetName(false).Name);
								_assembly = null;
								CompiledTypeCount = 0;
#endif
							}
						}
					}
				};
				return type;
			}
		}

		#endregion
	}
}