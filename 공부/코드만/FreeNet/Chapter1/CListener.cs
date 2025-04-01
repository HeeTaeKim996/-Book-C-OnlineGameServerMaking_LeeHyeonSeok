using System.Net;
using System.Net.Sockets;


namespace FreeNet
{
    internal class CListener 
    {
        private Socket listen_socket;
        private SocketAsyncEventArgs accept_args;
        private AutoResetEvent autoResetEvent;

        public delegate void NewClientHandler(Socket socket, object token);
        public NewClientHandler callback_on_newClient;


        public void Start(string host, int port, int backLog)
        {
            this.listen_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPAddress address;
            if(host == "0.0.0.0")
            {
                address = IPAddress.Any;
            }
            else
            {
                address = IPAddress.Parse(host);
            }
            IPEndPoint endPoint = new IPEndPoint(address, port);
            this.listen_socket.Bind(endPoint);
            this.listen_socket.Listen(backLog);

            try
            {
                this.accept_args = new SocketAsyncEventArgs();
                this.accept_args.Completed += on_accept_completed;

                Thread listen_thread = new Thread(Do_listen);
                listen_thread.Start();
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);               
            }
        }


        public void on_accept_completed(object sender, SocketAsyncEventArgs e)
        {
            this.autoResetEvent.Set();

            if (e.SocketError == SocketError.Success)
            {
                Socket accept_socket = e.AcceptSocket;
                this.callback_on_newClient?.Invoke(accept_socket, e.UserToken);            
            }
            else
            {
                Console.WriteLine("Failed to Accept Connecting Client");
            }
        }
        
        private void Do_listen()
        {
            this.autoResetEvent = new AutoResetEvent(false);

            while (true)
            {
                this.accept_args.AcceptSocket = null;

                try
                {
                    bool pending = listen_socket.AcceptAsync(accept_args);
                    if (!pending)
                    {
                        on_accept_completed(null, accept_args);
                    }

                    this.autoResetEvent.WaitOne();
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }
    }

}