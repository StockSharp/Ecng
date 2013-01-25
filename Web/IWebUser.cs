namespace Ecng.Web
{
	#region Using Directives

	using System;
	using Ecng.Security;

	#endregion

	public interface IWebUser
	{
		object Key { get; }
		string Name { get; }
		string Description { get; }
		string Email { get; set; }

		bool IsApproved { get; set; }
		bool IsLockedOut { get; set; }

		Secret Password { get; set; }
		string PasswordQuestion { get; set; }
		Secret PasswordAnswer { get; set; }

		DateTime CreationDate { get; }
		DateTime LastLoginDate { get; set; }
		DateTime LastActivityDate { get; set; }
		DateTime LastPasswordChangedDate { get; set; }
		DateTime LastLockOutDate { get; set; }

		IWebRoleCollection Roles { get; }
	}
}