using System;
using System.IO;
using Community.Imaging.Decoders.Gif;

namespace Community.Imaging.Decoders.Gif
{
    internal class DataStruct
    {
        private byte _blockSize;

        private byte[] _data = new byte[0];
        internal DataStruct() {}

        internal DataStruct(int blockSize, Stream stream)
        {
            var streamHelper = new StreamHelper(stream);
            _blockSize = (byte) blockSize;
            if(_blockSize > 0)
            {
                _data = streamHelper.ReadByte(_blockSize);
            }
        }


        internal byte BlockSize
        {
            get { return _blockSize; }
            set { _blockSize = value; }
        }

        internal byte[] Data
        {
            get { return _data; }
            set { _data = value; }
        }

        internal byte[] GetBuffer()
        {
            var buffer = new byte[_blockSize + 1];
            buffer[0] = _blockSize;
            Array.Copy(_data, 0, buffer, 1, _blockSize);
            return buffer;
        }
    }
}