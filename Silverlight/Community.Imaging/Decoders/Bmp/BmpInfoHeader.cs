using System;
using System.IO;

namespace Community.Imaging.Decoders.Bmp
{
    public class BmpInfoHeader
    {
        private const int _SIZE = 40;

        public int HeaderSize { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public short NA1 { get; set; }
        public short BitsPerPixel { get; set; }
        public int Compression { get; set; }
        public int ImageSize { get; set; }
        public int NA2 { get; set; }
        public int NA3 { get; set; }
        public int ColorCount { get; set; }
        public int NA4 { get; set; }

        public static BmpInfoHeader FillFromStream(Stream stream)
        {
            var buffer = new byte[_SIZE];
            var header = new BmpInfoHeader();

            stream.Read(buffer, 0, _SIZE);
            
            header.HeaderSize = BitConverter.ToInt32(buffer, 0);
            header.Width = BitConverter.ToInt32(buffer, 4);
            header.Height = BitConverter.ToInt32(buffer, 8);
            header.BitsPerPixel = BitConverter.ToInt16(buffer, 14);
            header.Compression = BitConverter.ToInt32(buffer, 16);
            header.ImageSize = BitConverter.ToInt32(buffer, 20);
            header.ColorCount = BitConverter.ToInt32(buffer, 32);

            if(header.ColorCount == 0)
            {
                header.ColorCount = (1 << header.BitsPerPixel);
            }

            if(header.ImageSize == 0)
            {
                var rowSize = 4 * Math.Ceiling(header.Width * header.BitsPerPixel / 32.0);
                var fileSize = header.HeaderSize + (4 * Math.Pow(2, header.BitsPerPixel)) + rowSize + header.Height;

                header.ImageSize = (int)fileSize;
            }


            return header;
        }
    }
}