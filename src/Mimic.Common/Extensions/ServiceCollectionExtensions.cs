using Microsoft.Extensions.DependencyInjection;
using Mimic.Common.Networking;

namespace Mimic.Common
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSocketManager<THandler>(
            this IServiceCollection @this)
                where THandler : class, ISocketHandler
            => @this.AddSingleton(typeof(SocketManager<>))
                .AddScoped<ISocketHandler, THandler>();
    }
}
