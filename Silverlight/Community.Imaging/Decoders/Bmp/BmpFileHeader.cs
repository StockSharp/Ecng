using System;
using System.IO;

namespace Community.Imaging.Decoders.Bmp
{
    public class BmpFileHeader
    {
        private const int _SIZE = 14;

        public short BitmapType { get; set; }
        public int Size { get; set; }
        public short NA1 { get; set; }
        public short NA2 { get; set; }
        public int OffsetToData { get; set; }

        public static BmpFileHeader FillFromStream(Stream stream)
        {
            var buffer = new byte[_SIZE];
            var header = new BmpFileHeader();

            stream.Read(buffer, 0, _SIZE);


            header.BitmapType = BitConverter.ToInt16(buffer, 0);
            header.Size = BitConverter.ToInt32(buffer, 2);
            header.OffsetToData = BitConverter.ToInt32(buffer, 10);


            return header;
        }
    }
}