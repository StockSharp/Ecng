using System;
using System.Net;
#if NETFRAMEWORK
using System.Runtime.InteropServices;
#endif
using System.Security;

namespace xNet
{
    [SuppressUnmanagedCodeSecurityAttribute]
    internal static class SafeNativeMethods
    {
        [Flags]
        internal enum InternetConnectionState : int
        {
            INTERNET_CONNECTION_MODEM = 0x1,
            INTERNET_CONNECTION_LAN = 0x2,
            INTERNET_CONNECTION_PROXY = 0x4,
            INTERNET_RAS_INSTALLED = 0x10,
            INTERNET_CONNECTION_OFFLINE = 0x20,
            INTERNET_CONNECTION_CONFIGURED = 0x40
        }

#if NETFRAMEWORK
        [DllImport("wininet.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        internal static extern bool InternetGetConnectedState(
            ref InternetConnectionState lpdwFlags, int dwReserved);
#else
        internal static bool InternetGetConnectedState(ref InternetConnectionState lpdwFlags, int dwReserved)
        {
            lpdwFlags = InternetConnectionState.INTERNET_CONNECTION_OFFLINE;

            try
            {
                using var client = new WebClient();
                using (client.OpenRead("http://google.com/generate_204"))
                {
                    lpdwFlags = InternetConnectionState.INTERNET_CONNECTION_LAN;
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
#endif
    }
}