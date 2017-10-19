namespace Ecng.Xaml
{
	using System.Windows.Documents;
	using System.Windows.Navigation;

	using Ecng.Interop;

	public class HyperlinkEx : Hyperlink
	{
		public HyperlinkEx()
		{
			RequestNavigate += Hyperlink_OnRequestNavigate;
		}

		private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
		{
			NavigateUri.ToString().OpenLink(false);
			e.Handled = true;
		}
	}
}