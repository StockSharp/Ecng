using System.Collections.Generic;

namespace Community.Imaging.Decoders.Gif
{
    internal struct CommentEx
    {
        internal List<string> CommentDatas;

        internal byte[] GetBuffer()
        {
            var list = new List<byte> {GifExtensions.ExtensionIntroducer, GifExtensions.CommentLabel};
            foreach (var coment in CommentDatas)
            {
                var commentCharArray = coment.ToCharArray();
                list.Add((byte) commentCharArray.Length);
                foreach (var c in commentCharArray)
                {
                    list.Add((byte) c);
                }
            }
            list.Add(GifExtensions.Terminator);
            return list.ToArray();
        }
    }
}