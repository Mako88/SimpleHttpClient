using Microsoft.Extensions.DependencyInjection;
using SimpleHttpClient.Extensions;
using SimpleHttpClient.Models;
using System.Net;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace SimpleHttpClient.Tests
{
    public class SimpleClientFactoryTests
    {
        [Fact]
        public void Factory_IsRegistered_AndCreatesClientWithHost()
        {
            var provider = BuildProvider();

            var factory = provider.GetRequiredService<ISimpleClientFactory>();
            var client = factory.CreateClient("https://example.com");

            Assert.NotNull(client);
            Assert.Equal("https://example.com", client.Host);
        }

        [Fact]
        public void Factory_CreatesIndependentClients_WithSeparateHosts()
        {
            var provider = BuildProvider();
            var factory = provider.GetRequiredService<ISimpleClientFactory>();

            var client1 = factory.CreateClient("https://one.com");
            var client2 = factory.CreateClient("https://two.com");

            Assert.NotSame(client1, client2);
            Assert.Equal("https://one.com", client1.Host);
            Assert.Equal("https://two.com", client2.Host);
        }

        [Fact]
        public void ISimpleClient_IsRegisteredAsTransient()
        {
            var provider = BuildProvider();

            var client1 = provider.GetRequiredService<ISimpleClient>();
            var client2 = provider.GetRequiredService<ISimpleClient>();

            // Transient => a new instance per resolution, so setting Host on one
            // consumer's client doesn't stomp on another's.
            Assert.NotSame(client1, client2);
        }

        [Fact]
        public async Task FactoryCreatedClient_CanMakeRequests()
        {
            var server = WireMockServer.Start();
            server.Given(Request.Create().WithPath("/get").UsingGet())
                .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.OK).WithBody("ok"));

            var provider = BuildProvider();
            var factory = provider.GetRequiredService<ISimpleClientFactory>();

            var client = factory.CreateClient(server.Url);
            var response = await client.MakeRequest(new SimpleRequest("/get"));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            server.Stop();
        }

        private static ServiceProvider BuildProvider()
        {
            var services = new ServiceCollection();
            services.AddSimpleHttpClient();
            return services.BuildServiceProvider();
        }
    }
}
