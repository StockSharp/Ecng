namespace Ecng.Xaml.DevExp
{
	using System;
	using System.Windows;

	using DevExpress.Xpf.Editors;

	using Ecng.ComponentModel;
	using Ecng.Localization;

	/// <summary>
	/// <see cref="Range{T}"/> editor.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public sealed class RangeEditor<T> : ButtonEdit
		where T : IComparable<T>
	{
		/// <summary>
		/// Create <see cref="RangeEditor{T}"/>.
		/// </summary>
		public RangeEditor()
		{
			AllowDefaultButton = false;

			var btnEdit = new ButtonInfo
			{
				GlyphKind = GlyphKind.Edit,
				Content = "Create".Translate()
			};
			btnEdit.Click += BtnEdit_OnClick;
			Buttons.Add(btnEdit);

			var btnReset = new ButtonInfo
			{
				GlyphKind = GlyphKind.Cancel,
				Content = "Cancel".Translate()
			};
			btnReset.Click += BtnReset_OnClick;
			Buttons.Add(btnReset);
		}

		private void BtnEdit_OnClick(object sender, RoutedEventArgs e)
		{
			EditValue = new Range<T>(default(T), default(T));
		}

		private void BtnReset_OnClick(object sender, RoutedEventArgs e)
		{
			EditValue = null;
		}
	}
}