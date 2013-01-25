using System;
using System.IO;

namespace Community.Imaging.Decoders.Png
{
    class SubInputStream : Stream
    {
        readonly Stream BaseStream;
        readonly int length;
        int position;

        public SubInputStream(Stream stream, int length)
        {
            BaseStream = stream;
            this.length = length;
        }

        public override void Flush() { throw new NotImplementedException(); }
        public override int Read(byte[] buffer, int offset, int count)
        {
            int todo = Math.Min(length - position, count);
            int read = BaseStream.Read(buffer, offset, todo);
            position += read;
            return read;
        }
        public override int ReadByte()
        {
            if (position == length) return -1;
            var val = BaseStream.ReadByte();
            position++;
            return val;
        }

        public override long Seek(long offset, SeekOrigin origin) { throw new NotImplementedException(); }
        public override void SetLength(long value) { throw new NotImplementedException(); }
        public override void Write(byte[] buffer, int offset, int count) { throw new NotImplementedException(); }
        public override void WriteByte(byte value) { throw new NotImplementedException(); }

        public override bool CanRead { get { return true; } }
        public override bool CanSeek { get { return false; } }
        public override bool CanTimeout { get { return BaseStream.CanTimeout; } }
        public override bool CanWrite { get { return false; } }
        public override long Length { get { return length; } }
        public override long Position { get { return position; } set { throw new NotImplementedException(); } }
        public override int ReadTimeout { get { return BaseStream.ReadTimeout; } set { BaseStream.ReadTimeout = value; } }
        public override int WriteTimeout { get { return BaseStream.WriteTimeout; } set { BaseStream.WriteTimeout = value; } }

    }
}