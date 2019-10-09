using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using static Monitor.Services.Proxy;

namespace Monitor.Client
{
    class Program
    {
        static async Task Main(string[] args)
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            var channel = GrpcChannel.ForAddress("http://localhost:5000");
            var client = new ProxyClient(channel);
            // var reply = await client.GetInfoAsync(new Empty());
            // Console.WriteLine(reply.Version);
            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(1));
            using var streamingCall = client.GetStat(new Empty(), cancellationToken: cancellationTokenSource.Token);

            try
            {
                await foreach (var stat in streamingCall.ResponseStream.ReadAllAsync(cancellationToken: cancellationTokenSource.Token))
                {
                    foreach (var service in stat.Services)
                    {
                        Console.WriteLine($"{nameof(service.Name)}:{service.Name} {nameof(service.BytesIn)}:{service.BytesIn} {nameof(service.BytesOut)}:{service.BytesOut}");
                    }
                }
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
            {
                Console.WriteLine("Stream cancelled.");
            }
        }
    }
}
