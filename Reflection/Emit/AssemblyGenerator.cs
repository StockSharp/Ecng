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

		public AssemblyGenerator(AssemblyName name, AssemblyBuilderAccess access)
			: base(CreateModule(name, access))
		{
		}

		private static ModuleBuilder CreateModule(AssemblyName name, AssemblyBuilderAccess access)
		{
			if (name is null)
				throw new ArgumentNullException(nameof(name));

			var assembly = AssemblyBuilder.DefineDynamicAssembly(name, access);
			return assembly.DefineDynamicModule(name.Name);
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

		public TypeGenerator CreateType(string typeName, TypeAttributes attrs, Type[] baseTypes, Action<Type> typeCompiled)
		{
			//if (typeName.IsEmpty())
			//	throw new ArgumentNullException(nameof(typeName));

			//if (baseTypes is null)
			//	throw new ArgumentNullException(nameof(baseTypes));

			var typeGen = new TypeGenerator(Builder.DefineType(typeName, attrs), baseTypes, typeCompiled);
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