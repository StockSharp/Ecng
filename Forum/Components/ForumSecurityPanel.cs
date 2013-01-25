namespace Ecng.Forum.Components
{
	#region Using Directives

	using System;
	using System.ComponentModel;
	using System.Security.Permissions;
	using System.Web;
	using System.Web.UI;
	using System.Web.UI.Design.WebControls;

	using Ecng.Forum.BusinessEntities;
	using Ecng.Web.UI.WebControls;

	#endregion

	[ToolboxData("<{0}:ForumSecurityPanel runat=\"server\"></{0}:ForumSecurityPanel>")]
	[ParseChildren(false)]
	[Designer(typeof(PanelContainerDesigner))]
	[AspNetHostingPermission(SecurityAction.LinkDemand, Level = AspNetHostingPermissionLevel.Minimal)]
	[AspNetHostingPermission(SecurityAction.InheritanceDemand, Level = AspNetHostingPermissionLevel.Minimal)]
	[PersistChildren(true)]
	[DefaultProperty("Entity")]
	public class ForumSecurityPanel : SecurityPanel
	{
		public ForumSecurityPanel()
		{
			Permissions = PermissionTypes.FullControl;
		}

		public ForumBaseEntity Entity { get; set; }

		[DefaultValue(PermissionTypes.FullControl)]
		public PermissionTypes Permissions { get; set; }

		protected override bool CanView()
		{
			if (Entity != null)
			{
				if (Entity is Message)
					return ForumHelper.SecurityBarrier.TryCheck((Message)Entity, Permissions);
				else if (Entity is Poll)
					return ForumHelper.SecurityBarrier.TryCheck((Poll)Entity, Permissions);
				else if (Entity is Topic)
					return ForumHelper.SecurityBarrier.TryCheck((Topic)Entity, Permissions);
				else if (Entity is Forum)
					return ForumHelper.SecurityBarrier.TryCheck((Forum)Entity, Permissions);
				else if (Entity is ForumFolder)
					return ForumHelper.SecurityBarrier.TryCheck((ForumFolder)Entity, Permissions);
				else
					throw new InvalidOperationException();
			}
			else
				return false;
		}
	}
}