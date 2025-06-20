using OpenTelemetry;
using OpenTelemetry.Trace;
using StackExchange.Redis;

namespace RedisOtelExporter
{
    public static class OpenTelemetryExtension
    {
        public static TracerProviderBuilder AddRedisOtelExporter(this TracerProviderBuilder builder, ConnectionMultiplexer connectionMultiplexer, ExportProcessorType exportType)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (connectionMultiplexer == null) throw new ArgumentNullException(nameof(connectionMultiplexer));
            var processor = new RedisExportProcessor(connectionMultiplexer);
            builder.AddProcessor(processor);

            builder.AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri("http://localhost:4317");
                options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
                options.ExportProcessorType = exportType;
                options.HttpClientFactory = () => new HttpClient(new DelegatingExporter(processor));
            });

            return builder;
        }
    }
}
