namespace Ecng.Web.UI.WebControls
{
	using System;
	using System.ComponentModel;
	using System.Linq;
	using System.Security.Permissions;
	using System.Threading;
	using System.Web;
	using System.Web.UI;
	using System.Web.UI.Design.WebControls;

	using Ecng.Common;

	[ToolboxData("<{0}:RoleSecurityPanel runat=\"server\"></{0}:RoleSecurityPanel>")]
	[ParseChildren(false)]
	[Designer(typeof(PanelContainerDesigner))]
	[AspNetHostingPermission(SecurityAction.LinkDemand, Level = AspNetHostingPermissionLevel.Minimal)]
	[AspNetHostingPermission(SecurityAction.InheritanceDemand, Level = AspNetHostingPermissionLevel.Minimal)]
	[PersistChildren(true)]
	[DefaultProperty("Roles")]
	public class RoleSecurityPanel : SecurityPanel
	{
		private string _roles = string.Empty;

		[Bindable(true)]
		[Category("Appearance")]
		[DefaultValue("")]
		public string Roles
		{
			get => _roles;
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				_roles = value;
			}
		}

		protected override bool CanView()
		{
			return Roles.SplitByComma().Any(CanView);
		}

		protected virtual bool CanView(string roleName)
		{
			return Thread.CurrentPrincipal.IsInRole(roleName);
		}
	}
}