namespace Ecng.Xaml
{
	using System;
	using System.Windows;
	using System.Windows.Documents;
	using System.Windows.Navigation;

	using Ecng.Common;

	public class HyperlinkEx : Hyperlink
	{
		private static readonly Uri DefaultNavigateUri = new Uri(".", UriKind.RelativeOrAbsolute);

		static HyperlinkEx() => NavigateUriProperty.OverrideMetadata(typeof(HyperlinkEx), new FrameworkPropertyMetadata(DefaultNavigateUri));

		public HyperlinkEx()
		{
			RequestNavigate += Hyperlink_OnRequestNavigate;
		}

		private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
		{
			if (NavigateUri == DefaultNavigateUri)
				return;

			NavigateUri.ToString().OpenLink(false);
			e.Handled = true;
		}
	}
}