using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace FreeNet
{
    public class CConnector
    {
        public delegate void ConnectedHandler(CUserToken token);
        public ConnectedHandler connected_callback { get; set; }

        private Socket client;

        private CNetworkService network_service;

        public CConnector(CNetworkService network_service)
        {
            this.network_service = network_service;
            this.connected_callback = null;
        }

        public void connect(IPEndPoint remote_endpoint)
        {
            this.client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            SocketAsyncEventArgs event_arg = new SocketAsyncEventArgs();
            event_arg.Completed += on_connect_completed;
            #region 공부정리
            // ○ 지금까지 교재에서는 SocketAsyncEventArgs(instance).Completed += new EventHandler<SocketAsyncEventArgs>(on_connect_completed); 로 했었는데, 위 처럼 해도 효과, 성능 면에서 동일하다 한다.
            //   아마 교재에서는 EvetHandler를 사용했음을 명시하기 위해 지금까지 그렇제 거은 거고, 일반적으로 위처럼 간결하게 적는다 한다. (From 지피티)
            #endregion
            event_arg.RemoteEndPoint = remote_endpoint;
            bool pending = client.ConnectAsync(event_arg);
            #region 공부정리
            // ○ ConnectAsync 와 AcceptAsync
            //  - 앞서 FreeNet - CListener 에서 AcceptAsync와 대응되는데, 서버에서 CListener 의 AcceptAsync로, 클라이언트의 이 클래스에서 보낸 Socket에 대응되는 e.AcceptSocket이 생성되며, ConnectAsync를 통해 이 클래스의 소켓은
            //    e.AcceptSocket과 연결된다
            #endregion
            if (!pending)
            {
                on_connect_completed(null, event_arg);
            }
        }
        private void on_connect_completed(object sender, SocketAsyncEventArgs e)
        {
            if(e.SocketError == SocketError.Success)
            {
                CUserToken token = new CUserToken();
                this.network_service.On_connect_completed(this.client, token);

                if(this.connected_callback != null)
                {
                    this.connected_callback(token);
                }
            }
            else
            {
                Console.WriteLine($"Failed to connect. {e.SocketError}");
            }
        }
        
    }
}
