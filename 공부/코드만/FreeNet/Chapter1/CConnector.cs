using System.Net;
using System.Net.Sockets;

namespace FreeNet
{
    public class CConnector
    {
        private CNetworkService cNetworkService;
        private Socket client_socket;

        public delegate void ConnectHandler(CUserToken token);
        public ConnectHandler connected_callback;

        public CConnector(CNetworkService cNetworkService)
        {
            this.cNetworkService = cNetworkService;
        }

        public void Connect(IPEndPoint remote_endPoint)
        {
            this.client_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            SocketAsyncEventArgs connect_args = new SocketAsyncEventArgs();
            connect_args.Completed += On_connect_completed;
            connect_args.RemoteEndPoint = remote_endPoint;

            bool pending = client_socket.ConnectAsync(connect_args);
            if (!pending)
            {
                On_connect_completed(null, connect_args);
            }
        }

        private void On_connect_completed(object sender, SocketAsyncEventArgs e)
        {
            if(e.SocketError == SocketError.Success)
            {
                CUserToken token = new CUserToken();
                this.cNetworkService.On_connect_completed(this.client_socket, token);
                connected_callback?.Invoke(token);
            }
            else
            {
                Console.WriteLine($"Failed to connect : {e.SocketError}");
            }
        }
    }
}