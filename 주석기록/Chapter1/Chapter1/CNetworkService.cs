using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace FreeNet
{
    public class CNetworkService
    {
        CListener client_listener;

        SocketAsyncEventArgsPool receive_event_args_pool;
        SocketAsyncEventArgsPool send_event_args_pool;




        BufferManager buffer_Manager = new BufferManager(max_Connections * buffer_Size * pre_Alloc_Count, buffer_Size);


        public delegate void SessionHandler(CUserToken token);
        public SessionHandler session_created_callback { get; set; }



        public void listen(string host, int port, int backlog)
        {
            CListener listener = new CListener();
            listener.callback_On_Newclient += on_new_client;
            listener.Start(host, port, backlog);

            receive_event_args_pool = new SocketAsyncEventArgsPool(max_Connections);
            send_event_args_pool = new SocketAsyncEventArgsPool(max_Connections);

            for (int i = 0; i < max_Connections; i++)
            {
                CUserToken token = new CUserToken();


                // receive Pool (교재내용그대로)
                {
                    SocketAsyncEventArgs arg = new SocketAsyncEventArgs();
                    arg.Completed += new EventHandler<SocketAsyncEventArgs>(receive_completed);
                    arg.UserToken = token;

                    buffer_Manager.SetBuffer(arg);

                    receive_event_args_pool.Push(arg);
                }

                // sendPool (교재내용그대로)
                {
                    SocketAsyncEventArgs arg = new SocketAsyncEventArgs();
                    arg.Completed += new EventHandler<SocketAsyncEventArgs>(send_Completed);
                    arg.UserToken = token;

                    buffer_Manager.SetBuffer(arg);

                    send_event_args_pool.Push(arg);
                }
            }

            void on_new_client(Socket client_socket, object token)
            {
                SocketAsyncEventArgs receive_args = receive_event_args_pool.Pop();
                SocketAsyncEventArgs send_args = send_event_args_pool.Pop();

                if (session_created_callback != null)
                {
                    CUserToken user_token = receive_args.UserToken as CUserToken;
                    session_created_callback(user_token);
                }

                begin_receive(client_socket, receive_args, send_args);

                void begin_receive(Socket socket, SocketAsyncEventArgs receive_args, SocketAsyncEventArgs send_args)
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
            }

            void receive_completed(object sender, SocketAsyncEventArgs e)
            {
                if (e.LastOperation == SocketAsyncOperation.Receive)
                {
                    process_receive(e);
                    return;
                }

                throw new ArgumentNullException("the last operation completed on the socket was not a receive.");
            }
        }

        private void process_receive(SocketAsyncEventArgs e)
        {
            CUserToken token = e.UserToken as CUserToken;
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                token.on_receive(e.Buffer, e.Offset, e.BytesTransferred);

                bool pending = token.socket.ReceiveAsync(e);
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
    }
}
