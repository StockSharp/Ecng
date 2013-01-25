namespace Ecng.Xaml
{
    public partial class LoadingAnimation
    {
        public LoadingAnimation()
        {
            InitializeComponent();
        }

    	public string AnimationText
    	{
			get { return _animationText.Text; }
			set { _animationText.Text = value; }
    	}
    }
}