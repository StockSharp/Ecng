namespace Ecng.Xaml.DevExp
{
	using System.Globalization;
	using System.IO;
	using System.Windows;
	using System.Windows.Controls;

	using DevExpress.Xpf.Editors;

	using Ecng.Common;
	using Ecng.Localization;

	using Ookii.Dialogs.Wpf;

	public partial class FileBrowserEditor
	{
		public FileBrowserEditor()
		{
			InitializeComponent();
		}

		protected override void AssignToEditCore(IBaseEdit edit)
		{
			var btnEdit = edit as ButtonEdit;

			if (btnEdit != null)
				ValidationHelper.SetBaseEdit(this, btnEdit);

			base.AssignToEditCore(edit);
		}

		private void OpenBtn_OnClick(object sender, RoutedEventArgs e)
		{
			var edit = BaseEdit.GetOwnerEdit((DependencyObject)sender);

			if (edit == null)
				return;

			var dlg = new VistaOpenFileDialog { CheckFileExists = true };
			var value = (string)edit.EditValue;

			if (!value.IsEmpty())
				dlg.FileName = value;

			var owner = ((DependencyObject)sender)?.GetWindow();

			if (dlg.ShowDialog(owner) == true)
				edit.EditValue = dlg.FileName;
		}

		private void ClearBtn_OnClick(object sender, RoutedEventArgs e)
		{
			var edit = BaseEdit.GetOwnerEdit((DependencyObject)sender);

			if (edit == null)
				return;

			edit.EditValue = null;
		}
	}

	class FileValidationRule : ValidationRule
	{
		public override ValidationResult Validate(object value, CultureInfo cultureInfo)
		{
			var path = (string)value;

			if (!path.IsEmpty() && !Directory.Exists(path))
				return new ValidationResult(false, "Invalid file path.".Translate());

			return ValidationResult.ValidResult;
		}
	}
}
