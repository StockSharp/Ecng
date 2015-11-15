//
//   Project:           WriteableBitmapEx - Silverlight WriteableBitmap extensions
//   Description:       Collection of interchange extension methods for the Silverlight WriteableBitmap class.
//
//   Changed by:        $Author: unknown $
//   Changed on:        $Date: 2011-10-27 21:32:30 +0100 (Thu, 27 Oct 2011) $
//   Changed in:        $Revision: 82056 $
//   Project:           $URL: https://writeablebitmapex.svn.codeplex.com/svn/trunk/Source/WriteableBitmapEx/WriteableBitmapConvertExtensions.cs $
//   Id:                $Id: WriteableBitmapConvertExtensions.cs 82056 2011-10-27 20:32:30Z unknown $
//
//
//   Copyright � 2009-2011 Rene Schulte and WriteableBitmapEx Contributors
//
//   This Software is weak copyleft open source. Please read the License.txt for details.
//
namespace System.Windows.Media.Imaging
{
    /// <summary>
    /// Collection of interchange extension methods for the Silverlight WriteableBitmap class.
    /// </summary>
    internal
#if !SAFECODE
#if !SILVERLIGHT 
        unsafe 
#endif
#endif
 static partial class WriteableBitmapExtensions
    {
        
        
        /// <summary>
        /// Copies the Pixels from the WriteableBitmap into a ARGB byte array starting at a specific Pixels index.
        /// </summary>
        /// <param name="bmp">The WriteableBitmap.</param>
        /// <param name="offset">The starting Pixels index.</param>
        /// <param name="count">The number of Pixels to copy, -1 for all</param>
        /// <returns>The color buffer as byte ARGB values.</returns>
        internal static byte[] ToByteArray(this WriteableBitmap bmp, int offset, int count)
        {
            using (var context = bmp.GetBitmapContext())
            {
                if (count == -1)
                {
                    // Copy all to byte array
                    count = context.Length;
                }

                int len = count * SizeOfArgb;
                byte[] result = new byte[len]; // ARGB
                BitmapContext.BlockCopy(context, offset, result, 0, len);
                return result;
            }
        }

        /// <summary>
        /// Copies the Pixels from the WriteableBitmap into a ARGB byte array.
        /// </summary>
        /// <param name="bmp">The WriteableBitmap.</param>
        /// <param name="count">The number of pixels to copy.</param>
        /// <returns>The color buffer as byte ARGB values.</returns>
        internal static byte[] ToByteArray(this WriteableBitmap bmp, int count)
        {
            return bmp.ToByteArray(0, count);
        }

        /// <summary>
        /// Copies all the Pixels from the WriteableBitmap into a ARGB byte array.
        /// </summary>
        /// <param name="bmp">The WriteableBitmap.</param>
        /// <returns>The color buffer as byte ARGB values.</returns>
        internal static byte[] ToByteArray(this WriteableBitmap bmp)
        {
            return bmp.ToByteArray(0, -1);
        }

        /// <summary>
        /// Copies color information from an ARGB byte array into this WriteableBitmap starting at a specific buffer index.
        /// </summary>
        /// <param name="bmp">The WriteableBitmap.</param>
        /// <param name="offset">The starting index in the buffer.</param>
        /// <param name="count">The number of bytes to copy from the buffer.</param>
        /// <param name="buffer">The color buffer as byte ARGB values.</param>
        /// <returns>The WriteableBitmap that was passed as parameter.</returns>
        internal static WriteableBitmap FromByteArray(this WriteableBitmap bmp, byte[] buffer, int offset, int count)
        {
            using (var context = bmp.GetBitmapContext())
            {
                BitmapContext.BlockCopy(buffer, offset, context, 0, count);
                return bmp;
            }
        }

        /// <summary>
        /// Copies color information from an ARGB byte array into this WriteableBitmap.
        /// </summary>
        /// <param name="bmp">The WriteableBitmap.</param>
        /// <param name="count">The number of bytes to copy from the buffer.</param>
        /// <param name="buffer">The color buffer as byte ARGB values.</param>
        /// <returns>The WriteableBitmap that was passed as parameter.</returns>
        internal static WriteableBitmap FromByteArray(this WriteableBitmap bmp, byte[] buffer, int count)
        {
            return bmp.FromByteArray(buffer, 0, count);
        }

