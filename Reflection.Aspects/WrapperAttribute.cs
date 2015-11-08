namespace Ecng.Reflection.Aspects
{
	#region Using Directives

	using System;
	using System.Reflection;

	using Ecng.Common;
	using Ecng.Reflection.Emit;

	#endregion

	public class WrapperAttribute : DefaultImpAttribute
	{
		#region Private Fields

		private readonly Type _wrapperType;

		#endregion

		#region WrapperAttribute.ctor()

		public WrapperAttribute(Type wrapperType)
		{
			if (wrapperType == null)
				throw new ArgumentNullException(nameof(wrapperType));

			_wrapperType = wrapperType;
		}

		#endregion

		#region DefaultImpAttribute Members

		protected override FieldGenerator CreateField(MetaExtensionContext context, MemberInfo member)
		{
			return context.TypeGenerator.CreateField("_" + member.Name, _wrapperType.Make(member.GetMemberType()), FieldAttributes.Private);
		}

		protected override void ImplementGetMethod(MetaExtensionContext context, MethodGenerator getMethod, PropertyInfo property, FieldGenerator impField)
		{
			getMethod
					.ldarg_0()
					.ldfld(impField)
					.callvirt(impField.Builder.FieldType, "get_Value")
					.ret();
		}

		protected override void ImplementSetMethod(MetaExtensionContext context, MethodGenerator setMethod, PropertyInfo property, FieldGenerator impField)
		{
			setMethod
					.ldarg_0()
					.ldfld(impField)
					.ldarg_1()
					.callvirt(impField.Builder.FieldType, "set_Value")
					.ret();
		}

		protected override void ImplementAddMethod(MetaExtensionContext context, MethodGenerator addMethod, EventInfo @event, FieldGenerator impField)
		{
			throw new NotSupportedException();
		}

		protected override void ImplementRemoveMethod(MetaExtensionContext context, MethodGenerator removeMethod, EventInfo @event, FieldGenerator impField)
		{
			throw new NotSupportedException();
		}

		protected override void ImplementCtor(MetaExtensionContext context, MethodGenerator ctorGen, ConstructorInfo ctor)
		{
			throw new NotSupportedException();
		}

		protected override void ImplementMethod(MetaExtensionContext context, MethodGenerator methodGen, MethodInfo method)
		{
			throw new NotSupportedException();
		}

		#endregion
	}
}