namespace Ecng.Interop
{
	using System;
	using System.Security;
	using System.DirectoryServices;
	using System.DirectoryServices.AccountManagement;

	using Ecng.Common;

	public static class WindowsIdentityManager
	{
		public static bool Validate(string userName, SecureString password, string domain = null)
		{
			using var context = new PrincipalContext(domain.IsEmpty() ? ContextType.Machine : ContextType.Domain, domain);
			return context.ValidateCredentials(userName, password.UnSecure());
		}

		// http://stackoverflow.com/a/642511
		public static bool DeleteUser(string userName)
		{
			var localDirectory = new DirectoryEntry("WinNT://" + Environment.MachineName);
			var users = localDirectory.Children;
			var user = users.Find(userName);
			users.Remove(user);
			return true;
		}

		// http://stackoverflow.com/a/6834015
		public static bool CreateUser(string userName, SecureString password, string domain = null)
		{
			using var context = new PrincipalContext(domain.IsEmpty() ? ContextType.Machine : ContextType.Domain, domain);
			var oUserPrincipal = new UserPrincipal(context)
			{
				Name = userName
			};
			oUserPrincipal.SetPassword(password.UnSecure());
			//User Log on Name
			//oUserPrincipal.UserPrincipalName = sUserName;
			oUserPrincipal.Save();
			return true;
		}

		public static bool ChangePassword(string userName, SecureString oldPassword, SecureString newPassword, string domain = null)
		{
			using var context = new PrincipalContext(domain.IsEmpty() ? ContextType.Machine : ContextType.Domain, domain);
			var user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, userName);

			if (user is null)
				return false;

			user.ChangePassword(oldPassword.UnSecure(), newPassword.UnSecure());
			return true;
		}
	}
}