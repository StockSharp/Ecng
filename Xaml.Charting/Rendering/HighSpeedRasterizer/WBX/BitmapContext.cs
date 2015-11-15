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

using System.Collections.Generic;

namespace System.Windows.Media.Imaging
{
    internal enum ReadWriteMode
    {
        /// <summary>
        /// On Dispose of a BitmapContext, do not Invalidate
        /// </summary>
        ReadOnly,

        /// <summary>
        /// On Dispose of a BitmapContext, invalidate the bitmap
        /// </summary>
        ReadWrite
    }

#if !SAFECODE
#if !SILVERLIGHT
    /// <summary>
    /// A disposable cross-platform wrapper around a WriteableBitmap, allowing a common API for Silverlight &quot; WPF with locking &quot; unlocking if necessary
    /// </summary>
    /// <remarks>Attempting to put as many preprocessor hacks in this file, to keep the rest of the codebase relatively clean</remarks>
    public unsafe struct BitmapContext : IDisposable
#else
    /// <summary>
    /// A disposable cross-platform wrapper around a WriteableBitmap, allowing a common API for Silverlight and WPF with locking and unlocking if necessary
    /// </summary>
    /// <remarks>Attempting to put as many preprocessor hacks in this file, to keep the rest of the codebase relatively clean</remarks>
    public struct BitmapContext : IDisposable
#endif
    {
        private readonly WriteableBitmap _writeableBitmap;
        private readonly int _pixelWidth;
        private readonly int _pixelHeight;

#if !SILVERLIGHT
        private static readonly IDictionary<WriteableBitmap, int> _updateCountByBmp = new Dictionary<WriteableBitmap, int>();
        private readonly ReadWriteMode _mode;
        private readonly int* _backBuffer;
        private readonly int _length;
#endif

        /// <summary>
        /// Creates an instance of a BitmapContext, with default mode = ReadWrite
        /// </summary>
        /// <param name="writeableBitmap"></param>
        internal BitmapContext(WriteableBitmap writeableBitmap)
        {
            _writeableBitmap = writeableBitmap;
            _pixelWidth = _writeableBitmap.PixelWidth;
            _pixelHeight = _writeableBitmap.PixelHeight;
#if !SILVERLIGHT

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
                    _writeableBitmap.Lock();
                }

                // Increment the update count
                _updateCountByBmp[_writeableBitmap]++;
            }

            _backBuffer = (int*)_writeableBitmap.BackBuffer;

            double pixelWidth = _writeableBitmap.BackBufferStride / WriteableBitmapExtensions.SizeOfArgb;
            double pixelHeight = _writeableBitmap.PixelHeight;
            _length = (int) (pixelWidth*pixelHeight);
#endif
        }

        /// <summary>
        /// Creates an instance of a BitmapContext, with specified ReadWriteMode
        /// </summary>
        /// <param name="writeableBitmap"></param>
        /// <param name="mode"></param>
        internal BitmapContext(WriteableBitmap writeableBitmap, ReadWriteMode mode)
            : this(writeableBitmap)
        {
#if !SILVERLIGHT
            // We only care about mode in Wpf
            _mode = mode;
#endif
        }

        internal WriteableBitmap WriteableBitmap { get { return _writeableBitmap; } }

        internal int PixelWidth
        {
            get { return _pixelWidth; }
        }

        internal int PixelHeight
        {
            get { return _pixelHeight; }
        }

