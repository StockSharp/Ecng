namespace Ecng.Xaml
{
	using System.Windows.Controls;
	using System.Windows;
	using System.Windows.Documents;
	using System.Windows.Input;

	public class EditableTextBlock : TextBlock
	{
		public static readonly DependencyProperty IsInEditModeProperty = DependencyProperty.Register(nameof(IsInEditMode), typeof(bool), typeof(EditableTextBlock), new UIPropertyMetadata(false, IsInEditModeUpdate));
		public static readonly DependencyProperty MaxLengthProperty	= DependencyProperty.Register(nameof(MaxLength), typeof(int), typeof(EditableTextBlock), new UIPropertyMetadata(0));

		public bool IsInEditMode
		{
			get => (bool) GetValue(IsInEditModeProperty);
			set => SetValue(IsInEditModeProperty, value);
		}

		public int MaxLength
		{
			get => (int) GetValue(MaxLengthProperty);
			set => SetValue(MaxLengthProperty, value);
		}

		private EditableTextBlockAdorner _adorner;

		private static void IsInEditModeUpdate(DependencyObject obj, DependencyPropertyChangedEventArgs e)
		{
			if (!(obj is EditableTextBlock textBlock))
				return;

			var layer = AdornerLayer.GetAdornerLayer(textBlock);

			if (textBlock.IsInEditMode)
			{
				if (null == textBlock._adorner)
				{
					textBlock._adorner = new EditableTextBlockAdorner(textBlock);

					textBlock._adorner.TextBoxKeyUp += textBlock.TextBoxKeyUp;
					textBlock._adorner.TextBoxLostFocus += textBlock.TextBoxLostFocus;
				}

				// ReSharper disable once PossibleNullReferenceException
				layer.Add(textBlock._adorner);
			}
			else
			{
				// ReSharper disable once PossibleNullReferenceException
				var adorners = layer.GetAdorners(textBlock);

				if (adorners != null)
				{
					foreach (var adorner in adorners)
					{
						if (adorner is EditableTextBlockAdorner)
							layer.Remove(adorner);
					}
				}

				textBlock.GetBindingExpression(TextProperty)?.UpdateTarget();
			}
		}

		private void TextBoxLostFocus(object sender, RoutedEventArgs e) => IsInEditMode = false;

		private void TextBoxKeyUp(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
				IsInEditMode = false;
		}

		protected override void OnMouseDown(MouseButtonEventArgs e)
		{
			if (e.MiddleButton == MouseButtonState.Pressed)
				IsInEditMode = true;
			else if (e.ClickCount == 2)
				IsInEditMode = true;
		}
	}
}
