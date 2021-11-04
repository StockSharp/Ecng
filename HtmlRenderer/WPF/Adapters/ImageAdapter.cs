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

using System.Windows.Media;
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
        public ImageAdapter(ImageSource image)
        {
            _image = image;
        }

        /// <summary>
        /// the underline WPF image.
        /// </summary>
        public ImageSource Image
        {
            get { return _image; }
        }

        public override double Width
        {
            get { return 30; }
        }

        public override double Height
        {
            get { return 30; }
        }

        public override void Dispose()
        {
            //if (_image.StreamSource != null)
            //    _image.StreamSource.Dispose();
        }
    }
}