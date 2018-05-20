namespace Ecng.Xaml.DevExp
{
	using System;
	using System.Windows;
	using System.Windows.Data;
	using System.Windows.Media;

	using DevExpress.Xpf.Core;

	/// <summary>
	/// Icon binding.
	/// </summary>
	public class ThemedIconBinding : MultiBinding
	{
		private readonly Binding _binding;

		/// <summary>
		/// Gets or sets the path to the binding source property.
		/// </summary>
		public PropertyPath Path
		{
			get => _binding.Path;
			set => _binding.Path = value;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ThemedIconBinding"/>.
		/// </summary>
		/// <param name="image">Drawing image.</param>
		public ThemedIconBinding(DrawingImage image)
		{
			if (image == null) 
				throw new ArgumentNullException(nameof(image));

			Init();
			Converter = new ThemedImageConverter(image);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ThemedIconBinding"/>.
		/// </summary>
		public ThemedIconBinding()
		{
			Init();
			Bindings.Add(_binding = new Binding());
			Converter = new ThemedImageConverter();
		}

		private void Init()
		{
			Bindings.Add(new Binding { RelativeSource = RelativeSource.Self });
			Bindings.Add(new Binding
			{
				Path = new PropertyPath(ThemeManager.TreeWalkerProperty),
				RelativeSource = RelativeSource.Self
			});
			Bindings.Add(new Binding
			{
				Path = new PropertyPath(WpfSvgPalette.PaletteProperty),
				RelativeSource = RelativeSource.Self
			});
		}
	}
}