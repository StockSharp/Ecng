namespace Ecng.Xaml
{
	using System.Diagnostics;
	using System.Windows.Documents;
	using System.Windows.Navigation;

	public class HyperlinkEx : Hyperlink
	{
		public HyperlinkEx()
		{
			RequestNavigate += Hyperlink_OnRequestNavigate;
		}

		private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
		{
			Process.Start(new ProcessStartInfo(NavigateUri.ToString()));
			e.Handled = true;
		}
	}
}