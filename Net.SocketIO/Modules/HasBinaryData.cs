using Newtonsoft.Json.Linq;
using System.Linq;

namespace Ecng.Net.SocketIO.Modules
{
    public static class HasBinaryData
    {
        public static bool HasBinary(object data)
        {
            return RecursiveCheckForBinary(data);
        }

        private static bool RecursiveCheckForBinary(object obj)
        {
            if (obj is null || obj is string)
            {
                return false;
            }

            if (obj is byte[])
            {
                return true;
            }


			if (obj is JArray array)
			{
				if (array.Any(token => RecursiveCheckForBinary(token)))
				{
					return true;
				}
			}

			if (obj is JObject jobject)
			{
				if (jobject.Children().Any(child => RecursiveCheckForBinary(child)))
				{
					return true;
				}
			}

			if (obj is JValue jvalue)
			{
				return RecursiveCheckForBinary(jvalue.Value);
			}

			if (obj is JProperty jprop)
			{
				return RecursiveCheckForBinary(jprop.Value);
			}

			return false;
        }
    }
}
