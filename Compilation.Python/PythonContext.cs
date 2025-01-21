namespace Ecng.Compilation.Python;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

using Ecng.Common;

using IronPython.Runtime;
using IronPython.Runtime.Types;

using Microsoft.Scripting.Hosting;

class PythonContext(ScriptScope scope) : Disposable, ICompilerContext
{
	private class AssemblyImpl : Assembly
	{
		private class TypeImpl : Type
		{
			private class EventImpl(string name, Type eventType, TypeImpl declaringType) : EventInfo
			{
				private readonly string _name = name;
				private readonly Type _eventType = eventType;
				private readonly TypeImpl _declaringType = declaringType;

				public override string Name => _name;
				public override Type EventHandlerType => _eventType;
				public override Type DeclaringType => _declaringType;
				public override Type ReflectedType => _declaringType;
				public override EventAttributes Attributes => EventAttributes.None;

				public override MethodInfo GetAddMethod(bool nonPublic) => null;
				public override MethodInfo GetRaiseMethod(bool nonPublic) => null;
				public override MethodInfo GetRemoveMethod(bool nonPublic) => null;
				public override MethodInfo[] GetOtherMethods(bool nonPublic) => [];

				public override object[] GetCustomAttributes(bool inherit) => [];
				public override object[] GetCustomAttributes(Type attributeType, bool inherit) => [];
				public override bool IsDefined(Type attributeType, bool inherit) => false;
			}

			private class MethodImpl(PythonFunction function, TypeImpl declaringType) : MethodInfo
			{
				private class ParameterImpl(string name, Type parameterType, int position, MethodInfo method) : ParameterInfo
				{
					private readonly string _name = name;
					private readonly Type _parameterType = parameterType;
					private readonly int _position = position;
					private readonly MethodInfo _method = method;

					public override string Name => _name;
					public override Type ParameterType => _parameterType;
					public override int Position => _position;
					public override ParameterAttributes Attributes => ParameterAttributes.None;
					public override MemberInfo Member => _method;
					public override object DefaultValue => null;
					public override object RawDefaultValue => null;
					public override bool HasDefaultValue => false;
				}

				private readonly PythonFunction _function = function;
				private readonly TypeImpl _declaringType = declaringType;
				private readonly ParameterInfo[] _parameters;

				public MethodImpl(PythonFunction function, TypeImpl declaringType, Type[] paramTypes)
					: this(function, declaringType)
				{
					_parameters = paramTypes.Select((t, i) => new ParameterImpl($"param{i}", t, i, this)).ToArray();
				}

				public override string Name => _function.__name__;
				public override Type DeclaringType => _declaringType;
				public override Type ReflectedType => _declaringType;
				public override RuntimeMethodHandle MethodHandle => throw new NotImplementedException();
				public override MethodAttributes Attributes => MethodAttributes.Public;
				public override CallingConventions CallingConvention => CallingConventions.Standard;
				public override Type ReturnType => typeof(object);

				public override ParameterInfo[] GetParameters() => _parameters;
				public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
					=> _function.__call__(DefaultContext.Default, obj, parameters);

				public override ICustomAttributeProvider ReturnTypeCustomAttributes => null;
				public override MethodInfo GetBaseDefinition() => this;
				public override object[] GetCustomAttributes(bool inherit) => [];
				public override object[] GetCustomAttributes(Type attributeType, bool inherit) => [];
				public override bool IsDefined(Type attributeType, bool inherit) => false;
				public override MethodImplAttributes GetMethodImplementationFlags() => throw new NotImplementedException();
			}

			private class PythonPropertyImpl(PythonProperty property, Type propertyType, TypeImpl declaringType) : PropertyInfo
			{
				private readonly PythonProperty _property = property;
				private readonly Type _propertyType = propertyType;
				private readonly TypeImpl _declaringType = declaringType;

				public override string Name => ((PythonFunction)_property.fget).__name__;
				public override Type PropertyType => _propertyType;
				public override PropertyAttributes Attributes => PropertyAttributes.None;
				public override bool CanRead => true;
				public override bool CanWrite => _property.fset != null;
				public override Type DeclaringType => _declaringType;
				public override Type ReflectedType => _declaringType;

				public override MethodInfo GetGetMethod(bool nonPublic) => null;
				public override MethodInfo GetSetMethod(bool nonPublic) => null;
				public override ParameterInfo[] GetIndexParameters() => [];

				public override object GetValue(object obj, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture)
					=> _declaringType._ops.GetMember(obj, Name);

