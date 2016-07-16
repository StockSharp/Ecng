namespace Ecng.Xaml.DevExp
{
	using System.Windows;

	using DevExpress.Xpf.Core;
	using DevExpress.Xpf.WindowsUI;

	public class DevExpMessageBoxHandler : MessageBoxBuilder.IMessageBoxHandler
	{
		MessageBoxResult MessageBoxBuilder.IMessageBoxHandler.Show(string text, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult, MessageBoxOptions options)
		{
			return DXMessageBox.Show(text, caption, button, icon, defaultResult, options);
		}

		MessageBoxResult MessageBoxBuilder.IMessageBoxHandler.Show(Window owner, string text, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult, MessageBoxOptions options)
		{
			return DXMessageBox.Show(owner, text, caption, button, icon, defaultResult, options);
		}
	}

	public class WinUIMessageBoxHandler : MessageBoxBuilder.IMessageBoxHandler
	{
		MessageBoxResult MessageBoxBuilder.IMessageBoxHandler.Show(string text, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult, MessageBoxOptions options)
		{
			return WinUIMessageBox.Show(text, caption, button, icon, defaultResult, options);
		}

		MessageBoxResult MessageBoxBuilder.IMessageBoxHandler.Show(Window owner, string text, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult, MessageBoxOptions options)
		{
			return WinUIMessageBox.Show(owner, text, caption, button, icon, defaultResult, options);
		}
	}
}