namespace Ecng.Reflection.Emit
{
	#region Using Directives

	using System;
	using System.Collections.Generic;
	using System.Reflection;
	using System.Reflection.Emit;

	#endregion

	public class AssemblyGenerator : BaseGenerator<ModuleBuilder>
	{
		#region AssemblyGenerator.ctor()

		public AssemblyGenerator(AssemblyName name, AssemblyBuilderAccess access, string dir)
			: base(CreateModule(name, access, dir))
		{
		}

		private static ModuleBuilder CreateModule(AssemblyName name, AssemblyBuilderAccess access, string dir)
		{
#if NETCOREAPP || NETSTANDARD
			throw new PlatformNotSupportedException();
#else
			var assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(name, access, dir);
			return assembly.DefineDynamicModule(name.Name);
#endif
		}

		#endregion

		#region Types

		private readonly List<TypeGenerator> _types = new();

		public IEnumerable<TypeGenerator> Types => _types;

		#endregion

		#region Enums

		private readonly List<EnumGenerator> _enums = new();

		public IEnumerable<EnumGenerator> Enums => _enums;

		#endregion

		#region CreateType

		public TypeGenerator CreateType(string typeName, TypeAttributes attrs, params Type[] baseTypes)
		{
			//if (typeName.IsEmpty())
			//	throw new ArgumentNullException(nameof(typeName));

			//if (baseTypes is null)
			//	throw new ArgumentNullException(nameof(baseTypes));

			var typeGen = new TypeGenerator(Builder.DefineType(typeName, attrs), baseTypes);
			_types.Add(typeGen);
			return typeGen;
		}

		#endregion

		#region CreateEnum

		public EnumGenerator CreateEnum(string typeName, TypeAttributes attrs, Type underlyingType)
		{
			//if (typeName.IsEmpty())
			//	throw new ArgumentNullException(nameof(typeName));

			//if (baseTypes is null)
			//	throw new ArgumentNullException(nameof(baseTypes));

			var gen = new EnumGenerator(Builder.DefineEnum(typeName, attrs, underlyingType));
			_enums.Add(gen);
			return gen;
		}

		#endregion
	}
}