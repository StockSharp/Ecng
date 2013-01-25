using System;
using System.Collections.Generic;
using Community.Imaging.Decoders.Gif;
using Jillzhang.GifUtility;

namespace Community.Imaging.Decoders.Gif
{
    internal class GraphicEx : ExData
    {
        #region private fields

        private short _delay;
        private int _disposalMethod;
        private byte _packed;
        private byte _tranIndex;
        private bool _transFlag;

        #endregion

        internal static readonly byte BlockSize = 4;


        internal bool TransparencyFlag
        {
            get { return _transFlag; }
            set { _transFlag = value; }
        }


        internal int DisposalMethod
        {
            get { return _disposalMethod; }
            set { _disposalMethod = value; }
        }


        internal byte Packed
        {
            get { return _packed; }
            set { _packed = value; }
        }


        internal short Delay
        {
            get { return _delay; }
            set { _delay = value; }
        }


        internal byte TranIndex
        {
            get { return _tranIndex; }
            set { _tranIndex = value; }
        }

        internal byte[] GetBuffer()
        {
            var list = new List<byte>
            {
                GifExtensions.ExtensionIntroducer,
                GifExtensions.GraphicControlLabel,
                BlockSize
            };

            var t = 0;
            if(_transFlag)
            {
                t = 1;
            }
            _packed = (byte) ((_disposalMethod << 2) | t);
            list.Add(_packed);
            list.AddRange(BitConverter.GetBytes(_delay));
            list.Add(_tranIndex);
            list.Add(GifExtensions.Terminator);
            return list.ToArray();
        }
    }
}