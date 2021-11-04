// "Therefore those skilled at the unorthodox
// are infinite as heaven and earth,
// inexhaustible as the great rivers.
// When they come to an end,
// they begin again,
// like the days and months;
// they die and are reborn,
// like the four seasons."
// 
// - Sun Tsu,
// "The Art of War"

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TheArtOfDev.HtmlRenderer.Adapters;

namespace TheArtOfDev.HtmlRenderer.WPF.Adapters
{
    /// <summary>
    /// Adapter for WPF Image object for core.
    /// </summary>
    internal sealed class ImageAdapter : RImage
    {
        /// <summary>
        /// the underline WPF image.
        /// </summary>
        private readonly ImageSource _image;

        /// <summary>
        /// Init.
        /// </summary>
        public ImageAdapter(ImageSource image, double width, double height)
        {
            _image = image;
			_width = width;
			_height = height;
		}

        /// <summary>
        /// the underline WPF image.
        /// </summary>
        public ImageSource Image
        {
            get { return _image; }
        }

		private readonly double _width;

		public override double Width
        {
            get { return _width; }
        }

		private readonly double _height;

		public override double Height
        {
            get { return _height; }
        }

        public override void Dispose()
        {
			if (_image is BitmapImage bmp)
				bmp.StreamSource?.Dispose();
		}

		internal ImageSource Crop(Int32Rect int32Rect)
		{
			if (_image is BitmapImage bmp)
				return new CroppedBitmap(bmp, int32Rect);

			return _image;
		}
	}
}