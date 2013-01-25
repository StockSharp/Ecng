namespace Ecng.UI
{
	using System;
	using System.Windows.Input;
	using System.Windows.Media;

	using Ecng.Common;

	public partial class Card
	{
		public Card()
		{
			InitializeComponent();
		}

		public ImageBrush PictureBrush
		{
			get { return (ImageBrush)this.Picture.Fill; }
		}

		public string DescriptionText
		{
			get { return this.Description.Text; }
			set { this.Description.Text = value; }
		}

		public bool Selected { get; set; }
		public event EventHandler<EventArgs> CardClicked;

		#region SelectionBrush

		private static Brush SelectionBrush
		{
			get { return new SolidColorBrush(Colors.Red); }
		}

		#endregion

		#region UnSelectionBrush

		private static Brush UnselectionBrush
		{
			get { return new SolidColorBrush(Colors.Yellow); }
		}

		#endregion

		private void UserControl_MouseEnter(object sender, MouseEventArgs e)
		{
			if (!this.Selected)
				this.Picture.Stroke = SelectionBrush;
		}

		private void UserControl_MouseLeave(object sender, MouseEventArgs e)
		{
			if (!this.Selected)
				this.Picture.Stroke = UnselectionBrush;
		}

		private void UserControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			this.CardClicked.SafeInvoke(this);
		}
	}
}