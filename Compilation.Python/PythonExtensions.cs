namespace Ecng.Compilation.Python;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Numerics;

using Ecng.Common;
using Ecng.Collections;

using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;

using IronPython.Runtime.Types;
using IronPython.Runtime;

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
	{
		var retVal = (Type)_underlyingSystemTypeProp.GetValue(type ?? throw new ArgumentNullException(nameof(type)));

		if (retVal == typeof(BigInteger))
			retVal = typeof(long);

		return retVal;
	}

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
	public static Type GetDotNetType(this PythonType type)
	{
		if (type is null)
			throw new ArgumentNullException(nameof(type));

		var baseType = type.GetUnderlyingSystemType();

		while (baseType?.IsPythonType() == true)
			baseType = baseType.BaseType;

		return baseType ?? typeof(object);
	}

	[CLSCompliant(false)]
	public static IEnumerable<(string name, Type type)> GetParams(this PythonFunction function)
	{
		if (function is null)
			throw new ArgumentNullException(nameof(function));

		var code = function.__code__;

		var dict = function.__annotations__?.ToDictionary();

		var argNames = code.co_varnames;
		var argCount = code.co_argcount;

		for (var i = 0; i < argCount; i++)
		{
			var paramName = (string)argNames[i];

			if (i == 0 && paramName == "self")
				continue;

			var paramType = typeof(object);

			if (dict?.TryGetValue(paramName, out var type) == true && type is PythonType pt)
				paramType = pt.GetUnderlyingSystemType() ?? paramType;

			yield return new(paramName, paramType);
		}
	}

	[CLSCompliant(false)]
	public static bool IsStatic(this PythonFunction function)
	{
		if (function is null)
			throw new ArgumentNullException(nameof(function));

		var code = function.__code__;

		if (code != null)
		{
			var argNames = code.co_varnames;
			if (code.co_argcount > 0 && argNames[0] as string == "self")
				return false;
		}

		var functionDict = function.__dict__;
		if (functionDict != null && functionDict.ContainsKey("__staticmethod__"))
			return true;

		return false;
	}
}