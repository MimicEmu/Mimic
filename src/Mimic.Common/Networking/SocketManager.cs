using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Mimic.Common.Networking
{
    public class SocketManager<THandler>
        where THandler : class, ISocketHandler
    {
        private const int BufferSize = 6144;

        private static readonly ObjectFactory _handlerFactory =
            ActivatorUtilities.CreateFactory(typeof(THandler),
                Array.Empty<Type>());

        private readonly ILogger _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        private TcpListener _server;
        private ConcurrentBag<Task> _clientTasks;

        private Task _listenTask;
        private bool _listening;


        public SocketManager(
            ILogger<SocketManager<THandler>> logger,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public void Setup(string address, int port)
        {
            if (_listening)
                throw new InvalidOperationException(
                    "Cannot setup when a server is currently listening");
            var listenAddress = IPAddress.Parse(address);
            _server = new TcpListener(listenAddress, (int)port);
        }

        public async Task StartAsync()
        {
            if (_listening)
                await StopAsync().ConfigureAwait(false);

            _clientTasks = new ConcurrentBag<Task>();

            _listening = true;
            _server.Start();
            _listenTask = ListenAsync();
        }

        public async Task StopAsync()
        {
            _listening = false;
            await _listenTask
                .ConfigureAwait(false);
        }

        private async Task ListenAsync()
        {
            _logger.LogDebug("Listening on {Address}", _server.LocalEndpoint);

            while (_listening)
            {
                var client = await _server.AcceptTcpClientAsync()
                    .ConfigureAwait(false);

                // TODO: this should be handled safer

                _logger.LogInformation("Client connecting from IP {Address}",
                    client.Client.RemoteEndPoint);

                _clientTasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        using (client)
                            await HandleClientAsync(client);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(
                            "Client exception thrown ({Type}): {Exception}",
                            e.GetType().Name, e.Message);
                    }
                }));
            }

            try
            {
                await Task.WhenAll(_clientTasks)
                    .ConfigureAwait(false);
            }
            finally
            {
                _server.Stop();
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var services = scope.ServiceProvider;

                var handler = _handlerFactory(services,
                    Array.Empty<object>()) as THandler;

                handler.SetClient(client);

                try
                {
                    await handler.RunAsync()
                        .ConfigureAwait(false);
                }
                finally
                {
                    (handler as IDisposable)?.Dispose();
                    client.Close();
                }
            }
        }
    }
}
