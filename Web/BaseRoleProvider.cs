namespace Ecng.Web
{
	#region Using Directives

	using System.Linq;
	using System.Collections.Generic;
	using System.Collections.Specialized;
	using System.Web.Security;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Serialization;

	#endregion

	[Ignore(FieldName = "_name")]
	[Ignore(FieldName = "_Initialized")]
	[Ignore(FieldName = "_Description")]
	[TypeSchemaFactory(SearchBy.Fields, VisibleScopes.Both)]
	public abstract class BaseRoleProvider : RoleProvider
		//where TUser : class, IWebUser
		//where TRole : class, IWebRole, new()
	{
		#region ProviderBase Members

		public override void Initialize(string name, NameValueCollection config)
		{
			this.Initialize(config);
			base.Initialize(name, config);
		}

		#endregion

		#region RoleProvider Members

		[ApplicationDefaultValue]
		private string _applicationName;

		public override string ApplicationName
		{
			get { return _applicationName; }
			set { _applicationName = value; }
		}

		public override bool IsUserInRole(string userName, string roleName)
		{
			return GetRolesForUser(userName).Contains(roleName);
		}

		public override bool RoleExists(string roleName)
		{
			return GetAllRoles().Contains(roleName);
		}

		public override void AddUsersToRoles(string[] userNames, string[] roleNames)
		{
			var users = userNames.Select(GetUser).ToList();
            var roles = roleNames.Select(GetRole).ToList();
			AddUsersToRoles(users, roles);
		}

		public override void RemoveUsersFromRoles(string[] userNames, string[] roleNames)
		{
			var users = userNames.Select(GetUser).ToList();
			var roles = roleNames.Select(GetRole).ToList();
			RemoveUsersFromRoles(users, roles);
		}

		public override void CreateRole(string roleName)
		{
			var role = CreateWebRole(roleName);
			Roles.Add(role);
		}

		public override bool DeleteRole(string roleName, bool throwOnPopulatedRole)
		{
			var role = GetRole(roleName);

			if (role != null)
			{
				Roles.Remove(role);
				return true;
			}
			else
				return false;
		}

		public override string[] GetAllRoles()
		{
			return Roles.Select(role => role.Name).ToArray();
		}

		public override string[] GetRolesForUser(string userName)
		{
			if (userName.IsEmpty())
				return new string[0];

			var roleNames = new List<string>();

			var user = GetUser(userName);
			if (user != null)
			{
				roleNames.AddRange(user.Roles.Select(role => role.Name));
			}

			return roleNames.ToArray();
		}

		public override string[] GetUsersInRole(string roleName)
		{
			var userNames = new List<string>();

			var role = GetRole(roleName);

			if (role != null)
			{
				userNames.AddRange(role.Users.Select(user => user.Name));
			}

			return userNames.ToArray();
		}

		public override string[] FindUsersInRole(string roleName, string userNameToMatch)
		{
			var userNames = new List<string>();

			var role = GetRole(roleName);

			if (role != null)
			{
				userNames.AddRange(role.Users.Select(user => user.Name));
			}

			return userNames.ToArray();
		}

		#endregion

		protected virtual void AddUsersToRoles(IEnumerable<IWebUser> users, IEnumerable<IWebRole> roles)
		{
			foreach (var user in users)
				user.Roles.AddRange(roles);
		}

		protected virtual void RemoveUsersFromRoles(IEnumerable<IWebUser> users, IEnumerable<IWebRole> roles)
		{
			foreach (var user in users)
				user.Roles.RemoveRange(roles);
		}

		protected virtual IWebRole GetRole(string roleName)
		{
			return Roles.GetByName(roleName);
		}

		protected virtual IWebUser GetUser(string userName)
		{
			return Users.GetByName(userName);
		}

		protected abstract IWebRoleCollection Roles { get; }
		protected abstract IWebUserCollection Users { get; }

		protected abstract IWebRole CreateWebRole(string name);
	}
}