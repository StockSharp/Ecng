namespace Ecng.Xaml
{
	using System.Windows;

	public static class Doc
	{
		public static readonly DependencyProperty UrlProperty = DependencyProperty.RegisterAttached("Url", typeof(string), typeof(Doc), new PropertyMetadata(null));

		public static void SetUrl(UIElement element, string value)
		{
			element.SetValue(UrlProperty, value);
		}

		public static string GetUrl(UIElement element)
		{
			return (string)element.GetValue(UrlProperty);
		}
	}
}