namespace Community.Imaging.Decoders.Png
{
    public interface IPngImageOutput
    {
        int Width { get; }
        int Height { get; }

        void Start(PngDecoder png, int width, int height);
        void WriteLine(byte[] data, int offset);
        void Finish();
    }
}