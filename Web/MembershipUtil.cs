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

		private static BaseMembershipProvider Provider
		{
			get { return (BaseMembershipProvider)Membership.Provider; }
		}

		public static SecurityErrorTypes? Validate(string userName, string password)
		{
			return Provider.ValidateUserInternal(userName, password);
		}

		public static void ChangePassword(this IWebUser user, string password)
		{
			Provider.ChangePassword(user, password);
		}

		public static void SetAuthCookie(this IWebUser user, bool createPersistentCookie)
		{
			FormsAuthentication.SetAuthCookie(user.Name, createPersistentCookie);
		}

		public static void RedirectFromLoginPage(this IWebUser user, bool createPersistentCookie)
		{
			FormsAuthentication.RedirectFromLoginPage(user.Name, createPersistentCookie);
		}
	}
}