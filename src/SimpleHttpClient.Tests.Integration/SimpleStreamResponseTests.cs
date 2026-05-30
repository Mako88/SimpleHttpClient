using SimpleHttpClient.Models;
using System.Net;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace SimpleHttpClient.Tests
{
    public class SimpleStreamResponseTests
    {
        [Fact]
        public async Task StreamRequest_ReturnsReadableBodyStream()
        {
            var server = WireMockServer.Start();

            server.Given(Request.Create().WithPath("/stream").UsingGet())
                .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.OK).WithBody("line1\nline2\nline3"));

            var client = new SimpleClient(server.Url);
            var request = new SimpleRequest("/stream");

            using var response = await client.MakeStreamRequest(request);

            using var reader = new StreamReader(response.Body);
            var body = await reader.ReadToEndAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(response.IsSuccessful);
            Assert.Equal("line1\nline2\nline3", body);

            server.Stop();
        }

        [Fact]
        public async Task StreamRequest_PopulatesHeaders()
        {
            var server = WireMockServer.Start();

            server.Given(Request.Create().WithPath("/stream").UsingGet())
                .RespondWith(Response.Create()
                    .WithStatusCode(HttpStatusCode.OK)
                    .WithHeader("X-Custom-Header", "customValue")
                    .WithBody("body"));

            var client = new SimpleClient(server.Url);
            var request = new SimpleRequest("/stream");

            using var response = await client.MakeStreamRequest(request);

            Assert.True(response.Headers.ContainsKey("X-Custom-Header"));
            Assert.Equal("customValue", response.Headers["X-Custom-Header"]);

            server.Stop();
        }

        [Fact]
        public async Task StreamRequest_HonorsAdditionalSuccessfulStatusCodes()
        {
            var server = WireMockServer.Start();

            server.Given(Request.Create().WithPath("/stream").UsingGet())
                .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.NotFound).WithBody("nope"));

            var client = new SimpleClient(server.Url);
            var request = new SimpleRequest("/stream");
            request.AdditionalSuccessfulStatusCodes.Add(HttpStatusCode.NotFound);

            using var response = await client.MakeStreamRequest(request);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.True(response.IsSuccessful);

            server.Stop();
        }

        [Fact]
        public async Task StreamRequest_WithCancelledToken_ThrowsOperationCanceled()
        {
            var server = WireMockServer.Start();

            server.Given(Request.Create().WithPath("/stream").UsingGet())
                .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.OK).WithBody("body"));

            var client = new SimpleClient(server.Url);
            var request = new SimpleRequest("/stream");

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // A caller-requested cancellation should surface as OperationCanceledException,
            // not be misreported as a TimeoutException like the timeout path is.
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                async () => await client.MakeStreamRequest(request, cts.Token));

            server.Stop();
        }

        [Fact]
        public async Task StreamRequest_Dispose_IsCleanAndIdempotent()
        {
            var server = WireMockServer.Start();

            server.Given(Request.Create().WithPath("/stream").UsingGet())
                .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.OK).WithBody("body"));

            var client = new SimpleClient(server.Url);
            var request = new SimpleRequest("/stream");

            var response = await client.MakeStreamRequest(request);

            // Disposing should release the connection without throwing, and be safe to call twice.
            var exception = Record.Exception(() =>
            {
                response.Dispose();
                response.Dispose();
            });

            Assert.Null(exception);

            server.Stop();
        }
    }
}
