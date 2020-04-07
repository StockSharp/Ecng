namespace Ecng.Reflection.Aspects
{
	#region Using Directives

	using System;
	using System.Collections.Generic;
	using System.Reflection;
	using System.Linq;

	using Ecng.Common;
	using Ecng.Collections;
	using Ecng.Reflection.Emit;

	#endregion

	[Flags]
	public enum InterceptTypes
	{
		None = 0,
		Begin = 1,
		End = Begin << 1,
		Catch = End << 1,
		Finally = Catch << 1,
		All = Begin | End | Catch | Finally,
	}

	public class InterceptorAttribute : MetaExtensionMethodAttribute
	{
		#region Private Fields

		private readonly static Dictionary<TypeGenerator, FieldGenerator> _interceptorFields = new Dictionary<TypeGenerator, FieldGenerator>();

		private readonly InterceptorChain _interceptorChain = new InterceptorChain();

		#endregion

		#region InterceptorMethodAttribute.ctor()

		public InterceptorAttribute()
		{
		}

		public InterceptorAttribute(Type interceptorType)
			: this(new[] { interceptorType })
		{
		}

		public InterceptorAttribute(Type[] interceptorTypes)
		{
			if (interceptorTypes.HasNullItem())
				throw new ArgumentException(nameof(interceptorTypes));

			if (interceptorTypes.IsEmpty())
				throw new ArgumentOutOfRangeException(nameof(interceptorTypes));

			foreach (var interceptorType in interceptorTypes)
				_interceptorChain.Interceptors.Add(interceptorType.CreateInstance<Interceptor>());
		}

		#endregion

		#region Type

		private InterceptTypes _type = InterceptTypes.All;

		public InterceptTypes Type
		{
			get => _type;
			set => _type = value;
		}

		#endregion

		#region MetaExtensionMethodAttribute Members

		protected internal override void Implement(MetaExtensionContext context, MethodGenerator methodGen, MethodBase method)
		{
			var interceptorField = _interceptorFields.SafeAdd(context.TypeGenerator, delegate
			{
				var field = context.TypeGenerator.CreateField("_interceptor", typeof(Interceptor), FieldAttributes.Static | FieldAttributes.Private);
				context.AfterEmitInitFields.Add(field, _interceptorChain);
				return field;
			});
			
			//FieldGenerator interceptorField = (FieldGenerator)context["Interceptor"];

			var inRefArgs = methodGen.CreateLocal(typeof(Dictionary<string, object>));
			//methodGen.Locals.Add(inRefArgs);

			// Init inRefArgs
			methodGen
					.newobj(inRefArgs.Builder.LocalType)
					.stloc(inRefArgs);

			foreach (var param in method.GetParameters().Where(arg => !arg.IsOut))
			{
				methodGen
						.ldloc(inRefArgs)
						.ldstr(param.Name)
						.ldarg_s((byte)(param.Position + 1));

				if (param.IsOutput())
				{
					methodGen
							.LoadObj(param.ParameterType.GetElementType())
							.BoxIfValueType(param.ParameterType.GetElementType());
				}
				else
					methodGen.BoxIfValueType(param.ParameterType);

				methodGen.callvirt(inRefArgs.Builder.LocalType, "Add", typeof(string), typeof(object));
			}

			// Call Begin method
			methodGen
					.ldsfld(interceptorField)
					//.ldtoken(context.BaseType)
					//.call(typeof(Type), "GetTypeFromHandle")
					//.ldstr(method.Name)
					.ldarg_0()
					.ldloc(inRefArgs)
					.callvirt(interceptorField.Builder.FieldType, "Begin");

			methodGen
					.@try()
					.ldarg_0();

			foreach (ParameterInfo param in method.GetParameters())
				methodGen.ldarg_s((byte)(param.Position + 1));

			methodGen.call(method);

			LocalGenerator retValue = null;

			if (method is MethodInfo && ((MethodInfo)method).ReturnType != typeof(void))
			{
				retValue = methodGen.CreateLocal(((MethodInfo)method).ReturnType);
				//methodGen.Locals.Add(retValue);
				methodGen.stloc(retValue);
			}

			var refOutArgs = methodGen.CreateLocal(typeof(Dictionary<string, object>));
			//methodGen.Locals.Add(refOutArgs);

			// Init refOutArgs
			methodGen
					.newobj(refOutArgs.Builder.LocalType)
					.stloc(refOutArgs);

			foreach (var param in method.GetParameters().Where(arg => arg.IsOutput()))
			{
				methodGen
						.ldloc(refOutArgs)
						.ldstr(param.Name)
						.ldarg_s((byte)(param.Position + 1))
						.LoadObj(param.ParameterType.GetElementType())
						.BoxIfValueType(param.ParameterType.GetElementType())
						.callvirt(refOutArgs.Builder.LocalType, "Add", typeof(string), typeof(object));
			}

			methodGen.ldsfld(interceptorField);

			if (retValue != null)
			{
				methodGen
						.ldloc(retValue)
						.BoxIfValueType(retValue.Builder.LocalType);
			}
			else
			{
				if (method is ConstructorInfo)
					methodGen.ldarg_0();
				else
					methodGen.ldnull();
			}
			
			methodGen
					.ldloc(refOutArgs)
					.callvirt(interceptorField.Builder.FieldType, "End");

			LocalGenerator retValueReal = null;

			if (method is MethodInfo && ((MethodInfo)method).ReturnType != typeof(void))
			{
				retValueReal = methodGen.CreateLocal(((MethodInfo)method).ReturnType);
				//methodGen.Locals.Add(retValueReal);

				methodGen
						.ldloc(retValue)
						.stloc(retValueReal);
			}

			var exception = methodGen.CreateLocal(typeof(Exception));
			//methodGen.Locals.Add(exception);

			methodGen
					.@catch()
					.stloc(exception)
					.ldsfld(interceptorField)
					.ldloc(exception)
					.callvirt(interceptorField.Builder.FieldType, "Catch", typeof(Exception))
					.rethrow()
					.@finally()
					.ldsfld(interceptorField)
					.callvirt(interceptorField.Builder.FieldType, "Finally")
					//.end_finally()
					.end_try();

			if (retValueReal != null)
				methodGen.ldloc(retValueReal);

			methodGen.ret();

			base.Implement(context, methodGen, method);
		}

		/*
		protected override void ImplementMethod(MetaExtensionContext context, MethodInfo method, MethodGenerator methodGen)
		{
			ExtendMethod(context, methodGen, method);
		}

		protected override void ImplementProperty(MetaExtensionContext context, PropertyInfo property, PropertyGenerator propGen)
		{
			if (property.CanRead)
				ExtendMethod(context, propGen.CreateGetMethod(), property.GetGetMethod(true));

			if (property.CanWrite)
				ExtendMethod(context, propGen.CreateSetMethod(), property.GetSetMethod(true));
		}

		protected override void ImplementEvent(MetaExtensionContext context, EventInfo @event, EventGenerator eventGenerator)
		{
			ExtendMethod(context, eventGenerator.CreateAddMethod(), @event.GetAddMethod(true));
			ExtendMethod(context, eventGenerator.CreateRemoveMethod(), @event.GetRemoveMethod(true));
		}

		protected override void ImplementCtor(MetaExtensionContext context, ConstructorInfo ctor, MethodGenerator<ConstructorBuilder> ctorGen)
		{
			ExtendMethod(context, ctorGen, ctor);
		}
		*/

		#endregion
	}
}