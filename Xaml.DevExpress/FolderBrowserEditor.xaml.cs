namespace Ecng.Xaml.DevExp
{
	using System.Windows;

	using DevExpress.Xpf.Dialogs;
	using DevExpress.Xpf.Editors;

	using Ecng.Common;

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

			if (edit == null || edit.IsReadOnly)
				return;

			var dlg = new DXFolderBrowserDialog();
			var value = (string)edit.EditValue;

			if (!value.IsEmpty())
				dlg.SelectedPath = value;

			var owner = ((DependencyObject)sender)?.GetWindow();

			if (dlg.ShowModal(owner))
				edit.EditValue = dlg.SelectedPath;
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
