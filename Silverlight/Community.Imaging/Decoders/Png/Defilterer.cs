using System;
using System.IO;
using Community.Imaging.Decoders.Png;

namespace Community.Imaging.Decoders.Png
{
    internal class Defilterer
    {
        private readonly int _bitDepth;
        private readonly int _bpp;
        private readonly int _samples;
        private readonly Stream _stream;

        public Defilterer(Stream stream, int bitDepth, int samples, int width)
        {
            _stream = stream;
            _bitDepth = bitDepth;
            _samples = samples;
            _bpp = Math.Max(1, (bitDepth * samples) >> 3);
        }

        private void FillBuffer(byte[] buf, int off, int len)
        {
            switch (len)
            {
                case 1:
                {
                    var temp = _stream.ReadByte();
                    if(temp == -1)
                    {
                        throw new EndOfStreamException();
                    }
                    buf[0] = (byte) temp;
                    return;
                }
                default:
                {
                    var ctr = 0;
                    do
                    {
                        var temp = _stream.Read(buf, off + ctr, len - ctr);
                        if(temp == 0)
                        {
                            throw new EndOfStreamException();
                        }
                        ctr += temp;
                    } while (ctr < len);
                }
                    break;
            }
        }

        private static void Defilter(byte[] cur, byte[] prev, int bpp, int filterType)
        {
            var rowSize = cur.Length;
            int xc, xp;
            switch (filterType)
            {
                case 0: // None
                    break;
                case 1: // Sub
                    for (xc = bpp, xp = 0; xc < rowSize; xc++, xp++)
                    {
                        cur[xc] = (byte) (cur[xc] + cur[xp]);
                    }
                    break;
                case 2: // Up
                    for (xc = bpp; xc < rowSize; xc++)
                    {
                        cur[xc] = (byte) (cur[xc] + prev[xc]);
                    }
                    break;
                case 3: // Average
                    for (xc = bpp, xp = 0; xc < rowSize; xc++, xp++)
                    {
                        cur[xc] = (byte) (cur[xc] + ((0xFF & cur[xp]) + (0xFF & prev[xc])) / 2);
                    }
                    break;
                case 4: // Paeth
                    for (xc = bpp, xp = 0; xc < rowSize; xc++, xp++)
                    {
                        var L = cur[xp];
                        var u = prev[xc];
                        var nw = prev[xp];
                        var a = 0xFF & L; // inline byte->int
                        var b = 0xFF & u;
                        var c = 0xFF & nw;
                        var p = a + b - c;
                        var pa = p - a;
                        if(pa < 0)
                        {
                            pa = -pa; // inline Math.abs
                        }
                        var pb = p - b;
                        if(pb < 0)
                        {
                            pb = -pb;
                        }
                        var pc = p - c;
                        if(pc < 0)
                        {
                            pc = -pc;
                        }
                        int result;
                        if(pa <= pb && pa <= pc)
                        {
                            result = a;
                        }
                        else result = pb <= pc ? b : c;
                        cur[xc] = (byte) (cur[xc] + result);
                    }
                    break;
                default:
                    throw new Exception(string.Format("Unrecognized filter type {0}", filterType));
            }
        }
        
        public void Defilter(int xOffset, int yOffset,
                             int xStep, int yStep,
                             int passWidth, int passHeight, IPngImageOutput output)
        {
            if(passWidth == 0 || passHeight == 0)
            {
                return;
            }

            var bytesPerRow = (_bitDepth * _samples * passWidth + 7) / 8;
            var isShort = _bitDepth == 16;
            var rowSize = bytesPerRow + _bpp;
            var prev = new byte[rowSize];
            var cur = new byte[rowSize];

            for (int srcY = 0, dstY = yOffset; srcY < passHeight; srcY++, dstY += yStep)
            {
                var filterType = _stream.ReadByte();
                if(filterType == -1)
                {
                    throw new EndOfStreamException();
                }

                FillBuffer(cur, _bpp, bytesPerRow);
                Defilter(cur, prev, _bpp, filterType);
                output.WriteLine(cur, _bpp);

                var tmp = cur;
                cur = prev;
                prev = tmp;
            }
        }
    }
}