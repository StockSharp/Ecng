namespace Ecng.Serialization
{
	#region Using Directives

	using System;

	using Ecng.Common;
	using Ecng.Reflection;

	#endregion

	[AttributeUsage(ReflectionHelper.Types)]
	public class EntityAttribute : NameAttribute
	{
		public EntityAttribute(string name)
			: base(name)
		{
		}

		public bool NoCache { get; set; }
	}
}