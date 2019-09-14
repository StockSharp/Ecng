namespace Ecng.Security
{
	using System;
	using System.ComponentModel;
	using System.DirectoryServices;
	using System.DirectoryServices.AccountManagement;
	using System.Runtime.InteropServices;
	using System.Security;
	using System.Security.Principal;
	using System.Threading;

	using Ecng.Common;

	public enum LogonType
	{
		/// <summary>
		/// This logon type is intended for users who will be interactively using the computer, such as a user being logged on  
		/// by a terminal server, remote shell, or similar process.
		/// This logon type has the additional expense of caching logon information for disconnected operations;
		/// therefore, it is inappropriate for some client/server applications,
		/// such as a mail server.
		/// </summary>
		Interactive = 2,

		/// <summary>
		/// This logon type is intended for high performance servers to authenticate plaintext passwords.

		/// The LogonUser function does not cache credentials for this logon type.
		/// </summary>
		Network = 3,

		/// <summary>
		/// This logon type is intended for batch servers, where processes may be executing on behalf of a user without
		/// their direct intervention. This type is also for higher performance servers that process many plaintext
		/// authentication attempts at a time, such as mail or Web servers.
		/// The LogonUser function does not cache credentials for this logon type.
		/// </summary>
		Batch = 4,

		/// <summary>
		/// Indicates a service-type logon. The account provided must have the service privilege enabled.
		/// </summary>
		Service = 5,

		/// <summary>
		/// This logon type is for GINA DLLs that log on users who will be interactively using the computer.
		/// This logon type can generate a unique audit record that shows when the workstation was unlocked.
		/// </summary>
		Unlock = 7,

		/// <summary>
		/// This logon type preserves the name and password in the authentication package, which allows the server to make
		/// connections to other network servers while impersonating the client. A server can accept plaintext credentials
		/// from a client, call LogonUser, verify that the user can access the system across the network, and still
		/// communicate with other servers.
		/// NOTE: Windows NT:  This value is not supported.
		/// </summary>
		NetworkClearText = 8,

		/// <summary>
		/// This logon type allows the caller to clone its current token and specify new credentials for outbound connections.
		/// The new logon session has the same local identifier but uses different credentials for other network connections.
		/// NOTE: This logon type is supported only by the LOGON32_PROVIDER_WINNT50 logon provider.
		/// NOTE: Windows NT:  This value is not supported.
		/// </summary>
		NewCredentials = 9,
	}

	public enum LogonProvider
	{
		/// <summary>
		/// Use the standard logon provider for the system.
		/// The default security provider is negotiate, unless you pass NULL for the domain name and the user name
		/// is not in UPN format. In this case, the default provider is NTLM.
		/// NOTE: Windows 2000/NT:   The default security provider is NTLM.
		/// </summary>
		Default = 0,
	}

	public enum SecurityImpersonationLevel
	{
		/// <summary>
		/// The server process cannot obtain identification information about the client,
		/// and it cannot impersonate the client. It is defined with no value given, and thus,
		/// by ANSI C rules, defaults to a value of zero.
		/// </summary>
		SecurityAnonymous = 0,

		/// <summary>
		/// The server process can obtain information about the client, such as security identifiers and privileges,
		/// but it cannot impersonate the client. This is useful for servers that export their own objects,
		/// for example, database products that export tables and views.
		/// Using the retrieved client-security information, the server can make access-validation decisions without
		/// being able to use other services that are using the client's security context.
		/// </summary>
		SecurityIdentification = 1,

		/// <summary>
		/// The server process can impersonate the client's security context on its local system.
		/// The server cannot impersonate the client on remote systems.
		/// </summary>
		SecurityImpersonation = 2,

		/// <summary>
		/// The server process can impersonate the client's security context on remote systems.
		/// NOTE: Windows NT:  This impersonation level is not supported.
		/// </summary>
		SecurityDelegation = 3,
	}

	public static class WindowsIdentityManager
	{
		[DllImport("advapi32.dll", SetLastError = true)]
		private static extern bool LogonUser(string lpszUsername, string lpszDomain, string lpszPassword, LogonType dwLogonType, LogonProvider dwLogonProvider, out IntPtr phToken);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool CloseHandle(IntPtr handle);

		[DllImport("advapi32.dll", SetLastError = true)]
		private static extern bool DuplicateToken(IntPtr existingTokenHandle, SecurityImpersonationLevel level, out IntPtr duplicateTokenHandle);

		public static void Login(string name, string password, Action action)
		{
			Login(name, string.Empty, password, action);
		}

		public static void Login(string name, string domain, string password, Action action)
		{
			Login(name, string.Empty, password, LogonType.Network, action);
		}

		public static void Login(string name, string domain, string password, LogonType logonType, Action action)
		{
			if (LogonUser(name, domain, password, logonType, LogonProvider.Default, out var token))
			{
				try
				{
					if (DuplicateToken(token, SecurityImpersonationLevel.SecurityImpersonation, out var tokenDuplicate))
					{
						try
						{
							using (var identity = new WindowsIdentity(tokenDuplicate))
							{
								var oldPrincipal = Thread.CurrentPrincipal;
								try
								{
									using (identity.Impersonate())
									{
										Thread.CurrentPrincipal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
										action();
									}
								}
								finally
								{
									Thread.CurrentPrincipal = oldPrincipal;
								}
							}
						}
						finally
						{
							CloseHandle(tokenDuplicate);
						}
					}
					else
						throw new Win32Exception();
				}
				finally
				{
					CloseHandle(token);
				}
			}
			else
				throw new Win32Exception();
		}

		public static bool Validate(string userName, SecureString password, string domain = null)
		{
			using (var context = new PrincipalContext(domain.IsEmpty() ? ContextType.Machine : ContextType.Domain, domain))
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
			using (var context = new PrincipalContext(domain.IsEmpty() ? ContextType.Machine : ContextType.Domain, domain))
			{
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
		}

		public static bool ChangePassword(string userName, SecureString oldPassword, SecureString newPassword, string domain = null)
		{
			using (var context = new PrincipalContext(domain.IsEmpty() ? ContextType.Machine : ContextType.Domain, domain))
			{
				var user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, userName);

				if (user == null)
					return false;

				user.ChangePassword(oldPassword.UnSecure(), newPassword.UnSecure());
				return true;
			}
		}
	}
}