namespace Ecng.Compilation.Python;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Ecng.Common;
using Ecng.Collections;
using Ecng.ComponentModel;

using Microsoft.Scripting.Hosting;

using IronPython.Runtime;
using IronPython.Runtime.Types;

class PythonCompilationResult(IEnumerable<CompilationError> errors)
	: CompilationResult(errors)
{
	private class AssemblyImpl(CompiledCode compiledCode) : IAssembly
	{
		private class TypeImpl(CompiledCode code, PythonType pythonType) : IType
		{
			private class PythonPropertyImpl(AssemblyImpl.TypeImpl parent, PythonProperty property, Type type) : IProperty
			{
				private readonly TypeImpl _parent = parent ?? throw new ArgumentNullException(nameof(parent));
				private readonly PythonProperty _property = property ?? throw new ArgumentNullException(nameof(property));
				private readonly Type _type = type ?? throw new ArgumentNullException(nameof(type));

				public string Name => ((PythonFunction)_property.fget).__name__;
				string IProperty.DisplayName => Name;

				Type IProperty.Type => _type;

				bool IProperty.IsBrowsable => true;
				bool IProperty.IsReadOnly => _property.fset is null;

				object IProperty.GetValue(object instance) => _parent.Ops.GetMember(instance, Name);
				void IProperty.SetValue(object instance, object value) => _parent.Ops.SetMember(instance, Name, value);
			}

			private class ReflectedPropertyImpl(TypeImpl parent, ReflectedProperty property, PropertyInfo baseTypeProp) : IProperty
			{
				private readonly TypeImpl _parent = parent ?? throw new ArgumentNullException(nameof(parent));
				private readonly ReflectedProperty _property = property ?? throw new ArgumentNullException(nameof(property));
				private readonly PropertyInfo _baseTypeProp = baseTypeProp;

				public string Name => _property.__name__;
				string IProperty.DisplayName => Name;
				Type IProperty.Type => _property.PropertyType;

				bool IProperty.IsBrowsable => _baseTypeProp?.IsBrowsable() != false;
				bool IProperty.IsReadOnly => !_property.GetSetters().Any();

				object IProperty.GetValue(object instance) => _parent.Ops.GetMember(instance, Name);
				void IProperty.SetValue(object instance, object value) => _parent.Ops.SetMember(instance, Name, value);

				public override string ToString() => _property.ToString();
			}

			private readonly CompiledCode _code = code ?? throw new ArgumentNullException(nameof(code));
			private readonly PythonType _pythonType = pythonType ?? throw new ArgumentNullException(nameof(pythonType));

			public string Name => _pythonType.GetName();
			string IType.DisplayName => TryGetAttr("display_name");
			string IType.Description => TryGetAttr("__doc__")?.Trim();
			string IType.DocUrl => TryGetAttr("documentation_url");
			Uri IType.IconUri => TryGetAttr("icon") is string url ? new(url) : (Uri)default;

			private ScriptEngine Engine => _code.Engine;
			private ObjectOperations Ops => Engine.Operations;

			bool IType.IsAbstract => _pythonType.GetUnderlyingSystemType()?.IsAbstract == true;
			bool IType.IsPublic => _pythonType.GetUnderlyingSystemType()?.IsPublic == true;
			bool IType.IsGenericTypeDefinition => _pythonType.GetUnderlyingSystemType()?.IsGenericTypeDefinition == true;
			object IType.GetConstructor(IType[] value) => new();

			private string TryGetAttr(string name)
				=> Ops.TryGetMember(_pythonType, name, out object value) ? value as string : null;

			object IType.CreateInstance(object[] args)
			{
				if (args is null)
					throw new ArgumentNullException(nameof(args));

				return Ops.Invoke(_pythonType, args);
			}

			bool IType.Is(Type type) => _pythonType.Is(type);

			IEnumerable<IProperty> IType.GetProperties()
			{
				var baseType = _pythonType.GetUnderlyingSystemType();

				while (baseType?.IsPythonType() == true)
					baseType = baseType.BaseType;

				var dotNetProperties = baseType is null
					? []
					: baseType.GetProperties().ToDictionary(p => p.Name);

				var properties = Ops
					.GetMemberNames(_pythonType)
					.Select(p => Ops.GetMember(_pythonType, p))
					.ToArray();

				var instance = Ops.Invoke(_pythonType);

				return properties.OfType<PythonProperty>().Select(p =>
				{
					var v = ((PythonFunction)p.fget).__call__(DefaultContext.Default, instance);
					return (IProperty)new PythonPropertyImpl(this, p, v?.GetType() ?? typeof(object));
				})
				.Concat(properties.OfType<ReflectedProperty>().Select(p => new ReflectedPropertyImpl(this, p, dotNetProperties.TryGetValue(p.__name__))))
				;
			}

			public override string ToString() => Name;
		}

		private readonly CompiledCode _compiledCode = compiledCode ?? throw new ArgumentNullException(nameof(compiledCode));
		private readonly SynchronizedSet<ScriptScope> _execScopes = [];

		byte[] IAssembly.AsBytes => throw new NotSupportedException();

		IEnumerable<IType> IAssembly.GetExportTypes(object context)
		{
			if (context is null)
				throw new ArgumentNullException(nameof(context));

			var scope = (ScriptScope)context;

			if (_execScopes.TryAdd(scope))
				_compiledCode.Execute(scope);

			return scope.GetTypes().Select(t => new TypeImpl(_compiledCode, t)).ToArray();
		}
	}

	public CompiledCode CompiledCode { get; set; }

	private IAssembly _assembly;
	public override IAssembly Assembly => CompiledCode is null ? null : _assembly ??= new AssemblyImpl(CompiledCode);
}