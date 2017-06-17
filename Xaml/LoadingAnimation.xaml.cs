namespace Ecng.Xaml
{
	using System.Windows;

	public partial class LoadingAnimation
    {
		public static readonly DependencyProperty IsBusyProperty = DependencyProperty.Register(nameof(IsBusy), typeof(bool), typeof(LoadingAnimation), new PropertyMetadata(false));

		public bool IsBusy
		{
			get => (bool)GetValue(IsBusyProperty);
			set => SetValue(IsBusyProperty, value);
		}

        public LoadingAnimation()
        {
            InitializeComponent();
        }

    	public string AnimationText
    	{
			get => AnimationTextBlock.Text;
		    set => AnimationTextBlock.Text = value;
	    }
    }
}