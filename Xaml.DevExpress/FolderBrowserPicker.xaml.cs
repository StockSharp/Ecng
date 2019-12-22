namespace Ecng.Xaml.DevExp
{
	using System;
	using System.Windows;
	using System.Windows.Controls;

	using DevExpress.Xpf.Dialogs;

	using Ecng.Common;

	/// <summary>
	/// Визуальный редактор для выбора директории.
	/// </summary>
	public partial class FolderBrowserPicker
	{
		/// <summary>
		/// Создать <see cref="FolderBrowserPicker"/>.
		/// </summary>
		public FolderBrowserPicker()
		{
			InitializeComponent();
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> для <see cref="Folder"/>.
		/// </summary>
		public static readonly DependencyProperty FolderProperty =
			DependencyProperty.Register(nameof(Folder), typeof (string), typeof(FolderBrowserPicker), new PropertyMetadata(default(string)));

		/// <summary>
		/// Директория.
		/// </summary>
		public string Folder
		{
			get => (string)GetValue(FolderProperty);
			set => SetValue(FolderProperty, value);
		}

		/// <summary>
		/// Событие изменения <see cref="Folder"/>.
		/// </summary>
		public event Action<string> FolderChanged;

		private void OpenFolder_Click(object sender, RoutedEventArgs e)
		{
			var dlg = new DXFolderBrowserDialog();

			if (!FolderPath.Text.IsEmpty())
				dlg.SelectedPath = FolderPath.Text;

			var owner = sender is DependencyObject depObj ? depObj.GetWindow() : null;

			if (dlg.ShowModalNative(owner))
			{
				FolderPath.Text = dlg.SelectedPath;
				FolderChanged?.Invoke(dlg.SelectedPath);
			}
		}

		private void FolderPath_OnTextChanged(object sender, TextChangedEventArgs e)
		{
			FolderChanged?.Invoke(FolderPath.Text);
		}
	}
}