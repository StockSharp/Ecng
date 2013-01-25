using System;
using System.IO;
using ICSharpCode.SharpZipLib.Checksums;

namespace Community.Imaging.Decoders.Png
{
    internal sealed class CRCInputStream : Stream
    {
        private readonly Stream BaseStream;
        private readonly Crc32 crc = new Crc32();

        public CRCInputStream(Stream stream)
        {
            BaseStream = stream;
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return BaseStream.CanSeek; }
        }

        public override bool CanTimeout
        {
            get { return BaseStream.CanTimeout; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override long Length
        {
            get { return BaseStream.Length; }
        }

        public override long Position
        {
            get { return BaseStream.Position; }
            set { BaseStream.Position = value; }
        }

        public override int ReadTimeout
        {
            get { return BaseStream.ReadTimeout; }
            set { BaseStream.ReadTimeout = value; }
        }

        public override int WriteTimeout
        {
            get { return BaseStream.WriteTimeout; }
            set { BaseStream.WriteTimeout = value; }
        }

        public long Value
        {
            get { return crc.Value; }
        }

        public int Count { get; private set; }

        public override void Flush()
        {
            BaseStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = BaseStream.Read(buffer, offset, count);
            crc.Update(buffer, 0, read);
            this.Count += read;
            return read;
        }

        public override int ReadByte()
        {
            var val = BaseStream.ReadByte();
            if(val != -1)
            {
                crc.Update(val);
            }
            Count++;
            return val;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return BaseStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            BaseStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override void WriteByte(byte value)
        {
            throw new NotImplementedException();
        }

        public void ResetCrc()
        {
            Count = 0;
            crc.Reset(); 
        }
    }
}