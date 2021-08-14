namespace Ecng.Reflection
{
	#region Using Directives

	using System;
	using System.Collections.Generic;
	using System.Reflection;
	using System.Reflection.Emit;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Reflection.Emit;

	#endregion

	public abstract class FastInvoker
	{
		private class ReflectionFastInvoker : FastInvoker
		{
			private readonly bool? _isGetter;
			private ParameterInfo[] _parameters;

			public ReflectionFastInvoker(MemberInfo member, bool? isGetter)
				: base(member)
			{
				_isGetter = isGetter;
			}

			private object Invoke(object instance, object arg)
			{
				if (Member is MethodInfo method)
				{
					if (_parameters == null)
						_parameters = method.GetParameters();

					if (_parameters.Length == 0)
						arg = ArrayHelper.Empty<object>();
					
					return method.Invoke(instance, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance, null, arg is object[] arr ? arr : new[] { arg }, null);
				}
				else if (Member is ConstructorInfo ctor)
				{
					if (_parameters == null)
						_parameters = ctor.GetParameters();

					if (_parameters.Length == 0)
						arg = ArrayHelper.Empty<object>();

					return ctor.Invoke(arg is object[] arr ? arr : new[] { arg });
				}
				else if (Member is FieldInfo field)
				{
					switch (_isGetter)
					{
						case true:
							return field.GetValue(instance);
						case false:
							field.SetValue(instance, arg);
							return null;
						default:
							throw new InvalidOperationException();
					}
				}
				else if (Member is PropertyInfo prop)
				{
					switch (_isGetter)
					{
						case true:
						{
							return prop.GetValue(instance, arg == null ? ArrayHelper.Empty<object>() : (arg is object[] arr ? arr : new[] { arg }));
						}
						case false:
						{
							//if (prop.IsIndexer())
							//	prop.SetValue(instance, args[0], args.Skip(1).ToArray());
							//else
							prop.SetValue(instance, arg is object[] arr ? arr[0] : arg);

							return null;
						}
						default:
							throw new InvalidOperationException();
					}
				}
				else
					throw new NotSupportedException(Member.GetType().ToString());
			}

			public override object Ctor(object arg)
			{
				return Invoke(null, arg);
			}

			public override object GetValue(object instance)
			{
				return Invoke(instance, null);
			}

			public override object ReturnInvoke(object arg)
			{
				return Invoke(null, arg);
			}

			public override object ReturnInvoke(object instance, object arg)
			{
				return Invoke(instance, arg);
			}

			public override void SetValue(object arg)
			{
				Invoke(null, arg);
			}

			public override object SetValue(object instance, object arg)
			{
				return Invoke(instance, arg);
			}

			public override object StaticGetValue()
			{
				return Invoke(null, null);
			}

			public override void VoidInvoke(object arg)
			{
				Invoke(null, arg);
			}

			public override void VoidInvoke(object instance, object arg)
			{
				Invoke(instance, arg);
			}
		}

		#region Private Fields

		private readonly static Dictionary<MemberInfo, FastInvoker> _getValueInvokeDelegates = new Dictionary<MemberInfo, FastInvoker>();
		private readonly static Dictionary<MemberInfo, FastInvoker> _setValueInvokeDelegates = new Dictionary<MemberInfo, FastInvoker>();
		private readonly static Dictionary<MemberInfo, FastInvoker> _methodInvokeDelegates = new Dictionary<MemberInfo, FastInvoker>();

		#endregion

		#region FastInvoker.ctor()

		protected FastInvoker(MemberInfo member)
		{
			Member = member ?? throw new ArgumentNullException(nameof(member));
		}

		#endregion

		#region Member

		public MemberInfo Member { get; }

		#endregion

		#region Create

		public static FastInvoker Create(ConstructorInfo ctor)
		{
			return CreateCore(ctor, null);
		}

		public static FastInvoker Create(PropertyInfo property, bool isGetter)
		{
			return CreateCore(property, isGetter);
		}

		public static FastInvoker Create(FieldInfo field, bool isGetter)
		{
			return CreateCore(field, isGetter);
		}

		public static FastInvoker Create(EventInfo evt, bool isSubscribe)
		{
			return CreateCore(evt, isSubscribe);
		}

		public static FastInvoker Create(MethodInfo method)
		{
			return CreateCore(method, null);
		}

		#endregion

		public abstract object Ctor(object arg);

		public abstract object StaticGetValue();
		public abstract object GetValue(object instance);

		public abstract void SetValue(object arg);
		public abstract object SetValue(object instance, object arg);

		public abstract void VoidInvoke(object arg);
		public abstract void VoidInvoke(object instance, object arg);

		public abstract object ReturnInvoke(object arg);
		public abstract object ReturnInvoke(object instance, object arg);

		#region CreateCore

		private static bool _dynMethodNotSupported;

		private static FastInvoker CreateCore(MemberInfo member, bool? isGetter)
		{
			if (member == null)
				throw new ArgumentNullException(nameof(member));

			return GetCache(member, isGetter).SafeAdd(member, delegate
			{
				if (_dynMethodNotSupported)
					return new ReflectionFastInvoker(member, isGetter);

				try
				{
					var method = member as MethodInfo;
					var ctor = member as ConstructorInfo;
					//FieldInfo field = member as FieldInfo;
					var property = member as PropertyInfo;

					if (member is EventInfo evt)
					{
						method = isGetter == false ? evt.GetAddMethod(true) : evt.GetRemoveMethod(true);
					}
					else if (property != null && member.IsIndexer())
					{
						method = isGetter == true ? property.GetGetMethod(true) : property.GetSetMethod(true);
					}

					Type returnType;
					Type instanceType;
					Type argType;

					Type memberType = member.GetMemberType();

					if (ctor != null || member.IsStatic())
						instanceType = typeof(VoidType);
					else
						instanceType = member.ReflectedType;

					if (member is MethodBase mb)
					{
						var parameters = mb.GetParameters();

						if (parameters.Length == 1 && !parameters[0].IsOutput())
							argType = parameters[0].ParameterType;
						else
							argType = typeof(object[]);

						if (ctor == null)
							returnType = memberType != typeof(void) ? memberType : typeof(VoidType);
						else
							returnType = ctor.ReflectedType;
					}
					else
					{
						if (member.IsIndexer())
						{
							if (isGetter == true)
							{
								argType = property.GetIndexerType();
								returnType = memberType;
							}
							else
							{
								argType = typeof(object[]);
								returnType = instanceType;
							}
						}
						else
						{
							if (isGetter == true)
							{
								argType = typeof(VoidType);
								returnType = memberType;
							}
							else
							{
								argType = memberType;
								returnType = typeof(VoidType);
							}
						}
					}

					var dlgType = GetDelegateType(member, isGetter).Make(instanceType, argType, returnType);
					var dlg = CreateDelegate(dlgType, instanceType, argType, ctor, method, member, isGetter);

					return (FastInvoker)typeof(FastInvoker<,,>)
						.Make(dlgType.GetGenericArguments())
						.GetMember<ConstructorInfo>(typeof(MemberInfo), typeof(Delegate))
						.Invoke(new object[] { member, dlg });
				}
				catch (PlatformNotSupportedException)
				{
					_dynMethodNotSupported = true;
					return new ReflectionFastInvoker(member, isGetter);
				}
			});
		}

		#endregion

		#region GetCache

		private static Dictionary<MemberInfo, FastInvoker> GetCache(MemberInfo member, bool? isGetter)
		{
			if (member is FieldInfo || member is PropertyInfo || member is EventInfo)
				return (isGetter == true) ? _getValueInvokeDelegates : _setValueInvokeDelegates;
			else
				return _methodInvokeDelegates;
		}

		#endregion

		#region GetDelegateType

		private static Type GetDelegateType(MemberInfo member, bool? isGetter)
		{
			bool isStatic = member.IsStatic();

			if (member is FieldInfo || member is PropertyInfo)
			{
				if (member.IsIndexer())
				{
					if (isGetter == true)
						return isStatic ? typeof(FastInvoker<,,>.StaticReturnMethodCallback) : typeof(FastInvoker<,,>.ReturnMethodCallback);
					else
						return isStatic ? typeof(FastInvoker<,,>.StaticVoidMethodCallback) : typeof(FastInvoker<,,>.ReturnMethodCallback);
				}
				else
				{
					if (isGetter == true)
						return isStatic ? typeof(FastInvoker<,,>.StaticGetValueCallback) : typeof(FastInvoker<,,>.GetValueCallback);
					else
						return isStatic ? typeof(FastInvoker<,,>.StaticSetValueCallback) : typeof(FastInvoker<,,>.SetValueCallback);
				}
			}
			else if (member is EventInfo)
				return isStatic ? typeof(FastInvoker<,,>.StaticVoidMethodCallback) : typeof(FastInvoker<,,>.VoidMethodCallback);
			else
			{
				if (member is MethodInfo mi)
				{
					if (mi.ReturnType == typeof(void))
						return isStatic ? typeof(FastInvoker<,,>.StaticVoidMethodCallback) : typeof(FastInvoker<,,>.VoidMethodCallback);
					else
						return isStatic ? typeof(FastInvoker<,,>.StaticReturnMethodCallback) : typeof(FastInvoker<,,>.ReturnMethodCallback);
				}
				else
					return typeof(FastInvoker<,,>.CtorCallback);
			}
		}

		#endregion

		#region CreateDelegate

		private static Delegate CreateDelegate(Type delegType, Type instanceType, Type argType, ConstructorInfo ctor, MethodInfo method, MemberInfo member, bool? isGetter)
		{
			var parameters = (member is MethodBase) ? member.To<MethodBase>().GetParameters() : new ParameterInfo[0];

			if (member.IsIndexer() && isGetter == false)
				parameters = ((PropertyInfo)member).GetSetMethod(true).GetParameters();

			var refParameters = parameters.Where(param => param.IsOutput()).ToArray();

			var refLocals = new Dictionary<ParameterInfo, LocalGenerator>();

			var invokeMethod = delegType.GetInvokeMethod();

			var returnType = invokeMethod.ReturnType;

			var isPropSet = instanceType != typeof(VoidType) && (member is PropertyInfo || member is FieldInfo) && isGetter == false;

			if (returnType == typeof(void) && isPropSet)
				returnType = instanceType;

			MethodGenerator methodGenerator;

			if (AssemblyHolder.NeedCache)
			{
				var typeGen = AssemblyHolder.CreateType("Test", TypeAttributes.Class);
				methodGenerator = typeGen.CreateMethod("Method", MethodAttributes.Public | MethodAttributes.Static, returnType, invokeMethod.GetParameterTypes(false).Select(t => t.type).ToArray());
			}
			else
			{
				var dymMethod = new DynamicMethod("", returnType, invokeMethod.GetParameterTypes().Select(t => t.type).ToArray()
#if !SILVERLIGHT
				, member.ReflectedType, true
#endif
				);

				methodGenerator = new MethodGenerator(dymMethod);
			}

			if (instanceType != typeof(VoidType))
				methodGenerator.CreateParameter("instance");

			if (argType != typeof(VoidType))
				methodGenerator.CreateParameter("arg");

			foreach (var refParam in refParameters)
			{
				var local = methodGenerator.CreateLocal(refParam.ParameterType.GetElementType());
				//methodGenerator.Locals.Add(local);

				if (!refParam.IsOut)
				{
					methodGenerator
								.ldarg_s(instanceType == typeof(VoidType) ? (byte)0 : (byte)1)
								.ldc_i4_s((byte)refParam.Position)
								.ldelem_ref()
								.Cast(local.Builder.LocalType)
								.stloc(local);
				}

				refLocals.Add(refParam, local);
			}

			// If method isn't static push target instance on top of stack. Argument 0 of dynamic method is target instance.
			if (instanceType != typeof(VoidType))
			{
				if (member.ReflectedType.IsClass)
					methodGenerator.ldarg_0();
				else
					methodGenerator.ldarga_s(0);
			}

			LocalGenerator returnLocal = null;

			if (refParameters.Length > 0)
			{
				if (ctor != null || (method != null && method.ReturnType != typeof(void)))
				{
					returnLocal = methodGenerator.CreateLocal(invokeMethod.ReturnType);
					//methodGenerator.Locals.Add(returnLocal);

					if (ctor != null && ctor.ReflectedType.IsValueType)
						methodGenerator.ldloca(returnLocal);
				}
			}

			if (argType != typeof(VoidType))
			{
				if ((member.IsIndexer() && isGetter == false) || !((parameters.IsEmpty() && !(member is MethodBase)) || (parameters.Length == 1 && !parameters[0].IsOutput())))
				{
					// Lay out inRefArgs array onto stack.
					foreach (var param in parameters)
					{
						// Push inRefArgs array reference onto the stack, followed by the current argument index (i).
						// The Ldelem_Ref opcode will resolve them to inRefArgs[i].

						if (!param.IsOutput())
						{
							// Argument 1 of dynamic method is argument array.
							methodGenerator
										.ldarg_s(instanceType == typeof(VoidType) ? (byte)0 : (byte)1)
										.ldc_i4_s((byte)param.Position)
										.ldelem_ref()
										.Cast(param.ParameterType);
						}
						else
							methodGenerator.ldloca(refLocals[param]);
					}
				}
				else
					methodGenerator.ldarg_s(instanceType == typeof(VoidType) ? (byte)0 : (byte)1);
			}

			if (member is MethodBase)
			{
				if (ctor != null)
				{
					if (ctor.ReflectedType.IsValueType)
					{
						if (parameters.IsEmpty())
						{
							var local = methodGenerator.CreateLocal(ctor.ReflectedType);
							//methodGenerator.Locals.Add(local);

							methodGenerator
										.ldloca(local)
										.initobj(ctor.ReflectedType)
										.ldloc(local);
						}
						else if (refParameters.Length > 0)
							methodGenerator.call(ctor);
						else
							methodGenerator.newobj(ctor);
					}
					else
						methodGenerator.newobj(ctor);
				}
				else
					methodGenerator.CallMethod(method);

				if (returnLocal != null && (returnLocal.Builder.LocalType.IsClass || returnLocal.Builder.LocalType.IsPrimitive) && refParameters.Length > 0)
					methodGenerator.stloc(returnLocal);

				foreach (var refParam in refParameters)
				{
					methodGenerator
								.ldarg_s(instanceType == typeof(VoidType) ? (byte)0 : (byte)1)
								.ldc_i4_s((byte)refParam.Position)
								.ldloc(refLocals[refParam])
								.BoxIfValueType(refLocals[refParam].Builder.LocalType)
								.stelem_ref();
				}
			}
			else
			{
				if (isGetter == true)
					methodGenerator.GetMember(true, member);
				else
				{
					if (member.IsIndexer())
					{
						methodGenerator
									.ldarg_1()
									.ldc_i4_1()
									.ldelem_ref()
									.Cast(member.GetMemberType());
					}

					methodGenerator.SetMember(member);

					if (isPropSet)
						methodGenerator.ldarg_0();
				}
			}

			if (returnLocal != null)
				methodGenerator.ldloc(returnLocal);

			methodGenerator.ret();

			if (AssemblyHolder.NeedCache)
				return Delegate.CreateDelegate(delegType, methodGenerator.TypeGenerator.CompileType().GetMethod("Method"));
			else
				return ((DynamicMethod)methodGenerator.Builder).CreateDelegate(delegType);
		}

		#endregion
	}

	public sealed class FastInvoker<I, A, R> : FastInvoker
	{
		#region Private Fields

		internal delegate R CtorCallback(A arg);
		internal delegate R ReturnMethodCallback(I instance, A arg);
		internal delegate void VoidMethodCallback(I instance, A arg);
		internal delegate R GetValueCallback(I instance);
		internal delegate I SetValueCallback(I instance, A arg);
		internal delegate R StaticReturnMethodCallback(A arg);
		internal delegate void StaticVoidMethodCallback(A arg);
		internal delegate R StaticGetValueCallback();
		internal delegate void StaticSetValueCallback(A arg);

		private readonly CtorCallback _ctor;
		private readonly ReturnMethodCallback _returnMethod;
		private readonly VoidMethodCallback _voidMethod;
		private readonly GetValueCallback _getValue;
		private readonly SetValueCallback _setValue;
		private readonly StaticReturnMethodCallback _staticReturnMethod;
		private readonly StaticVoidMethodCallback _staticVoidMethod;
		private readonly StaticGetValueCallback _staticGetValue;
		private readonly StaticSetValueCallback _staticSetValue;

		private readonly FastInvoker _invoker;

		#endregion

		#region FastInvoker.ctor()

#if SILVERLIGHT
		public
#else
		private
#endif
			FastInvoker(MemberInfo member, Delegate callback)
			: base(member)
		{
			if (callback == null)
				throw new ArgumentNullException(nameof(callback));

			if (callback is CtorCallback cb1)
				_ctor = cb1;
			else if (callback is ReturnMethodCallback cb2)
				_returnMethod = cb2;
			else if (callback is VoidMethodCallback cb3)
				_voidMethod = cb3;
			else if (callback is GetValueCallback cb4)
				_getValue = cb4;
			else if (callback is SetValueCallback cb5)
				_setValue = cb5;
			else if (callback is StaticReturnMethodCallback cb6)
				_staticReturnMethod = cb6;
			else if (callback is StaticVoidMethodCallback cb7)
				_staticVoidMethod = cb7;
			else if (callback is StaticGetValueCallback cb8)
				_staticGetValue = cb8;
			else if (callback is StaticSetValueCallback cb9)
				_staticSetValue = cb9;
		}

		private FastInvoker(FastInvoker invoker)
			: base(invoker.Member)
		{
			_invoker = invoker;
		}

		#endregion

		#region Create

		public new static FastInvoker<I, A, R> Create(ConstructorInfo ctor)
		{
			//return new FastInvoker<I, A, R>(FastInvoker.Create(ctor));
			return Create(FastInvoker.Create(ctor));
		}

		public new static FastInvoker<I, A, R> Create(PropertyInfo property, bool isGetter)
		{
			//return new FastInvoker<I, A, R>(FastInvoker.Create(property, isGetter));
			return Create(FastInvoker.Create(property, isGetter));
		}

		public new static FastInvoker<I, A, R> Create(FieldInfo field, bool isGetter)
		{
			//return new FastInvoker<I, A, R>(FastInvoker.Create(field, isGetter));
			return Create(FastInvoker.Create(field, isGetter));
		}

		public new static FastInvoker<I, A, R> Create(EventInfo evt, bool isSubscribe)
		{
			//return new FastInvoker<I, A, R>(FastInvoker.Create(evt, isSubscribe));
			return Create(FastInvoker.Create(evt, isSubscribe));
		}

		public new static FastInvoker<I, A, R> Create(MethodInfo method)
		{
			//return new FastInvoker<I, A, R>(FastInvoker.Create(method));
			return Create(FastInvoker.Create(method));
		}

		private static FastInvoker<I, A, R> Create(FastInvoker invoker)
		{
			if (invoker is FastInvoker<I, A, R>)
				return (FastInvoker<I, A, R>)invoker;
			else
				return new FastInvoker<I, A, R>(invoker);
		}

		#endregion

		#region Ctor

		public R Ctor(A arg)
		{
			if (_invoker == null)
				return _ctor(arg);
			else
				return (R)_invoker.Ctor(arg);
		}

		#endregion

		#region GetValue

		public R GetValue()
		{
			if (_invoker == null)
				return _staticGetValue();
			else
				return (R)_invoker.StaticGetValue();
		}

		public R GetValue(I instance)
		{
			if (_invoker == null)
				return _getValue(instance);
			else
				return (R)_invoker.GetValue(instance);
		}

		#endregion

		#region SetValue

		public void SetValue(A arg)
		{
			if (_invoker == null)
				_staticSetValue(arg);
			else
				_invoker.SetValue(arg);
		}

		public I SetValue(I instance, A arg)
		{
			if (_invoker == null)
				return _setValue(instance, arg);
			else
				return (I)_invoker.SetValue(instance, arg);
		}

		#endregion

		#region VoidInvoke

		public void VoidInvoke(A arg)
		{
			if (_invoker == null)
				_staticVoidMethod(arg);
			else
				_invoker.VoidInvoke(arg);
		}

		public void VoidInvoke(I instance, A arg)
		{
			if (_invoker == null)
				_voidMethod(instance, arg);
			else
				_invoker.VoidInvoke(instance, arg);
		}

		#endregion

		#region ReturnInvoke

		public R ReturnInvoke(A arg)
		{
			if (_invoker == null)
				return _staticReturnMethod(arg);
			else
				return (R)_invoker.ReturnInvoke(arg);
		}

		public R ReturnInvoke(I instance, A arg)
		{
			if (_invoker == null)
				return _returnMethod(instance, arg);
			else
				return (R)_invoker.ReturnInvoke(instance, arg);
		}

		#endregion

		#region FastInvoker Members

		public override object Ctor(object arg)
		{
			return Ctor((A)arg);
		}

		public override object StaticGetValue()
		{
			return GetValue();
		}

		public override object GetValue(object instance)
		{
			return GetValue((I)instance);
		}

		public override void SetValue(object arg)
		{
			SetValue((A)arg);
		}

		public override object SetValue(object instance, object arg)
		{
			return SetValue((I)instance, (A)arg);
		}

		public override object ReturnInvoke(object arg)
		{
			return ReturnInvoke((A)arg);
		}

		public override object ReturnInvoke(object instance, object arg)
		{
			return ReturnInvoke((I)instance, (A)arg);
		}

		public override void VoidInvoke(object arg)
		{
			VoidInvoke((A)arg);
		}

		public override void VoidInvoke(object instance, object arg)
		{
			VoidInvoke((I)instance, (A)arg);
		}

		#endregion
	}
}