#if SILVERLIGHT

        /// <summary>
        /// Gets the Pixels array 
        /// </summary>        
        internal int[] Pixels { get { return _writeableBitmap.Pixels; } }

        /// <summary>
        /// Gets the length of the Pixels array 
        /// </summary>
        internal int Length { get { return _writeableBitmap.Pixels.Length; } }

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
        /// Clears the BitmapContext, filling the underlying bitmap with zeros
        /// </summary>
        internal void Clear()
        {
            var pixels = _writeableBitmap.Pixels;
            Array.Clear(pixels, 0, pixels.Length);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <remarks></remarks>
        public void Dispose()
        {
            // For silverlight, do nothing except redraw
            _writeableBitmap.Invalidate();
        }

#else

        internal unsafe int* Pixels
        {
            //[System.Runtime.TargetedPatchingOptOut("Candidate for inlining across NGen boundaries for performance reasons")]
            get { return _backBuffer; }
        }


        internal int Length
        {
            //[System.Runtime.TargetedPatchingOptOut("Candidate for inlining across NGen boundaries for performance reasons")]
            get
            {
                return _length;
            }
        }

        /// <summary>
        /// Performs a Copy operation from source Bto destination BitmapContext
        /// </summary>
        /// <remarks>Equivalent to calling Buffer.BlockCopy in Silverlight, or native memcpy in WPF</remarks>
        //[System.Runtime.TargetedPatchingOptOut("Candidate for inlining across NGen boundaries for performance reasons")]
        internal static unsafe void BlockCopy(BitmapContext src, int srcOffset, BitmapContext dest, int destOffset, int count)
        {
            NativeMethods.CopyUnmanagedMemory((byte*)src.Pixels, srcOffset, (byte*)dest.Pixels, destOffset, count);
        }

        /// <summary>
        /// Performs a Copy operation from source Array to destination BitmapContext
        /// </summary>
        /// <remarks>Equivalent to calling Buffer.BlockCopy in Silverlight, or native memcpy in WPF</remarks>
        //[System.Runtime.TargetedPatchingOptOut("Candidate for inlining across NGen boundaries for performance reasons")]
        internal static unsafe void BlockCopy(int[] src, int srcOffset, BitmapContext dest, int destOffset, int count)
        {
            fixed (int* srcPtr = src)
            {
                NativeMethods.CopyUnmanagedMemory((byte*)srcPtr, srcOffset, (byte*)dest.Pixels, destOffset, count);
            }
        }

        /// <summary>
        /// Performs a Copy operation from source Array to destination BitmapContext
        /// </summary>
        /// <remarks>Equivalent to calling Buffer.BlockCopy in Silverlight, or native memcpy in WPF</remarks>
        //[System.Runtime.TargetedPatchingOptOut("Candidate for inlining across NGen boundaries for performance reasons")]
        internal static unsafe void BlockCopy(byte[] src, int srcOffset, BitmapContext dest, int destOffset, int count)
        {
            fixed (byte* srcPtr = src)
            {
                NativeMethods.CopyUnmanagedMemory(srcPtr, srcOffset, (byte*)dest.Pixels, destOffset, count);
            }
        }

        /// <summary>
        /// Performs a Copy operation from source BitmapContext to destination Array
        /// </summary>
        /// <remarks>Equivalent to calling Buffer.BlockCopy in Silverlight, or native memcpy in WPF</remarks>
        //[System.Runtime.TargetedPatchingOptOut("Candidate for inlining across NGen boundaries for performance reasons")]
        internal static unsafe void BlockCopy(BitmapContext src, int srcOffset, byte[] dest, int destOffset, int count)
        {
            fixed (byte* destPtr = dest)
            {
                NativeMethods.CopyUnmanagedMemory((byte*)src.Pixels, srcOffset, destPtr, destOffset, count);
            }
        }

        /// <summary>
        /// Performs a Copy operation from source BitmapContext to destination Array
        /// </summary>
        /// <remarks>Equivalent to calling Buffer.BlockCopy in Silverlight, or native memcpy in WPF</remarks>
        //[System.Runtime.TargetedPatchingOptOut("Candidate for inlining across NGen boundaries for performance reasons")]
        internal static unsafe void BlockCopy(BitmapContext src, int srcOffset, int[] dest, int destOffset, int count)
        {
            fixed (int* destPtr = dest)
            {
                NativeMethods.CopyUnmanagedMemory((byte*)src.Pixels, srcOffset, (byte*)destPtr, destOffset, count);
            }
        }

        /// <summary>
        /// Clears the BitmapContext, filling the underlying bitmap with zeros
        /// </summary>
        //[System.Runtime.TargetedPatchingOptOut("Candidate for inlining across NGen boundaries for performance reasons")]
        internal void Clear()
        {
            NativeMethods.SetUnmanagedMemory(_writeableBitmap.BackBuffer, 0, _writeableBitmap.BackBufferStride * _writeableBitmap.PixelHeight);
        }

        //[System.Runtime.TargetedPatchingOptOut("Candidate for inlining across NGen boundaries for performance reasons")]
        private static int Dec(WriteableBitmap target)
        {
//            int current = _updateCountByBmp[target];
//            current--;
//            _updateCountByBmp[target] = current;
//            return current;

            int current;
            if (!_updateCountByBmp.TryGetValue(target, out current)) return -1;

            current--;
            _updateCountByBmp[target] = current;
            return current;
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
                    // Remove this bitmap from the update map 
                    _updateCountByBmp.Remove(_writeableBitmap);

                    // Invalidate the bitmap if ReadWrite mode
                    if (_mode == ReadWriteMode.ReadWrite)
                        _writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, _writeableBitmap.PixelWidth,
                                                                    _writeableBitmap.PixelHeight));

                    // Unlock the bitmap
                    _writeableBitmap.Unlock();
                }
            }
        }
#endif
    }
#endif
}