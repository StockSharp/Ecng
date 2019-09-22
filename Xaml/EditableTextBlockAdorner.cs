namespace Ecng.Xaml
{
	using System;
	using System.Windows.Documents;
	using System.Windows;
	using System.Windows.Media;
	using System.Windows.Controls;
	using System.Windows.Data;
	using System.Windows.Input;

	using Ecng.Common;

	/// <summary>
	/// Adorner class which shows TextBox over the text block when the Edit mode is on.
	/// </summary>
	public class EditableTextBlockAdorner : Adorner
	{
		private readonly VisualCollection _collection;
		private readonly TextBox _textBox;
		private readonly TextBlock _textBlock;

		public EditableTextBlockAdorner(EditableTextBlock adornedElement) : base(adornedElement)
		{
			var binding = new Binding(nameof(_textBox.Text)) {Source = adornedElement};

			_collection = new VisualCollection(this);
			_textBlock = adornedElement;

			_textBox = new TextBox
			{
				AcceptsReturn = true, 
				MaxLength = adornedElement.MaxLength, 
				HorizontalAlignment = HorizontalAlignment.Stretch
			};

			_textBox.SetBinding(TextBox.TextProperty, binding);

			_textBox.KeyUp += TextBox_KeyUp;
			_textBox.Loaded += (sender, args) => _textBox.SelectAll();

			_collection.Add(_textBox);
		}

		private void TextBox_KeyUp(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Enter && e.Key != Key.Escape)
				return;

			_textBox.Text = _textBox.Text.Remove(Environment.NewLine);
			_textBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
		}

		protected override Visual GetVisualChild(int index)
		{
			return _collection[index];
		}

		protected override int VisualChildrenCount => _collection.Count;

		protected override Size ArrangeOverride(Size finalSize)
		{
			var rect = new Rect(-5, -3, _textBlock.ActualWidth + 60, _textBlock.ActualHeight * 1.5);
			_textBox.Arrange(rect);
			_textBox.Focus();
			return finalSize;
		}

		public event RoutedEventHandler TextBoxLostFocus
		{
			add => _textBox.LostFocus += value;
			remove => _textBox.LostFocus -= value;
		}

		public event KeyEventHandler TextBoxKeyUp
		{
			add => _textBox.KeyUp += value;
			remove => _textBox.KeyUp -= value;
		}
	}
}