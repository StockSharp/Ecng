// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NET45 || NET461 || NET48 || NETCOREAPP || NETSTANDARD1_3 || NETSTANDARD

using System;
#if !NETFX_CORE && !PORTABLE
using System.Security.Cryptography.X509Certificates;
#endif
using System.Net.Http;

namespace Microsoft.AspNet.SignalR.Client.Http
{
#if NET45 || NET461 || NET48
    public class DefaultHttpHandler : WebRequestHandler
#elif NETSTANDARD1_3 || NETSTANDARD || NETCOREAPP
    public class DefaultHttpHandler : HttpClientHandler
#else
#error Unsupported target framework.
#endif
    {
        private readonly IConnection _connection;

        public DefaultHttpHandler(IConnection connection)
        {
            if (connection != null)
            {
                _connection = connection;
            }
            else
            {
                throw new ArgumentNullException("connection");
            }

            Credentials = _connection.Credentials;
            PreAuthenticate = true;

            if (_connection.CookieContainer != null)
            {
                CookieContainer = _connection.CookieContainer;
            }

            if (_connection.Proxy != null)
            {
                Proxy = _connection.Proxy;
            }

            foreach (X509Certificate cert in _connection.Certificates)
            {
                ClientCertificates.Add(cert);
            }
        }
    }
}

#elif NET40
// Not required on this framework.
#else 
#error Unsupported target framework.
#endif

