#region Copyright & License

#endregion

using System.Collections.Generic;

namespace Community.Imaging.Decoders.Gif
{
    internal struct ApplicationEx
    {
        internal static readonly byte BlockSize = 0X0B;


        internal char[] ApplicationAuthenticationCode;
        internal char[] ApplicationIdentifier;


        internal List<DataStruct> Datas;

       internal byte[] GetBuffer()
       {
           var list = new List<byte>
           {
               GifExtensions.ExtensionIntroducer,
               GifExtensions.ApplicationExtensionLabel,
               BlockSize
           };

           if(ApplicationIdentifier == null)
           {
               ApplicationIdentifier = "NETSCAPE".ToCharArray();
           }
           foreach (var c in ApplicationIdentifier)
           {
               list.Add((byte) c);
           }
           if(ApplicationAuthenticationCode == null)
           {
               ApplicationAuthenticationCode = "2.0".ToCharArray();
           }
           foreach (var c in ApplicationAuthenticationCode)
           {
               list.Add((byte) c);
           }
           if(Datas != null)
           {
               foreach (var ds in Datas)
               {
                   list.AddRange(ds.GetBuffer());
               }
           }
           list.Add(GifExtensions.Terminator);
           return list.ToArray();
       }
    }
}