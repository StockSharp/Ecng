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


            var array = obj as JArray;
            if (array != null)
            {
                if (array.Any(token => RecursiveCheckForBinary(token)))
                {
                    return true;
                }
            }

            var jobject = obj as JObject;
            if (jobject != null)
            {
                if (jobject.Children().Any(child => RecursiveCheckForBinary(child)))
                {
                    return true;
                }
            }

            var jvalue = obj as JValue;
            if (jvalue != null)
            {
                return RecursiveCheckForBinary(jvalue.Value);
            }

            var jprop = obj as JProperty;
            if (jprop != null)
            {
                return RecursiveCheckForBinary(jprop.Value);
            }

            return false;
        }
    }
}
