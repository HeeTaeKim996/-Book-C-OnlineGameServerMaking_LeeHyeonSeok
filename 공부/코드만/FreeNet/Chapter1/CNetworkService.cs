using System.Net.Sockets;

namespace FreeNet
{
    public class CNetworkService
    {
        private int connected_count = 0;

        private CListener client_listener;
        private SocketAsyncEventArgsPool receive_event_args_pool;
        private SocketAsyncEventArgsPool send_event_args_pool;
        private BufferManager bufferManager;

        public delegate void SessionHandler(CUserToken token);
        public SessionHandler session_created_callback { get; set; }

        private int max_connections;
        private int bufferSize;
        private readonly int pre_alloc_count = 2;


        public CNetworkService()
        {
            this.connected_count = 0;
            this.session_created_callback = null;
        }

        public void Initialize()
        {
            this.max_connections = 10_000;
            this.bufferSize = 1024;

            this.bufferManager = new BufferManager(max_connections * bufferSize * pre_alloc_count, bufferSize);
            this.bufferManager.InitBuffer();

            this.receive_event_args_pool = new SocketAsyncEventArgsPool(max_connections);
            this.send_event_args_pool = new SocketAsyncEventArgsPool(max_connections);



            SocketAsyncEventArgs arg;
            for(int i = 0; i < max_connections; i++)
            {
                CUserToken token = new CUserToken();

                //receive pool
                {
                    arg = new SocketAsyncEventArgs();
                    arg.Completed += Receive_completed;
                    arg.UserToken = token;

                    bufferManager.SetBuffer(arg);

                    receive_event_args_pool.Push(arg);
                }

                // send pool
                {
                    arg = new SocketAsyncEventArgs();
                    arg.Completed += Send_Completed;
                    arg.UserToken = token;

                    bufferManager.SetBuffer(arg);

                    send_event_args_pool.Push(arg);
                }
            }
        }
        private void Receive_completed(object sender, SocketAsyncEventArgs e)
        {
            if (e.LastOperation == SocketAsyncOperation.Receive)
            {
                Process_receive(e);
            }
            else
            {
                throw new ArgumentNullException("마지막으로 받은 소켓오퍼레이션이 SocketAsyncOperation.Receive 가 아님");
            }
        }
        private void Send_Completed(object sender, SocketAsyncEventArgs e)
        {
            CUserToken token = e.UserToken as CUserToken;
            token.process_send(e);
        }


        public void Listen(string host, int port, int backLog)
        {
            this.client_listener = new CListener();
            this.client_listener.callback_On_Newclient += On_new_client;
            this.client_listener.Start(host, port, backLog);
        }
        private void On_new_client(Socket client_socket, object sender)
        {
            Interlocked.Increment(ref this.connected_count);

            Console.WriteLine($"{Thread.CurrentThread.ManagedThreadId} 클라이언트 연결 핸들 : {client_socket.Handle}. 연결된 총 클라이언트 : {this.connected_count}");


            SocketAsyncEventArgs receive_args = this.receive_event_args_pool.Pop();
            SocketAsyncEventArgs send_args = this.send_event_args_pool.Pop();

            Begin_receive(client_socket, receive_args, send_args);



            CUserToken user_token = receive_args.UserToken as CUserToken;
            if (this.session_created_callback != null)
            {
                this.session_created_callback(user_token);
            }
        }

        public void On_connect_completed(Socket socket, CUserToken token)
        {
            SocketAsyncEventArgs receive_arg = new SocketAsyncEventArgs();
            receive_arg.Completed += Receive_completed;
            receive_arg.UserToken = token;
            receive_arg.SetBuffer(new byte[1024], 0, 1024);

            SocketAsyncEventArgs send_arg = new SocketAsyncEventArgs();
            send_arg.Completed += Send_Completed;
            send_arg.UserToken = token;
            send_arg.SetBuffer(new byte[1024], 0, 1024);

            Begin_receive(socket, receive_arg, send_arg);
        }



        private void Begin_receive(Socket socket, SocketAsyncEventArgs receive_args, SocketAsyncEventArgs send_args)
        {
            CUserToken token = receive_args.UserToken as CUserToken;
            token.socket = socket;
            token.set_event_args(receive_args, send_args);

            bool pending = socket.ReceiveAsync(receive_args);
            if (!pending)
            {
                Process_receive(receive_args);
            }
        }

        private void Process_receive(SocketAsyncEventArgs e)
        {
            CUserToken token = e.UserToken as CUserToken;

            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                token.on_receive(e.Buffer, e.Offset, e.BytesTransferred);

                bool pending = token.socket.ReceiveAsync(e);
                if (!pending)
                {
                    Process_receive(e);
                }
            }
            else
            {
                Console.WriteLine($"Error {e.SocketError}, Transffered {e.BytesTransferred}");
                close_clientSocket(token);
            }
        }

        private void close_clientSocket(CUserToken token)
        {
            token.on_removed();

            if(this.receive_event_args_pool != null)
            {
                this.receive_event_args_pool.Push(token.receive_event_args);
            }
            if(this.send_event_args_pool != null)
            {
                this.send_event_args_pool.Push(token.send_event_args);
            }
        }
    }
}
