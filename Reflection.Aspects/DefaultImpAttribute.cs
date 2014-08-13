namespace Ecng.Reflection.Aspects
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Reflection.Emit;

	public class DefaultImpAttribute : MetaExtensionMethodAttribute
	{
		#region Private Fields

		private static readonly Dictionary<Tuple<TypeGenerator, MemberInfo>, FieldGenerator> _fields = new Dictionary<Tuple<TypeGenerator, MemberInfo>, FieldGenerator>();

		#endregion

		#region MetaExtensionMethodAttribute Members

		private enum AccessorTypes
		{
			Get,
			Set,
			Add,
			Remove,
		}

		protected internal override void Implement(MetaExtensionContext context, MethodGenerator methodGen, MethodBase method)
		{
			MemberInfo owner = null;

			if (method is MethodInfo)
				owner = ((MethodInfo)method).GetAccessorOwner();

			if (owner != null)
			{
				AccessorTypes type;

				if (owner is PropertyInfo)
					type = (((PropertyInfo)owner).GetGetMethod(true) == method) ? AccessorTypes.Get : AccessorTypes.Set;
				else if (owner is EventInfo)
					type = (((EventInfo)owner).GetAddMethod(true) == method) ? AccessorTypes.Add : AccessorTypes.Remove;
				else
					throw new ArgumentException("method");

				switch (type)
				{
					case AccessorTypes.Get:
						ImplementGetMethod(context, methodGen, (PropertyInfo)owner, GetField(context, owner));
						break;
					case AccessorTypes.Set:
						ImplementSetMethod(context, methodGen, (PropertyInfo)owner, GetField(context, owner));
						break;
					case AccessorTypes.Add:
						ImplementAddMethod(context, methodGen, (EventInfo)owner, GetField(context, owner));
						break;
					case AccessorTypes.Remove:
						ImplementRemoveMethod(context, methodGen, (EventInfo)owner, GetField(context, owner));
						break;
					default:
						throw new ArgumentException("method");
				}
			}
			else
			{
				if (method is MethodInfo)
					ImplementMethod(context, methodGen, (MethodInfo)method);
				else if (method is ConstructorInfo)
					ImplementCtor(context, methodGen, (ConstructorInfo)method);
				else
					throw new ArgumentException("method");
			}

			base.Implement(context, methodGen, method);
		}

		#endregion

		private FieldGenerator GetField(MetaExtensionContext context, MemberInfo owner)
		{
			return _fields.SafeAdd(new Tuple<TypeGenerator, MemberInfo>(context.TypeGenerator, owner), delegate
			{
				return CreateField(context, owner);
			});
		}

		protected virtual FieldGenerator CreateField(MetaExtensionContext context, MemberInfo member)
		{
			var field = context.TypeGenerator.CreateField("_" + member.Name, member.GetMemberType(), FieldAttributes.Private);

			field.Attributes.AddRange(AttributeGenerator.CreateAttrs(member).Where(arg =>
			{
				var attr = arg.Ctor.ReflectedType.GetAttribute<AttributeUsageAttribute>();
				return attr.ValidOn.Contains(AttributeTargets.Field);
			}));

			return field;
		}

		protected virtual void ImplementGetMethod(MetaExtensionContext context, MethodGenerator getMethod, PropertyInfo property, FieldGenerator impField)
		{
			getMethod
					.ldarg_0()
					.ldfld(impField)
					.ret();
		}

		protected virtual void ImplementSetMethod(MetaExtensionContext context, MethodGenerator setMethod, PropertyInfo property, FieldGenerator impField)
		{
			setMethod
					.ldarg_0()
					.ldarg_1()
					.stfld(impField.Builder)
					.ret();
		}

		protected virtual void ImplementAddMethod(MetaExtensionContext context, MethodGenerator addMethod, EventInfo @event, FieldGenerator impField)
		{
			addMethod
					.ldarg_0()
					.ldarg_0()
					.GetMember(false, impField.Builder)
					.ldarg_1()
					.call(typeof(Delegate).GetMember<MethodInfo>("Combine", new [] { typeof(Delegate), typeof(Delegate) }))
					.castclass(impField.Builder.FieldType)
					.stfld(impField)
					.ret();
		}

		protected virtual void ImplementRemoveMethod(MetaExtensionContext context, MethodGenerator removeMethod, EventInfo @event, FieldGenerator impField)
		{
			removeMethod
						.ldarg_0()
						.ldarg_0()
						.GetMember(false, impField.Builder)
						.ldarg_1()
						.call(typeof(Delegate).GetMember<MethodInfo>("Remove"))
						.castclass(impField.Builder.FieldType)
						.stfld(impField)
						.ret();
		}

		protected virtual void ImplementMethod(MetaExtensionContext context, MethodGenerator methodGen, MethodInfo method)
		{
			if (method.ReturnType != typeof(void))
			{
				if (method.ReturnType.ContainsGenericParameters)
					methodGen.ldnull();
				else
					methodGen.Load(method.ReturnType);
			}

			methodGen.ret();
		}

		protected virtual void ImplementCtor(MetaExtensionContext context, MethodGenerator ctorGen, ConstructorInfo ctor)
		{
			foreach (var field in context.TypeGenerator.Fields)
			{
				if (field.Attributes.Any(arg => arg.Ctor.DeclaringType.IsAssignableFrom(typeof(WrapperAttribute))))
				{
					ctorGen
							.ldarg_0()
							.newobj(field.Builder.FieldType)
							.stfld(field.Builder);
				}
			}

			ctorGen.ldarg_0();

			foreach (var param in ctor.GetParameters())
				ctorGen.ldarg_s((byte)(param.Position + 1));

			ctorGen
					.call(ctor)
					.ret();
		}
	}
}