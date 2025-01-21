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

using IronPython.Runtime;
using IronPython.Runtime.Types;

using Microsoft.Scripting.Hosting;

static class PythonAttrs
{
	private class PythonCustomAttributeData(
		ConstructorInfo ctor,
		IList<CustomAttributeTypedArgument> constructorArgs,
		IList<CustomAttributeNamedArgument> namedArgs = null) : CustomAttributeData
	{
		public override ConstructorInfo Constructor { get; } = ctor ?? throw new ArgumentNullException(nameof(ctor));
		public override IList<CustomAttributeTypedArgument> ConstructorArguments { get; } = constructorArgs ?? throw new ArgumentNullException(nameof(constructorArgs));
		public override IList<CustomAttributeNamedArgument> NamedArguments { get; } = namedArgs ?? [];
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
				return (CustomAttributeData)new PythonCustomAttributeData(ctor, [new CustomAttributeTypedArgument(typeof(string), doc.DocUrl)]);
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
				return new PythonCustomAttributeData(ctor, [new CustomAttributeTypedArgument(typeof(string), icon.Icon)]);
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
			private class EventImpl(string name, Type eventType, TypeImpl declaringType) : EventInfo
			{
				private readonly string _name = name.ThrowIfEmpty(nameof(name));
				private readonly Type _eventType = eventType ?? throw new ArgumentNullException(nameof(eventType));
				private readonly TypeImpl _declaringType = declaringType ?? throw new ArgumentNullException(nameof(declaringType));

				public override string Name => _name;
				public override Type EventHandlerType => _eventType;
				public override Type DeclaringType => _declaringType;
				public override Type ReflectedType => _declaringType;
				public override EventAttributes Attributes => EventAttributes.None;

				public override IEnumerable<CustomAttributeData> CustomAttributes => GetCustomAttributesData();

				public override MethodInfo GetAddMethod(bool nonPublic) => null;
				public override MethodInfo GetRaiseMethod(bool nonPublic) => null;
				public override MethodInfo GetRemoveMethod(bool nonPublic) => null;
				public override MethodInfo[] GetOtherMethods(bool nonPublic) => [];

				public override object[] GetCustomAttributes(bool inherit) => [];
				public override object[] GetCustomAttributes(Type attributeType, bool inherit) => [];
				public override bool IsDefined(Type attributeType, bool inherit) => GetCustomAttributes(attributeType, inherit).Any();
				public override IList<CustomAttributeData> GetCustomAttributesData() => base.GetCustomAttributesData();
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

				private readonly PythonFunction _function = function ?? throw new ArgumentNullException(nameof(function));
				private readonly TypeImpl _declaringType = declaringType ?? throw new ArgumentNullException(nameof(declaringType));
				private readonly ParameterInfo[] _parameters;

				public MethodImpl(PythonFunction function, TypeImpl declaringType, Type[] paramTypes)
					: this(function, declaringType)
				{
					_parameters = paramTypes.Select((t, i) => new ParameterImpl($"param{i}", t, i, this)).ToArray();
				}

				public override string Name => _function.__name__;
				public override Type DeclaringType => _declaringType;
				public override Type ReflectedType => _declaringType;
				public override RuntimeMethodHandle MethodHandle => throw new NotSupportedException();
				public override MethodAttributes Attributes => MethodAttributes.Public;
				public override CallingConventions CallingConvention => CallingConventions.Standard;
				public override Type ReturnType => typeof(object);

				public override ParameterInfo[] GetParameters() => _parameters;
				public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
					=> _function.__call__(DefaultContext.Default, obj, parameters);

				public override ICustomAttributeProvider ReturnTypeCustomAttributes => null;
				public override MethodInfo GetBaseDefinition() => this;
				public override object[] GetCustomAttributes(bool inherit)
					=> _declaringType._ops.GetCustomAttributes(_function, inherit);
				public override object[] GetCustomAttributes(Type attributeType, bool inherit)
					=> _declaringType._ops.GetCustomAttributes(_function, attributeType, inherit);
				public override bool IsDefined(Type attributeType, bool inherit) => GetCustomAttributes(attributeType, inherit).Any();
				public override IList<CustomAttributeData> GetCustomAttributesData()
					=> _declaringType._ops.GetCustomAttributesData(_function);

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
				public override IEnumerable<CustomAttributeData> CustomAttributes => GetCustomAttributesData();

				public override MethodInfo GetGetMethod(bool nonPublic) => null;
				public override MethodInfo GetSetMethod(bool nonPublic) => null;
				public override ParameterInfo[] GetIndexParameters() => [];

				public override object GetValue(object obj, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture)
					=> _declaringType._ops.GetMember(obj, Name);

				public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture)
					=> _declaringType._ops.SetMember(obj, Name, value);

				public override MethodInfo[] GetAccessors(bool nonPublic) => throw new NotImplementedException();

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

			private class ConstructorImpl(Type declaringType, PythonFunction init) : ConstructorInfo
			{
				private readonly Type _declaringType = declaringType ?? throw new ArgumentNullException(nameof(declaringType));
				private readonly PythonFunction _init = init;

				public override Type DeclaringType => _declaringType;
				public override string Name => ConstructorName;
				public override Type ReflectedType => _declaringType;
				public override MethodAttributes Attributes => MethodAttributes.Public;
				public override RuntimeMethodHandle MethodHandle => throw new NotSupportedException();

				public override ParameterInfo[] GetParameters() => [];

				public override object[] GetCustomAttributes(bool inherit) => [];
				public override object[] GetCustomAttributes(Type attributeType, bool inherit) => [];
				public override bool IsDefined(Type attributeType, bool inherit) => false;

				public override object Invoke(BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture) => null;
				public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture) => null;
				public override MethodImplAttributes GetMethodImplementationFlags() => throw new NotImplementedException();
			}

