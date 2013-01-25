namespace Community.Imaging.Decoders.Gif
{
    internal class GifExtensions
    {
        internal const byte ApplicationExtensionLabel = 0xFF;
        internal const byte CommentLabel = 0xFE;
        internal const byte EndIntroducer = 0x3B;
        internal const byte ExtensionIntroducer = 0x21;
        internal const byte GraphicControlLabel = 0xF9;
        internal const byte ImageDescriptorLabel = 0x2C;
        internal const byte ImageLabel = 0x2C;
        internal const byte PlainTextLabel = 0x01;
        internal const byte Terminator = 0;
    }
}