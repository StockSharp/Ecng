namespace Ecng.Xaml
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Windows;
	using System.Windows.Controls;

	using MoreLinq;

	using Xceed.Wpf.Toolkit.PropertyGrid.Editors;

	public class EncodingComboBox : ComboBox
	{
		private static readonly Dictionary<int, EncodingInfo> _encodingInfos;

		static EncodingComboBox()
		{
			_encodingInfos = Encoding.GetEncodings().ToDictionary(e => e.CodePage, e => e);
		}

		public EncodingComboBox()
		{
			var priorityCopePages = new[]
			{
				Encoding.ASCII, Encoding.UTF7, Encoding.GetEncoding(1251), Encoding.UTF8, Encoding.Unicode
			}.Select(e => e.CodePage).ToHashSet();

			DisplayMemberPath = "DisplayName";
			ItemsSource = _encodingInfos.Values
				.Select(e => Tuple.Create(e, priorityCopePages.Contains(e.CodePage) ? 0 : 1))
				.OrderBy(t => t.Item2)
				.Select(t => t.Item1)
				.ToArray();

			SelectedEncoding = Encoding.UTF8;
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> ��� <see cref="SelectedEncoding"/>.
		/// </summary>
		public static readonly DependencyProperty SelectedEncodingProperty =
			DependencyProperty.Register("SelectedEncoding", typeof(Encoding), typeof(EncodingComboBox), new UIPropertyMetadata(SelectedEncodingChanged));

		private static void SelectedEncodingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var ctrl = d as EncodingComboBox;
			if (ctrl == null)
				return;

			ctrl.SelectedValue = e.NewValue == null ? null : _encodingInfos[((Encoding)e.NewValue).CodePage];
		}

		public Encoding SelectedEncoding
		{
			get { return (Encoding)GetValue(SelectedEncodingProperty); }
			set { SetValue(SelectedEncodingProperty, value); }
		}

		/// <summary>
		/// Responds to a <see cref="T:System.Windows.Controls.ComboBox"/> selection change by raising a <see cref="E:System.Windows.Controls.Primitives.Selector.SelectionChanged"/> event. 
		/// </summary>
		/// <param name="e">Provides data for <see cref="T:System.Windows.Controls.SelectionChangedEventArgs"/>. </param>
		protected override void OnSelectionChanged(SelectionChangedEventArgs e)
		{
			base.OnSelectionChanged(e);

			var item = e.AddedItems.Cast<EncodingInfo>().FirstOrDefault();
			SelectedEncoding = item == null ? null : Encoding.GetEncoding(item.CodePage);
		}
	}

	/// <summary>
	/// <see cref="ITypeEditor"/> ��� <see cref="EncodingComboBox"/>.
	/// </summary>
	public class Level1FieldsComboBoxEditor : TypeEditor<EncodingComboBox>
	{
		/// <summary>
		/// ���������� <see cref="TypeEditor{T}.ValueProperty"/> ��������� <see cref="EncodingComboBox.SelectedEncodingProperty"/>.
		/// </summary>
		protected override void SetValueDependencyProperty()
		{
			ValueProperty = EncodingComboBox.SelectedEncodingProperty;
		}
	}
}