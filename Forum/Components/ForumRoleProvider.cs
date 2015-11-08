namespace Ecng.Forum.Components
{
	#region Using Directives

	using System.Collections.Generic;

	using Ecng.Web;
	using Ecng.Forum.BusinessEntities;
	using Ecng.Transactions;

	#endregion

	public class ForumRoleProvider : BaseRoleProvider
	{
		#region BaseRoleProvider Members

		protected override void AddUsersToRoles(IEnumerable<IWebUser> users, IEnumerable<IWebRole> roles)
		{
			AutoComplete.Do(() => AddUsersToRoles(users, roles));
		}

		protected override void RemoveUsersFromRoles(IEnumerable<IWebUser> users, IEnumerable<IWebRole> roles)
		{
			AutoComplete.Do(() => RemoveUsersFromRoles(users, roles));
		}

		protected override IWebRole CreateWebRole(string name)
		{
			return new ForumRole { Name = name };
		}

		protected override IWebRoleCollection Roles => ForumHelper.GetRootObject<ForumRootObject>().Roles;

		protected override IWebUserCollection Users => ForumHelper.GetRootObject<ForumRootObject>().Users;

		#endregion
	}
}