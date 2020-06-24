namespace Ecng.Xaml
{
	using System.Windows;

	using DevExpress.Xpf.Core;
	using DevExpress.Xpf.WindowsUI;

	public class DevExpMessageBoxHandler : IMessageBoxHandler
	{
		MessageBoxResult IMessageBoxHandler.Show(string text, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult, MessageBoxOptions options)
		{
			return DXMessageBox.Show(text, caption, button, icon, defaultResult, options);
		}

		MessageBoxResult IMessageBoxHandler.Show(Window owner, string text, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult, MessageBoxOptions options)
		{
			return DXMessageBox.Show(owner, text, caption, button, icon, defaultResult, options);
		}
	}

	public class WinUIMessageBoxHandler : IMessageBoxHandler
	{
		MessageBoxResult IMessageBoxHandler.Show(string text, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult, MessageBoxOptions options)
		{
			return WinUIMessageBox.Show(text, caption, button, icon, defaultResult, options);
		}

		MessageBoxResult IMessageBoxHandler.Show(Window owner, string text, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult, MessageBoxOptions options)
		{
			return WinUIMessageBox.Show(owner, text, caption, button, icon, defaultResult, options);
		}
	}
}