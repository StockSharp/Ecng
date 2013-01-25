namespace Ecng.UI
{
	using System;
	using System.Windows.Media;
	using System.Windows.Shapes;

	using Ecng.Common;

	public class ScrollerSlot
	{
		#region ScrollerSlot.ctor()

		public ScrollerSlot(ImageSource imageSource, object tag)
			: this(imageSource, tag, tag.ToString())
		{}

		public ScrollerSlot(ImageSource imageSource, object tag, string tooltip)
		{
			if (imageSource == null)
				throw new ArgumentNullException("imageSource");

			if (tag == null)
				throw new ArgumentNullException("tag");

			if (tooltip.IsEmpty())
				throw new ArgumentNullException("tooltip");

			this.ImageSource = imageSource;
			this.Tag = tag;
			this.Tooltip = tooltip;
		}

		#endregion

		public ImageSource ImageSource { get; private set; }
		public object Tag { get; private set; }
		public string Tooltip { get; private set; }

		internal Rectangle Rect { get; set; }
	}
}