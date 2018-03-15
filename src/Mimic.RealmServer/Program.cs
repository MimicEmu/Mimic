using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mimic.Common;
using Mimic.Common.Networking;
using System;
using System.Threading.Tasks;

namespace Mimic.RealmServer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var services = BuildServiceProvider();

            var socketManager = services
                .GetRequiredService<SocketManager<AuthHandler>>();

            socketManager.Setup("0.0.0.0", 3724);

            await socketManager.StartAsync();
            await Task.Delay(-1);
        }

        static IServiceProvider BuildServiceProvider()
        {
            var services = new ServiceCollection();

            services.AddLogging(options =>
            {
                options.SetMinimumLevel(LogLevel.Trace);
                options.AddConsole();
            });

            services.AddSocketManager<AuthHandler>();

            return services.BuildServiceProvider();
        }
    }
}
