namespace Ecng.Web.UI.WebControls
{
	#region Using Directives

	using System.ComponentModel;
	using System.Security.Permissions;
	using System.Web;
	using System.Web.UI;
	using System.Web.UI.Design.WebControls;
	using System.Web.UI.WebControls;

	using Ecng.Common;
	using Ecng.Web.Properties;

	#endregion

	[ToolboxData("<{0}:ScrollablePanel runat=\"server\"></{0}:ScrollablePanel>")]
	[ParseChildren(false)]
	[Designer(typeof(PanelContainerDesigner))]
	[AspNetHostingPermission(SecurityAction.LinkDemand, Level = AspNetHostingPermissionLevel.Minimal)]
	[AspNetHostingPermission(SecurityAction.InheritanceDemand, Level = AspNetHostingPermissionLevel.Minimal)]
	[PersistChildren(true)]
	public class ScrollablePanel : Panel
	{
		#region WebControl Members

		protected override void Render(HtmlTextWriter writer)
		{
			//const string initKey = "scroll_init";
			//if (!base.Page.ClientScript.IsStartupScriptRegistered(typeof(ScrollablePanel), initKey))
			//{
			//    base.Page.ClientScript.RegisterStartupScript(typeof(ScrollablePanel), "flexcroll", Resources.FlexcrollCore, true);
			//    base.Page.ClientScript.RegisterStartupScript(typeof(ScrollablePanel), initKey, Resources.FlexcrollInit, true);
			//}

			//string setKey = "scroll_set_" + base.ClientID;

			//if (!base.Page.ClientScript.IsStartupScriptRegistered(setKey))
			//    base.Page.ClientScript.RegisterStartupScript(typeof(ScrollablePanel), setKey, "PutScrollablePanelNewId('{0}');".Put(base.ClientID), true);

			this.RegisterScript("flexcroll", Resources.FlexcrollInit);
			this.RegisterScript("scroll_init", Resources.FlexcrollInit);
			this.RegisterScript("scroll_set_" + base.ClientID, "PutScrollablePanelNewId('{0}');".Put(base.ClientID));

			base.Render(writer);
		}

		#endregion
	}
}