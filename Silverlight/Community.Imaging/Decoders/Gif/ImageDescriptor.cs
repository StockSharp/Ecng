using System;
using System.Collections.Generic;
using Community.Imaging.Decoders.Gif;

namespace Community.Imaging.Decoders.Gif
{
    internal class ImageDescriptor
    {
        internal short Height;
        internal bool InterlaceFlag;
        internal bool LctFlag;
        internal int LctSize;
        internal byte Packed;
        internal bool SortFlag;
        internal short Width;
        internal short XOffSet;
        internal short YOffSet;

        internal byte[] GetBuffer()
        {
            var list = new List<byte> {GifExtensions.ImageDescriptorLabel};
            list.AddRange(BitConverter.GetBytes(XOffSet));
            list.AddRange(BitConverter.GetBytes(YOffSet));
            list.AddRange(BitConverter.GetBytes(Width));
            list.AddRange(BitConverter.GetBytes(Height));

            byte packed = 0;

            var m = 0;
            if(LctFlag)
            {
                m = 1;
            }

            var i = 0;
            if(InterlaceFlag)
            {
                i = 1;
            }

            var s = 0;
            if(SortFlag)
            {
                s = 1;

            }
            var pixel = (byte) (Math.Log(LctSize, 2) - 1);
            packed = (byte) (pixel | (s << 5) | (i << 6) | (m << 7));
            list.Add(packed);
            return list.ToArray();
        }
    }
}