namespace Ecng.Xaml
{
	using System.Windows;

	public partial class LoadingAnimation
    {
		public static readonly DependencyProperty IsBusyProperty = DependencyProperty.Register("IsBusy", typeof(bool), typeof(LoadingAnimation), new PropertyMetadata(false));

		public bool IsBusy
		{
			get { return (bool)GetValue(IsBusyProperty); }
			set { SetValue(IsBusyProperty, value); }
		}

        public LoadingAnimation()
        {
            InitializeComponent();
        }

    	public string AnimationText
    	{
			get { return AnimationTextBlock.Text; }
			set { AnimationTextBlock.Text = value; }
    	}
    }
}