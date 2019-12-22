namespace Ecng.Xaml.DevExp
{
	using System;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Data;

	using DevExpress.Xpf.Dialogs;

	using Ecng.Common;

	/// <summary>
	/// Визуальный редактор для выбора файла.
	/// </summary>
	public partial class FileBrowserPicker
	{
		/// <summary>
		/// Создать <see cref="FileBrowserPicker"/>.
		/// </summary>
		public FileBrowserPicker()
		{
			InitializeComponent();
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="DefaultExt"/>.
		/// </summary>
		public static readonly DependencyProperty DefaultExtProperty =
			DependencyProperty.Register(nameof(DefaultExt), typeof(string), typeof(FileBrowserPicker), new PropertyMetadata(default(string)));

		public string DefaultExt
		{
			get => (string)GetValue(DefaultExtProperty);
			set => SetValue(DefaultExtProperty, value);
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="Filter"/>.
		/// </summary>
		public static readonly DependencyProperty FilterProperty =
			DependencyProperty.Register(nameof(Filter), typeof(string), typeof(FileBrowserPicker), new PropertyMetadata(default(string)));

		public string Filter
		{
			get => (string)GetValue(FilterProperty);
			set => SetValue(FilterProperty, value);
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> для <see cref="File"/>.
		/// </summary>
		public static readonly DependencyProperty FileProperty =
			DependencyProperty.Register(nameof(File), typeof(string), typeof(FileBrowserPicker), new PropertyMetadata(default(string)));

		/// <summary>
		/// Директория.
		/// </summary>
		public string File
		{
			get => (string)GetValue(FileProperty);
			set => SetValue(FileProperty, value);
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="IsSaving"/>.
		/// </summary>
		public static readonly DependencyProperty IsSavingProperty =
			DependencyProperty.Register(nameof(IsSaving), typeof(bool), typeof(FileBrowserPicker), new PropertyMetadata(default(bool), (o, args) =>
			{
				var picker = (FileBrowserPicker)o;
				var binding = BindingOperations.GetBinding(picker.FilePath, TextBox.TextProperty);
				(((FileValidationRule)binding.ValidationRules[0])).IsActive = !(bool)args.NewValue;
			}));

		public bool IsSaving
		{
			get => (bool)GetValue(IsSavingProperty);
			set => SetValue(IsSavingProperty, value);
		}

		/// <summary>
		/// Событие изменения <see cref="File"/>.
		/// </summary>
		public event Action<string> FileChanged;

		private void OpenFile_Click(object sender, RoutedEventArgs e)
		{
			var dlg = IsSaving
				? (DXFileDialog)new DXSaveFileDialog()
				: new DXOpenFileDialog { CheckFileExists = true };

			if (!Filter.IsEmpty())
				dlg.Filter = Filter;

			if (!DefaultExt.IsEmpty())
				dlg.DefaultExt = DefaultExt;

			if (!File.IsEmpty())
				dlg.FileName = File;

			var owner = (sender as DependencyObject)?.GetWindow();

			if (dlg.ShowDialog(owner) == true)
			{
				File = dlg.FileName;
				FileChanged?.Invoke(dlg.FileName);
			}
		}

		private void FilePath_OnTextChanged(object sender, TextChangedEventArgs e)
		{
			FileChanged?.Invoke(FilePath.Text);
		}
	}
}