			private readonly Assembly _assembly;
			private readonly CompiledCode _code;
			private readonly PythonType _pythonType;
			private readonly ScriptEngine _engine;
			private readonly ObjectOperations _ops;
			private readonly Type _underlyingType;

			public TypeImpl(Assembly assembly, CompiledCode code, PythonType pythonType, Type underlyingType)
			{
				_assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
				_code = code ?? throw new ArgumentNullException(nameof(code));
				_pythonType = pythonType ?? throw new ArgumentNullException(nameof(pythonType));
				_engine = code.Engine;
				_ops = _engine.Operations;
				_underlyingType = underlyingType ?? throw new ArgumentNullException(nameof(underlyingType));
			}

			private object CreateInstance(params object[] args)
				=> _ops.Invoke(_pythonType, args);

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

				return [new ConstructorImpl(_underlyingType, init)];
			}

			protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
				=> GetConstructors(bindingAttr).FirstOrDefault();

			public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
			{
				var baseType = _underlyingType;

				while (baseType?.IsPythonType() == true)
					baseType = baseType.BaseType;

				var dotNetProps = baseType?.GetProperties(bindingAttr).ToDictionary(p => p.Name) ?? [];

				var pythonProperties = _ops
					.GetMemberNames(_pythonType)
					.Select(p => _ops.GetMember(_pythonType, p))
					.ToArray();

				var pythonProps = new List<PropertyInfo>();

				foreach (var prop in pythonProperties)
				{
					if (prop is PythonProperty pythonProp)
					{
						var instance = CreateInstance();
						var value = ((PythonFunction)pythonProp.fget).__call__(DefaultContext.Default, instance);
						pythonProps.Add(new PythonPropertyImpl(pythonProp, value?.GetType() ?? typeof(object), this));
					}
					else if (prop is ReflectedProperty reflectedProp)
					{
						if (dotNetProps.ContainsKey(reflectedProp.__name__))
							continue;

						pythonProps.Add(new ReflectedPropertyImpl(reflectedProp, this));
					}
				}

				return [.. pythonProps, .. dotNetProps.Values];
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

			object ITypeConstructor.CreateInstance(object[] args)
				=> _ops.Invoke(_pythonType, args);
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