using System;
using System.IO;
using Community.Imaging.Decoders.Png;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace Community.Imaging.Decoders
{
    public struct IHdr
    {
        public byte _bitDepth;
        public ColorType _colorType;
        public byte _filter;
        public int _height;
        public byte _interlace;
        public byte _method;
        public int _width;

        internal void Read(BigEndianBinaryReader reader)
        {
            _width = reader.ReadInt32();
            _height = reader.ReadInt32();
            _bitDepth = reader.ReadByte();
            _colorType = (ColorType) reader.ReadByte();
            _method = reader.ReadByte();
            _filter = reader.ReadByte();
            _interlace = reader.ReadByte();
        }

        public int getSamples()
        {
            switch (_colorType)
            {
                case ColorType.GRAY_ALPHA:
                    return 2;
                case ColorType.RGB:
                    return 3;
                case ColorType.RGB_ALPHA:
                    return 4;
            }
            return 1;
        }

        public bool isInterlaced()
        {
            return _interlace != 0;
        }
    }

    public enum ColorType : byte
    {
        GRAY = 0,
        GRAY_ALPHA = 4,
        PALETTE = 3,
        RGB = 2,
        RGB_ALPHA = 6
    }

    public class PngDecoder
    {
        private const int type_bKGD = 0x624b4744;
        private const int type_cHRM = 0x6348524d;
        private const int type_gAMA = 0x67414d41;
        private const int type_gIFg = 0x67494667;
        private const int type_gIFx = 0x67494678;
        private const int type_hIST = 0x68495354;
        private const int type_iCCP = 0x69434350;
        private const int type_IDAT = 0x49444154;
        private const int type_IEND = 0x49454E44;
        private const int type_IHDR = 0x49484452;
        private const int type_iTXt = 0x69545874;
        private const int type_oFFs = 0x6f464673;
        private const int type_pCAL = 0x7043414c;
        private const int type_pHYs = 0x70485973;
        private const int type_PLTE = 0x504C5445;
        private const int type_sBIT = 0x73424954;
        private const int type_sCAL = 0x7343414c;
        private const int type_sPLT = 0x73504c54;
        private const int type_sRGB = 0x73524742;
        private const int type_sTER = 0x73544552;
        private const int type_tEXt = 0x74455874;
        private const int type_tIME = 0x74494D45;
        private const int type_tRNS = 0x74524E53;
        private const int type_zTXt = 0x7a545874;

        private static readonly byte[] signature = new byte[] {137, 80, 78, 71, 13, 10, 26, 10};

        public IHdr ihdr;
        private IPngImageOutput _imageOutput;
        public byte[] palette;

        private void ReadPlte(BinaryReader reader, int length)
        {
            if(length == 0)
            {
                throw new BadImageFormatException("PLTE chunk cannot be empty");
            }
            if(length % 3 != 0)
            {
                throw new BadImageFormatException(string.Format("PLTE chunk length indivisible by 3: {0}", length));
            }

            var size = length / 3;
            if(size > 256)
            {
                throw new BadImageFormatException(string.Format("Too many palette entries: {0}", size));
            }

            switch (ihdr._colorType)
            {
                case ColorType.PALETTE:
                    if(size > (1 << ihdr._bitDepth))
                    {
                        throw new BadImageFormatException(string.Format("Too many palette entries: {0}", size));
                    }
                    palette = new byte[length];
                    reader.Read(palette, 0, length);
                    break;
                default:
                    throw new BadImageFormatException("PLTE chunk found in non-palette colorformat image");
            }
        }

        private void ReadImageData(BinaryReader reader, int length)
        {
            if(reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            var width = ihdr._width;
            var height = ihdr._height;
            int bitDepth = ihdr._bitDepth;
            var samples = ihdr.getSamples();

            // var interlaced = ihdr.isInterlaced();
            var sis = new SubInputStream(reader.BaseStream, length);
            var iis = new InflaterInputStream(sis);
            var d = new Defilterer(iis, bitDepth, samples, width);
            
            // Perform the decoding
            _imageOutput.Start(this, width, height);
            d.Defilter(0, 0, 1, 1, width, height, _imageOutput);
            _imageOutput.Finish();
        }

        public PngImage Read(Stream stream)
        {
            _imageOutput = new PngImage();

            var crcstream = new CRCInputStream(stream);
            var reader = new BigEndianBinaryReader(crcstream);
            
            for (var i = 0; i < 8; i++)
            {
                if(reader.ReadByte() != signature[i])
                {
                    throw new BadImageFormatException("signature invalid");
                }
            }
            
            var msIDAT = new MemoryStream();

            var state = 0;
            for (;;)
            {
                var skipChunk = false;

                var chunkLen = reader.ReadInt32();
                crcstream.ResetCrc();
                var chunkType = reader.ReadInt32();
                
                switch (state)
                {
                    case 0:
                        if(chunkType != type_IHDR)
                        {
                            throw new BadImageFormatException("did not encounter initial IHDR");
                        }
                        ihdr.Read(reader);
                        state = 1;
                        break;
                    case 1:
                        switch (chunkType)
                        {
                            case type_IEND:
                                msIDAT.Position = 0;
                                ReadImageData(new BigEndianBinaryReader(msIDAT), (int) msIDAT.Length);
                                state = 2;
                                break;
                            case type_IHDR:
                                throw new BadImageFormatException("encountered supernumerary IHDR");
                            case type_IDAT:
                                var buf = new byte[chunkLen];
                                reader.BaseStream.Read(buf, 0, chunkLen);
                                msIDAT.Write(buf, 0, chunkLen);
                                break;
                            case type_PLTE:
                                ReadPlte(reader, chunkLen);
                                break;
                            default:
                            {
                                skipChunk = true;
                            }
                            break;
                        }
                        break;
                }
                
                if(skipChunk)
                {
                    reader.BaseStream.Position += chunkLen + 4; 
                }
                else
                {
                    if(!skipChunk)
                    {
                        var actualCrc = (int) crcstream.Value;
                        var targetCrc = reader.ReadInt32();
                        if(actualCrc != targetCrc)
                        {
                            throw new BadImageFormatException("Crc failure");
                        }
                    }
                }

                if(state == 2)
                {
                    break;
                }
            }

            return _imageOutput as PngImage;
        }
    }
}