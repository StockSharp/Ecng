using Community.Imaging.Decoders.Gif;

namespace Community.Imaging.Decoders.Gif
{
    internal class PaletteHelper
    {
        internal static Color32[] GetColor32s(byte[] table)
        {
            var tab = new Color32[table.Length / 3];
            var i = 0;
            var j = 0;
            while (i < table.Length)
            {
                var r = table[i++];
                var g = table[i++];
                var b = table[i++];
                const byte a = 255;
                var c = new Color32(a, r, g, b);
                tab[j++] = c;
            }
            return tab;
        }
    }
}