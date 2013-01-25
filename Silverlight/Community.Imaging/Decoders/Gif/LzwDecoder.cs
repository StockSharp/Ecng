using System.IO;

namespace Community.Imaging.Decoders
{
    public class LzwDecoder
    {
        protected static readonly int MaxStackSize = 4096;
        protected Stream _stream;

        public LzwDecoder(Stream stream)
        {
            _stream = stream;
        }

        internal byte[] DecodeImageData(int width, int height, int dataSize)
        {
            var NullCode = -1;
            var pixelCount = width * height;
            var pixels = new byte[pixelCount];

            var codeSize = dataSize + 1;
            var clearFlag = 1 << dataSize;

            var endFlag = clearFlag + 1;
            var available = endFlag + 1;

            int code;
            var old_code = NullCode;
            var code_mask = (1 << codeSize) - 1;
            var bits = 0;
            
            var prefix = new int[MaxStackSize];
            var suffix = new int[MaxStackSize];
            var pixelStack = new int[MaxStackSize + 1];

            var top = 0;
            var count = 0;

            var bi = 0;
            var i = 0;

            var data = 0;
            var first = 0;

            for (code = 0; code < clearFlag; code++)
            {
                prefix[code] = 0;

                suffix[code] = (byte) code;
            }

            byte[] buffer = null;
            while (i < pixelCount)
            {
                if(top == 0)
                {
                    if(bits < codeSize)
                    {
                        if(count == 0)
                        {
                            buffer = ReadData();
                            count = buffer.Length;
                            if(count == 0)
                            {
                                break;
                            }

                            bi = 0;
                        }

                        if(buffer != null)
                        {
                            data += buffer[bi] << bits;
                        }

                        bits += 8;
                        bi++;
                        count--;
                        continue;
                    }


                    code = data & code_mask;
                    data >>= codeSize;
                    bits -= codeSize;

                    if(code > available || code == endFlag)
                    {
                        break;
                    }
                    if(code == clearFlag)
                    {
                        codeSize = dataSize + 1;
                        code_mask = (1 << codeSize) - 1;
                        available = clearFlag + 2;
                        old_code = NullCode;
                        continue;
                    }

                    if(old_code == NullCode)
                    {
                        pixelStack[top++] = suffix[code];
                        old_code = code;
                        first = code;
                        continue;
                    }

                    var inCode = code;
                    if(code == available)
                    {
                        pixelStack[top++] = (byte) first;
                        code = old_code;
                    }
                    while (code > clearFlag)
                    {
                        pixelStack[top++] = suffix[code];
                        code = prefix[code];
                    }

                    first = suffix[code];
                    if(available > MaxStackSize)
                    {
                        break;
                    }

                    pixelStack[top++] = suffix[code];
                    prefix[available] = old_code;
                    suffix[available] = first;

                    available++;
                    if(available == code_mask + 1 && available < MaxStackSize)
                    {
                        codeSize++;
                        code_mask = (1 << codeSize) - 1;
                    }

                    old_code = inCode;
                }

                top--;

                pixels[i++] = (byte) pixelStack[top];
            }
            return pixels;
        }

        private byte[] ReadData()
        {
            var blockSize = Read();
            return ReadByte(blockSize);
        }

        private byte[] ReadByte(int len)
        {
            var buffer = new byte[len];
            _stream.Read(buffer, 0, len);
            return buffer;
        }

        private int Read()
        {
            return _stream.ReadByte();
        }
    }
}