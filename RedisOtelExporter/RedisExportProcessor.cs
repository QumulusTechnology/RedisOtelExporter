using System.Diagnostics;
using System.Threading.Channels;
using OpenTelemetry;
using StackExchange.Redis;

namespace RedisOtelExporter
{
    internal class RedisExportProcessor : BaseProcessor<Activity>
    {
        private readonly Channel<byte[]> channel;
        private volatile bool isDraining = false;
        private readonly ManualResetEventSlim drainCompletedEvent = new();
        private readonly IDatabase redisDB;

        public RedisExportProcessor(ConnectionMultiplexer connection)
        {

            redisDB = connection.GetDatabase();
            channel = Channel.CreateUnbounded<byte[]>();

            Task.Run(async () =>
            {
                await foreach (var message in channel.Reader.ReadAllAsync())
                {
                    await ProcessMessageAsync(message);

                    if (isDraining && channel.Reader.Count == 0)
                    {
                        drainCompletedEvent.Set(); // Notify StopAndDrain() that we're done
                    }
                }
            });
        }

        public async Task EnqueueAsync(byte[] message)
        {
            while (isDraining)
            {
                await Task.Delay(10); // Wait until drain is over
            }

            await channel.Writer.WriteAsync(message);
        }


        private async Task ProcessMessageAsync(byte[] message)
        {
            await redisDB.ListLeftPushAsync("otel-traces", message);
        }

        protected override bool OnForceFlush(int timeoutMilliseconds)
        {
            if (base.OnForceFlush(timeoutMilliseconds))
            {

                isDraining = true;
                drainCompletedEvent.Reset();

                // Block the thread until queue is fully processed
                while (channel.Reader.Count > 0)
                {
                    // Wait with timeout in case processing is fast
                    drainCompletedEvent.Wait(10);
                }

                isDraining = false;

                return true;
            }
            else
            {
                return false;
            }
        }

        protected override bool OnShutdown(int timeoutMilliseconds)
        {
            return base.OnShutdown(timeoutMilliseconds);
        }
    }
}