				public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture)
					=> _declaringType._ops.SetMember(obj, Name, value);

				public override MethodInfo[] GetAccessors(bool nonPublic) => throw new NotImplementedException();
				public override object[] GetCustomAttributes(bool inherit) => throw new NotImplementedException();
				public override object[] GetCustomAttributes(Type attributeType, bool inherit) => throw new NotImplementedException();
				public override bool IsDefined(Type attributeType, bool inherit) => throw new NotImplementedException();
			}

			private class ReflectedPropertyImpl(ReflectedProperty property, TypeImpl declaringType) : PropertyInfo
			{
				private readonly ReflectedProperty _property = property;
				private readonly TypeImpl _declaringType = declaringType;

				public override string Name => _property.__name__;
				public override Type PropertyType => _property.PropertyType;
				public override PropertyAttributes Attributes => PropertyAttributes.None;
				public override bool CanRead => true;
				public override bool CanWrite => _property.GetSetters().Any();
				public override Type DeclaringType => _declaringType;
				public override Type ReflectedType => _declaringType;

				public override MethodInfo GetGetMethod(bool nonPublic) => null;
				public override MethodInfo GetSetMethod(bool nonPublic) => null;
				public override ParameterInfo[] GetIndexParameters() => [];

				public override object GetValue(object obj, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture)
					=> _declaringType._ops.GetMember(obj, Name);

				public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture)
					=> _declaringType._ops.SetMember(obj, Name, value);

				public override MethodInfo[] GetAccessors(bool nonPublic) => throw new NotImplementedException();
				public override object[] GetCustomAttributes(bool inherit) => throw new NotImplementedException();
				public override object[] GetCustomAttributes(Type attributeType, bool inherit) => throw new NotImplementedException();
				public override bool IsDefined(Type attributeType, bool inherit) => throw new NotImplementedException();
			}

			private class ConstructorImpl(Type declaringType, PythonFunction init) : ConstructorInfo
			{
				private readonly Type _declaringType = declaringType ?? throw new ArgumentNullException(nameof(declaringType));
				private readonly PythonFunction _init = init;

				public override Type DeclaringType => _declaringType;
				public override string Name => ConstructorName;
				public override Type ReflectedType => _declaringType;
				public override MethodAttributes Attributes => MethodAttributes.Public;
				public override RuntimeMethodHandle MethodHandle => throw new NotImplementedException();

				public override ParameterInfo[] GetParameters() => [];

				public override object[] GetCustomAttributes(bool inherit) => [];
				public override object[] GetCustomAttributes(Type attributeType, bool inherit) => [];
				public override bool IsDefined(Type attributeType, bool inherit) => false;

