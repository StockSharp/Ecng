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
using Microsoft.Scripting.Actions;

using IronPython.Runtime;
using IronPython.Runtime.Types;

/// <summary>
/// Provides extension methods for working with IronPython types and functions.
/// </summary>
public static class PythonExtensions
{
	private const BindingFlags _nonPublic = BindingFlags.Instance | BindingFlags.NonPublic;

	private static readonly PropertyInfo _nameProp = typeof(PythonType).GetProperty("Name", _nonPublic);
	private static readonly PropertyInfo _setters = typeof(ReflectedGetterSetter).GetProperty("Setter", _nonPublic);
	private static readonly FieldInfo _propInfo = typeof(ReflectedProperty).GetField("_info", _nonPublic);

	/// <summary>
	/// Gets the name of the specified Python type.
	/// </summary>
	/// <param name="type">The Python type.</param>
	/// <returns>The name of the Python type.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> is null.</exception>
	[CLSCompliant(false)]
	public static string GetName(this PythonType type)
		=> (string)_nameProp.GetValue(type ?? throw new ArgumentNullException(nameof(type)));

	/// <summary>
	/// Gets the underlying system type for the specified Python type.
	/// </summary>
	/// <param name="type">The Python type.</param>
	/// <returns>The underlying .NET type.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> is null.</exception>
	[CLSCompliant(false)]
	public static Type GetUnderlyingSystemType(this PythonType type)
	{
		if (type is null)
			throw new ArgumentNullException(nameof(type));

		var retVal = type.__clrtype__();

		if (retVal == typeof(BigInteger))
			retVal = typeof(long);

		return retVal;
	}

	/// <summary>
	/// Determines whether the specified Python type is assignable to the base type <typeparamref name="TBase"/>.
	/// </summary>
	/// <typeparam name="TBase">The base type to check against.</typeparam>
	/// <param name="type">The Python type.</param>
	/// <returns><c>true</c> if the Python type is assignable to <typeparamref name="TBase"/>; otherwise, <c>false</c>.</returns>
	[CLSCompliant(false)]
	public static bool Is<TBase>(this PythonType type)
		=> type.Is(typeof(TBase));

	/// <summary>
	/// Determines whether the specified Python type is assignable to the provided base type.
	/// </summary>
	/// <param name="type">The Python type.</param>
	/// <param name="baseType">The base type to check against.</param>
	/// <returns><c>true</c> if the Python type is assignable to <paramref name="baseType"/>; otherwise, <c>false</c>.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> or <paramref name="baseType"/> is null.</exception>
	[CLSCompliant(false)]
	public static bool Is(this PythonType type, Type baseType)
	{
		if (type is null)		throw new ArgumentNullException(nameof(type));
		if (baseType is null)	throw new ArgumentNullException(nameof(baseType));

		var underlying = type.GetUnderlyingSystemType();

		return underlying?.Is(baseType, false) == true;
	}

	/// <summary>
	/// Retrieves all Python types defined in the specified script scope.
	/// </summary>
	/// <param name="scope">The script scope.</param>
	/// <returns>An enumerable of Python types contained in the scope.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="scope"/> is null.</exception>
	[CLSCompliant(false)]
	public static IEnumerable<PythonType> GetTypes(this ScriptScope scope)
	{
		if (scope is null)
			throw new ArgumentNullException(nameof(scope));

		return scope.GetVariableNames().Select(scope.GetVariable).OfType<PythonType>();
	}

	/// <summary>
	/// Converts a <see cref="Severity"/> value to its corresponding <see cref="CompilationErrorTypes"/>.
	/// </summary>
	/// <param name="severity">The severity of the error or warning.</param>
	/// <returns>The corresponding <see cref="CompilationErrorTypes"/> value.</returns>
	public static CompilationErrorTypes ToErrorType(this Severity severity)
		=> severity switch
		{
			Severity.Error or Severity.FatalError => CompilationErrorTypes.Error,
			Severity.Warning => CompilationErrorTypes.Warning,
			_ => CompilationErrorTypes.Info,
		};

	/// <summary>
	/// Determines whether the specified object is an IronPython object.
	/// </summary>
	/// <param name="obj">The object to test.</param>
	/// <returns><c>true</c> if the object implements <see cref="IPythonObject"/>; otherwise, <c>false</c>.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="obj"/> is null.</exception>
	public static bool IsPythonObject(this object obj)
	{
		if (obj is null)
			throw new ArgumentNullException(nameof(obj));

		return obj is IPythonObject;
	}

	/// <summary>
	/// Determines whether the specified type is an IronPython type.
	/// </summary>
	/// <param name="type">The type to test.</param>
	/// <returns><c>true</c> if the type is an IronPython type; otherwise, <c>false</c>.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> is null.</exception>
	public static bool IsPythonType(this Type type)
	{
		if (type is null)
			throw new ArgumentNullException(nameof(type));

		return type.FullName?.StartsWith(nameof(IronPython)) ?? false;
	}

	/// <summary>
	/// Gets the setter methods of the reflected property.
	/// </summary>
	/// <param name="property">The reflected property.</param>
	/// <returns>An array of setter <see cref="MethodInfo"/> objects.</returns>
	[CLSCompliant(false)]
	public static MethodInfo[] GetSetters(this ReflectedProperty property)
		=> (MethodInfo[])_setters.GetValue(property);

	/// <summary>
	/// Gets the underlying property information for the reflected property.
	/// </summary>
	/// <param name="property">The reflected property.</param>
	/// <returns>The underlying <see cref="PropertyInfo"/> object.</returns>
	[CLSCompliant(false)]
	public static PropertyInfo GetPropInfo(this ReflectedProperty property)
		=> (PropertyInfo)_propInfo.GetValue(property);

	/// <summary>
	/// Gets the .NET type corresponding to the specified Python type.
	/// </summary>
	/// <param name="type">The Python type.</param>
	/// <returns>The underlying .NET type; or <see cref="object"/> if not found.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> is null.</exception>
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

	/// <summary>
	/// Retrieves the parameters of the specified Python function along with their corresponding types.
	/// </summary>
	/// <param name="function">The Python function.</param>
	/// <returns>
	/// An enumerable of tuples where each tuple contains the name of the parameter and its corresponding type.
	/// </returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="function"/> is null.</exception>
	[CLSCompliant(false)]
	public static IEnumerable<(string name, Type type)> GetParams(this PythonFunction function)
	{
		if (function is null)
			throw new ArgumentNullException(nameof(function));

		var code = function.__code__;

		var dict = function.__annotations__.ToDictionary();

		var argNames = code.co_varnames;
		var argCount = code.co_argcount;

		for (var i = 0; i < argCount; i++)
		{
			var paramName = (string)argNames[i];

			if (i == 0 && paramName == "self")
				continue;

			var paramType = typeof(object);

			if (dict?.TryGetValue(paramName, out var type) == true)
			{
				if (type is PythonType pt)
					paramType = pt.GetUnderlyingSystemType() ?? paramType;
				else if (type is TypeGroup tg)
					paramType = tg.Types.First();
			}

			yield return new(paramName, paramType);
		}
	}

	/// <summary>
	/// Determines whether the specified Python function is static.
	/// </summary>
	/// <param name="function">The Python function.</param>
	/// <returns><c>true</c> if the function is static; otherwise, <c>false</c>.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="function"/> is null.</exception>
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