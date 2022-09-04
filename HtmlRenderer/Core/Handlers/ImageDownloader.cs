// "Therefore those skilled at the unorthodox
// are infinite as heaven and earth,
// inexhaustible as the great rivers.
// When they come to an end,
// they begin again,
// like the days and months;
// they die and are reborn,
// like the four seasons."
// 
// - Sun Tsu,
// "The Art of War"

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using TheArtOfDev.HtmlRenderer.Core.Utils;
using Ecng.Common;

namespace TheArtOfDev.HtmlRenderer.Core.Handlers
{
    /// <summary>
    /// On download file async complete, success or fail.
    /// </summary>
    /// <param name="imageUri">The online image uri</param>
    /// <param name="filePath">the path to the downloaded file</param>
    /// <param name="error">the error if download failed</param>
    /// <param name="canceled">is the file download request was canceled</param>
    public delegate void DownloadFileAsyncCallback(Uri imageUri, byte[] body, Exception error, bool canceled);

    /// <summary>
    /// Handler for downloading images from the web.<br/>
    /// Single instance of the handler used for all images downloaded in a single html, this way if the html contains more
    /// than one reference to the same image it will be downloaded only once.<br/>
    /// Also handles corrupt, partial and canceled downloads by first downloading to temp file and only if successful moving to cached 
    /// file location.
    /// </summary>
    internal sealed class ImageDownloader : IDisposable
    {
		private readonly CancellationTokenSource _cts = new();

        /// <summary>
        /// dictionary of image cache path to callbacks of download to handle multiple requests to download the same image 
        /// </summary>
        private readonly Dictionary<Uri, List<DownloadFileAsyncCallback>> _imageDownloadCallbacks = new();

        /// <summary>
        /// Makes a request to download the image from the server and raises the <see cref="cachedFileCallback"/> when it's down.<br/>
        /// </summary>
        /// <param name="imageUri">The online image uri</param>
        /// <param name="filePath">the path on disk to download the file to</param>
        /// <param name="async">is to download the file sync or async (true-async)</param>
        /// <param name="cachedFileCallback">This callback will be called with local file path. If something went wrong in the download it will return null.</param>
        public void DownloadImage(Uri imageUri, DownloadFileAsyncCallback cachedFileCallback)
        {
            ArgChecker.AssertArgNotNull(imageUri, "imageUri");
            ArgChecker.AssertArgNotNull(cachedFileCallback, "cachedFileCallback");

            // to handle if the file is already been downloaded
            bool download = true;
            lock (_imageDownloadCallbacks)
            {
                if (_imageDownloadCallbacks.ContainsKey(imageUri))
                {
                    download = false;
                    _imageDownloadCallbacks[imageUri].Add(cachedFileCallback);
                }
                else
                {
                    _imageDownloadCallbacks[imageUri] = new() { cachedFileCallback };
                }
            }

            if (download)
            {
				Task.Run(async () =>
				{
					try
					{
						var (cts, token) = _cts.Token.CreateChildToken(TimeSpan.FromSeconds(120));

						var client = HtmlRendererUtils.EnsureGetHttp();
						var content = (await client.GetAsync(imageUri, token)).EnsureSuccessStatusCode().Content;
						var body = new MemoryStream();
						await content.CopyToAsync(body, token);
						body.Position = 0;
						OnDownloadImageCompleted(content.Headers.ContentType.MediaType, imageUri, body.To<byte[]>(), null, false);
					}
					catch (Exception ex)
					{
						OnDownloadImageCompleted(null, imageUri, null, ex, false);
					}
				});
			}
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
			ReleaseObjects();
		}


        #region Private/Protected methods

        /// <summary>
        /// Checks if the file was downloaded and raises the cachedFileCallback from <see cref="_imageDownloadCallbacks"/>
        /// </summary>
        private void OnDownloadImageCompleted(string contentType, Uri source, byte[] image, Exception error, bool cancelled)
        {
            if (!cancelled)
            {
                if (error == null)
                {
                    if (contentType.IsEmpty() || !contentType.StartsWith("image", StringComparison.OrdinalIgnoreCase))
                        error = new Exception("Failed to load image, not image content type: " + contentType);
                }
            }

            List<DownloadFileAsyncCallback> callbacksList;
            lock (_imageDownloadCallbacks)
            {
                if (_imageDownloadCallbacks.TryGetValue(source, out callbacksList))
                    _imageDownloadCallbacks.Remove(source);
            }

            if (callbacksList != null)
            {
                foreach (var cachedFileCallback in callbacksList)
                {
                    try
                    {
                        cachedFileCallback(source, image, error, cancelled);
                    }
                    catch
                    { }
                }
            }
        }

        /// <summary>
        /// Release the image and client objects.
        /// </summary>
        private void ReleaseObjects()
        {
            _imageDownloadCallbacks.Clear();
			_cts.Cancel();
        }

        #endregion
    }
}
