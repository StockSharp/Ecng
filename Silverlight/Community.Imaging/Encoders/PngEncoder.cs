using System;
using System.IO;

namespace Community.Imaging.Encoders
{
    public class PngEncoder
    {
        private const int _ADLER32_BASE = 65521;
        private const int _MAXBLOCK = 0xFFFF;
        private static readonly byte[] _4BYTEDATA = {0, 0, 0, 0};
        private static readonly byte[] _ARGB = {0, 0, 0, 0, 0, 0, 0, 0, 8, 6, 0, 0, 0};
        private static readonly uint[] _crcTable = new uint[256];
        private static readonly byte[] _GAMA = {(byte) 'g', (byte) 'A', (byte) 'M', (byte) 'A'};
        private static readonly byte[] _HEADER = {0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A};
        private static readonly byte[] _IDAT = {(byte) 'I', (byte) 'D', (byte) 'A', (byte) 'T'};
        private static readonly byte[] _IEND = {(byte) 'I', (byte) 'E', (byte) 'N', (byte) 'D'};
        private static readonly byte[] _IHDR = {(byte) 'I', (byte) 'H', (byte) 'D', (byte) 'R'};
        private static bool _crcTableComputed;

        public static Stream Encode(byte[] data, int width, int height)
        {
            var ms = new MemoryStream();
            ms.Write(_HEADER, 0, _HEADER.Length);
            
            var size = BitConverter.GetBytes(width);
            _ARGB[0] = size[3];
            _ARGB[1] = size[2];
            _ARGB[2] = size[1];
            _ARGB[3] = size[0];

            size = BitConverter.GetBytes(height);
            _ARGB[4] = size[3];
            _ARGB[5] = size[2];
            _ARGB[6] = size[1];
            _ARGB[7] = size[0];
            
            WriteChunk(ms, _IHDR, _ARGB);
            
            size = BitConverter.GetBytes(1 * 100000);
            _4BYTEDATA[0] = size[3];
            _4BYTEDATA[1] = size[2];
            _4BYTEDATA[2] = size[1];
            _4BYTEDATA[3] = size[0];
            
            WriteChunk(ms, _GAMA, _4BYTEDATA);
            
            var widthLength = (uint) (width * 4) + 1;
            var dcSize = widthLength * (uint) height;
            
            var adler = ComputeAdler32(data);
            var comp = new MemoryStream();

            var rowsPerBlock = _MAXBLOCK / widthLength;
            var blockSize = rowsPerBlock * widthLength;
            var remainder = dcSize;
            var blockCount = (dcSize % blockSize) == 0 ? dcSize / blockSize : (dcSize / blockSize) + 1;

            comp.WriteByte(0x78);
            comp.WriteByte(0xDA);

            for (uint blocks = 0; blocks < blockCount; blocks++)
            {
                var length = (ushort) ((remainder < blockSize) ? remainder : blockSize);

                if(length == remainder)
                {
                    comp.WriteByte(0x01);
                }
                else
                {
                    comp.WriteByte(0x00);
                }

                comp.Write(BitConverter.GetBytes(length), 0, 2);
                comp.Write(BitConverter.GetBytes((ushort) ~length), 0, 2);
                comp.Write(data, (int) (blocks * blockSize), length);

                remainder -= blockSize;
            }

            WriteReversedBuffer(comp, BitConverter.GetBytes(adler));
            comp.Seek(0, SeekOrigin.Begin);

            var dat = new byte[comp.Length];
            comp.Read(dat, 0, (int) comp.Length);

            WriteChunk(ms, _IDAT, dat);
            WriteChunk(ms, _IEND, new byte[0]);
            
            ms.Seek(0, SeekOrigin.Begin);
            return ms;
        }

        private static void WriteReversedBuffer(Stream stream, byte[] data)
        {
            var size = data.Length;
            var reorder = new byte[size];

            for (var idx = 0; idx < size; idx++)
            {
                reorder[idx] = data[size - idx - 1];
            }
            stream.Write(reorder, 0, size);
        }

        private static void WriteChunk(Stream stream, byte[] type, byte[] data)
        {
            int idx;
            var size = type.Length;
            var buffer = new byte[type.Length + data.Length];

            for (idx = 0; idx < type.Length; idx++)
            {
                buffer[idx] = type[idx];
            }

            for (idx = 0; idx < data.Length; idx++)
            {
                buffer[idx + size] = data[idx];
            }

            WriteReversedBuffer(stream, BitConverter.GetBytes(data.Length));

            stream.Write(buffer, 0, buffer.Length);

            WriteReversedBuffer(stream, BitConverter.GetBytes(GetCRC(buffer)));
        }

        private static void MakeCRCTable()
        {
            for (var n = 0; n < 256; n++)
            {
                var c = (uint) n;
                for (var k = 0; k < 8; k++)
                {
                    if((c & (0x00000001)) > 0)
                    {
                        c = 0xEDB88320 ^ (c >> 1);
                    }
                    else
                    {
                        c = c >> 1;
                    }
                }
                _crcTable[n] = c;
            }

            _crcTableComputed = true;
        }

        private static uint UpdateCRC(uint crc, byte[] buf, int len)
        {
            var c = crc;

            if(!_crcTableComputed)
            {
                MakeCRCTable();
            }

            for (var n = 0; n < len; n++)
            {
                c = _crcTable[(c ^ buf[n]) & 0xFF] ^ (c >> 8);
            }

            return c;
        }

        private static uint GetCRC(byte[] buf)
        {
            return UpdateCRC(0xFFFFFFFF, buf, buf.Length) ^ 0xFFFFFFFF;
        }

        private static uint ComputeAdler32(byte[] buf)
        {
            uint s1 = 1;
            uint s2 = 0;
            var length = buf.Length;

            for (var idx = 0; idx < length; idx++)
            {
                s1 = (s1 + buf[idx]) % _ADLER32_BASE;
                s2 = (s2 + s1) % _ADLER32_BASE;
            }

            return (s2 << 16) + s1;
        }
    }
}