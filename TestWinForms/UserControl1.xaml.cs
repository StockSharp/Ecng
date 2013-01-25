namespace TestWinForms
{
	using System.Windows;

	using Ecng.Xaml;

	/// <summary>
	/// Interaction logic for UserControl1.xaml
	/// </summary>
	public partial class UserControl1
	{
		public UserControl1()
		{
			InitializeComponent();
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			new Window1().ShowModal(this.GetWindow());
		}
	}
}
