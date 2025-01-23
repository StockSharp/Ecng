namespace Ecng.Compilation.Python;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

using Ecng.Common;
using Ecng.ComponentModel;
using Ecng.Reflection;
using Ecng.Collections;

using IronPython.Runtime;
using IronPython.Runtime.Types;

using Microsoft.Scripting.Hosting;

static class PythonAttrs
{
	private class PythonCustomAttributeData(
		ConstructorInfo ctor,
		IList<CustomAttributeTypedArgument> constructorArgs,
		IList<CustomAttributeNamedArgument> namedArgs) : CustomAttributeData
	{
		public override ConstructorInfo Constructor { get; } = ctor ?? throw new ArgumentNullException(nameof(ctor));
		public override IList<CustomAttributeTypedArgument> ConstructorArguments { get; } = constructorArgs ?? throw new ArgumentNullException(nameof(constructorArgs));
		public override IList<CustomAttributeNamedArgument> NamedArguments { get; } = namedArgs ?? throw new ArgumentNullException(nameof(namedArgs));
	}

	private const string DocumentationUrl = "documentation_url";
	private const string DisplayName = "display_name";
	private const string Description = "__doc__";
	private const string Icon = "icon";

	private static string TryGetAttr(ObjectOperations ops, object obj, string name)
		=> ops.TryGetMember(obj, name, out object value) ? value as string : null;

	public static object[] GetCustomAttributes(this ObjectOperations ops, object obj, bool inherit)
	{
		var attrs = new List<object>();

		if (TryGetAttr(ops, obj, DocumentationUrl) is string docUrl)
			attrs.Add(new DocAttribute(docUrl.Trim()));

		var dispName = TryGetAttr(ops, obj, DisplayName);
		var desc = TryGetAttr(ops, obj, Description);

		if (!dispName.IsEmpty() || !desc.IsEmpty())
			attrs.Add(new DisplayAttribute() { Name = dispName, Description = desc });

		if (TryGetAttr(ops, obj, Icon) is string icon)
			attrs.Add(new IconAttribute(icon));

		return [.. attrs];
	}

	public static object[] GetCustomAttributes(this ObjectOperations ops, object obj, Type attributeType, bool inherit)
	{
		if (attributeType == typeof(DocAttribute) && TryGetAttr(ops, obj, DocumentationUrl) is string docUrl)
			return [new DocAttribute(docUrl.Trim())];
		else if (attributeType == typeof(DisplayAttribute))
		{
			var dispName = TryGetAttr(ops, obj, DisplayName);
			var desc = TryGetAttr(ops, obj, Description);

			if (!dispName.IsEmpty() || !desc.IsEmpty())
				return [new DisplayAttribute() { Name = dispName, Description = desc }];
		}
		else if (attributeType == typeof(IconAttribute) && TryGetAttr(ops, obj, Icon) is string icon)
			return [new IconAttribute(icon)];

		return [];
	}

	public static IList<CustomAttributeData> GetCustomAttributesData(this ObjectOperations ops, object obj)
		=> GetCustomAttributes(ops, obj, true).Select(attr =>
		{
			if (attr is DocAttribute doc)
			{
				var ctor = typeof(DocAttribute).GetConstructor([typeof(string)]);
				return (CustomAttributeData)new PythonCustomAttributeData(ctor, [new CustomAttributeTypedArgument(typeof(string), doc.DocUrl)], []);
			}

			if (attr is DisplayAttribute display)
			{
				var ctor = typeof(DisplayAttribute).GetConstructor([]);
				var namedProps = new List<CustomAttributeNamedArgument>();

				if (display.Name != null)
					namedProps.Add(new(typeof(DisplayAttribute).GetProperty(nameof(DisplayAttribute.Name)),
						new CustomAttributeTypedArgument(typeof(string), display.Name)));

				if (display.Description != null)
					namedProps.Add(new(typeof(DisplayAttribute).GetProperty(nameof(DisplayAttribute.Description)),
						new CustomAttributeTypedArgument(typeof(string), display.Description)));

				return new PythonCustomAttributeData(ctor, [], namedProps);
			}

			if (attr is IconAttribute icon)
			{
				var ctor = typeof(IconAttribute).GetConstructor([typeof(string)]);
				return new PythonCustomAttributeData(ctor, [new CustomAttributeTypedArgument(typeof(string), icon.Icon)], []);
			}

			return null;
		})
		.Where(attr => attr != null)
		.ToList();
}

