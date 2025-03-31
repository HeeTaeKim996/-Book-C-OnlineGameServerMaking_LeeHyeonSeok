using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace FreeNet
{
    public class CNetworkService
    {
        int connected_count;

        CListener client_listener;
        SocketAsyncEventArgsPool receive_event_args_pool;
        SocketAsyncEventArgsPool send_event_args_pool;
        BufferManager buffer_manager;

        public delegate void SessionHandler(CUserToken token);
        public SessionHandler session_created_callback { get; set; }

        int max_connections;
        int buffer_size;
        readonly int pre_alloc_count = 2;


        public CNetworkService()
        {
            this.connected_count = 0;
            this.session_created_callback = null;
        }

        public void Initialize()
        {
            this.max_connections = 10_000;
            this.buffer_size = 1024;

            this.buffer_manager = new BufferManager(this.max_connections * this.buffer_size * this.pre_alloc_count, this.buffer_size);
            #region 공부정리
            // ○ pre_alloc_count =2 를 곱하는 이유 => 클라이언트별 송신용, 수신용 버퍼로 버퍼가 총 2개 필요
            #endregion
            this.receive_event_args_pool = new SocketAsyncEventArgsPool(this.max_connections);
            this.send_event_args_pool = new SocketAsyncEventArgsPool(this.max_connections);

            this.buffer_manager.InitBuffer();

            SocketAsyncEventArgs arg;

            for (int i = 0; i < max_connections; i++)
            {
                CUserToken token = new CUserToken();
                // receive Pool 
                {
                    arg = new SocketAsyncEventArgs();
                    arg.Completed += new EventHandler<SocketAsyncEventArgs>(receive_completed);
                    arg.UserToken = token;

                    buffer_manager.SetBuffer(arg);

                    receive_event_args_pool.Push(arg);
                }

                // sendPool 
                {
                    arg = new SocketAsyncEventArgs();
                    arg.Completed += new EventHandler<SocketAsyncEventArgs>(send_completed);
                    arg.UserToken = token;

                    buffer_manager.SetBuffer(arg);
                    
                    send_event_args_pool.Push(arg);
                }
            }
        }

        public void listen(string host, int port, int backlog)
        {
            this.client_listener = new CListener();
            this.client_listener.callback_On_Newclient += on_new_client;
            this.client_listener.Start(host, port, backlog);
        }
        private void on_new_client(Socket client_socket, object token)
        {
            #region 공부정리
            // ○ 이 매서드 on_new_client는 아래 매서드 on_connect_completed 와 대응되게, 프로젝트 CSampleServer에서 CListener를 통해 델리게이트로 발동되는 매서드
            #endregion
            Interlocked.Increment(ref this.connected_count);

            Console.WriteLine($"{Thread.CurrentThread.ManagedThreadId} A client connected. handle {client_socket.Handle}, couunt {this.connected_count}");

            SocketAsyncEventArgs receive_args = this.receive_event_args_pool.Pop();
            SocketAsyncEventArgs send_args = this.send_event_args_pool.Pop();

            CUserToken user_token = null;
            if(this.session_created_callback != null)
            {
                user_token = receive_args.UserToken as CUserToken;
                this.session_created_callback(user_token);
            }

            begin_receive(client_socket, receive_args, send_args);
        }

        public void on_connect_completed(Socket socket, CUserToken token)
        {
            #region 
            // ○ 이 매서드 on_connect_completed 는 위 매서드 on_new_client와 대응되게, 프로젝트 CSampleClient에서 CConnector를 통해 델리게이트로 발동되는 매서드
            #endregion
            SocketAsyncEventArgs receive_event_arg = new SocketAsyncEventArgs();
            receive_event_arg.Completed += new EventHandler<SocketAsyncEventArgs>(receive_completed);
            receive_event_arg.UserToken = token;
            receive_event_arg.SetBuffer(new byte[1024], 0, 1024);

            SocketAsyncEventArgs send_event_arg = new SocketAsyncEventArgs();
            send_event_arg.Completed += new EventHandler<SocketAsyncEventArgs>(send_completed);
            send_event_arg.UserToken = token;
            send_event_arg.SetBuffer(new byte[1024], 0, 1024);

            begin_receive(socket, receive_event_arg, send_event_arg);
        }
        private void begin_receive(Socket socket, SocketAsyncEventArgs receive_args, SocketAsyncEventArgs send_args)
        {
            CUserToken token = receive_args.UserToken as CUserToken;
            token.set_event_args(receive_args, send_args);
            token.socket = socket;

            bool pending = socket.ReceiveAsync(receive_args);
            if (!pending)
            {
                process_receive(receive_args);
            }
        }

        private void receive_completed(object sender, SocketAsyncEventArgs e)
        {
            if (e.LastOperation == SocketAsyncOperation.Receive)
            {
                process_receive(e);
                return;
            }

            throw new ArgumentNullException("the last operation completed on the socket was not a receive.");
        }
        private void process_receive(SocketAsyncEventArgs e)
        {
            CUserToken token = e.UserToken as CUserToken;
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                token.on_receive(e.Buffer, e.Offset, e.BytesTransferred);

                bool pending = token.socket.ReceiveAsync(e);
                #region 공부정리
                // ○ token.socket.ReceiveAsync(e)
                //  - 위 token.on_receive(...) 로 받은 메세지를 버퍼로 토큰에게 보낸 후, 즉시 다시 bool pending = token.socket.ReceiveAsync(e); 로, 다음 메세지가 오기를 수신 처리하는 것
                #endregion
                if (!pending)
                {
                    process_receive(e);
                }
            }
            else
            {
                Console.WriteLine(string.Format("error {0}, transferred {1}", e.SocketError, e.BytesTransferred));
                close_clientsocket(token);
            }
        }
        private void send_completed(object sender, SocketAsyncEventArgs e)
        {
            CUserToken token = e.UserToken as CUserToken;
            token.process_send(e);
        }

        private void close_clientsocket(CUserToken token)
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
