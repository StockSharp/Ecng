namespace Ecng.Net
{
	#region Using Directives
	
	using System;
	using System.Reflection;
	
	#endregion

	[AttributeUsage(AttributeTargets.Method)]
	public abstract class RemoteMethodAttribute : Attribute
	{
		public bool IsCached { get; set; }
		public bool IsCompressed { get; set; }

		protected internal abstract object GetId(MethodInfo method);
	}
}