namespace Ecng.Reflection.Emit
{
	using System;
	using System.Reflection;
	using System.Reflection.Emit;
	using System.Linq;

#if SILVERLIGHT
	using Ecng.Collections;
#else
	using MoreLinq;
#endif

	public class PropertyGenerator : BaseGenerator<PropertyBuilder>
	{
		#region Private Fields

		private readonly TypeGenerator _typeGenerator;

		#endregion

		#region PropertyGenerator.ctor()

		internal PropertyGenerator(PropertyBuilder builder, TypeGenerator typeGenerator)
			: base(builder)
		{
			_typeGenerator = typeGenerator;
		}

		#endregion

		#region CreateGetMethod

		public MethodGenerator CreateGetMethod(MethodAttributes methodAttrs, params Type[] additionalTypes)
		{
			MethodGenerator generator = _typeGenerator.CreateMethod("get_" + Builder.Name, methodAttrs, Builder.PropertyType, additionalTypes);
			Builder.SetGetMethod((MethodBuilder)generator.Builder);
			return generator;
		}

		#endregion

		#region CreateSetMethod

		public MethodGenerator CreateSetMethod(MethodAttributes methodAttrs, params Type[] additionalTypes)
		{
			var generator = _typeGenerator.CreateMethod("set_" + Builder.Name, methodAttrs, typeof(void), additionalTypes.Concat(Builder.PropertyType).ToArray());
			Builder.SetSetMethod((MethodBuilder)generator.Builder);
			return generator;
		}

		#endregion
	}
}