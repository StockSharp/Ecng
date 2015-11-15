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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Text;

namespace System.Windows.Media.Imaging
{
#if SAFECODE    
    /// <summary>
    /// A disposable cross-platform wrapper around a WriteableBitmap, allowing a common API for Silverlight
    /// and WPF with locking and unlocking if necessary.
    /// </summary>
    /// <remarks>
    /// Attempting to put as many preprocessor hacks in this file, to keep the rest of the codebase relatively
    /// clean.
    /// </remarks>
    public struct BitmapContext : IDisposable
    {
        private readonly WriteableBitmap _writeableBitmap;
        private readonly int[] _buffer;
        private static readonly IDictionary<WriteableBitmap, int> _updateCountByBmp = new Dictionary<WriteableBitmap, int>();
        private static readonly IDictionary<WriteableBitmap, int[]> _backBuffers = new Dictionary<WriteableBitmap, int[]>();
        private readonly ReadWriteMode _mode;

        /// <summary>
        /// Initializes a new instance of the <see cref="BitmapContext"/> struct.
        /// </summary>
        /// <param name="writeableBitmap">Bitmap to wrap.</param>
        internal BitmapContext(WriteableBitmap writeableBitmap)
        {
            _writeableBitmap = writeableBitmap;

            // Mode is used to invalidate the bmp at the end of the update if mode==ReadWrite
            _mode = ReadWriteMode.ReadWrite;

            lock (_updateCountByBmp)
            {
                // Ensure the bitmap is in the dictionary of mapped Instances
                if (!_updateCountByBmp.ContainsKey(_writeableBitmap))
                {
                    // Set UpdateCount to 1 for this bitmap 
                    _updateCountByBmp.Add(_writeableBitmap, 0);

                    // Lock the bitmap                    
                    int stride = _writeableBitmap.PixelWidth * WriteableBitmapExtensions.SizeOfArgb;
                    double pixelWidth = _writeableBitmap.PixelWidth;
                    double pixelHeight = _writeableBitmap.PixelHeight;
                    var buf = new int[(int)(pixelWidth * pixelHeight)];
                    _writeableBitmap.CopyPixels(buf, stride, 0);                    
                    _backBuffers[_writeableBitmap] = buf;                    
                    _writeableBitmap.Lock();
                }

                // Increment the update count
                _updateCountByBmp[_writeableBitmap]++;
                _buffer = _backBuffers[_writeableBitmap];
            }            
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BitmapContext"/> struct.
        /// </summary>
        /// <param name="writeableBitmap">Bitmap to wrap.</param>
        /// <param name="mode">Access mode for the bitmap.</param>
        internal BitmapContext(WriteableBitmap writeableBitmap, ReadWriteMode mode) : this(writeableBitmap)
        {
        }

        internal WriteableBitmap WriteableBitmap
        {
            get
            {
                return _writeableBitmap;
            }
        }

        /// <summary>
        /// Gets the Pixels array.
        /// </summary>
        internal int[] Pixels
        {
            get
            {
                return _buffer;
            }
        }

        /// <summary>
        /// Gets the length of the Pixels array.
        /// </summary>
        internal int Length
        {
            get
            {
                double pixelWidth = _writeableBitmap.BackBufferStride / WriteableBitmapExtensions.SizeOfArgb;
                double pixelHeight = _writeableBitmap.PixelHeight;
                return (int)(pixelWidth * pixelHeight);
            }
        }

        /// <summary>
        /// Performs a Copy operation from source BitmapContext to destination BitmapContext
        /// </summary>
        /// <remarks>Equivalent to calling Buffer.BlockCopy in Silverlight, or native memcpy in WPF</remarks>
        internal static void BlockCopy(BitmapContext src, int srcOffset, BitmapContext dest, int destOffset, int count)
        {
            Buffer.BlockCopy(src.Pixels, srcOffset, dest.Pixels, destOffset, count);
        }

        /// <summary>
        /// Performs a Copy operation from source Array to destination BitmapContext
        /// </summary>
        /// <remarks>Equivalent to calling Buffer.BlockCopy in Silverlight, or native memcpy in WPF</remarks>
        internal static void BlockCopy(Array src, int srcOffset, BitmapContext dest, int destOffset, int count)
        {
            Buffer.BlockCopy(src, srcOffset, dest.Pixels, destOffset, count);
        }

        /// <summary>
        /// Performs a Copy operation from source BitmapContext to destination Array
        /// </summary>
        /// <remarks>Equivalent to calling Buffer.BlockCopy in Silverlight, or native memcpy in WPF</remarks>
        internal static void BlockCopy(BitmapContext src, int srcOffset, Array dest, int destOffset, int count)
        {
            Buffer.BlockCopy(src.Pixels, srcOffset, dest, destOffset, count);
        }

        /// <summary>
        /// Clears the BitmapContext, filling the underlying bitmap with zeros.
        /// </summary>
        internal void Clear()
        {
            for (int index = 0; index < Length; index++)
                Pixels[index] = 0;
        }

        /// <summary>
        /// Disposes the BitmapContext, unlocking it and invalidating if WPF
        /// </summary>
        public void Dispose()
        {
            // Decrement the update count. If it hits zero
            lock (_updateCountByBmp)
            {
                if (Dec(_writeableBitmap) == 0)
                {
                    // Remove this bitmap from the update map and backbuffer map
                    _updateCountByBmp.Remove(_writeableBitmap);
                    _backBuffers.Remove(_writeableBitmap);

                    // Invalidate the bitmap if ReadWrite mode
                    if (_mode == ReadWriteMode.ReadWrite)
                        _writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, _writeableBitmap.PixelWidth,
                                                                    _writeableBitmap.PixelHeight));

                    // Unlock the bitmap
                    Int32Rect rect = new Int32Rect(0, 0, _writeableBitmap.PixelWidth, _writeableBitmap.PixelHeight);
                    _writeableBitmap.WritePixels(rect, _buffer, _writeableBitmap.BackBufferStride, 0);
                    _writeableBitmap.Unlock();
                }
            }
        }

        private static int Dec(WriteableBitmap target)
        {
            int current = _updateCountByBmp[target];
            current--;
            _updateCountByBmp[target] = current;
            return current;
        }
    }
#endif
}