				public override object Invoke(BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture) => null;
				public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture) => null;
				public override MethodImplAttributes GetMethodImplementationFlags() => throw new NotImplementedException();
			}

			private readonly CompiledCode _code;
			private readonly PythonType _pythonType;
			private readonly ScriptEngine _engine;
			private readonly ObjectOperations _ops;
			private readonly Type _underlyingType;

			public TypeImpl(CompiledCode code, PythonType pythonType)
			{
				_code = code ?? throw new ArgumentNullException(nameof(code));
				_pythonType = pythonType ?? throw new ArgumentNullException(nameof(pythonType));
				_engine = code.Engine;
				_ops = _engine.Operations;
				_underlyingType = pythonType.GetUnderlyingSystemType();
			}

			private string TryGetAttr(string name)
				=> _ops.TryGetMember(_pythonType, name, out object value) ? value as string : null;

			private object CreateInstance(params object[] args)
				=> _ops.Invoke(_pythonType, args);

			public override Assembly Assembly => _underlyingType?.Assembly;
			public override string AssemblyQualifiedName => _underlyingType?.AssemblyQualifiedName;
			public override Type BaseType => _underlyingType?.BaseType;
			public override string FullName => _pythonType.GetName();
			public override Guid GUID => _underlyingType?.GUID ?? Guid.Empty;
			public override Module Module => _underlyingType?.Module;
			public override string Namespace => string.Empty;
			public override string Name => _pythonType.GetName();
			public override Type UnderlyingSystemType => _underlyingType;

			public override bool IsGenericType => _underlyingType?.IsGenericType ?? false;
			public override bool IsGenericTypeDefinition => _underlyingType?.IsGenericTypeDefinition ?? false;

			public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
			{
				var init = _ops.GetMemberNames(_pythonType)
					.Select(name => _ops.GetMember(_pythonType, name))
					.OfType<PythonFunction>()
					.FirstOrDefault(f => f.__name__ == "__init__");

				return [new ConstructorImpl(_underlyingType, init)];
			}

			protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
				=> GetConstructors(bindingAttr).FirstOrDefault();

			public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
			{
				var baseType = _underlyingType;

				while (baseType?.IsPythonType() == true)
					baseType = baseType.BaseType;

				var dotNetProperties = baseType?.GetProperties(bindingAttr) ?? [];

				var pythonProperties = _ops
					.GetMemberNames(_pythonType)
					.Select(p => _ops.GetMember(_pythonType, p))
					.ToArray();

				var propertyInfos = new List<PropertyInfo>();

				foreach (var prop in pythonProperties)
				{
					if (prop is PythonProperty pythonProp)
					{
						var instance = CreateInstance();
						var value = ((PythonFunction)pythonProp.fget).__call__(DefaultContext.Default, instance);
						propertyInfos.Add(new PythonPropertyImpl(pythonProp, value?.GetType() ?? typeof(object), this));
					}
					else if (prop is ReflectedProperty reflectedProp)
					{
						propertyInfos.Add(new ReflectedPropertyImpl(reflectedProp, this));
					}
				}

				return [.. propertyInfos, .. dotNetProperties];
			}

			protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers)
				=> throw new NotImplementedException();

			protected override TypeAttributes GetAttributeFlagsImpl() => TypeAttributes.Public;

			public override Type GetElementType() => null;
			protected override bool HasElementTypeImpl() => false;

			protected override bool IsArrayImpl() => false;
			protected override bool IsByRefImpl() => false;
			protected override bool IsCOMObjectImpl() => false;
			protected override bool IsPointerImpl() => false;
			protected override bool IsPrimitiveImpl() => false;

			public override FieldInfo GetField(string name, BindingFlags bindingAttr) => null;
			public override FieldInfo[] GetFields(BindingFlags bindingAttr) => [];

			public override Type GetInterface(string name, bool ignoreCase) => null;
			public override Type[] GetInterfaces() => [];

			public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
				=> [.. GetProperties(bindingAttr), .. GetMethods(bindingAttr), .. GetEvents(bindingAttr)];

			public override Type GetNestedType(string name, BindingFlags bindingAttr) => null;
			public override Type[] GetNestedTypes(BindingFlags bindingAttr) => [];

			public override object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters)
				=> throw new NotImplementedException();

			public override object[] GetCustomAttributes(bool inherit) => [];
			public override object[] GetCustomAttributes(Type attributeType, bool inherit) => [];

			public override bool IsDefined(Type attributeType, bool inherit) => false;

			public override EventInfo GetEvent(string name, BindingFlags bindingAttr)
				=> GetEvents(bindingAttr).FirstOrDefault(e => e.Name == name);

			public override EventInfo[] GetEvents(BindingFlags bindingAttr)
			{
				var events = new List<EventInfo>();

				var pythonEvents = _ops
					.GetMemberNames(_pythonType)
					.Where(name => name.StartsWith("on_", StringComparison.InvariantCultureIgnoreCase))
					.Select(name => new EventImpl(name, typeof(EventHandler), this));

				return [.. events, .. pythonEvents];
			}

			public override MethodInfo[] GetMethods(BindingFlags bindingAttr) => throw new NotImplementedException();

			protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
				=> null;
		}

		private readonly CompiledCode _compiledCode;
		private readonly Type[] _types;

		public AssemblyImpl(CompiledCode compiledCode, ScriptScope scope)
		{
			_compiledCode = compiledCode ?? throw new ArgumentNullException(nameof(compiledCode));
			_types = scope.CheckOnNull(nameof(scope)).GetTypes().Select(t => new TypeImpl(_compiledCode, t)).ToArray();
		}

		public override Type[] GetTypes() => GetExportedTypes();
		public override Type[] GetExportedTypes() => _types;
	}

	private readonly ScriptScope _scope = scope ?? throw new ArgumentNullException(nameof(scope));

	public Assembly LoadFromCode(CompiledCode code)
	{
		if (code is null)
			throw new ArgumentNullException(nameof(code));

		code.Execute(_scope);

		return new AssemblyImpl(code, scope);
	}

	Assembly ICompilerContext.LoadFromBinary(byte[] body)
		=> throw new NotSupportedException();
}