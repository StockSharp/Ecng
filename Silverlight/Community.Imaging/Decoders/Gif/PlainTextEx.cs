using System;
using System.Collections.Generic;
using Community.Imaging.Decoders.Gif;

namespace Community.Imaging.Decoders.Gif
{
    internal struct PlainTextEx
    {
        internal static readonly byte BlockSize = 0X0C;
        internal byte BgColorIndex;
        internal byte CharacterCellHeight;
        internal byte CharacterCellWidth;
        internal byte ForegroundColorIndex;
        internal short Height;
        internal List<string> TextDatas;
        internal short Width;
        internal short XOffSet;
        internal short YOffSet;

        internal byte[] GetBuffer()
        {
            var list = new List<byte> {GifExtensions.ExtensionIntroducer, GifExtensions.PlainTextLabel, BlockSize};
            list.AddRange(BitConverter.GetBytes(XOffSet));
            list.AddRange(BitConverter.GetBytes(YOffSet));
            list.AddRange(BitConverter.GetBytes(Width));
            list.AddRange(BitConverter.GetBytes(Height));
            list.Add(CharacterCellWidth);
            list.Add(CharacterCellHeight);
            list.Add(ForegroundColorIndex);
            list.Add(BgColorIndex);

            if(TextDatas != null)
            {
                foreach (var text in TextDatas)
                {
                    list.Add((byte) text.Length);
                    foreach (var c in text)
                    {
                        list.Add((byte) c);
                    }
                }
            }

            list.Add(GifExtensions.Terminator);
            return list.ToArray();
        }
    }
}