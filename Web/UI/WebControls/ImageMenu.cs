namespace Ecng.Web.UI.WebControls
{
	using System;
	using System.ComponentModel;
	using System.Web.UI.WebControls;

	public class ImageMenu : Menu
	{
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

		protected override void OnMenuItemDataBound(MenuEventArgs e)
		{
			base.OnMenuItemDataBound(e);

			var handler = (EventHandler<ImageResolvingEventArgs>)Events[_eventImageResolving];

			if (handler == null)
				return;

			handler(this, new ImageResolvingEventArgs(e.Item));

			e.Item.ImageUrl = e.Item.ImageUrl;
			e.Item.Text = string.Empty;
		}
	}
}