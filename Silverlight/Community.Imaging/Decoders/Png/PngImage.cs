using System.IO;
using Community.Imaging.Decoders;
using Community.Imaging.Decoders.Png;

namespace Community.Imaging.Decoders.Png
{
    public class PngImage : IPngImageOutput
    {
        private Stream _stream;

        public int Width { get; private set; }
        public int Height { get; private set; }
        public byte[] Buffer { get; private set; }

        #region Implementation of IPngImageOutput

        public void Start(PngDecoder png, int width, int height)
        {
            _stream = new MemoryStream();
            Width = width;
            Height = height;
        }

        public void WriteLine(byte[] data, int offset)
        {
            var temp = new byte[Width * 4];
            
            for(int i = 0, x = 0; i < Width;i++, x+=4)
            {
                temp[x] = data[x + offset + 3];
                temp[x + 1] = data[x + offset + 2];
                temp[x + 2] = data[x + offset + 1];
				temp[x + 3] = data[x + offset];
            }

            _stream.Write(temp, 0, temp.Length);
        }

        public void Finish()
        {
            Buffer = new byte[_stream.Length];
            _stream.Position = 0;
            _stream.Read(Buffer, 0, (int)_stream.Length);

            _stream.Flush();
            _stream.Close();
            _stream.Dispose();
        }

        #endregion
    }
}