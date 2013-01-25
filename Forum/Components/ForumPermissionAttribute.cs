namespace Ecng.Forum.Components
{
	using System;
	using System.Security;
	using System.Security.Permissions;

	using Ecng.Forum.BusinessEntities;

	public abstract class ForumPermissionAttribute : CodeAccessSecurityAttribute
	{
		protected ForumPermissionAttribute(SecurityAction action)
			: base(action)
		{
		}

		protected ForumRole Role { get; set; }

		public override IPermission CreatePermission()
		{
			return Unrestricted
				? new PrincipalPermission(PermissionState.Unrestricted)
				: new PrincipalPermission(null, Role.Name, true);
		}
	}

	[Serializable]
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
	public class UserPermissionAttribute : ForumPermissionAttribute
	{
		public UserPermissionAttribute(SecurityAction action)
			: base(action)
		{
			Role = ForumHelper.GetRootObject<ForumRootObject>().Roles.Users;
		}
	}

	[Serializable]
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
	public class ModeratorPermissionAttribute : ForumPermissionAttribute
	{
		public ModeratorPermissionAttribute(SecurityAction action)
			: base(action)
		{
			Role = ForumHelper.GetRootObject<ForumRootObject>().Roles.Moderators;
		}
	}

	[Serializable]
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
	public class EditorPermissionAttribute : ForumPermissionAttribute
	{
		public EditorPermissionAttribute(SecurityAction action)
			: base(action)
		{
			Role = ForumHelper.GetRootObject<ForumRootObject>().Roles.Editors;
		}
	}

	[Serializable]
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
	public class AdminPermissionAttribute : ForumPermissionAttribute
	{
		public AdminPermissionAttribute(SecurityAction action)
			: base(action)
		{
			Role = ForumHelper.GetRootObject<ForumRootObject>().Roles.Administrators;
		}
	}
}
