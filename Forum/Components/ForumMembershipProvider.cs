namespace Ecng.Forum.Components
{
	using System;
	using System.Collections.Generic;

	using Ecng.Web;
	using Ecng.Forum.BusinessEntities;
	
	public abstract class ForumMembershipProvider : BaseMembershipProvider
	{
		#region BaseMembershipProvider<User> Members

		protected override IWebRoleCollection Roles => ForumHelper.GetRootObject<ForumRootObject>().Roles;

		protected override IWebUserCollection Users => ForumHelper.GetRootObject<ForumRootObject>().Users;

		protected override IEnumerable<IWebUser> GetUserRange(int pageIndex, int pageSize, out int totalRecords)
		{
			totalRecords = Users.Count;
			return ForumUsers.GetRange(pageIndex, pageSize);
		}

		protected override IEnumerable<IWebUser> GetUserRangeByName(string userNameToMatch, int pageIndex, int pageSize, out int totalRecords)
		{
			throw new NotSupportedException();
		}

		protected override IEnumerable<IWebUser> GetUserRangeByEmail(string emailToMatch, int pageIndex, int pageSize, out int totalRecords)
		{
			totalRecords = Users.Count;
			return ForumUsers.ReadAllByEmail(emailToMatch, pageIndex, pageSize);
		}

		protected override void UpdateUser(IWebUser user)
		{
			ForumUsers.Update((ForumUser)user);
		}

		protected override void SecurityError(UnauthorizedAccessException ex)
		{
			ForumHelper.Logger.Log(ex);
		}
		
		#endregion
		
		#region MembershipProvider Members

		public override int GetNumberOfUsersOnline()
		{
			return ForumHelper.GetRootObject<ForumRootObject>().OnlineUsers.Count;
		}

		#endregion

		private static ForumUserList ForumUsers => ForumHelper.GetRootObject<ForumRootObject>().Users;
	}
}