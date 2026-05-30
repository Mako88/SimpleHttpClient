using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleHttpClient.Models
{
    /// <summary>
    /// Wraps the raw response stream so the cancellation token supplied to
    /// <c>MakeStreamRequest</c> is observed by reads, not just by sending the
    /// request. The token is baked into the wrapper, so even readers that give
    /// you no place to pass a token (such as <see cref="StreamReader"/>) still
    /// honor cancellation through the reads they make internally.
    /// </summary>
    /// <remarks>
    /// Async reads link the baked-in token with any per-call token, so a
    /// cancellation surfaces as an <see cref="OperationCanceledException"/> even
    /// mid-read. Synchronous reads observe cancellation between reads; a single
    /// synchronous read already blocked on the socket can only be aborted by
    /// disposing the response.
    /// </remarks>
    internal sealed class CancellationAwareStream : Stream
    {
        private readonly Stream inner;
        private readonly CancellationToken token;

        public CancellationAwareStream(Stream inner, CancellationToken token)
        {
            this.inner = inner ?? throw new ArgumentNullException(nameof(inner));
            this.token = token;
        }

        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => inner.CanSeek;
        public override bool CanWrite => inner.CanWrite;
        public override long Length => inner.Length;

        public override long Position
        {
            get => inner.Position;
            set => inner.Position = value;
        }

        public override void Flush() => inner.Flush();

        public override int Read(byte[] buffer, int offset, int count)
        {
            token.ThrowIfCancellationRequested();
            return inner.Read(buffer, offset, count);
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(token, cancellationToken))
            {
                return await inner.ReadAsync(buffer, offset, count, linked.Token).ConfigureAwait(false);
            }
        }

#if NET8_0_OR_GREATER
        public override int Read(Span<byte> buffer)
        {
            token.ThrowIfCancellationRequested();
            return inner.Read(buffer);
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(token, cancellationToken))
            {
                return await inner.ReadAsync(buffer, linked.Token).ConfigureAwait(false);
            }
        }
#endif

        public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);

        public override void SetLength(long value) => inner.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count) => inner.Write(buffer, offset, count);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                inner.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
