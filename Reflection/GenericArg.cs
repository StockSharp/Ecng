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
			if (type == null)
				throw new ArgumentNullException(nameof(type));

			if (name.IsEmpty())
				throw new ArgumentNullException(nameof(name));

			if (constraints == null)
				throw new ArgumentNullException(nameof(constraints));

			Type = type;
			Name = name;
			Constraints = constraints;
		}

		#endregion

		public string Name { get; private set; }
		public IEnumerable<Constraint> Constraints { get; private set; }
		public Type Type { get; private set; }
	}
}