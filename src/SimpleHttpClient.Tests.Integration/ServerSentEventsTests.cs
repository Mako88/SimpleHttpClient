using Moq;
using SimpleHttpClient.Models;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace SimpleHttpClient.Tests
{
    public class ServerSentEventsTests
    {
        // ---- ReadLinesAsync ----

        [Fact]
        public async Task ReadLines_SplitsOnNewlines()
        {
            using var response = StreamFrom("a\nb\nc\n");

            var lines = await Collect(response.ReadLinesAsync());

            Assert.Equal(new[] { "a", "b", "c" }, lines);
        }

        [Fact]
        public async Task ReadLines_HandlesCrlf()
        {
            using var response = StreamFrom("a\r\nb\r\n");

            var lines = await Collect(response.ReadLinesAsync());

            Assert.Equal(new[] { "a", "b" }, lines);
        }

        // ---- ReadServerSentEventsAsync ----

        [Fact]
        public async Task Sse_SingleEvent_ParsesData()
        {
            using var response = StreamFrom("data: hello\n\n");

            var events = await Collect(response.ReadServerSentEventsAsync());

            var sse = Assert.Single(events);
            Assert.Equal("hello", sse.Data);
            Assert.Null(sse.EventType);
            Assert.Null(sse.Id);
            Assert.Null(sse.Retry);
        }

        [Fact]
        public async Task Sse_MultipleDataLines_JoinedWithNewline()
        {
            using var response = StreamFrom("data: a\ndata: b\ndata: c\n\n");

            var events = await Collect(response.ReadServerSentEventsAsync());

            Assert.Equal("a\nb\nc", Assert.Single(events).Data);
        }

        [Fact]
        public async Task Sse_NoSpaceAfterColon_IsAccepted()
        {
            using var response = StreamFrom("data:hello\n\n");

            Assert.Equal("hello", Assert.Single(await Collect(response.ReadServerSentEventsAsync())).Data);
        }

        [Fact]
        public async Task Sse_CommentLines_AreIgnored()
        {
            using var response = StreamFrom(": this is a keep-alive\ndata: real\n\n");

            Assert.Equal("real", Assert.Single(await Collect(response.ReadServerSentEventsAsync())).Data);
        }

        [Fact]
        public async Task Sse_ParsesEventTypeIdAndRetry()
        {
            using var response = StreamFrom("event: update\nid: 42\nretry: 1500\ndata: payload\n\n");

            var sse = Assert.Single(await Collect(response.ReadServerSentEventsAsync()));
            Assert.Equal("update", sse.EventType);
            Assert.Equal("42", sse.Id);
            Assert.Equal(1500, sse.Retry);
            Assert.Equal("payload", sse.Data);
        }

        [Fact]
        public async Task Sse_Id_CarriesForwardToLaterEvents()
        {
            using var response = StreamFrom("id: 1\ndata: a\n\ndata: b\n\n");

            var events = await Collect(response.ReadServerSentEventsAsync());

            Assert.Equal(2, events.Count);
            Assert.Equal("1", events[0].Id);
            Assert.Equal("1", events[1].Id); // id carries forward per the SSE spec
            Assert.Equal("b", events[1].Data);
        }

        [Fact]
        public async Task Sse_EventTypeAndRetry_ResetBetweenEvents()
        {
            using var response = StreamFrom("event: first\nretry: 100\ndata: a\n\ndata: b\n\n");

            var events = await Collect(response.ReadServerSentEventsAsync());

            Assert.Equal("first", events[0].EventType);
            Assert.Equal(100, events[0].Retry);
            Assert.Null(events[1].EventType); // event type does not carry forward
            Assert.Null(events[1].Retry);
        }

        [Fact]
        public async Task Sse_DoneSentinel_PassesThroughAsData()
        {
            // The library must NOT special-case [DONE]; the caller handles it.
            using var response = StreamFrom("data: {\"x\":1}\n\ndata: [DONE]\n\n");

            var events = await Collect(response.ReadServerSentEventsAsync());

            Assert.Equal(2, events.Count);
            Assert.Equal("{\"x\":1}", events[0].Data);
            Assert.Equal("[DONE]", events[1].Data);
        }

        [Fact]
        public async Task Sse_EventWithoutData_IsNotDispatched()
        {
            using var response = StreamFrom("event: ping\n\ndata: real\n\n");

            var events = await Collect(response.ReadServerSentEventsAsync());

            // Per spec, an event with no data is not dispatched.
            var sse = Assert.Single(events);
            Assert.Equal("real", sse.Data);
        }

        [Fact]
        public async Task Sse_IncompleteTrailingEvent_IsNotDispatched()
        {
            using var response = StreamFrom("data: complete\n\ndata: incomplete");

            var events = await Collect(response.ReadServerSentEventsAsync());

            Assert.Equal("complete", Assert.Single(events).Data);
        }

        [Fact]
        public async Task Sse_PreCancelledToken_Throws()
        {
            using var response = StreamFrom("data: a\n\n");
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAnyAsync<System.OperationCanceledException>(
                async () => await Collect(response.ReadServerSentEventsAsync(cts.Token)));
        }

        [Fact]
        public async Task Sse_EndToEnd_OverMakeStreamRequest()
        {
            var server = WireMockServer.Start();
            server.Given(Request.Create().WithPath("/sse").UsingGet())
                .RespondWith(Response.Create()
                    .WithStatusCode(HttpStatusCode.OK)
                    .WithHeader("Content-Type", "text/event-stream")
                    .WithBody("data: one\n\ndata: two\n\ndata: [DONE]\n\n"));

            var client = new SimpleClient(server.Url);

            var datas = new List<string>();
            using (var response = await client.MakeStreamRequest(new SimpleRequest("/sse")))
            {
                await foreach (var sse in response.ReadServerSentEventsAsync())
                {
                    if (sse.Data == "[DONE]")
                    {
                        break;
                    }

                    datas.Add(sse.Data);
                }
            }

            Assert.Equal(new[] { "one", "two" }, datas);

            server.Stop();
        }

        [Fact]
        public async Task ReadServerSentEventsAsync_IsMockable()
        {
            // The readers are interface members (not extension methods) specifically so callers
            // can mock them. This test fails to compile if they ever become extension methods.
            var events = new[] { new ServerSentEvent("mocked") };
            var mock = new Mock<ISimpleStreamResponse>();
            mock.Setup(x => x.ReadServerSentEventsAsync(It.IsAny<CancellationToken>()))
                .Returns(ToAsync(events));

            var result = await Collect(mock.Object.ReadServerSentEventsAsync());

            Assert.Equal("mocked", Assert.Single(result).Data);
        }

        // ---- helpers ----

        private static async IAsyncEnumerable<T> ToAsync<T>(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                yield return item;
            }

            await Task.CompletedTask;
        }

        private static ISimpleStreamResponse StreamFrom(string body, string contentType = "text/event-stream")
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(body));
            var response = new SimpleStreamResponse(new HttpResponseMessage(HttpStatusCode.OK), stream)
            {
                StatusCode = HttpStatusCode.OK,
                IsSuccessful = true,
            };

            if (contentType != null)
            {
                response.Headers["Content-Type"] = contentType;
            }

            return response;
        }

        private static async Task<List<T>> Collect<T>(IAsyncEnumerable<T> sequence)
        {
            var list = new List<T>();
            await foreach (var item in sequence)
            {
                list.Add(item);
            }

            return list;
        }
    }
}
