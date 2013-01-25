namespace Ecng.Reflection.Aspects
{
	#region Using Directives

	using System;
	using System.Reflection;

	using Ecng.Reflection.Emit;

	#endregion

	public class NotImpExceptionAttribute : MetaExtensionMethodAttribute
	{
		#region MetaExtensionMethodAttribute Members

		protected internal override void Implement(MetaExtensionContext context, MethodGenerator methodGen, MethodBase method)
		{
			methodGen.@throw(typeof(NotImplementedException));
			base.Implement(context, methodGen, method);
		}

		/*
		protected override void ImplementMethod(MetaExtensionContext context, MethodInfo method, MethodGenerator methodGen)
		{
			methodGen.@throw(typeof(NotImplementedException));
		}

		protected override void ImplementProperty(MetaExtensionContext context, PropertyInfo property, PropertyGenerator propGen)
		{
			if (property.CanRead)
			{
				propGen
					.CreateGetMethod()
					.@throw(typeof(NotImplementedException));
			}

			if (property.CanWrite)
			{
				propGen
					.CreateSetMethod()
					.@throw(typeof(NotImplementedException));
			}
		}

		protected override void ImplementEvent(MetaExtensionContext context, EventInfo @event, EventGenerator eventGenerator)
		{
			MethodAttributes attr = GetAttributes(context.BaseType, @event.GetAddMethod(true));

			eventGenerator
					.CreateAddMethod(attr)
					.@throw(typeof(NotImplementedException));

			eventGenerator
					.CreateRemoveMethod(attr)
					.@throw(typeof(NotImplementedException));
		}

		protected override void ImplementCtor(MetaExtensionContext context, ConstructorInfo ctor, MethodGenerator<ConstructorBuilder> ctorGen)
		{
			ctorGen.@throw(typeof(NotImplementedException));
		}
		*/

		#endregion
	}
}