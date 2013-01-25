namespace Ecng.Common
{
	#region Using Directives

	using System;

	#endregion

	public abstract class OrderedAttribute : Attribute
	{
		public int Order { get; set; }
	}
}