class PythonContext(ScriptScope scope) : Disposable, ICompilerContext
{
	private class AssemblyImpl : Assembly
	{
		private class TypeImpl : Type, ITypeConstructor
		{
			private class EventImpl(string name, Type eventType, PythonFunction addMethod, PythonFunction removeMethod, TypeImpl declaringType) : EventInfo
			{
				private readonly string _name = name.ThrowIfEmpty(nameof(name));
				private readonly Type _eventType = eventType ?? throw new ArgumentNullException(nameof(eventType));
				private readonly TypeImpl _declaringType = declaringType ?? throw new ArgumentNullException(nameof(declaringType));
				private readonly MethodInfo _addMethod = new MethodImpl(addMethod ?? throw new ArgumentNullException(nameof(addMethod)), declaringType);
				private readonly MethodInfo _removeMethod = new MethodImpl(removeMethod ?? throw new ArgumentNullException(nameof(removeMethod)), declaringType);

				public override string Name => _name;
				public override Type EventHandlerType => _eventType;
				public override Type DeclaringType => _declaringType;
				public override Type ReflectedType => _declaringType;
				public override EventAttributes Attributes => EventAttributes.None;

				public override IEnumerable<CustomAttributeData> CustomAttributes => GetCustomAttributesData();

				public override MethodInfo GetAddMethod(bool nonPublic) => _addMethod;
				public override MethodInfo GetRaiseMethod(bool nonPublic) => null;
				public override MethodInfo GetRemoveMethod(bool nonPublic) => _removeMethod;
				public override MethodInfo[] GetOtherMethods(bool nonPublic) => [];

				public override object[] GetCustomAttributes(bool inherit)
					=> _declaringType._ops.GetCustomAttributes(_addMethod, inherit);
				public override object[] GetCustomAttributes(Type attributeType, bool inherit)
					=> _declaringType._ops.GetCustomAttributes(_addMethod, attributeType, inherit);
				public override bool IsDefined(Type attributeType, bool inherit) => GetCustomAttributes(attributeType, inherit).Any();
				public override IList<CustomAttributeData> GetCustomAttributesData()
					=> _declaringType._ops.GetCustomAttributesData(_addMethod);

				public override string ToString() => Name;
			}

			private class ParameterImpl(string name, Type parameterType, int position, MemberInfo method) : ParameterInfo
			{
				private readonly string _name = name.ThrowIfEmpty(nameof(name));
				private readonly Type _parameterType = parameterType ?? throw new ArgumentNullException(nameof(parameterType));
				private readonly int _position = position;
				private readonly MemberInfo _method = method ?? throw new ArgumentNullException(nameof(method));

				public override string Name => _name;
				public override Type ParameterType => _parameterType;
				public override int Position => _position;
				public override ParameterAttributes Attributes => ParameterAttributes.None;
				public override MemberInfo Member => _method;
				public override object DefaultValue => null;
				public override object RawDefaultValue => null;
				public override bool HasDefaultValue => false;

				public override string ToString() => Name;
			}

			private class MethodImpl : MethodInfo
			{
				private readonly PythonFunction _function;
				private readonly TypeImpl _declaringType;
				private readonly ParameterImpl[] _parameters;

				public MethodImpl(PythonFunction function, TypeImpl declaringType)
				{
					_function = function ?? throw new ArgumentNullException(nameof(function));
					_declaringType = declaringType ?? throw new ArgumentNullException(nameof(declaringType));

					_parameters = [.. function.GetParams().Select((p, i) => new ParameterImpl(p.name, p.type, i, this))];

					Attributes = MethodAttributes.Public | (function.IsStatic() ? MethodAttributes.Static : 0);
				}

				public override string Name => _function.__name__;
				public override Type DeclaringType => _declaringType;
				public override Type ReflectedType => _declaringType;
				public override RuntimeMethodHandle MethodHandle => throw new NotSupportedException();
				public override MethodAttributes Attributes { get; }
				public override CallingConventions CallingConvention => CallingConventions.Standard;
				public override Type ReturnType => typeof(object);
				public override IEnumerable<CustomAttributeData> CustomAttributes => GetCustomAttributesData();

