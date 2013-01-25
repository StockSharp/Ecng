namespace Ecng.Forum.Components
{
	using Ecng.Web.UI.WebControls;
	using Ecng.Forum.BusinessEntities;

	public class ModeratorsRoleSecurityPanel : RoleSecurityPanel
	{
		public ModeratorsRoleSecurityPanel()
		{
			Roles = ForumHelper.GetRootObject<ForumRootObject>().Roles.Moderators.Name;
		}
	}

	public class AdminitratorsRoleSecurityPanel : RoleSecurityPanel
	{
		public AdminitratorsRoleSecurityPanel()
		{
			Roles = ForumHelper.GetRootObject<ForumRootObject>().Roles.Administrators.Name;
		}
	}

	public class EditorsRoleSecurityPanel : RoleSecurityPanel
	{
		public EditorsRoleSecurityPanel()
		{
			Roles = ForumHelper.GetRootObject<ForumRootObject>().Roles.Editors.Name;
		}
	}
}