using System;
using System.IO;
using System.Windows.Browser;
using System.Windows.Media;
using Community.Imaging.Decoders.Bmp;

namespace Community.Imaging.Decoders
{
    public class BmpDecoder
    {
        private const int _BLUEMASK = 0x001F;
        private const int _GREENMASK = 0x07E0;
        private const int _REDMASK = 0xF800;

        public static ClientImage Decode(Stream stream)
        {
            ClientImage image = null;

            try
            {
                byte[] buffer;
                var fHeader = BmpFileHeader.FillFromStream(stream);

                if(fHeader.BitmapType != 19778)
                {
                    throw new Exception("Invalid BMP file");
                }

                var iHeader = BmpInfoHeader.FillFromStream(stream);
                if((iHeader.Compression == 0) && (iHeader.BitsPerPixel == 24))
                {
                    buffer = new byte[iHeader.ImageSize];
                    stream.Read(buffer, 0, iHeader.ImageSize);


                    image = Read24BitBmp(buffer, iHeader);
                }
                else if((iHeader.Compression == 0) && (iHeader.BitsPerPixel <= 8))
                {
                    var count = iHeader.ColorCount * 4;

                    buffer = new byte[count];
                    stream.Read(buffer, 0, count);

                    var palette = FillColorPalette(buffer, iHeader.ColorCount);

                    buffer = new byte[iHeader.ImageSize];
                    stream.Read(buffer, 0, iHeader.ImageSize);

                    image = ReadPaletteBmp(buffer, palette, iHeader, iHeader.BitsPerPixel);
                }
                else if((iHeader.Compression == 3) && (iHeader.BitsPerPixel == 16))
                {
                    var remainder = fHeader.OffsetToData - (int) stream.Position;

                    buffer = new byte[remainder];
                    stream.Read(buffer, 0, remainder);
                    
                    var rMask = BitConverter.ToInt32(buffer, 0);
                    var gMask = BitConverter.ToInt32(buffer, 4);
                    var bMask = BitConverter.ToInt32(buffer, 8);

                    if((_REDMASK != rMask) || (_GREENMASK != gMask) || (_BLUEMASK != bMask))
                    {
                        throw new Exception(string.Format("Unsupported 16 bit format: {0}, {1}, {2}", rMask.ToString("X2"), bMask.ToString("X2"), gMask.ToString("X2")));
                    }
                    
                    remainder = iHeader.Height * iHeader.Width * 2;
                    buffer = new byte[remainder];
                    stream.Read(buffer, 0, remainder);


                    image = Read565Bmp(buffer, iHeader);
                }
                else
                {
                    throw new Exception(string.Format("Unsupported format (compression: {0}, Bits per pixel: {1})", iHeader.Compression, iHeader.BitsPerPixel));
                }
            }
            catch (Exception ex)
            {
                HtmlPage.Window.Alert(string.Format("Error parsing BMP file: {0}", ex.Message));
            }

            return image;
        }

        private static Color[] FillColorPalette(byte[] buffer, int count)
        {
            var colors = new Color[count];

            for (var idx = 0; idx < count; idx++)
            {
                var baseIdx = idx * 4;
                var alpha = buffer[baseIdx + 3];
                colors[idx] = Color.FromArgb(((alpha == 0) ? (byte) 255 : alpha), buffer[baseIdx + 2],
                                             buffer[baseIdx + 1], buffer[baseIdx]);
            }

            return colors;
        }

        private static ClientImage Read565Bmp(byte[] buffer, BmpInfoHeader header)
        {
            const int scaleR = 256 / 32;
            const int scaleG = 256 / 64;

            var image = new ClientImage(header.Width, header.Height);

            for (var row = 0; row < header.Height; row++)
            {
                var rowbase = (row * header.Width * 2);
                for (var col = 0; col < header.Width; col++)
                {
                    var offset = rowbase + (col * 2);
                    var realRow = header.Height - row - 1;


                    var color = BitConverter.ToInt16(buffer, offset);
                    var red = (byte) (((color & _REDMASK) >> 11) * scaleR);
                    var green = (byte) (((color & _GREENMASK) >> 5) * scaleG);
                    var blue = (byte) (((color & _BLUEMASK)) * scaleR);


                    image.SetPixel(col, realRow, red, green, blue, 255);
                }
            }

            return image;
        }