        /// <summary>
        /// Copies all the color information from an ARGB byte array into this WriteableBitmap.
        /// </summary>
        /// <param name="bmp">The WriteableBitmap.</param>
        /// <param name="buffer">The color buffer as byte ARGB values.</param>
        /// <returns>The WriteableBitmap that was passed as parameter.</returns>
        internal static WriteableBitmap FromByteArray(this WriteableBitmap bmp, byte[] buffer)
        {
            return bmp.FromByteArray(buffer, 0, buffer.Length);
        }


        
        /// <summary>
        /// Writes the WriteableBitmap as a TGA image to a stream. 
        /// Used with permission from Nokola: http://nokola.com/blog/post/2010/01/21/Quick-and-Dirty-Output-of-WriteableBitmap-as-TGA-Image.aspx
        /// </summary>
        /// <param name="bmp">The WriteableBitmap.</param>
        /// <param name="destination">The destination stream.</param>
        internal static void WriteTga(this WriteableBitmap bmp, System.IO.Stream destination)
        {
            int width = bmp.PixelWidth;
            int height = bmp.PixelHeight;
            using (var context = bmp.GetBitmapContext())
            {
                var pixels = context.Pixels;
                byte[] data = new byte[context.Length * SizeOfArgb];

                // Copy bitmap data as BGRA
                int offsetSource = 0;
                int width4 = width << 2;
                int width8 = width << 3;
                int offsetDest = (height - 1) * width4;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int color = pixels[offsetSource];
                        data[offsetDest] = (byte)(color & 255); // B
                        data[offsetDest + 1] = (byte)((color >> 8) & 255); // G
                        data[offsetDest + 2] = (byte)((color >> 16) & 255); // R
                        data[offsetDest + 3] = (byte)(color >> 24); // A

                        offsetSource++;
                        offsetDest += SizeOfArgb;
                    }
                    offsetDest -= width8;
                }

                // Create header
                byte[] header = new byte[]
                                 {
                                     0, // ID length
                                     0, // no color map
                                     2, // uncompressed, true color
                                     0, 0, 0, 0,
                                     0,
                                     0, 0, 0, 0, // x and y origin
                                     (byte) (width & 0x00FF),
                                     (byte) ((width & 0xFF00) >> 8),
                                     (byte) (height & 0x00FF),
                                     (byte) ((height & 0xFF00) >> 8),
                                     32, // 32 bit bitmap
                                     0
                                 };

                // Write header and data
                using (var writer = new System.IO.BinaryWriter(destination))
                {
                    writer.Write(header);
                    writer.Write(data);
                }
            }
        }



        
        /// <summary>
        /// Loads an image from the applications resource file and fills this WriteableBitmap with it.
        /// </summary>
        /// <param name="bmp">The WriteableBitmap.</param>
        /// <param name="relativePath">Only the relative path to the resource file. The assembly name is retrieved automatically.</param>
        /// <returns>The WriteableBitmap that was passed as parameter.</returns>
        internal static WriteableBitmap FromResource(this WriteableBitmap bmp, string relativePath)
        {
            var fullName = Reflection.Assembly.GetCallingAssembly().FullName;
            var asmName = new Reflection.AssemblyName(fullName).Name;
            return bmp.FromContent(asmName + ";component/" + relativePath);
        }

        /// <summary>
        /// Loads an image from the applications content and fills this WriteableBitmap with it.
        /// </summary>
        /// <param name="bmp">The WriteableBitmap.</param>
        /// <param name="relativePath">Only the relative path to the content file.</param>
        /// <returns>The WriteableBitmap that was passed as parameter.</returns>
        internal static WriteableBitmap FromContent(this WriteableBitmap bmp, string relativePath)
        {
            using (var bmpStream = Application.GetResourceStream(new Uri(relativePath, UriKind.Relative)).Stream)
            {
                var bmpi = new BitmapImage();
#if SILVERLIGHT
            bmpi.SetSource(bmpStream);
#else
                bmpi.StreamSource = bmpStream;
#endif
                bmp = new WriteableBitmap(bmpi);
                bmpi.UriSource = null;
                return bmp;
            }
        }


    }
}