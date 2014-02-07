namespace Ecng.Reflection.Emit
{
	#region Using Directives

	using System;
	using System.Collections.Generic;
	using System.Reflection;
	using System.Reflection.Emit;

#if !SILVERLIGHT
	using Ecng.Configuration;
	using Ecng.Reflection.Configuration;
#endif

	#endregion

	public static class AssemblyHolder
	{
		#region Private Fields

		private static AssemblyGenerator _assembly;
		private static readonly object _initializeSync = new object();

		#endregion

		#region AssemblyHolder.cctor()

		static AssemblyHolder()
		{
#if !SILVERLIGHT
			var settings = ConfigManager.GetSection<ReflectionSection>();

			if (settings != null)
			{
				NeedCache = settings.NeedCache;
				AssemblyCachePath = settings.AssemblyCachePath;
				CompiledTypeLimit = settings.CompiledTypeLimit;
			}
#endif
		}

		#endregion

		#region NeedCache

		public static bool NeedCache { get; set; }

		#endregion

		#region AssemblyCachePath

		private static string _assemblyCachePath = string.Empty;

		public static string AssemblyCachePath
		{
			get { return _assemblyCachePath; }
			set
			{
				if (value == null)
					value = string.Empty;

				_assemblyCachePath = value;
			}
		}

		#endregion

		public static int CompiledTypeLimit { get; set; }
		public static int CompiledTypeCount { get; private set; }

		#region CachedTypes

		private static readonly List<Type> _cachedTypes = new List<Type>();

		public static IList<Type> CachedTypes
		{
			get { return _cachedTypes; }
		}

		#endregion

		#region CreateType

		public static TypeGenerator CreateType(string typeName, TypeAttributes attrs, params Type[] baseTypes)
		{
			lock (_initializeSync)
			{
				if (_assembly == null)
				{
					var access = 
#if SILVERLIGHT
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
								var builder = (AssemblyBuilder)_assembly.Builder.Assembly;
								builder.Save(builder.GetName(false).Name);
								_assembly = null;
								CompiledTypeCount = 0;
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