				public override bool IsSecurityCritical => true;
				public override bool IsSecuritySafeCritical => false;
				public override bool IsSecurityTransparent => false;

				public override ParameterInfo[] GetParameters() => _parameters;
				public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
					=> _function.__call__(DefaultContext.Default, [obj, .. parameters]);

				public override ICustomAttributeProvider ReturnTypeCustomAttributes => null;
				public override MethodInfo GetBaseDefinition() => this;
				public override object[] GetCustomAttributes(bool inherit)
					=> _declaringType._ops.GetCustomAttributes(_function, inherit);
				public override object[] GetCustomAttributes(Type attributeType, bool inherit)
					=> _declaringType._ops.GetCustomAttributes(_function, attributeType, inherit);
				public override bool IsDefined(Type attributeType, bool inherit) => GetCustomAttributes(attributeType, inherit).Any();
				public override IList<CustomAttributeData> GetCustomAttributesData()
					=> _declaringType._ops.GetCustomAttributesData(_function);

				public override MethodImplAttributes GetMethodImplementationFlags() => MethodImplAttributes.IL;

				public override string ToString() => Name;
			}

			private class PythonPropertyImpl : PropertyInfo
			{
				private readonly PythonProperty _property;
				private readonly Type _propertyType;
				private readonly TypeImpl _declaringType;

				public PythonPropertyImpl(PythonProperty property, TypeImpl declaringType)
				{
					_property = property ?? throw new ArgumentNullException(nameof(property));
					_declaringType = declaringType ?? throw new ArgumentNullException(nameof(declaringType));

					var pt = ((PythonFunction)property.fget)?.__annotations__.TryGetValue("return") as PythonType;
					_propertyType = (pt?.GetUnderlyingSystemType()) ?? typeof(object);
				}

				public override string Name => ((PythonFunction)_property.fget).__name__;
				public override Type PropertyType => _propertyType;
				public override PropertyAttributes Attributes => PropertyAttributes.None;
				public override bool CanRead => true;
				public override bool CanWrite => _property.fset != null;
				public override Type DeclaringType => _declaringType;
				public override Type ReflectedType => _declaringType;
				public override IEnumerable<CustomAttributeData> CustomAttributes => GetCustomAttributesData();

				public override MethodInfo GetGetMethod(bool nonPublic) => _property.fget is null ? null : new MethodImpl((PythonFunction)_property.fget, _declaringType);
				public override MethodInfo GetSetMethod(bool nonPublic) => _property.fset is null ? null : new MethodImpl((PythonFunction)_property.fset, _declaringType);
				public override ParameterInfo[] GetIndexParameters() => [];

				public override object GetValue(object obj, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture)
					=> _declaringType._ops.GetMember(obj, Name);

				public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture)
					=> _declaringType._ops.SetMember(obj, Name, value);

				public override MethodInfo[] GetAccessors(bool nonPublic)
				{
					var getter = GetGetMethod(nonPublic);
					var setter = GetSetMethod(nonPublic);

					return new MethodInfo[] { getter, setter }.Where(m => m is not null).ToArray();
				}

				public override object[] GetCustomAttributes(bool inherit)
					=> _declaringType._ops.GetCustomAttributes(_property, inherit);

				public override object[] GetCustomAttributes(Type attributeType, bool inherit)
					=> _declaringType._ops.GetCustomAttributes(_property, attributeType, inherit);

				public override bool IsDefined(Type attributeType, bool inherit) => GetCustomAttributes(attributeType, inherit).Any();
				public override IList<CustomAttributeData> GetCustomAttributesData()
					=> _declaringType._ops.GetCustomAttributesData(_property);

				public override string ToString() => Name;
			}

			private class ReflectedPropertyImpl(ReflectedProperty property, TypeImpl declaringType) : PropertyInfo
			{
				private readonly ReflectedProperty _property = property ?? throw new ArgumentNullException(nameof(property));
				private readonly TypeImpl _declaringType = declaringType ?? throw new ArgumentNullException(nameof(declaringType));
				private readonly PropertyInfo _propInfo = property.GetPropInfo();

				public override string Name => _propInfo.Name;
				public override Type PropertyType => _propInfo.PropertyType;
				public override PropertyAttributes Attributes => _propInfo.Attributes;
				public override bool CanRead => _propInfo.CanRead;
				public override bool CanWrite => _propInfo.CanWrite;
				public override Type DeclaringType => _declaringType;
				public override Type ReflectedType => _declaringType;
				public override IEnumerable<CustomAttributeData> CustomAttributes => _propInfo.CustomAttributes;

