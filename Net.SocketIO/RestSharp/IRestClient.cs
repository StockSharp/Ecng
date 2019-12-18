﻿#region License

//   Copyright 2010 John Sheehan
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License. 

#endregion

using System;
using System.Net;
using System.Collections.Generic;
using System.Text;
using RestSharp.Authenticators;
using RestSharp.Deserializers;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Cache;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using RestSharp.Serialization;

namespace RestSharp
{
    public interface IRestClient
    {
        [Obsolete("Use the overload that accepts the delegate factory")]
        IRestClient UseSerializer(IRestSerializer serializer);

        CookieContainer CookieContainer { get; set; }

        bool AutomaticDecompression { get; set; }

        int? MaxRedirects { get; set; }

        string UserAgent { get; set; }

        int Timeout { get; set; }

        int ReadWriteTimeout { get; set; }

        bool UseSynchronizationContext { get; set; }

        IAuthenticator Authenticator { get; set; }

        Uri BaseUrl { get; set; }

        Encoding Encoding { get; set; }

        bool FailOnDeserializationError { get; set; }
        
        string ConnectionGroupName { get; set; }

        bool PreAuthenticate { get; set; }

        bool UnsafeAuthenticatedConnectionSharing { get; set; }

        IList<Parameter> DefaultParameters { get; }

        string BaseHost { get; set; }

        bool AllowMultipleDefaultParametersWithSameName { get; set; }

        [Obsolete("This method will be removed soon in favour of the proper async call")]
        RestRequestAsyncHandle ExecuteAsync(IRestRequest request,
            Action<IRestResponse, RestRequestAsyncHandle> callback);

        [Obsolete("This method will be removed soon in favour of the proper async call")]
        RestRequestAsyncHandle ExecuteAsync<T>(IRestRequest request,
            Action<IRestResponse<T>, RestRequestAsyncHandle> callback);

        [Obsolete("This method will be removed soon in favour of the proper async call")]
        RestRequestAsyncHandle ExecuteAsync(IRestRequest request,
            Action<IRestResponse, RestRequestAsyncHandle> callback, Method httpMethod);

        [Obsolete("This method will be removed soon in favour of the proper async call")]
        RestRequestAsyncHandle ExecuteAsync<T>(IRestRequest request,
            Action<IRestResponse<T>, RestRequestAsyncHandle> callback, Method httpMethod);

        IRestResponse<T> Deserialize<T>(IRestResponse response);
        
        /// <summary>
        /// Allows to use a custom way to encode URL parameters
        /// </summary>
        /// <param name="encoder">A delegate to encode URL parameters</param>
        /// <example>client.UseUrlEncoder(s => HttpUtility.UrlEncode(s));</example>
        /// <returns></returns>
        IRestClient UseUrlEncoder(Func<string, string> encoder);

        /// <summary>
        /// Allows to use a custom way to encode query parameters
        /// </summary>
        /// <param name="queryEncoder">A delegate to encode query parameters</param>
        /// <example>client.UseUrlEncoder((s, encoding) => HttpUtility.UrlEncode(s, encoding));</example>
        /// <returns></returns>
        IRestClient UseQueryEncoder(Func<string, Encoding, string> queryEncoder);
            
        IRestResponse Execute(IRestRequest request);

        IRestResponse Execute(IRestRequest request, Method httpMethod);

        IRestResponse<T> Execute<T>(IRestRequest request) where T : new();

        IRestResponse<T> Execute<T>(IRestRequest request, Method httpMethod) where T : new();

        byte[] DownloadData(IRestRequest request);

        byte[] DownloadData(IRestRequest request, bool throwOnError);

        /// <summary>
        /// X509CertificateCollection to be sent with request
        /// </summary>
        X509CertificateCollection ClientCertificates { get; set; }

        IWebProxy Proxy { get; set; }

        RequestCachePolicy CachePolicy { get; set; }

        bool Pipelined { get; set; }

        bool FollowRedirects { get; set; }

        Uri BuildUri(IRestRequest request);

        string BuildUriWithoutQueryParameters(IRestRequest request);

        /// <summary>
        /// Callback function for handling the validation of remote certificates. Useful for certificate pinning and
        /// overriding certificate errors in the scope of a request.
        /// </summary>
        RemoteCertificateValidationCallback RemoteCertificateValidationCallback { get; set; }

        /// <summary>
        /// Executes a GET-style request and callback asynchronously, authenticating if needed
        /// </summary>
        /// <param name="request">Request to be executed</param>
        /// <param name="callback">Callback function to be executed upon completion providing access to the async handle.</param>
        /// <param name="httpMethod">The HTTP method to execute</param>
        [Obsolete("This method will be removed soon in favour of the proper async call")]
        RestRequestAsyncHandle ExecuteAsyncGet(IRestRequest request, Action<IRestResponse,
            RestRequestAsyncHandle> callback, string httpMethod);

