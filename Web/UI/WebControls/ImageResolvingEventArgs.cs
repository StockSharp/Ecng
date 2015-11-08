namespace Ecng.Web.UI.WebControls
{
	#region Using Directives

	using System;
	using System.Web.UI.WebControls;

	#endregion

	public class ImageResolvingEventArgs : EventArgs
	{
		#region ImageResolvingEventArgs.ctor()

		public ImageResolvingEventArgs(MenuItem item)
		{
			if (item == null)
				throw new ArgumentNullException(nameof(item));

			_item = item;
		}

		#endregion

		#region Item

		private readonly MenuItem _item;

		public MenuItem Item => _item;

		#endregion

		#region ImageUrl

		public string ImageUrl { get; set; }

		#endregion
	}
}