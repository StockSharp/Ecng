using System;

namespace Ecng.Net.BBCodes
{
    [Serializable]
    public class BBCodeParsingException : Exception
    {
        public BBCodeParsingException()
        {
        }
        public BBCodeParsingException(string message)
            : base(message)
        {
        }
    }
}