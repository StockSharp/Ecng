namespace Ecng.Xaml
{
	using System;
	using System.Windows;
	using System.Windows.Documents;
	using System.Windows.Media;
	using System.Linq;

	using Ecng.Common;

	/// <summary>
	/// A specialized highlighting text block control.
	/// http://www.jeff.wilcox.name/2008/11/highlighting-autocompletebox/
	/// </summary>
	public partial class HighlightingTextBlock
	{
		private bool _initialized;

		/// <summary>
		/// Initializes a new HighlightingTextBlock class.
		/// </summary>
		public HighlightingTextBlock()
		{
			DefaultStyleKey = typeof(HighlightingTextBlock);
		}

		/// <summary>
		/// Gets or sets the highlighted text.
		/// </summary>
		public string HighlightText
		{
			get => GetValue(HighlightTextProperty) as string;
			set => SetValue(HighlightTextProperty, value);
		}

		/// <summary>
		/// Identifies the HighlightText dependency property.
		/// </summary>
		public static readonly DependencyProperty HighlightTextProperty =
			DependencyProperty.Register(
				"HighlightText",
				typeof(string),
				typeof(HighlightingTextBlock),
				new PropertyMetadata(OnHighlightTextPropertyChanged));

		/// <summary>
		/// HighlightText property changed handler.
		/// </summary>
		/// <param name="d">AutoCompleteBox that changed its HighlightText.</param>
		/// <param name="e">Event arguments.</param>
		private static void OnHighlightTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var source = (HighlightingTextBlock)d;
			source.ApplyHighlighting();
		}

		/// <summary>
		/// Gets or sets the highlight brush.
		/// </summary>
		public Brush HighlightBrush
		{
			get => GetValue(HighlightBrushProperty) as Brush;
			set => SetValue(HighlightBrushProperty, value);
		}

		/// <summary>
		/// Identifies the HighlightBrush dependency property.
		/// </summary>
		public static readonly DependencyProperty HighlightBrushProperty =
			DependencyProperty.Register(
				"HighlightBrush",
				typeof(Brush),
				typeof(HighlightingTextBlock),
				new PropertyMetadata(null, OnHighlightBrushPropertyChanged));

		/// <summary>
		/// HighlightBrushProperty property changed handler.
		/// </summary>
		/// <param name="d">HighlightingTextBlock that changed its HighlightBrush.</param>
		/// <param name="e">Event arguments.</param>
		private static void OnHighlightBrushPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var source = (HighlightingTextBlock)d;
			source.ApplyHighlighting();
		}
		
		/// <summary>
		/// Gets or sets the font weight used on highlighted text.
		/// </summary>
		public FontWeight HighlightFontWeight
		{
			get => (FontWeight)GetValue(HighlightFontWeightProperty);
			set => SetValue(HighlightFontWeightProperty, value);
		}

		/// <summary>
		/// Identifies the HighlightFontWeight dependency property.
		/// </summary>
		public static readonly DependencyProperty HighlightFontWeightProperty =
			DependencyProperty.Register(
				"HighlightFontWeight",
				typeof(FontWeight),
				typeof(HighlightingTextBlock),
				new PropertyMetadata(FontWeights.Normal, OnHighlightFontWeightPropertyChanged));

		/// <summary>
		/// HighlightFontWeightProperty property changed handler.
		/// </summary>
		/// <param name="d">HighlightingTextBlock that changed its HighlightFontWeight.</param>
		/// <param name="e">Event arguments.</param>
		private static void OnHighlightFontWeightPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			//HighlightingTextBlock source = d as HighlightingTextBlock;
			//FontWeight value = (FontWeight)e.NewValue;
		}

		/// <summary>
		/// Apply the visual highlighting.
		/// </summary>
		private void ApplyHighlighting()
		{
			if (!_initialized)
			{
				Inlines.Clear();

				foreach (var c in Text)
				{
					Inline run = new Run { Text = c.To<string>() };
					Inlines.Add(run);
				}

				_initialized = true;
			}

			var text = Text ?? string.Empty;
			var highlight = HighlightText ?? string.Empty;
			const StringComparison compare = StringComparison.OrdinalIgnoreCase;

			var cur = 0;
			while (cur < text.Length)
			{
				var i = highlight.Length == 0 ? -1 : text.IndexOf(highlight, cur, compare);
				i = i < 0 ? text.Length : i;

				// Clear
				while (cur < i && cur < text.Length)
				{
					var inline = Inlines.ElementAt(cur);
					inline.Foreground = Foreground;
					inline.FontWeight = FontWeight;
					cur++;
				}

				// Highlight
				var start = cur;
				while (cur < start + highlight.Length && cur < text.Length)
				{
					var inline = Inlines.ElementAt(cur);
					inline.Foreground = HighlightBrush;
					inline.FontWeight = HighlightFontWeight;
					cur++;
				}
			}
		}
	}
}