        /// <summary>
        /// Executes a POST-style request and callback asynchronously, authenticating if needed
        /// </summary>
        /// <param name="request">Request to be executed</param>
        /// <param name="callback">Callback function to be executed upon completion providing access to the async handle.</param>
        /// <param name="httpMethod">The HTTP method to execute</param>
        [Obsolete("This method will be removed soon in favour of the proper async call")]
        RestRequestAsyncHandle ExecuteAsyncPost(IRestRequest request, Action<IRestResponse,
            RestRequestAsyncHandle> callback, string httpMethod);

        /// <summary>
        /// Executes a GET-style request and callback asynchronously, authenticating if needed
        /// </summary>
        /// <typeparam name="T">Target deserialization type</typeparam>
        /// <param name="request">Request to be executed</param>
        /// <param name="callback">Callback function to be executed upon completion</param>
        /// <param name="httpMethod">The HTTP method to execute</param>
        [Obsolete("This method will be removed soon in favour of the proper async call")]
        RestRequestAsyncHandle ExecuteAsyncGet<T>(IRestRequest request, Action<IRestResponse<T>,
            RestRequestAsyncHandle> callback, string httpMethod);

        /// <summary>
        /// Executes a GET-style request and callback asynchronously, authenticating if needed
        /// </summary>
        /// <typeparam name="T">Target deserialization type</typeparam>
        /// <param name="request">Request to be executed</param>
        /// <param name="callback">Callback function to be executed upon completion</param>
        /// <param name="httpMethod">The HTTP method to execute</param>
        [Obsolete("This method will be removed soon in favour of the proper async call")]
        RestRequestAsyncHandle ExecuteAsyncPost<T>(IRestRequest request, Action<IRestResponse<T>,
            RestRequestAsyncHandle> callback, string httpMethod);

        /// <summary>
        /// Add a delegate to apply custom configuration to HttpWebRequest before making a call
        /// </summary>
        /// <param name="configurator">Configuration delegate for HttpWebRequest</param>
        void ConfigureWebRequest(Action<HttpWebRequest> configurator);

        /// <summary>
        /// Adds or replaces a deserializer for the specified content type
        /// </summary>
        /// <param name="contentType">Content type for which the deserializer will be replaced</param>
        /// <param name="deserializer">Custom deserializer</param>
        [Obsolete("Use the overload that accepts a factory delegate")]
        void AddHandler(string contentType, IDeserializer deserializer);

        /// <summary>
        /// Adds or replaces a deserializer for the specified content type
        /// </summary>
        /// <param name="contentType">Content type for which the deserializer will be replaced</param>
        /// <param name="deserializerFactory">Custom deserializer factory</param>
        void AddHandler(string contentType, Func<IDeserializer> deserializerFactory);

        /// <summary>
        /// Removes custom deserialzier for the specified content type
        /// </summary>
        /// <param name="contentType">Content type for which deserializer needs to be removed</param>
        void RemoveHandler(string contentType);

        /// <summary>
        /// Remove deserializers for all content types
        /// </summary>
        void ClearHandlers();

        IRestResponse ExecuteAsGet(IRestRequest request, string httpMethod);

        IRestResponse ExecuteAsPost(IRestRequest request, string httpMethod);

        IRestResponse<T> ExecuteAsGet<T>(IRestRequest request, string httpMethod) where T : new();

        IRestResponse<T> ExecuteAsPost<T>(IRestRequest request, string httpMethod) where T : new();

        /// <summary>
        /// Executes the request and callback asynchronously, authenticating if needed
        /// </summary>
        /// <typeparam name="T">Target deserialization type</typeparam>
        /// <param name="request">Request to be executed</param>
        /// <param name="token">The cancellation token</param>
        [Obsolete("This method will be renamed to ExecuteAsync soon")]
        Task<IRestResponse<T>> ExecuteTaskAsync<T>(IRestRequest request, CancellationToken token);

        /// <summary>
        /// Executes the request asynchronously, authenticating if needed
        /// </summary>
        /// <typeparam name="T">Target deserialization type</typeparam>
        /// <param name="request">Request to be executed</param>
        /// <param name="httpMethod">Override the request method</param>
        [Obsolete("This method will be renamed to ExecuteAsync soon")]
        Task<IRestResponse<T>> ExecuteTaskAsync<T>(IRestRequest request, Method httpMethod);

