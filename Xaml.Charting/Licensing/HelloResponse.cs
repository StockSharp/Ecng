namespace Ecng.Xaml.Licensing.Core
{
    public class HelloResponse
    {
        public HelloResponse() { }

        public HelloResponse(string result)
        {
            Result = result;
        }

        public string Result { get; set; }
    }
}