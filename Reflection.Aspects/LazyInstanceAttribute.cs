namespace Ecng.Reflection.Aspects
{
	#region Using Directives

	using System;
	using System.Reflection;
	using System.Reflection.Emit;

	using Ecng.Common;
	using Ecng.Reflection.Emit;
	using Ecng.Collections;

	#endregion

	[AttributeUsage(AttributeTargets.Property)]
	public class LazyInstanceAttribute : DefaultImpAttribute
	{
		#region Private Fields

		private readonly object[] _args;

		#endregion

		#region LazyInstanceAttribute.ctor()

		public LazyInstanceAttribute()
		{
			_args = ArrayHelper<object>.EmptyArray;
		}

		public LazyInstanceAttribute(params object[] args)
		{
			if (args.HasNullItem())
				throw new ArgumentException("inRefArgs");

			_args = args;
		}

		#endregion

		#region Synchronize

		private bool _synchronize;

		public bool Synchronize
		{
			get { return _synchronize; }
			set
			{
				_synchronize = value;
				throw new NotImplementedException();
			}
		}

		#endregion

		#region DefaultImpAttribute Members

		protected override FieldGenerator CreateField(MetaExtensionContext context, MemberInfo member)
		{
			var memberType = member.GetMemberType();

			if (memberType.IsValueType)
				return context.TypeGenerator.CreateField("_" + member.Name, typeof(Nullable<>).Make(memberType), FieldAttributes.Private);
			else
				return base.CreateField(context, member);
		}

		protected override void ImplementGetMethod(MetaExtensionContext context, MethodGenerator getMethod, PropertyInfo property, FieldGenerator impField)
		{
			getMethod
					.ldarg_0();

			if (property.PropertyType.IsValueType)
			{
				getMethod
						.ldflda(impField)
						.call(impField.Builder.FieldType, "get_HasValue");
			}
			else
				getMethod.ldfld(impField);

			Label label = getMethod.DefineLabel();

			getMethod
					.brtrue_s(label)
					.ldarg_0();

			foreach (var arg in _args)
			{
				var field = context.TypeGenerator.CreateField(Guid.NewGuid().ToString(), arg.GetType(), FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.InitOnly);
				getMethod.ldsfld(field);
				context.AfterEmitInitFields.Add(field, arg);
			}

			Type[] argTypes;

			if (property.PropertyType.IsValueType)
			{
				if (_args.IsEmpty())
				{
					var local = getMethod.CreateLocal(property.PropertyType);
					//getMethod.Locals.Add(local);

					getMethod
							.ldloca(local)
							.initobj(property.PropertyType)
							.ldloc(local);
				}
				else
					getMethod.newobj(property.PropertyType, ReflectionHelper.GetArgTypes(_args));

				argTypes = new [] { property.PropertyType };
			}
			else
				argTypes = ReflectionHelper.GetArgTypes(_args);

			getMethod
					.newobj(impField.Builder.FieldType, argTypes)
					.stfld(impField)
					.MarkLabel(label);

			if (property.PropertyType.IsValueType)
			{
				getMethod
						.ldarg_0()
						.ldflda(impField)
						.call(impField.Builder.FieldType, "get_Value")
						.ret();
			}
			else
				base.ImplementGetMethod(context, getMethod, property, impField);
		}

		protected override void ImplementAddMethod(MetaExtensionContext context, MethodGenerator addMethod, EventInfo @event, FieldGenerator impField)
		{
			throw new NotSupportedException();
		}

		protected override void ImplementRemoveMethod(MetaExtensionContext context, MethodGenerator removeMethod, EventInfo @event, FieldGenerator impField)
		{
			throw new NotSupportedException();
		}

		protected override void ImplementMethod(MetaExtensionContext context, MethodGenerator methodGen, MethodInfo method)
		{
			throw new NotSupportedException();
		}

		protected override void ImplementCtor(MetaExtensionContext context, MethodGenerator ctorGen, ConstructorInfo ctor)
		{
			throw new NotSupportedException();
		}

		#endregion
	}
}