				public override MethodInfo GetGetMethod(bool nonPublic) => _propInfo.GetGetMethod(nonPublic);
				public override MethodInfo GetSetMethod(bool nonPublic) => _propInfo.GetSetMethod(nonPublic);
				public override ParameterInfo[] GetIndexParameters() => _propInfo.GetIndexParameters();

				public override object GetValue(object obj, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture)
					=> _declaringType._ops.GetMember(obj, Name);

				public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture)
					=> _declaringType._ops.SetMember(obj, Name, value);

				public override MethodInfo[] GetAccessors(bool nonPublic) => _propInfo.GetAccessors();
				public override object[] GetCustomAttributes(bool inherit) => _propInfo.GetCustomAttributes(inherit);
				public override object[] GetCustomAttributes(Type attributeType, bool inherit) => _propInfo.GetCustomAttributes(attributeType, inherit);
				public override bool IsDefined(Type attributeType, bool inherit) => _propInfo.IsDefined(attributeType, inherit);
				public override IList<CustomAttributeData> GetCustomAttributesData() => _propInfo.GetCustomAttributesData();

				public override string ToString() => Name;
			}

			private class ConstructorImpl : ConstructorInfo
			{
				private readonly TypeImpl _declaringType;
				private readonly PythonFunction _function;
				private readonly ParameterImpl[] _parameters;

				public ConstructorImpl(PythonFunction function, TypeImpl declaringType)
				{
					_function = function;
					_declaringType = declaringType ?? throw new ArgumentNullException(nameof(declaringType));

					var parameters = new List<ParameterImpl>();

					if (function is not null)
						parameters.AddRange(function.GetParams().Select((p, i) => new ParameterImpl(p.name, p.type, i, this)));

					_parameters = [.. parameters];

					var isStatic = function?.IsStatic() == true;
					Attributes = MethodAttributes.Public | (isStatic ? MethodAttributes.Static : 0);

					Name = isStatic ? TypeConstructorName : ConstructorName;
				}

				public override Type DeclaringType => _declaringType;
				public override string Name { get; }
				public override Type ReflectedType => DeclaringType;
				public override MethodAttributes Attributes { get; }
				public override RuntimeMethodHandle MethodHandle => throw new NotSupportedException();
				public override IEnumerable<CustomAttributeData> CustomAttributes => GetCustomAttributesData();

				public override bool IsSecurityCritical => true;
				public override bool IsSecuritySafeCritical => false;
				public override bool IsSecurityTransparent => false;

				public override ParameterInfo[] GetParameters() => _parameters;

				public override object[] GetCustomAttributes(bool inherit)
					=> _declaringType._ops.GetCustomAttributes(_function, inherit);
				public override object[] GetCustomAttributes(Type attributeType, bool inherit)
					=> _declaringType._ops.GetCustomAttributes(_function, attributeType, inherit);
				public override bool IsDefined(Type attributeType, bool inherit) => GetCustomAttributes(attributeType, inherit).Any();
				public override IList<CustomAttributeData> GetCustomAttributesData()
					=> _declaringType._ops.GetCustomAttributesData(_function);

				public override object Invoke(BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
					=> _declaringType.CreateInstance(parameters);
				public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
					=> Invoke(invokeAttr, binder, parameters, culture);

				public override MethodImplAttributes GetMethodImplementationFlags() => MethodImplAttributes.IL;

				public override string ToString() => Name;
			}

			private readonly Assembly _assembly;
			private readonly CompiledCode _code;
			private readonly PythonType _pythonType;
			private readonly ScriptEngine _engine;
			private readonly ObjectOperations _ops;
			private readonly Type _underlyingType;
			private readonly Type _dotNetBaseType;

			public TypeImpl(Assembly assembly, CompiledCode code, PythonType pythonType, Type underlyingType)
			{
				_assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
				_code = code ?? throw new ArgumentNullException(nameof(code));
				_pythonType = pythonType ?? throw new ArgumentNullException(nameof(pythonType));
				_engine = code.Engine;
				_ops = _engine.Operations;
				_underlyingType = underlyingType ?? throw new ArgumentNullException(nameof(underlyingType));
				_dotNetBaseType = pythonType.GetDotNetType();
			}