        /// <summary>
        /// Executes the request asynchronously, authenticating if needed
        /// </summary>
        /// <typeparam name="T">Target deserialization type</typeparam>
        /// <param name="request">Request to be executed</param>
        [Obsolete("This method will be renamed to ExecuteAsync soon")]
        Task<IRestResponse<T>> ExecuteTaskAsync<T>(IRestRequest request);

        /// <summary>
        /// Executes a GET-style request asynchronously, authenticating if needed
        /// </summary>
        /// <typeparam name="T">Target deserialization type</typeparam>
        /// <param name="request">Request to be executed</param>
        [Obsolete("This method will be renamed to ExecuteGetAsync soon")]
        Task<IRestResponse<T>> ExecuteGetTaskAsync<T>(IRestRequest request);

        /// <summary>
        /// Executes a GET-style request asynchronously, authenticating if needed
        /// </summary>
        /// <typeparam name="T">Target deserialization type</typeparam>
        /// <param name="request">Request to be executed</param>
        /// <param name="token">The cancellation token</param>
        [Obsolete("This method will be renamed to ExecuteGetAsync soon")]
        Task<IRestResponse<T>> ExecuteGetTaskAsync<T>(IRestRequest request, CancellationToken token);

        /// <summary>
        /// Executes a POST-style request asynchronously, authenticating if needed
        /// </summary>
        /// <typeparam name="T">Target deserialization type</typeparam>
        /// <param name="request">Request to be executed</param>
        [Obsolete("This method will be renamed to ExecutePostAsync soon")]
        Task<IRestResponse<T>> ExecutePostTaskAsync<T>(IRestRequest request);

        /// <summary>
        /// Executes a POST-style request asynchronously, authenticating if needed
        /// </summary>
        /// <typeparam name="T">Target deserialization type</typeparam>
        /// <param name="request">Request to be executed</param>
        /// <param name="token">The cancellation token</param>
        [Obsolete("This method will be renamed to ExecutePostAsync soon")]
        Task<IRestResponse<T>> ExecutePostTaskAsync<T>(IRestRequest request, CancellationToken token);

        /// <summary>
        /// Executes the request and callback asynchronously, authenticating if needed
        /// </summary>
        /// <param name="request">Request to be executed</param>
        /// <param name="token">The cancellation token</param>
        [Obsolete("This method will be renamed to ExecuteAsync soon")]
        Task<IRestResponse> ExecuteTaskAsync(IRestRequest request, CancellationToken token);

        /// <summary>
        /// Executes the request and callback asynchronously, authenticating if needed
        /// </summary>
        /// <param name="request">Request to be executed</param>
        /// <param name="token">The cancellation token</param>
        /// <param name="httpMethod">Override the request method</param>
        [Obsolete("This method will be renamed to ExecuteAsync soon")]
        Task<IRestResponse> ExecuteTaskAsync(IRestRequest request, CancellationToken token, Method httpMethod);

        /// <summary>
        /// Executes the request asynchronously, authenticating if needed
        /// </summary>
        /// <param name="request">Request to be executed</param>
        [Obsolete("This method will be renamed to ExecuteAsync soon")]
        Task<IRestResponse> ExecuteTaskAsync(IRestRequest request);

        /// <summary>
        /// Executes a GET-style asynchronously, authenticating if needed
        /// </summary>
        /// <param name="request">Request to be executed</param>
        [Obsolete("This method will be renamed to ExecuteGetAsync soon")]
        Task<IRestResponse> ExecuteGetTaskAsync(IRestRequest request);

        /// <summary>
        /// Executes a GET-style asynchronously, authenticating if needed
        /// </summary>
        /// <param name="request">Request to be executed</param>
        /// <param name="token">The cancellation token</param>
        [Obsolete("This method will be renamed to ExecuteGetAsync soon")]
        Task<IRestResponse> ExecuteGetTaskAsync(IRestRequest request, CancellationToken token);

        /// <summary>
        /// Executes a POST-style asynchronously, authenticating if needed
        /// </summary>
        /// <param name="request">Request to be executed</param>
        [Obsolete("This method will be renamed to ExecutePostAsync soon")]
        Task<IRestResponse> ExecutePostTaskAsync(IRestRequest request);

        /// <summary>
        /// Executes a POST-style asynchronously, authenticating if needed
        /// </summary>
        /// <param name="request">Request to be executed</param>
        /// <param name="token">The cancellation token</param>
        [Obsolete("This method will be renamed to ExecutePostAsync soon")]
        Task<IRestResponse> ExecutePostTaskAsync(IRestRequest request, CancellationToken token);
    }
}