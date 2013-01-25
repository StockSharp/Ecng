using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;

namespace Ecng.Xaml.Fonts
{
	using Ecng.Common;

	internal class ColorPickerViewModel : INotifyPropertyChanged
	{
		private readonly ReadOnlyCollection<FontColor> _roFontColors;
		private FontColor _selectedFontColor;

		public ColorPickerViewModel()
		{
			_selectedFontColor = AvailableColors.GetFontColor(Colors.Black);
			_roFontColors = new ReadOnlyCollection<FontColor>(new AvailableColors());
		}

		public ReadOnlyCollection<FontColor> FontColors
		{
			get { return _roFontColors; }
		}

		public FontColor SelectedFontColor
		{
			get { return _selectedFontColor; }

			set
			{
				if (_selectedFontColor == value)
					return;

				_selectedFontColor = value;
				PropertyChanged.SafeInvoke(this, "SelectedFontColor");
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
	}
}