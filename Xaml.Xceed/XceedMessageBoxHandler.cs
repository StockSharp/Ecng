namespace Ecng.Xaml.Xceed
{
	using System.Windows;

	using Ecng.Xaml;

	public class XceedMessageBoxHandler : IMessageBoxHandler
	{
		MessageBoxResult IMessageBoxHandler.Show(string text, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult, MessageBoxOptions options)
		{
			return global::Xceed.Wpf.Toolkit.MessageBox.Show(text, caption, button, icon, defaultResult);
		}

		MessageBoxResult IMessageBoxHandler.Show(Window owner, string text, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult, MessageBoxOptions options)
		{
			return global::Xceed.Wpf.Toolkit.MessageBox.Show(owner, text, caption, button, icon, defaultResult);
		}
	}
}