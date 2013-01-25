namespace Jillzhang.GifUtility
{
    internal class ExData
    {
        private static readonly byte _blockTerminator;
        private static readonly byte _extensionIntroducer = 0x21;

        internal byte ExtensionIntroducer
        {
            get { return _extensionIntroducer; }
        }

        internal byte BlockTerminator
        {
            get { return _blockTerminator; }
        }
    }
}