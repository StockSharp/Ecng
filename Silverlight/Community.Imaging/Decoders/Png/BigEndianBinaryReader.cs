using System;
using System.IO;
using System.Text;

namespace Community.Imaging.Decoders.Png
{
    internal class BigEndianBinaryReader : BinaryReader
    {
        private readonly byte[] buf = new byte[8];
        public BigEndianBinaryReader(Stream input) : base(input) {}
        public BigEndianBinaryReader(Stream input, Encoding encoding) : base(input, encoding) {}
        //public virtual void Close();
        //protected virtual void Dispose(bool disposing);
        //protected virtual void FillBuffer(int numBytes);
        //private int InternalReadChars(char[] buffer, int index, int count);
        //private int InternalReadOneChar();
        //public virtual int PeekChar();
        //public virtual int Read();
        //public virtual int Read(byte[] buffer, int index, int count);
        //public virtual int Read(char[] buffer, int index, int count);
        public override bool ReadBoolean()
        {
            throw new Exception("not implemented");
        }

        //public virtual byte ReadByte();
        public override byte[] ReadBytes(int count)
        {
            throw new Exception("not implemented");
        }

        public override char ReadChar()
        {
            throw new Exception("not implemented");
        }

        public override char[] ReadChars(int count)
        {
            throw new Exception("not implemented");
        }

        public decimal ReadDecimal()
        {
            throw new Exception("not implemented");
        }

        public override double ReadDouble()
        {
            throw new Exception("not implemented");
        }

        public override short ReadInt16()
        {
            Read(buf, 0, 2);
            return (short) ((buf[0] << 8) | (buf[1] << 0));
        }

        public override int ReadInt32()
        {
            Read(buf, 0, 4);
            return ((((buf[0] << 24) | (buf[1] << 16)) | (buf[2] << 8)) | (buf[3] << 0));
        }

        public override long ReadInt64()
        {
            throw new Exception("not implemented");
        }

        //public override sbyte ReadSByte() {}
        public override float ReadSingle()
        {
            throw new Exception("not implemented");
        }

        public override string ReadString()
        {
            throw new Exception("not implemented");
        }

        public override ushort ReadUInt16()
        {
            return (ushort) ReadInt16();
        }

        public override uint ReadUInt32()
        {
            return ReadUInt32();
        }

        public override ulong ReadUInt64()
        {
            throw new Exception("not implemented");
        }

        //void IDisposable.Dispose();
    }
}