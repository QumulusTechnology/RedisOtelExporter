using System.Net;

namespace RedisOtelExporter
{
    internal class DelegatingExporter : DelegatingHandler
    {
        private readonly RedisExportProcessor queueProcessor;

        public DelegatingExporter(RedisExportProcessor processor)
        {
            queueProcessor = processor;
        }
        protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Capture the OTLP payload
            var contentBytes = request.Content.ReadAsByteArrayAsync(cancellationToken).Result;

            queueProcessor.EnqueueAsync(contentBytes).GetAwaiter().GetResult();

            // Return a successful response to prevent exporter errors
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Capture the OTLP payload
            var contentBytes = await request.Content.ReadAsByteArrayAsync(cancellationToken);

            await queueProcessor.EnqueueAsync(contentBytes);
            // Return a successful response to prevent exporter errors
            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
