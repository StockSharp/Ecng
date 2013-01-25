#region File License

#endregion

namespace Community.Imaging.Decoders.Gif
{
    public class GifFrame
    {
        #region private fields

        private int _colorSize = 3;
        private GraphicEx _graphicEx;

        #endregion

        #region internal property

        internal Color32 BgColor
        {
            get
            {
                var act = PaletteHelper.GetColor32s(LocalColorTable);
                return act[GraphicExtension.TranIndex];
            }
        }


        internal ImageDescriptor ImageDescriptor { get; set; }


        internal Color32[] Palette
        {
            get
            {
                var act = PaletteHelper.GetColor32s(LocalColorTable);
                if(GraphicExtension != null && GraphicExtension.TransparencyFlag)
                {
                    act[GraphicExtension.TranIndex] = new Color32(0);
                }
                return act;
            }
        }


        public ClientImage Image { get; set; }


        internal int ColorDepth
        {
            get { return _colorSize; }
            set { _colorSize = value; }
        }


        internal byte[] LocalColorTable { get; set; }


        internal GraphicEx GraphicExtension
        {
            get { return _graphicEx; }
            set { _graphicEx = value; }
        }


        internal short Delay
        {
            get { return _graphicEx.Delay; }
            set { _graphicEx.Delay = value; }
        }


        internal byte[] IndexedPixel { get; set; }

        #endregion
    }
}