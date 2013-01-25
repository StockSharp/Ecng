namespace Ecng.Web.UI.WebControls
{
	#region Using Directives

	using System;
	using System.ComponentModel;
	using System.Web.UI.WebControls;

	using Ecng.Common;

	#endregion

	public class ImageMenu : Menu
	{
		#region ImageResolving

		private static readonly object _eventImageResolving = new object();

		[Description("")]
		[Category("Action")]
		public event EventHandler<ImageResolvingEventArgs> ImageResolving
		{
			add
			{
				Events.AddHandler(_eventImageResolving, value);
			}
			remove
			{
				Events.RemoveHandler(_eventImageResolving, value);
			}
		}

		#endregion

		protected override void OnMenuItemDataBound(MenuEventArgs e)
		{
			base.OnMenuItemDataBound(e);

			((EventHandler<ImageResolvingEventArgs>)Events[_eventImageResolving]).SafeInvoke(this, new ImageResolvingEventArgs(e.Item),
			args =>
			{
				e.Item.ImageUrl = args.ImageUrl;
				e.Item.Text = string.Empty;
			});
		}
	}
}