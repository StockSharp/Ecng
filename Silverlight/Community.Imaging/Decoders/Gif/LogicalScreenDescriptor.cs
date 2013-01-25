#region Copyright & License

#endregion

using System;

namespace Community.Imaging.Decoders.Gif
{
    internal class LogicalScreenDescriptor
    {
        internal short Width { get; set; }
        internal short Height { get; set; }
        internal byte Packed { get; set; }
        internal byte BgColorIndex { get; set; }
        internal byte PixelAspect { get; set; }
        internal bool GlobalColorTableFlag { get; set; }
        internal byte ColorResolution { get; set; }
        internal int SortFlag { get; set; }
        internal int GlobalColorTableSize { get; set; }

        internal byte[] GetBuffer()
        {
            var buffer = new byte[7];
            Array.Copy(BitConverter.GetBytes(Width), 0, buffer, 0, 2);
            Array.Copy(BitConverter.GetBytes(Height), 0, buffer, 2, 2);
            var m = 0;
            if(GlobalColorTableFlag)
            {
                m = 1;
            }
            var pixel = (byte) (Math.Log(GlobalColorTableSize, 2) - 1);
            Packed = (byte) (pixel | (SortFlag << 4) | (ColorResolution << 5) | (m << 7));
            buffer[4] = Packed;
            buffer[5] = BgColorIndex;
            buffer[6] = PixelAspect;
            return buffer;
        }
    }
}