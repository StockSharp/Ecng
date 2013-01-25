using System.Runtime.InteropServices;
using System.Windows.Media;

namespace Community.Imaging.Decoders.Gif
{
    [StructLayout(LayoutKind.Explicit)]
    internal struct Color32
    {
        [FieldOffset(0)] internal byte Blue;
        [FieldOffset(1)] internal byte Green;
        [FieldOffset(2)] internal byte Red;
        [FieldOffset(3)] internal byte Alpha;
        [FieldOffset(0)] internal int ARGB;
        
        internal Color Color
        {
            get { return Color.FromArgb(Alpha, Red, Green, Blue); }
        }

        internal Color32(int c)
        {
            Alpha = 0;
            Red = 0;
            Green = 0;
            Blue = 0;
            ARGB = c;
        }

        internal Color32(byte a, byte r, byte g, byte b)
        {
            ARGB = 0;
            Alpha = a;
            Red = r;
            Green = g;
            Blue = b;
        }
    }
}