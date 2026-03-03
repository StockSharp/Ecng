using System;

namespace YandexDisk.Client.Http
{
    /// <summary>
    /// Abstract request sender for testing purpose
    /// </summary>
    public interface IHttpClient: IDisposable
    {
        /// <summary>
        /// Send http-request to API
        /// </summary>
        [ItemNotNull]
        Task<HttpResponseMessage> SendAsync([NotNull] HttpRequestMessage request, CancellationToken cancellationToken = default(CancellationToken));
    }
}
