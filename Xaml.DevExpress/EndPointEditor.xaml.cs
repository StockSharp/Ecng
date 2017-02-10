namespace Ecng.Xaml.DevExp
{
	using System.Net;
	using System.Windows;

	/// <summary>
	/// Editor for <see cref="EndPoint"/>.
	/// </summary>
	public partial class EndPointEditor
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="EndPointEditor"/>.
		/// </summary>
		public EndPointEditor()
		{
			InitializeComponent();
			//Address.Mask = @"[а-яА-Яa-zA-Z0-9\.\-]+:?\d+";
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="EndPoint"/>.
		/// </summary>
		public static readonly DependencyProperty EndPointProperty =
			DependencyProperty.Register(nameof(EndPoint), typeof(EndPoint), typeof(EndPointEditor), new PropertyMetadata(default(EndPoint)));

		/// <summary>
		/// Address.
		/// </summary>
		public EndPoint EndPoint
		{
			get { return (EndPoint)GetValue(EndPointProperty); }
			set { SetValue(EndPointProperty, value); }
		}
	}
}