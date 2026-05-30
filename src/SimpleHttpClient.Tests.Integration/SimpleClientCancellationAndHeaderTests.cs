using SimpleHttpClient.Models;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace SimpleHttpClient.Tests
{
    public class SimpleClientCancellationAndHeaderTests
    {
        [Fact]
        public async Task MakeRequest_WithCancelledToken_ThrowsOperationCanceled()
        {
            var server = WireMockServer.Start();
            server.Given(Request.Create().WithPath("/get").UsingGet())
                .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.OK).WithBody("ok"));

            var client = new SimpleClient(server.Url);
            var request = new SimpleRequest("/get");

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // A caller-requested cancellation should surface as OperationCanceledException,
            // not be misreported as a TimeoutException like the timeout path is.
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                async () => await client.MakeRequest(request, cts.Token));

            server.Stop();
        }

        [Fact]
        public async Task MakeRequestTyped_WithCancelledToken_ThrowsOperationCanceled()
        {
            var server = WireMockServer.Start();
            server.Given(Request.Create().WithPath("/get").UsingGet())
                .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.OK).WithBodyAsJson(new { value = "ok" }));

            var client = new SimpleClient(server.Url);
            var request = new SimpleRequest("/get");

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                async () => await client.MakeRequest<object>(request, cts.Token));

            server.Stop();
        }

        [Fact]
        public async Task Request_WithCustomRequestHeader_IsSent()
        {
            var server = WireMockServer.Start();
            server.Given(Request.Create().WithPath("/get").UsingGet())
                .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.OK).WithBody("ok"));

            var client = new SimpleClient(server.Url);
            var request = new SimpleRequest("/get");
            request.Headers["X-Custom"] = "custom-value";

            var response = await client.MakeRequest(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var received = server.LogEntries.First().RequestMessage;
            Assert.Contains(received.Headers.Keys, k => k.Equals("X-Custom", StringComparison.OrdinalIgnoreCase));

            server.Stop();
        }

        [Fact]
        public async Task Request_WithContentLevelHeader_DoesNotThrow_AndIsSent()
        {
            var server = WireMockServer.Start();
            server.Given(Request.Create().WithPath("/post").UsingPost())
                .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.OK).WithBody("ok"));

            var client = new SimpleClient(server.Url);
            var request = new SimpleRequest("/post", HttpMethod.Post, new { value = "test" });

            // Content-Disposition is a content-level header; setting it on the request
            // previously threw because it can't be added to the request headers collection.
            request.Headers["Content-Disposition"] = "form-data; name=\"field\"";

            var response = await client.MakeRequest(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var received = server.LogEntries.First().RequestMessage;
            Assert.Contains(received.Headers.Keys, k => k.Equals("Content-Disposition", StringComparison.OrdinalIgnoreCase));

            server.Stop();
        }
    }
}