			public override Assembly Assembly => _assembly;
			public override string AssemblyQualifiedName => _underlyingType.AssemblyQualifiedName;
			public override Type BaseType => _underlyingType.BaseType;
			public override string FullName => _pythonType.GetName();
			public override Guid GUID => _underlyingType.GUID;
			public override Module Module => _underlyingType.Module;
			public override string Namespace => string.Empty;
			public override string Name => _pythonType.GetName();
			public override Type UnderlyingSystemType => _underlyingType;

			public override bool IsGenericType => _underlyingType.IsGenericType;
			public override bool IsGenericTypeDefinition => _underlyingType.IsGenericTypeDefinition;
			public override bool IsTypeDefinition => _underlyingType.IsTypeDefinition;
			public override bool IsByRefLike => _underlyingType.IsByRefLike;
			public override bool IsConstructedGenericType => _underlyingType.IsConstructedGenericType;

			public override bool IsSecurityCritical => _underlyingType.IsSecurityCritical;
			public override bool IsSecuritySafeCritical => _underlyingType.IsSecuritySafeCritical;
			public override bool IsSecurityTransparent => _underlyingType.IsSecurityTransparent;
			
			public override bool IsSZArray => _underlyingType.IsSZArray;
			public override int MetadataToken => _underlyingType.MetadataToken;
			public override StructLayoutAttribute StructLayoutAttribute => _underlyingType.StructLayoutAttribute;
			public override RuntimeTypeHandle TypeHandle => _underlyingType.TypeHandle;

			public override IEnumerable<CustomAttributeData> CustomAttributes => GetCustomAttributesData();

			public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
			{
				var init = _ops.GetMemberNames(_pythonType)
					.Select(name => _ops.GetMember(_pythonType, name))
					.OfType<PythonFunction>()
					.FirstOrDefault(f => f.__name__ == "__init__");

				return [new ConstructorImpl(init, this)];
			}

			protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
				=> GetConstructors(bindingAttr).FirstOrDefault();

			public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
			{
				var dotNetProps = _dotNetBaseType.GetProperties(ReflectionHelper.AllMembers).GroupBy(p => p.Name).ToDictionary();

				var pythonProps = new List<PropertyInfo>();

				foreach (var prop in _ops
					.GetMemberNames(_pythonType)
					.Select(p => _ops.GetMember(_pythonType, p)))
				{
					if (prop is PythonProperty pythonProp)
					{
						pythonProps.Add(new PythonPropertyImpl(pythonProp, this));
					}
					else if (prop is ReflectedProperty reflectedProp)
					{
						if (dotNetProps.ContainsKey(reflectedProp.__name__))
							continue;

						pythonProps.Add(new ReflectedPropertyImpl(reflectedProp, this));
					}
				}

				return [.. pythonProps.Concat(dotNetProps.Values.SelectMany()).Where(p => p.IsMatch(bindingAttr))];
			}

			protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers)
				=> GetProperties(bindingAttr).FirstOrDefault(p => p.Name == name);

			protected override TypeAttributes GetAttributeFlagsImpl() => TypeAttributes.Public;

			public override Type GetElementType() => null;
			protected override bool HasElementTypeImpl() => false;

			protected override bool IsArrayImpl() => false;
			protected override bool IsByRefImpl() => false;
			protected override bool IsCOMObjectImpl() => false;
			protected override bool IsPointerImpl() => false;
			protected override bool IsPrimitiveImpl() => false;

			public override FieldInfo GetField(string name, BindingFlags bindingAttr) => GetFields(bindingAttr).FirstOrDefault(f => f.Name == name);
			public override FieldInfo[] GetFields(BindingFlags bindingAttr) => _dotNetBaseType.GetFields(bindingAttr);

			public override Type GetInterface(string name, bool ignoreCase) => GetInterfaces().FirstOrDefault(i => ignoreCase ? i.Name.EqualsIgnoreCase(name) : i.Name == name);
			public override Type[] GetInterfaces() => _dotNetBaseType.GetInterfaces();

			public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
				=> [.. GetProperties(bindingAttr), .. GetMethods(bindingAttr), .. GetEvents(bindingAttr)];

