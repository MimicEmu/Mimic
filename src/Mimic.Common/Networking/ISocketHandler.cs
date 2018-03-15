using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Mimic.Common.Networking
{
    public interface ISocketHandler
    {
        Task RunAsync();

        TcpClient Client { get; }
        void SetClient(TcpClient client);
    }
}
