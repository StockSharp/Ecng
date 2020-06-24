namespace Ecng.Xaml
{
	using System.Windows;

	using DevExpress.Xpf.Dialogs;
	using DevExpress.Xpf.Editors;

	using Ecng.Common;
	using Ecng.ComponentModel;

	public partial class FileBrowserEditor : IFileBrowserEditor
	{
		public FileBrowserEditor()
		{
			InitializeComponent();
		}

		public string DefaultExt { get; set; }
		public string Filter { get; set; }
		public bool IsSaving { get; set; }

		protected override void AssignToEditCore(IBaseEdit edit)
		{
			if (edit is ButtonEdit btnEdit)
				ValidationHelper.SetBaseEdit(this, btnEdit);

			base.AssignToEditCore(edit);
		}

		private void OpenBtn_OnClick(object sender, RoutedEventArgs e)
		{
			var edit = BaseEdit.GetOwnerEdit((DependencyObject)sender);

			if (edit == null || edit.IsReadOnly)
				return;

			var dlg = IsSaving ? (DXFileDialog)new DXSaveFileDialog() : new DXOpenFileDialog { CheckFileExists = true };
			dlg.RestoreDirectory = true;

			if (!Filter.IsEmpty())
				dlg.Filter = Filter;

			if (!DefaultExt.IsEmpty())
				dlg.DefaultExt = DefaultExt;

			var value = (string)edit.EditValue;

			if (!value.IsEmpty())
				dlg.FileName = value;

			var owner = ((DependencyObject)sender)?.GetWindow();

			if (dlg.ShowModal(owner))
				edit.EditValue = dlg.FileName;
		}

		private void ClearBtn_OnClick(object sender, RoutedEventArgs e)
		{
			var edit = BaseEdit.GetOwnerEdit((DependencyObject)sender);

			if (edit == null || edit.IsReadOnly)
				return;

			edit.EditValue = null;
		}
	}
}