			public override Type GetNestedType(string name, BindingFlags bindingAttr) => null;
			public override Type[] GetNestedTypes(BindingFlags bindingAttr) => [];

			public override object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters)
			{
				var ops = _engine.Operations;

				if (!ops.ContainsMember(target, name))
					throw new MissingMemberException($"Member '{name}' doesn't exist.");

				var member = ops.GetMember(target, name);

				switch (member)
				{
					case PythonFunction pythonFunction:
						return pythonFunction.__call__(DefaultContext.Default, target, args);

					case PythonProperty:

						if (args == null || args.Length == 0)
							return ops.GetMember(target, name);
						else
						{
							ops.SetMember(target, name, args[0]);
							return null;
						}

					default:
						return ops.InvokeMember(target, name, args);
				}
			}

			public override bool IsDefined(Type attributeType, bool inherit) => GetCustomAttributes(attributeType, inherit).Any();

			public override object[] GetCustomAttributes(bool inherit)
				=> _ops.GetCustomAttributes(_pythonType, inherit);

			public override object[] GetCustomAttributes(Type attributeType, bool inherit)
				=> _ops.GetCustomAttributes(_pythonType, attributeType, inherit);

			public override IList<CustomAttributeData> GetCustomAttributesData()
				=> _ops.GetCustomAttributesData(_pythonType);

			public override EventInfo GetEvent(string name, BindingFlags bindingAttr)
				=> GetEvents(bindingAttr).FirstOrDefault(e => e.Name == name);

			public override EventInfo[] GetEvents(BindingFlags bindingAttr)
			{
				var dotNetEvents = _dotNetBaseType.GetEvents(bindingAttr);

				const string prefix = "add_";

				var pythonEvents = _ops
					.GetMemberNames(_pythonType)
					.Where(name => name.StartsWithIgnoreCase(prefix))
					.Select(name => _ops.GetMember(_pythonType, name))
					.OfType<PythonFunction>()
					.Select(addMethod =>
					{
						var name = addMethod.__name__.Remove(prefix, true);

						if (!_ops.TryGetMember(_pythonType, "remove_" + name, true, out var r) || r is not PythonFunction removeMethod)
							return null;

						var eventType = ((addMethod.__annotations__.TryGetValue("handler") as PythonType)?.GetUnderlyingSystemType()) ?? typeof(EventHandler);
						return new EventImpl(name, eventType, addMethod, removeMethod, this);
					})
					.Where(e => e?.GetAddMethod().IsMatch(bindingAttr) == true);

				return [.. dotNetEvents, .. pythonEvents];
			}

			public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
			{
				var dotNetMethods = _dotNetBaseType.GetMethods(ReflectionHelper.AllMembers).GroupBy(m => m.Name).ToDictionary();

				var methods = new List<MethodInfo>();

				var pythonMethods = _ops
					.GetMemberNames(_pythonType)
					.Select(name => _ops.GetMember(_pythonType, name))
					.OfType<PythonFunction>();

				foreach (var pythonMethod in pythonMethods)
				{
					var impl = new MethodImpl(pythonMethod, this);

					if (dotNetMethods.TryGetValue(impl.Name, out var existing) && existing.Any(m => m.IsStatic == impl.IsStatic && m.GetParameters().Length == impl.GetParameters().Length))
						continue;

					methods.Add(impl);
				}

				methods.AddRange(dotNetMethods.Values.SelectMany());

				return [.. methods.Where(m => m.IsMatch(bindingAttr))];
			}

			protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
				=> GetMethods(bindingAttr).FirstOrDefault(m => m.Name == name && m.GetParameters().Select(p => p.ParameterType).SequenceEqual(types));

			public object CreateInstance(params object[] args)
				=> _ops.CreateInstance(_pythonType, args);
		}

		private readonly CompiledCode _compiledCode;
		private readonly Type[] _types;

		public AssemblyImpl(CompiledCode compiledCode, ScriptScope scope)
		{
			_compiledCode = compiledCode ?? throw new ArgumentNullException(nameof(compiledCode));
			_types = scope.CheckOnNull(nameof(scope)).GetTypes().Where(t => t.GetUnderlyingSystemType()?.IsPythonType() == true).Select(t => new TypeImpl(this, _compiledCode, t, t.GetUnderlyingSystemType())).ToArray();
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