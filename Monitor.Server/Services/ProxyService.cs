using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Monitor.Services;
using static Monitor.Services.Proxy;

namespace Monitor.Server
{
    public class ProxyService : ProxyBase
    {
        private readonly ILogger<ProxyService> _logger;
        public ProxyService(ILogger<ProxyService> logger)
        {
            _logger = logger;
        }

        public override async Task<Info> GetInfo(Empty request, ServerCallContext context)
        {
            string response = default;
            using (var client = new TcpClient("localhost", 9999))
            {
                var buffer = Encoding.ASCII.GetBytes($"show info{Environment.NewLine}");
                using (var stream = client.GetStream())
                {
                    await stream.WriteAsync(buffer, 0, buffer.Length, context.CancellationToken);
                    using (var reader = new StreamReader(stream))
                    {
                        response = await reader.ReadToEndAsync();
                    }
                }
            }

            return new Info { Version = response };
        }

        public override async Task GetStat(Empty request, IServerStreamWriter<Stat> responseStream, ServerCallContext context)
        {
            while (!context.CancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                string response = default;
                using (var client = new TcpClient("localhost", 9999))
                {
                    var buffer = Encoding.ASCII.GetBytes($"show stat{Environment.NewLine}");
                    using (var stream = client.GetStream())
                    {
                        await stream.WriteAsync(buffer, 0, buffer.Length, context.CancellationToken);
                        using (var reader = new StreamReader(stream))
                        {
                            response = await reader.ReadToEndAsync();
                        }
                    }
                }
                var stat = new Stat();
                stat.Services.AddRange(response.Trim().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)[1..].Select(line => line.Trim().Split(",", StringSplitOptions.None)).Select(slots => new Service { Name = slots[0], BytesIn = int.Parse(slots[8]), BytesOut = int.Parse(slots[9] )}));
                await responseStream.WriteAsync(stat);
            }
        }
    }
}
