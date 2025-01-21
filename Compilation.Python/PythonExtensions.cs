namespace Ecng.Compilation.Python;

using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Ecng.Common;
using Ecng.ComponentModel;

using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;

using IronPython.Runtime.Types;

public static class PythonExtensions
{
	private const BindingFlags _nonPublic = BindingFlags.Instance | BindingFlags.NonPublic;

	private static readonly PropertyInfo _nameProp = typeof(PythonType).GetProperty("Name", _nonPublic);
	private static readonly PropertyInfo _underlyingSystemTypeProp = typeof(PythonType).GetProperty("UnderlyingSystemType", _nonPublic);
	private static readonly PropertyInfo _setters = typeof(ReflectedGetterSetter).GetProperty("Setter", _nonPublic);
	private static readonly FieldInfo _propInfo = typeof(ReflectedProperty).GetField("_info", _nonPublic);

	[CLSCompliant(false)]
	public static string GetName(this PythonType type)
		=> (string)_nameProp.GetValue(type ?? throw new ArgumentNullException(nameof(type)));

	[CLSCompliant(false)]
	public static Type GetUnderlyingSystemType(this PythonType type)
		=> (Type)_underlyingSystemTypeProp.GetValue(type ?? throw new ArgumentNullException(nameof(type)));

	[CLSCompliant(false)]
	public static bool Is<TBase>(this PythonType type)
		=> type.Is(typeof(TBase));

	[CLSCompliant(false)]
	public static bool Is(this PythonType type, Type baseType)
	{
		if (type is null)		throw new ArgumentNullException(nameof(type));
		if (baseType is null)	throw new ArgumentNullException(nameof(baseType));

		var underlying = type.GetUnderlyingSystemType();

		return underlying?.Is(baseType, false) == true;
	}

	[CLSCompliant(false)]
	public static IEnumerable<PythonType> GetTypes(this ScriptScope scope)
	{
		if (scope is null)
			throw new ArgumentNullException(nameof(scope));

		return scope.GetVariableNames().Select(scope.GetVariable).OfType<PythonType>();
	}

	public static CompilationErrorTypes ToErrorType(this Severity severity)
		=> severity switch
		{
			Severity.Error or Severity.FatalError => CompilationErrorTypes.Error,
			Severity.Warning => CompilationErrorTypes.Warning,
			_ => CompilationErrorTypes.Info,
		};

	public static bool IsPythonObject(this object obj)
	{
		if (obj is null)
			throw new ArgumentNullException(nameof(obj));

		return obj.GetType().IsPythonType();
	}

	public static bool IsPythonType(this Type type)
	{
		if (type is null)
			throw new ArgumentNullException(nameof(type));

		return type.FullName?.StartsWith(nameof(IronPython)) ?? false;
	}

	[CLSCompliant(false)]
	public static MethodInfo[] GetSetters(this ReflectedProperty property)
		=> (MethodInfo[])_setters.GetValue(property);

	[CLSCompliant(false)]
	public static PropertyInfo GetPropInfo(this ReflectedProperty property)
		=> (PropertyInfo)_propInfo.GetValue(property);

	[CLSCompliant(false)]
	public static IEnumerable<PropertyDescriptor> GetProperties(this PythonType type)
	{
		var baseType = type.GetUnderlyingSystemType();

		while (baseType?.IsPythonType() == true)
			baseType = baseType.BaseType;

		var dotNetProperties = baseType is null
			? []
			: TypeDescriptor
			.GetProperties(baseType)
			.Typed()
			.ToDictionary(p => p.Name);

		var pythonProperties = TypeDescriptor
			.GetProperties(type)
			.Typed()
			.Where(pd => pd.PropertyType.IsPrimitive && !pd.Name.StartsWith("_") && !dotNetProperties.ContainsKey(pd.Name));

		return dotNetProperties.Values.Concat(pythonProperties).Where(pd => pd.IsBrowsable);
	}
}