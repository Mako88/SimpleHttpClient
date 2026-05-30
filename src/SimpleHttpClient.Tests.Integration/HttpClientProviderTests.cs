using Moq;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace SimpleHttpClient.Tests
{
    public class HttpClientProviderTests
    {
        [Fact]
        public void Factory_Create_ReturnsProviderForTargetFramework()
        {
            using var provider = HttpClientProviderFactory.Create(null);

#if NETFRAMEWORK
            // The netstandard2.0 asset (used by .NET Framework) rotates a cached client.
            Assert.IsType<RotatingHttpClientProvider>(provider);
#else
            // The net8.0 asset relies on SocketsHttpHandler's pooled connection lifetime.
            Assert.IsType<PooledHttpClientProvider>(provider);
#endif
        }

        [Fact]
        public void GetClient_WithoutFactory_ReturnsConfiguredClient_CachedAcrossCalls()
        {
            using var provider = HttpClientProviderFactory.Create(null);

            var first = provider.GetClient();
            var second = provider.GetClient();

            Assert.NotNull(first);
            // With no factory the provider owns and caches a single client.
            Assert.Same(first, second);
            // Configured with an infinite timeout - SimpleClient applies its own per-request timeout.
            Assert.Equal(System.Threading.Timeout.InfiniteTimeSpan, first.Timeout);
        }

        [Fact]
        public void GetClient_WithFactory_DelegatesToFactory()
        {
            using var factoryClient = new HttpClient();
            var factory = new Mock<IHttpClientFactory>();
            factory.Setup(f => f.CreateClient(Constants.HttpClientNameString)).Returns(factoryClient);

            using var provider = HttpClientProviderFactory.Create(factory.Object);

            var client = provider.GetClient();

            Assert.Same(factoryClient, client);
            factory.Verify(f => f.CreateClient(Constants.HttpClientNameString), Times.AtLeastOnce);
        }

        [Fact]
        public async Task Dispose_DisposesOwnedClient_AndIsIdempotent()
        {
            var provider = HttpClientProviderFactory.Create(null);
            var client = provider.GetClient();

            provider.Dispose();
            provider.Dispose(); // idempotent: a second dispose must not throw

            // The client the provider owns is disposed along with it.
            await Assert.ThrowsAsync<ObjectDisposedException>(() => client.GetAsync("http://localhost"));
        }
    }
}
