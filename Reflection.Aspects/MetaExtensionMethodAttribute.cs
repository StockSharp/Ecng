namespace Ecng.Reflection.Aspects
{
	#region Using Directives

	using System;
	using System.Reflection;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Reflection.Emit;

	#endregion

	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Property | AttributeTargets.Event, AllowMultiple = true)]
	public abstract class MetaExtensionMethodAttribute : OrderedAttribute
	{
		#region Implement

		protected internal virtual void Implement(MetaExtensionContext context, MethodGenerator methodGen, MethodBase method)
		{
			methodGen.Attributes.AddRange(AttributeGenerator.CreateAttrs(method));

			foreach (var parameter in method.GetParameters())
			{
				var paramGen = methodGen.CreateParameter(parameter.Name, parameter.Attributes);
				paramGen.Attributes.AddRange(AttributeGenerator.CreateAttrs(parameter));
			}
		}

		#endregion
	}
}