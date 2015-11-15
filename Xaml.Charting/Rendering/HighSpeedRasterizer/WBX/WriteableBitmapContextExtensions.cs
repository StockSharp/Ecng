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
using System.Windows.Media.Imaging;

namespace System.Windows.Media.Imaging
{
    internal static partial class WriteableBitmapContextExtensions
    {
        /// <summary>
        /// Gets a BitmapContext within which to perform nested IO operations on the bitmap
        /// </summary>
        /// <remarks>For WPF the BitmapContext will lock the bitmap. Call Dispose on the context to unlock</remarks>
        /// <param name="bmp"></param>
        /// <returns></returns>
        internal static BitmapContext GetBitmapContext(this WriteableBitmap bmp)
        {
            return new BitmapContext(bmp);
        }

        /// <summary>
        /// Gets a BitmapContext within which to perform nested IO operations on the bitmap
        /// </summary>
        /// <remarks>For WPF the BitmapContext will lock the bitmap. Call Dispose on the context to unlock</remarks>
        /// <param name="bmp">The writeable bitmap to get a context for</param>
        /// <param name="mode">The ReadWriteMode. If set to ReadOnly, the bitmap will not be invalidated on dispose of the context, else it will</param>
        /// <returns></returns>
        internal static BitmapContext GetBitmapContext(this WriteableBitmap bmp, ReadWriteMode mode)
        {
            return new BitmapContext(bmp, mode);
        }      
    }
}
