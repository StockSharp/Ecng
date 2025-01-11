namespace Ecng.Compilation.Python;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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
	public static IEnumerable<PythonType> GetTypes(this ScriptScope scope)
	{
		if (scope is null)
			throw new ArgumentNullException(nameof(scope));

		return scope.GetVariableNames().Select(scope.GetVariable).OfType<PythonType>();
	}
}