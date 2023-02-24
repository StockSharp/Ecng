namespace Ecng.Reflection
{
	#region Using Directives

	using System;
	using System.Collections.Generic;

	using Ecng.Common;
	using Ecng.Reflection.Emit;

	#endregion

	public class GenericArg
	{
		#region GenericArg.ctor()

		internal GenericArg(Type type, string name, IEnumerable<Constraint> constraints)
		{
			Type = type ?? throw new ArgumentNullException(nameof(type));
			Name = name.ThrowIfEmpty(nameof(name));
			Constraints = constraints ?? throw new ArgumentNullException(nameof(constraints));
		}

		#endregion

		public string Name { get; }
		public IEnumerable<Constraint> Constraints { get; }
		public Type Type { get; }
	}
}