using Moq;
using Newtonsoft.Json.Linq;
using SimpleHttpClient.Models;
using SimpleHttpClient.Serialization;
using System.Net;
using System.Text;

namespace SimpleHttpClient.Tests
{
    public class SimpleResponseTests
    {
        [Fact]
        public async Task IsSuccessful_IsSet()
        {
            var client = new SimpleClient("https://postman-echo.com");

            var request = new SimpleRequest("/get");

            var response = await client.MakeRequest<PostmanEchoResponse>(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(response.IsSuccessful);
        }

        [Fact]
        public async Task IsSuccessful_IncludesAdditionalStatusCodes()
        {
            var client = new SimpleClient("https://postman-echo.com");
            client.AdditionalSuccessfulStatusCodes.Add(HttpStatusCode.NotFound);

            var request = new SimpleRequest("/some/nonexistant/path");

            var response = await client.MakeRequest<PostmanEchoResponse>(request);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.True(response.IsSuccessful);
        }

        [Fact]
        public async Task StringBody_IsSet()
        {
            var client = new SimpleClient("https://postman-echo.com");

            var request = new SimpleRequest("/post", HttpMethod.Post, new
            {
                param1 = "value1",
                param2 = "value2",
            });

            var response = await client.MakeRequest<PostmanEchoResponse>(request);

            var body = JToken.Parse(response.StringBody);

            Assert.Equal("value1", body["data"]?["param1"]?.ToString());
            Assert.Equal("value2", body["data"]?["param2"]?.ToString());
            Assert.Contains("url", response.StringBody);
        }

        [Fact]
        public async Task ByteBody_IsSet()
        {
            var client = new SimpleClient("https://postman-echo.com");

            var request = new SimpleRequest("/post", HttpMethod.Post, new
            {
                param1 = "value1",
                param2 = "value2",
            });

            var response = await client.MakeRequest<PostmanEchoResponse>(request);

            var body = JToken.Parse(Encoding.UTF8.GetString(response.ByteBody));

            Assert.NotNull(response.ByteBody);
            Assert.NotEmpty(response.ByteBody);
            Assert.Equal("value1", body["data"]?["param1"]?.ToString());
            Assert.Equal("value2", body["data"]?["param2"]?.ToString());
            Assert.Contains("url", Encoding.UTF8.GetString(response.ByteBody));
        }

        [Fact]
        public async Task SerializationException_IsSet()
        {
            var serializer = new Mock<ISimpleHttpSerializer>(MockBehavior.Loose);

            serializer.Setup(x => x.Serialize(It.IsAny<object>())).Returns("{}");

            serializer.Setup(x => x.Deserialize<It.IsAnyType>(It.IsAny<string>())).Throws(new InvalidOperationException());

            var client = new SimpleClient("https://postman-echo.com", serializer: serializer.Object);

            var request = new SimpleRequest("/post", HttpMethod.Post, new
            {
                param1 = "value1",
                param2 = "value2",
            });

            var response = await client.MakeRequest<PostmanEchoResponse>(request);

            Assert.IsType<InvalidOperationException>(response.SerializationException);
        }
    }
}
