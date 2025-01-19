namespace Ecng.Compilation.Python;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Ecng.Common;

using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;

using IronPython.Runtime.Types;

public static class PythonExtensions
{
	private static readonly PropertyInfo _nameProp = typeof(PythonType).GetProperty("Name", BindingFlags.Instance | BindingFlags.NonPublic);
	private static readonly PropertyInfo _underlyingSystemTypeProp = typeof(PythonType).GetProperty("UnderlyingSystemType", BindingFlags.Instance | BindingFlags.NonPublic);

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
}