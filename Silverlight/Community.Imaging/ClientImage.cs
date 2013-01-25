using System;
using System.IO;
using System.Windows.Media;
using Community.Imaging.Encoders;

namespace Community.Imaging
{
    public class ClientImage
    {
        public byte[] Buffer { get; set; }
        private int _height;
        private int _rowLength;
        private int _width;

        public ClientImage(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public bool Initialized { get; private set; }

        public int Width
        {
            get { return _width; }
            set
            {
                if(Initialized)
                {
                    OnImageError("Error: Cannot change Width after the ClientImage has been initialized");
                }
                else if((value <= 0) || (value > 3000))
                {
                    OnImageError("Error: Width must be between 0 and 3000");
                }
                else
                {
                    _width = value;
                }
            }
        }

        public int Height
        {
            get { return _height; }
            set
            {
                if(Initialized)
                {
                    OnImageError("Error: Cannot change Height after the ClientImage has been initialized");
                }
                else if((value <= 0) || (value > 3000))
                {
                    OnImageError("Error: Height must be between 0 and 3000");
                }
                else
                {
                    _height = value;
                }
            }
        }

        public event EventHandler<ClientImageErrorEventArgs> ImageError;

        public void SetPixel(int col, int row, Color color)
        {
            SetPixel(col, row, color.R, color.G, color.B, color.A);
        }

        public void SetPixel(int col, int row, byte red, byte green, byte blue, byte alpha)
        {
            if(!Initialized)
            {
                _rowLength = _width * 4 + 1;
                Buffer = new byte[_rowLength * _height];
                
                for (var idx = 0; idx < _height; idx++)
                {
                    Buffer[idx * _rowLength] = 0;
                }

                Initialized = true;
            }

            if((col > _width) || (col < 0))
            {
                OnImageError("Error: Column must be greater than 0 and less than the Width");
            }
            else if((row > _height) || (row < 0))
            {
                OnImageError("Error: Row must be greater than 0 and less than the Height");
            }
            
            var start = _rowLength * row + col * 4 + 1;
            Buffer[start] = red;
            Buffer[start + 1] = green;
            Buffer[start + 2] = blue;
            Buffer[start + 3] = alpha;
        }

        public Color GetPixel(int col, int row)
        {
            if((col > _width) || (col < 0))
            {
                OnImageError("Error: Column must be greater than 0 and less than the Width");
            }
            else if((row > _height) || (row < 0))
            {
                OnImageError("Error: Row must be greater than 0 and less than the Height");
            }

            var color = new Color();
            var start = _rowLength * row + col * 4 + 1;

            color.R = Buffer[start];
            color.G = Buffer[start + 1];
            color.B = Buffer[start + 2];
            color.A = Buffer[start + 3];

            return color;
        }

        public Stream GetStream()
        {
            Stream stream;

            if(!Initialized)
            {
                OnImageError("Error: Image has not been initialized");
                stream = null;
            }
            else
            {
                stream = PngEncoder.Encode(Buffer, _width, _height);
            }

            return stream;
        }

        private void OnImageError(string msg)
        {
            if(null == ImageError)
            {
                return;
            }

            var args = new ClientImageErrorEventArgs {ErrorMessage = msg};
            ImageError(this, args);
        }

        #region Nested type: ClientImageErrorEventArgs

        public class ClientImageErrorEventArgs : EventArgs
        {
            private string _errorMessage = string.Empty;
            public string ErrorMessage
            {
                get { return _errorMessage; }
                set { _errorMessage = value; }
            }
        }

        #endregion
    }
}