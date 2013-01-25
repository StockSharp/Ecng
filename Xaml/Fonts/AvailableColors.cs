using System.Collections.Generic;
using System.Reflection;
using System.Windows.Media;

namespace Ecng.Xaml.Fonts
{
	using System.Linq;

	class AvailableColors : List<FontColor>
    {
        #region Conversion Utils Static Methods

        public static FontColor GetFontColor(SolidColorBrush b)
        {
            var brushList = new AvailableColors();
            return brushList.GetFontColorByBrush(b);
        }

        public static FontColor GetFontColor(string name)
        {
            var brushList = new AvailableColors();
            return brushList.GetFontColorByName(name);
        }

        public static FontColor GetFontColor(Color c)
        {
            return AvailableColors.GetFontColor(new SolidColorBrush(c));
        }

        public static int GetFontColorIndex(FontColor c)
        {
            var brushList = new AvailableColors();
        	var colorBrush = c.Brush;

        	return brushList.TakeWhile(brush => !brush.Brush.Color.Equals(colorBrush.Color)).Count();
        }

        #endregion

        public AvailableColors()
        {
            Init();
        }

        public FontColor GetFontColorByName(string name)
        {
        	return this.FirstOrDefault(b => b.Name == name);
        }

		public FontColor GetFontColorByBrush(SolidColorBrush b)
		{
			return this.FirstOrDefault(brush => brush.Brush.Color.Equals(b.Color));
		}

		private void Init()
        {
            var brushesType = typeof(Colors);
            var properties = brushesType.GetProperties(BindingFlags.Static | BindingFlags.Public);

            foreach (var prop in properties)
            {
                var name = prop.Name;
                var brush = new SolidColorBrush((Color)(prop.GetValue(null, null)));
                Add(new FontColor(name, brush));
            }
        }
    }
}