        private static ClientImage ReadPaletteBmp(byte[] buffer, Color[] palette, BmpInfoHeader header, int bpp)
        {
            var ppb = 8 / bpp;
            var width = (header.Width + ppb - 1) / ppb;
            var alignment = width % 4;
            var mask = (0xFF >> (8 - bpp));
            Color color;

            var image = new ClientImage(header.Width, header.Height);

            if(alignment != 0)
            {
                alignment = 4 - alignment;
            }

            for (var row = 0; row < header.Height; row++)
            {
                var rowbase = (row * (width + alignment));
                for (var col = 0; col < width; col++)
                {
                    var offset = rowbase + col;
                    var colbase = col * ppb;
                    var realRow = header.Height - row - 1;
                    for (var shift = 0; ((shift < ppb) && ((colbase + shift) < header.Width)); shift++)
                    {
                        color = palette[((buffer[offset]) >> (8 - bpp - (shift * bpp))) & mask];
                        image.SetPixel(colbase + shift, realRow, color.R, color.G, color.B, 255);
                    }
                }
            }

            return image;
        }

        private static ClientImage Read4BitBmp(byte[] buffer, Color[] palette, BmpInfoHeader header)
        {
            var width = (header.Width + 1) / 2;
            var alignment = width % 4;
            Color color1;
            Color color2;

            var image = new ClientImage(header.Width, header.Height);

            if(alignment != 0)
            {
                alignment = 4 - alignment;
            }

            for (var row = 0; row < header.Height; row++)
            {
                var rowbase = (row * (width + alignment));
                for (var col = 0; col < width; col++)
                {
                    var colbase = col * 2;
                    var offset = rowbase + col;
                    var realRow = header.Height - row - 1;
                    color1 = palette[(buffer[offset]) >> 4];
                    color2 = palette[(buffer[offset]) & 0x0F];
                    image.SetPixel(colbase, realRow, color1.R, color1.G, color1.B, 255);
                    image.SetPixel(colbase + 1, realRow, color2.R, color2.G, color2.B, 255);
                }
            }

            return image;
        }

        private static ClientImage Read8BitBmp(byte[] buffer, Color[] palette, BmpInfoHeader header)
        {
            var alignment = header.Width % 4;
            Color color;

            var image = new ClientImage(header.Width, header.Height);

            if(alignment != 0)
            {
                alignment = 4 - alignment;
            }

            for (var row = 0; row < header.Height; row++)
            {
                var rowbase = (row * (header.Width + alignment));
                for (var col = 0; col < header.Width; col++)
                {
                    var offset = rowbase + col;
                    var realRow = header.Height - row - 1;
                    color = palette[buffer[offset]];
                    image.SetPixel(col, realRow, color.R, color.G, color.B, color.A);
                }
            }

            return image;
        }

        private static ClientImage Read24BitBmp(byte[] buffer, BmpInfoHeader header)
        {
            var alignment = (header.Width * 3) % 4;

            var image = new ClientImage(header.Width, header.Height);

            if(alignment != 0)
            {
                alignment = 4 - alignment;
            }

            for (var row = 0; row < header.Height; row++)
            {
                var rowbase = (row * ((header.Width * 3) + alignment));
                for (var col = 0; col < header.Width; col++)
                {
                    var offset = rowbase + (col * 3);
                    var realRow = header.Height - row - 1;
                    if(offset >= buffer.Length)
                    {
                        HtmlPage.Window.Alert("Error - outside of bounds and not sure why");
                    }
                    image.SetPixel(col, realRow, buffer[offset + 2], buffer[offset + 1], buffer[offset], 255);
                }
            }

            return image;
        }
    }
}