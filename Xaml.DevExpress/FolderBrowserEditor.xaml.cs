namespace Ecng.Xaml.DevExp
{
	using System.Collections.Generic;
	using System.Globalization;
	using System.IO;
	using System.Windows;
	using System.Windows.Controls;

	using DevExpress.Xpf.Editors;

	using Ecng.Common;
	using Ecng.Localization;

	using Ookii.Dialogs.Wpf;

	public partial class FolderBrowserEditor
	{
		public FolderBrowserEditor()
		{
			InitializeComponent();
		}

		protected override void AssignToEditCore(IBaseEdit edit)
		{

			if (edit is ButtonEdit btnEdit)
				ValidationHelper.SetBaseEdit(this, btnEdit);

			base.AssignToEditCore(edit);
		}

		private void OpenBtn_OnClick(object sender, RoutedEventArgs e)
		{
			var edit = BaseEdit.GetOwnerEdit((DependencyObject)sender);

			if (edit == null)
				return;

			var dlg = new VistaFolderBrowserDialog();
			var value = (string)edit.EditValue;

			if (!value.IsEmpty())
				dlg.SelectedPath = value;

			var owner = ((DependencyObject)sender)?.GetWindow();

			if (dlg.ShowDialog(owner) == true)
				edit.EditValue = dlg.SelectedPath;
		}

		private void ClearBtn_OnClick(object sender, RoutedEventArgs e)
		{
			var edit = BaseEdit.GetOwnerEdit((DependencyObject)sender);

			if (edit == null)
				return;

			edit.EditValue = null;
		}
	}

	class FolderValidationRule : ValidationRule
	{
		public override ValidationResult Validate(object value, CultureInfo cultureInfo)
		{
			var path = (string)value;

			if (!path.IsEmpty() && !Directory.Exists(path))
				return new ValidationResult(false, "Invalid folder path.".Translate());

			return ValidationResult.ValidResult;
		}
	}
}
