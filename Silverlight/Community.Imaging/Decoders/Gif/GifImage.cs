using System.Collections.Generic;
using Community.Imaging.Decoders.Gif;

namespace Community.Imaging.Decoders.Gif
{
    public class GifImage
    {
        public short Width
        {
            get { return lcd.Width; }
        }

        public short Height
        {
            get { return lcd.Height; }
        }

        private string header = "";

        public string Header
        {
            get { return header; }
            set { header = value; }
        }

        public byte[] GlobalColorTable { get; set; }

        internal Color32[] Palette
        {
            get
            {
                var act = PaletteHelper.GetColor32s(GlobalColorTable);
                act[lcd.BgColorIndex] = new Color32(0);
                return act;
            }
        }

        private List<CommentEx> comments = new List<CommentEx>();
        internal List<CommentEx> CommentExtensions
        {
            get { return comments; }
            set { comments = value; }
        }

        private List<ApplicationEx> applictions = new List<ApplicationEx>();
        internal List<ApplicationEx> ApplictionExtensions
        {
            get { return applictions; }
            set { applictions = value; }
        }
        
        private List<PlainTextEx> texts = new List<PlainTextEx>();
        internal List<PlainTextEx> PlainTextEntensions
        {
            get { return texts; }
            set { texts = value; }
        }

        private LogicalScreenDescriptor lcd;
        internal LogicalScreenDescriptor LogicalScreenDescriptor
        {
            get { return lcd; }
            set { lcd = value; }
        }

        private List<GifFrame> frames = new List<GifFrame>();
        public List<GifFrame> Frames
        {
            get { return frames; }
            set { frames = value; }
        }
    }
}