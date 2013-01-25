using System;
using System.Collections.Generic;
using System.IO;
using Community.Imaging.Decoders.Gif;

namespace Community.Imaging.Decoders.Gif
{
    internal class StreamHelper
    {
        private readonly Stream _stream;

        internal StreamHelper(Stream stream)
        {
            _stream = stream;
        }

        internal byte[] ReadByte(int len)
        {
            var buffer = new byte[len];
            _stream.Read(buffer, 0, len);
            return buffer;
        }
        
        internal int Read()
        {
            return _stream.ReadByte();
        }

        internal short ReadShort()
        {
            var buffer = new byte[2];
            _stream.Read(buffer, 0, buffer.Length);
            return BitConverter.ToInt16(buffer, 0);
        }

        internal string ReadString(int length)
        {
            return new string(ReadChar(length));
        }

        internal char[] ReadChar(int length)
        {
            var buffer = new byte[length];
            _stream.Read(buffer, 0, length);
            var charBuffer = new char[length];
            buffer.CopyTo(charBuffer, 0);
            return charBuffer;
        }

        internal void WriteString(string str)
        {
            var charBuffer = str.ToCharArray();
            var buffer = new byte[charBuffer.Length];
            var index = 0;
            foreach (var c in charBuffer)
            {
                buffer[index] = (byte) c;
                index++;
            }
            WriteBytes(buffer);
        }

        internal void WriteBytes(byte[] buffer)
        {
            _stream.Write(buffer, 0, buffer.Length);
        }

        internal ApplicationEx GetApplicationEx(Stream stream)
        {
            var appEx = new ApplicationEx();
            var blockSize = Read();
            if(blockSize != ApplicationEx.BlockSize)
            {
                throw new Exception("数据格式错误！");
            }
            appEx.ApplicationIdentifier = ReadChar(8);
            appEx.ApplicationAuthenticationCode = ReadChar(3);
            var nextFlag = Read();
            appEx.Datas = new List<DataStruct>();
            while (nextFlag != 0)
            {
                var data = new DataStruct(nextFlag, stream);
                appEx.Datas.Add(data);
                nextFlag = Read();
            }
            return appEx;
        }

        internal CommentEx GetCommentEx(Stream stream)
        {
            var cmtEx = new CommentEx();
            var streamHelper = new StreamHelper(stream);
            cmtEx.CommentDatas = new List<string>();
            var nextFlag = streamHelper.Read();
            cmtEx.CommentDatas = new List<string>();
            while (nextFlag != 0)
            {
                var blockSize = nextFlag;
                var data = streamHelper.ReadString(blockSize);
                cmtEx.CommentDatas.Add(data);
                nextFlag = streamHelper.Read();
            }
            return cmtEx;
        }

        internal PlainTextEx GetPlainTextEx(Stream stream)
        {
            var pltEx = new PlainTextEx();
            var blockSize = Read();
            if(blockSize != PlainTextEx.BlockSize)
            {
                throw new Exception("数据格式错误！");
            }
            pltEx.XOffSet = ReadShort();
            pltEx.YOffSet = ReadShort();
            pltEx.Width = ReadShort();
            pltEx.Height = ReadShort();
            pltEx.CharacterCellWidth = (byte) Read();
            pltEx.CharacterCellHeight = (byte) Read();
            pltEx.ForegroundColorIndex = (byte) Read();
            pltEx.BgColorIndex = (byte) Read();
            var nextFlag = Read();
            pltEx.TextDatas = new List<string>();
            while (nextFlag != 0)
            {
                blockSize = nextFlag;
                var data = ReadString(blockSize);
                pltEx.TextDatas.Add(data);
                nextFlag = Read();
            }
            return pltEx;
        }

        internal ImageDescriptor GetImageDescriptor(Stream stream)
        {
            var ides = new ImageDescriptor
            {
                XOffSet = ReadShort(),
                YOffSet = ReadShort(),
                Width = ReadShort(),
                Height = ReadShort(),
                Packed = ((byte) Read())
            };

            ides.LctFlag = ((ides.Packed & 0x80) >> 7) == 1;
            ides.InterlaceFlag = ((ides.Packed & 0x40) >> 6) == 1;
            ides.SortFlag = ((ides.Packed & 0x20) >> 5) == 1;
            ides.LctSize = (2 << (ides.Packed & 0x07));
            return ides;
        }

        internal GraphicEx GetGraphicControlExtension(Stream stream)
        {
            var gex = new GraphicEx();
            var blockSize = Read();
            if(blockSize != GraphicEx.BlockSize)
            {
                throw new Exception("数据格式错误！");
            }
            gex.Packed = (byte) Read();
            gex.TransparencyFlag = (gex.Packed & 0x01) == 1;
            gex.DisposalMethod = (gex.Packed & 0x1C) >> 2;
            gex.Delay = ReadShort();
            gex.TranIndex = (byte) Read();
            Read();
            return gex;
        }

        internal LogicalScreenDescriptor GetLCD(Stream stream)
        {
            var lcd = new LogicalScreenDescriptor
            {
                Width = ReadShort(),
                Height = ReadShort(),
                Packed = ((byte) Read())
            };

            lcd.GlobalColorTableFlag = ((lcd.Packed & 0x80) >> 7) == 1;
            lcd.ColorResolution = (byte) ((lcd.Packed & 0x60) >> 5);
            lcd.SortFlag = (byte) (lcd.Packed & 0x10) >> 4;
            lcd.GlobalColorTableSize = 2 << (lcd.Packed & 0x07);
            lcd.BgColorIndex = (byte) Read();
            lcd.PixelAspect = (byte) Read();
            return lcd;
        }

        internal void WriteHeader(string header)
        {
            WriteString(header);
        }

        internal void WriteLSD(LogicalScreenDescriptor lsd)
        {
            WriteBytes(lsd.GetBuffer());
        }

        internal void SetGlobalColorTable(byte[] buffer)
        {
            WriteBytes(buffer);
        }

        internal void SetCommentExtensions(List<CommentEx> comments)
        {
            foreach (var ce in comments)
            {
                WriteBytes(ce.GetBuffer());
            }
        }

        internal void SetApplicationExtensions(List<ApplicationEx> applications)
        {
            foreach (var ap in applications)
            {
                WriteBytes(ap.GetBuffer());
            }
        }
    }
}