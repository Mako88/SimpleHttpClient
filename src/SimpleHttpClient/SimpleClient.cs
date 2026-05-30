using SimpleHttpClient.Extensions;
using SimpleHttpClient.Logging;
using SimpleHttpClient.Models;
using SimpleHttpClient.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace SimpleHttpClient
{
    /// <summary>
    /// The primary Simple Http Client
    /// </summary>
    public class SimpleClient : ISimpleClient, IDisposable
    {
        private const double HttpClientReplacementIntervalMs = 300000; // 5 minutes

        // How long a retired HttpClient is kept alive before being disposed, so in-flight
        // requests (and reasonably-lived streams) using it can finish first.
        private const int HttpClientDisposeDelayMs = 300000; // 5 minutes

        private readonly IHttpClientFactory httpClientFactory = null;
        private readonly object httpClientLock = new object();

        private HttpClient httpClient = null;
        private System.Timers.Timer httpClientReplacementTimer = null;
        private bool disposedValue;

        /// <summary>
        /// Creates a SimpleClient instance.
        /// </summary>
        /// <param name="host">The base url all requests sent through this client will use. If not provided, it is assumed that the path property on requests passed to this client will be full URLs.</param>
        /// <param name="httpClientFactory">An IHttpClientFactory to create HttpClients. This is resolved through dependency injection when using the IServiceCollection.AddSimpleHttpClient() extension method.</param>
        /// <param name="serializer">The serializer to convert request/response bodies to types. If not provided, SimpleHttpDefaultJsonSerializer will be used.</param>
        /// <param name="logger">The logger used for logging requests and responses.</param>
        /// <param name="logRequest">An optional method for logging a request. This is called right before the request is sent.</param>
        /// <param name="logResponse">An optional method for logging a response. This is called right after a response is received.</param>
        public SimpleClient(string host = null,
            IHttpClientFactory httpClientFactory = null,
            ISimpleHttpSerializer serializer = null,
            ISimpleHttpLogger logger = null,
            LogRequest logRequest = null,
            LogResponse logResponse = null)
        {
            this.httpClientFactory = httpClientFactory;

            Host = host;
            Serializer = serializer ?? new SimpleHttpDefaultJsonSerializer();
            Logger = logger;
            LogRequest = logRequest;
            LogResponse = logResponse;
        }

        /// <summary>
        /// The base url all requests sent with this client will use.
        /// If not set, it is assumed that the path property on requests passed to this client will be full URLs.
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// The serializer to convert request/response bodies to types. If not provided, SimpleHttpDefaultJsonSerializer will be used.
        /// </summary>
        public ISimpleHttpSerializer Serializer { get; set; }

        /// <summary>
        /// The Logger for logging requests and responses.
        /// </summary>
        public ISimpleHttpLogger Logger { get; set; }

        /// <summary>
        /// Any status codes to be considered successful when setting IsSuccessful in addition to the 200-299 status codes.
        /// This applies to all requests sent with this client.
        /// </summary>
        public List<HttpStatusCode> AdditionalSuccessfulStatusCodes { get; private set; } = new List<HttpStatusCode>();

        /// <summary>
        /// Headers that will be included with all requests sent with this client.
        /// </summary>
        public Dictionary<string, string> DefaultHeaders { get; private set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Timeout in seconds of all requests sent with this client.
        /// To disable the timeout, set to -1.
        /// </summary>
        public int Timeout { get; set; } = 30;

        /// <summary>
        /// An optional action for logging a request.
        /// This is called right before the request is sent.
        /// </summary>
        public LogRequest LogRequest { get; set; }

        /// <summary>
        /// An optional action for logging a response.
        /// This is called right after a response is received.
        /// </summary>
        public LogResponse LogResponse { get; set; }

        /// <summary>
        /// Make an untyped request.
        /// </summary>
        /// <param name="request">The request that will be sent.</param>
        /// <returns>A response object without a strongly-typed body property.</returns>
        public async Task<ISimpleResponse> MakeRequest(ISimpleRequest request, CancellationToken cancellationToken = default) =>
            await MakeRequestInternal(request, new SimpleResponse(request.Id), AddResponseBody, cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Make a typed request.
        /// </summary>
        /// <typeparam name="T">The type the response body will be serialized into.</typeparam>
        /// <param name="request">The request that will be sent.</param>
        /// <returns>A response object with a strongly-typed body property.</returns>
        public async Task<ISimpleResponse<T>> MakeRequest<T>(ISimpleRequest request, CancellationToken cancellationToken = default) =>
            await MakeRequestInternal(request, new SimpleResponse<T>(request.Id), AddResponseBody, cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Make a request and get back the live, unbuffered response stream.
        /// The body is not read into memory; the connection is held open until the
        /// returned ISimpleStreamResponse is disposed, so callers should dispose it
        /// (ideally with a using block) once they're done reading.
        /// </summary>
        /// <param name="request">The request that will be sent.</param>
        /// <param name="cancellationToken">A token to cancel sending the request and reading the response stream.</param>
        /// <returns>A disposable response exposing the raw response stream.</returns>
        public async Task<ISimpleStreamResponse> MakeStreamRequest(ISimpleRequest request, CancellationToken cancellationToken = default)
        {
            var httpRequest = CreateHttpRequest(request);
            AddRequestBody(httpRequest, request);
            ApplyHeaders(httpRequest, request);

            var url = httpRequest.RequestUri.ToString();

            Logger?.LogRequest(url, request);

            if (LogRequest != null)
            {
                LogRequest(url, request);
            }

            // ResponseHeadersRead so SendAsync returns as soon as the headers are
            // available instead of buffering the whole body, which is what lets us stream.
            var httpResponse = await SendHttpRequest(request, httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

            var body = await httpResponse.Content.ReadAsStreamAsync().ConfigureAwait(false);

            // The HttpResponseMessage is handed to the response so its lifetime (and the
            // underlying connection) is controlled by the caller disposing the response.
            var response = new SimpleStreamResponse(httpResponse, body)
            {
                StatusCode = httpResponse.StatusCode,
                IsSuccessful = ResponseIsSuccessful(httpResponse, request.AdditionalSuccessfulStatusCodes),
            };

            PopulateHeaders(httpResponse, response.Headers);

            return response;
        }

        /// <summary>
        /// Get the URL the given request will be sent to by this client.
        /// </summary>
        /// <param name="request">The request to determine the URL for.</param>
        /// <returns>The URL the given request will be made to.</returns>
        public string GetUrl(ISimpleRequest request) => CreateUrl(request);

        /// <summary>
        /// Execute a request.
        /// </summary>
        private async Task<T> MakeRequestInternal<T>(ISimpleRequest request, T response, Func<HttpResponseMessage, T, ISimpleHttpSerializer, Task> addResponseBody, CancellationToken cancellationToken) where T : ISimpleResponse
        {
            var httpRequest = CreateHttpRequest(request);
            AddRequestBody(httpRequest, request);
            ApplyHeaders(httpRequest, request);

            var url = httpRequest.RequestUri.ToString();

            Logger?.LogRequest(url, request);

            if (LogRequest != null)
            {
                LogRequest(url, request);
            }

            var httpResponse = await SendHttpRequest(request, httpRequest, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false);

            PopulateResponse(httpResponse, response, request.AdditionalSuccessfulStatusCodes);
            await addResponseBody(httpResponse, response, request.SerializerOverride ?? Serializer);

            Logger?.LogResponse(response);

            if (LogResponse != null)
            {
                LogResponse(response);
            }

            return response;
        }

        /// <summary>
        /// Send an HttpRequestMessage, applying the request/client timeout to the send and
        /// honoring the caller's cancellation token. A timeout surfaces as a TimeoutException;
        /// a caller-requested cancellation propagates as an OperationCanceledException.
        /// </summary>
        private async Task<HttpResponseMessage> SendHttpRequest(ISimpleRequest request, HttpRequestMessage httpRequest, HttpCompletionOption completionOption, CancellationToken cancellationToken)
        {
            var timeout = request.TimeoutOverride ?? Timeout;

            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                if (timeout != -1)
                {
                    cts.CancelAfter(TimeSpan.FromSeconds(timeout));
                }

                try
                {
                    return await GetHttpClient().SendAsync(httpRequest, completionOption, cts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    throw new TimeoutException($"Request timed out after {timeout} seconds");
                }
            }
        }

        /// <summary>
        /// Create an HttpRequestMessage for use with HttpClient from an IRequest.
        /// </summary>
        private HttpRequestMessage CreateHttpRequest(ISimpleRequest request)
        {
            var url = CreateUrl(request);

            var httpRequest = new HttpRequestMessage(request.Method, url);

            // Resolve a custom Content-Type header before the body is built so the body
            // content is created with the correct content type. The remaining headers are
            // applied after the body exists (see ApplyHeaders), which lets content-level
            // headers be routed to the body content where HttpClient requires them.
            var headers = MergeHeaders(request);

            // Only update Content-Type if it's still the default value
            // so we don't overwrite a custom Content-Type on the request
            if (headers.TryGetValue("Content-Type", out var contentType) && request.ContentType == Constants.DefaultContentType)
            {
                request.ContentType = contentType;
            }

            return httpRequest;
        }

        /// <summary>
        /// Merge the request headers with the client's default headers, with the request's
        /// headers taking precedence on any conflicts.
        /// </summary>
        private Dictionary<string, string> MergeHeaders(ISimpleRequest request) =>
            request.Headers.Concat(DefaultHeaders.Where(x => !request.Headers.Keys.Contains(x.Key)))
                .ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Apply the merged request/default headers to the HttpRequestMessage. Must be called
        /// after the request body has been set so content-level headers can be applied to it.
        /// </summary>
        private void ApplyHeaders(HttpRequestMessage httpRequest, ISimpleRequest request)
        {
            var headers = MergeHeaders(request);

            if (!headers.Keys.Contains("User-Agent", StringComparer.OrdinalIgnoreCase))
            {
                httpRequest.Headers.TryAddWithoutValidation("User-Agent", Constants.DefaultUserAgent);
            }

            foreach (var header in headers)
            {
                // Content-Type is applied to the request body content (see AddRequestBody), not here.
                if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // TryAddWithoutValidation avoids throwing on header values HttpClient would
                // otherwise reject. Content-level headers can't go on the request headers, so
                // fall back to applying them to the body content where they belong.
                if (!httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value))
                {
                    httpRequest.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }
        }

        /// <summary>
        /// Add a body to the request.
        /// </summary>
        private void AddRequestBody(HttpRequestMessage httpRequest, ISimpleRequest request)
        {
            if (request.FormUrlEncodedParameters.Any())
            {
                httpRequest.Content = new FormUrlEncodedContent(request.FormUrlEncodedParameters);
            }
            else if (!string.IsNullOrEmpty(request.StringBody))
            {
                httpRequest.Content = new StringContent(request.StringBody, request.ContentEncoding, request.ContentType);
            }
            else if (request.Body != null)
            {
                var serializer = request.SerializerOverride ?? Serializer;

                var serializedBody = serializer.Serialize(request.Body);

                httpRequest.Content = new StringContent(serializedBody, request.ContentEncoding, request.ContentType);

                // Set StringBody to the serialized body for more accurate logging
                request.StringBody = serializedBody;
            }
        }

        /// <summary>
        /// Add the body to the response.
        /// </summary>
        private async Task AddResponseBody(HttpResponseMessage httpResponse, ISimpleResponse response, ISimpleHttpSerializer serializer)
        {
            response.StringBody = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

            response.ByteBody = await httpResponse.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Add the body to the response.
        /// </summary>
        private async Task AddResponseBody<T>(HttpResponseMessage httpResponse, ISimpleResponse<T> response, ISimpleHttpSerializer serializer)
        {
            await AddResponseBody(httpResponse, (ISimpleResponse) response, serializer).ConfigureAwait(false);

            try
            {
                response.Body = serializer.Deserialize<T>(response.StringBody);
            }
            catch (Exception ex)
            {
                response.SerializationException = ex;
            }
        }

        /// <summary>
        /// Create a response from an httpResponse.
        /// </summary>
        private void PopulateResponse(HttpResponseMessage httpResponse, ISimpleResponse response, IEnumerable<HttpStatusCode> successfulStatusCodes)
        {
            PopulateHeaders(httpResponse, response.Headers);

            response.StatusCode = httpResponse.StatusCode;
            response.IsSuccessful = ResponseIsSuccessful(httpResponse, successfulStatusCodes);
        }

        /// <summary>
        /// Copy the response and content headers from an httpResponse into the given dictionary.
        /// </summary>
        private void PopulateHeaders(HttpResponseMessage httpResponse, Dictionary<string, string> headers)
        {
            foreach (var header in httpResponse.Headers.Concat(httpResponse.Content.Headers))
            {
                var value = string.Join(", ", header.Value);

                // Use the indexer rather than Add so a header appearing in both the response
                // and content header collections doesn't throw on a duplicate key.
                headers[header.Key] = value;
            }
        }

        /// <summary>
        /// Determine whether an httpResponse should be considered successful, taking into
        /// account both the client-level and request-level additional successful status codes.
        /// </summary>
        private bool ResponseIsSuccessful(HttpResponseMessage httpResponse, IEnumerable<HttpStatusCode> successfulStatusCodes) =>
            httpResponse.IsSuccessStatusCode ||
            AdditionalSuccessfulStatusCodes.Concat(successfulStatusCodes).Any(x => x == httpResponse.StatusCode);

        /// <summary>
        /// Create a URL for the given request.
        /// </summary>
        private string CreateUrl(ISimpleRequest request)
        {
            var url = request.UrlOverride.HasValue() ? request.UrlOverride : CombineUrls(Host, request.Path);

            var builder = new UriBuilder(url);
            var query = HttpUtility.ParseQueryString(builder.Query);

            foreach (var parameter in request.QueryStringParameters)
            {
                query[parameter.Key] = parameter.Value;
            }

            builder.Query = query.ToString();

            return builder.ToString();
        }

        /// <summary>
        /// Combine two urls, handling slashes.
        /// </summary>
        private string CombineUrls(string url1, string url2)
        {
            if (!url1.HasValue())
            {
                return url2;
            }

            if (!url2.HasValue())
            {
                return url1;
            }

            url1 = url1.TrimEnd('/', '\\');
            url2 = url2.TrimStart('/', '\\');

            return $"{url1}/{url2}";
        }

        /// <summary>
        /// Try to get an HttpClient using best practices (which don't actually exist for .NET Standard 2.0 projects - See Ref 1).
        /// Ref 1: https://github.com/dotnet/aspnetcore/issues/28385#issuecomment-853766480
        /// Ref 2: https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient-guidelines
        /// Ref 3: https://www.siakabaro.com/how-to-manage-httpclient-connections-in-net/
        /// Ref 4: https://github.com/dotnet/runtime/issues/18348
        /// </summary>
        private HttpClient GetHttpClient()
        {
            // If we don't have a factory, all we can do is new up an HttpClient.
            // Per Ref 3, this will cause DNS issues for long-lived connections so
            // we replace the httpClient instance every 5 minutes, per Ref 4
            if (httpClientFactory == null)
            {
                lock (httpClientLock)
                {
                    SetupHttpClientReplacementTimerIfNeeded(false);

                    if (httpClient == null)
                    {
                        httpClient = CreateConfiguredHttpClient();
                    }

                    return httpClient;
                }
            }

            // Per Ref 2, don't create a new HttpClient for each request on .NET Framework
            if (RuntimeInformation.FrameworkDescription.Contains("Framework", StringComparison.OrdinalIgnoreCase))
            {
                lock (httpClientLock)
                {
                    // Since this is a long-lived client, we need to setup
                    // periodic replacement using the factory, per Ref 4
                    SetupHttpClientReplacementTimerIfNeeded(true);

                    if (httpClient == null)
                    {
                        httpClient = httpClientFactory.CreateClient(Constants.HttpClientNameString);
                    }

                    return httpClient;
                }
            }

            return httpClientFactory.CreateClient(Constants.HttpClientNameString);
        }

        /// <summary>
        /// Create and configure a new HttpClient with the opinionated default handler.
        /// </summary>
        private HttpClient CreateConfiguredHttpClient()
        {
            var handler = HttpClientConfigurator.GetMessageHandler();

            var client = new HttpClient(handler);

            HttpClientConfigurator.ConfigureHttpClient(client);

            return client;
        }

        /// <summary>
        /// Setup the HttpClient replacement timer if it hasn't already been setup.
        /// Callers must hold httpClientLock.
        /// </summary>
        private void SetupHttpClientReplacementTimerIfNeeded(bool shouldUseFactory)
        {
            if (httpClientReplacementTimer == null)
            {
                httpClientReplacementTimer = new System.Timers.Timer();
                httpClientReplacementTimer.Elapsed += (sender, e) => ReplaceHttpClient(shouldUseFactory);
                httpClientReplacementTimer.Interval = HttpClientReplacementIntervalMs;
                httpClientReplacementTimer.AutoReset = true;
                httpClientReplacementTimer.Start();
            }
        }

        /// <summary>
        /// Update the HttpClient instance with a new one to prevent DNS going stale.
        /// </summary>
        private void ReplaceHttpClient(bool shouldUseFactory)
        {
            HttpClient retiredClient;

            lock (httpClientLock)
            {
                retiredClient = httpClient;

                httpClient = shouldUseFactory
                    ? httpClientFactory.CreateClient(Constants.HttpClientNameString)
                    : CreateConfiguredHttpClient();
            }

            // Only dispose clients we own. Factory-created clients are managed by the factory,
            // which pools and rotates their handlers, so we must not dispose those ourselves.
            if (retiredClient != null && !shouldUseFactory)
            {
                ScheduleRetiredClientDisposal(retiredClient);
            }
        }

        /// <summary>
        /// Dispose a retired HttpClient after a grace period so that in-flight requests using
        /// it can complete. Note that requests (or streams) still running after the grace period
        /// will be aborted when the retired client is disposed.
        /// </summary>
        private void ScheduleRetiredClientDisposal(HttpClient retiredClient) =>
            Task.Delay(HttpClientDisposeDelayMs).ContinueWith(_ => retiredClient.Dispose());

        /// <summary>
        /// Dispose.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    httpClientReplacementTimer?.Stop();
                    httpClientReplacementTimer?.Dispose();
                    httpClient?.Dispose();
                }

                disposedValue = true;
            }
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// The method used when logging a request.
    /// </summary>
    /// <param name="url">The URL the request is pointing to.</param>
    /// <param name="request">The request object to log.</param>
    public delegate void LogRequest(string url, ISimpleRequest request);

    /// <summary>
    /// The method used when logging a response.
    /// </summary>
    /// <param name="response">The response object to log.</param>
    public delegate void LogResponse(ISimpleResponse response);
}
