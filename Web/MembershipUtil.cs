namespace Ecng.Web
{
	using System.Diagnostics;
	using System.Web.Hosting;
	using System.Web.Security;

	using Ecng.Common;

	public static class MembershipUtil
	{
		private static string _defaultAppName;

		public static string DefaultAppName
		{
			get
			{
				if (_defaultAppName == null)
				{
					if (HostingEnvironment.ApplicationVirtualPath.IsEmpty())
					{
						var moduleName = Process.GetCurrentProcess().MainModule.ModuleName;

						var index = moduleName.IndexOf('.');
						if (index != -1)
							moduleName = moduleName.Remove(index);

						_defaultAppName = moduleName.IsEmpty() ? "/" : moduleName;
					}
					else
						_defaultAppName = HostingEnvironment.ApplicationVirtualPath;
				}

				return _defaultAppName;
			}
		}

		public static SecurityErrorTypes? Validate(string userName, string password)
		{
			return ((BaseMembershipProvider)Membership.Provider).ValidateUserInternal(userName, password);
		}
	}
}