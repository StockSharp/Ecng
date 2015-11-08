namespace Ecng.Forum.BusinessEntities
{
	#region Using Directives

	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Ecng.Logic.BusinessEntities;
	using Ecng.Serialization;
	using Ecng.Web;

	#endregion

	[Serializable]
	public class ForumRoleList : BaseRoleList<ForumUser, ForumRole>, IWebRoleCollection
	{
		public ForumRoleList(IStorage storage)
			: base(storage)
		{
		}

		#region Users

		private static ForumRole _users;

		public ForumRole Users => _users ?? (_users = ReadById((long)Identities.RoleUsers));

		#endregion

		#region Administrators

		private static ForumRole _administrators;

		public ForumRole Administrators => _administrators ?? (_administrators = ReadById((long)Identities.RoleAdministrators));

		#endregion

		#region Moderators

		private static ForumRole _moderators;

		public ForumRole Moderators => _moderators ?? (_moderators = ReadById((long)Identities.RoleModerators));

		#endregion

		#region Editors

		private static ForumRole _editors;

		public ForumRole Editors => _editors ?? (_editors = ReadById((long)Identities.RoleEditors));

		#endregion

		#region IWebRoleCollection

		IEnumerator<IWebRole> IEnumerable<IWebRole>.GetEnumerator()
		{
			return this.Cast<IWebRole>().GetEnumerator();
		}

		void ICollection<IWebRole>.Add(IWebRole role)
		{
			base.Add((ForumRole)role);
		}

		bool ICollection<IWebRole>.Contains(IWebRole role)
		{
			return base.Contains((ForumRole)role);
		}

		void ICollection<IWebRole>.CopyTo(IWebRole[] array, int arrayIndex)
		{
			CopyTo((ForumRole[])array, arrayIndex);
		}

		bool ICollection<IWebRole>.Remove(IWebRole role)
		{
			return base.Remove((ForumRole)role);
		}

		IWebRole IWebRoleCollection.GetByName(string roleName)
		{
			return ReadByName(roleName);
		}

		#endregion
	}

	[Serializable]
	class UserRoleList : ForumRoleList
	{
		#region UserRoleList.ctor()

		public UserRoleList(IStorage storage, ForumUser owner)
			: base(storage)
		{
			AddFilter(new VoidField<long>("Owner"), owner);
			OverrideCreateDelete = true;
		}

		#endregion
	}
}