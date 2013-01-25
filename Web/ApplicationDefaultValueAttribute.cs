namespace Ecng.Web
{
	#region Using Directives

	using System.ComponentModel;

	#endregion

	public class ApplicationDefaultValueAttribute : DefaultValueAttribute
	{
		#region ApplicationDefaultValueAttribute.ctor()

		public ApplicationDefaultValueAttribute()
			: base(MembershipUtil.DefaultAppName)
		{
		}

		#endregion
	}
}