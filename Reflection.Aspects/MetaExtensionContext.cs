namespace Ecng.Reflection.Aspects
{
	#region Using Directives

	using System;
	using System.Collections.Generic;

	using Ecng.Reflection.Emit;

	#endregion

	public class MetaExtensionContext
	{
		#region MetaExtensionContext.ctor()

		internal MetaExtensionContext(TypeGenerator typeGenerator, Type baseType)
		{
			TypeGenerator = typeGenerator;
			BaseType = baseType;
			AfterEmitInitFields = new Dictionary<FieldGenerator, object>();
		}

		#endregion

		public TypeGenerator TypeGenerator { get; private set; }
		public Type BaseType { get; private set; }
		public IDictionary<FieldGenerator, object> AfterEmitInitFields { get; private set; }
	}
}