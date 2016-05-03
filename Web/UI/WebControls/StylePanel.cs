namespace Ecng.Web.UI.WebControls
{
	using System.ComponentModel;
	using System.Linq;
	using System.Security.Permissions;
	using System.Web;
	using System.Web.UI;
	using System.Web.UI.Design.WebControls;
	using System.Web.UI.WebControls;

	using Ecng.Collections;
	using Ecng.Common;

	public enum StyleTypes
	{
		ByClass,
		ById,
	}

	[ToolboxData("<{0}:StylePanel runat=\"server\"></{0}:StylePanel>")]
	[ParseChildren(false)]
	[Designer(typeof(PanelContainerDesigner))]
	[AspNetHostingPermission(SecurityAction.LinkDemand, Level = AspNetHostingPermissionLevel.Minimal)]
	[AspNetHostingPermission(SecurityAction.InheritanceDemand, Level = AspNetHostingPermissionLevel.Minimal)]
	[PersistChildren(true)]
	[DefaultProperty("Styles")]
	public class StylePanel : Panel
	{
		#region Styles

		[Description("Wrapped styles")]
		[DefaultValue("")]
		[Category("Accessibility")]
		public string Styles { get; set; } = string.Empty;

		#endregion

		#region WebControl Members

		public override void RenderBeginTag(HtmlTextWriter writer)
		{
			var styles = GetStyles();

			if (!styles.IsEmpty())
			{
				foreach (var style in styles)
				{
					writer.AddAttribute(HtmlTextWriterAttribute.Class, style);
					writer.RenderBeginTag(HtmlTextWriterTag.Div);
				}
			}
			else
				base.RenderBeginTag(writer);
		}

		public override void RenderEndTag(HtmlTextWriter writer)
		{
			var styles = GetStyles();
			var count = styles.Length;

			if (count > 0)
			{
				for (var i = 0; i < count; i++)
				{
					writer.RenderEndTag();
				}
			}
			else
				base.RenderEndTag(writer);
		}

		#endregion

		private string[] GetStyles()
		{
			var styles = Styles;
			return !styles.IsEmpty() ? styles.Split(',').Select(arg => arg.Trim()).ToArray() : ArrayHelper.Empty<string>();
		}
	}
}