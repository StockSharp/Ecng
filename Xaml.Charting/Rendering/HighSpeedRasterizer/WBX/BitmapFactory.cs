//
//   Project:           WriteableBitmapEx - Silverlight WriteableBitmap extensions
//   Description:       Collection of draw extension methods for the Silverlight WriteableBitmap class.
//
//   Changed by:        $Author: unknown $
//   Changed on:        $Date: 2011-10-27 21:32:30 +0100 (Thu, 27 Oct 2011) $
//   Changed in:        $Revision: 82056 $
//   Project:           $URL: https://writeablebitmapex.svn.codeplex.com/svn/trunk/Source/WriteableBitmapEx/WriteableBitmapBaseExtensions.cs $
//   Id:                $Id: WriteableBitmapBaseExtensions.cs 82056 2011-10-27 20:32:30Z unknown $
//
//
//   Copyright © 2009-2011 Rene Schulte and WriteableBitmapEx Contributors
//
//   This Software is weak copyleft open source. Please read the License.txt for details.
//

namespace System.Windows.Media.Imaging
{
    /// <summary>
    /// Cross-platform factory for WriteableBitmaps
    /// </summary>
    internal static class BitmapFactory
    {
        /// <summary>
        /// Creates a new WriteableBitmap of the specified width and height
        /// </summary>
        /// <remarks>For WPF the default DPI is 96x96 and PixelFormat is BGRA32</remarks>
        /// <param name="pixelWidth"></param>
        /// <param name="pixelHeight"></param>
        /// <returns></returns>
        internal static WriteableBitmap New(int pixelWidth, int pixelHeight)
        {
            if (pixelHeight < 1) pixelHeight = 1;
            if (pixelWidth < 1) pixelWidth = 1;

#if SILVERLIGHT
            return new WriteableBitmap(pixelWidth, pixelHeight);
#else
            return new WriteableBitmap(pixelWidth, pixelHeight, 96.0, 96.0, PixelFormats.Pbgra32, null);
#endif
        }        
    }
}
