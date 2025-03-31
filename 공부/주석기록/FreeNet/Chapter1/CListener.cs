using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FreeNet
{
    internal class CListener
    {
        SocketAsyncEventArgs accept_Args;
        #region 공부정리
        // SocketAsyncEventArgs : SendAsync, ReceiveAsync 등 비동기 호출을 위해 사용되는 객체
        #endregion

        Socket listen_Socket;

        AutoResetEvent flow_Control_Event;
        #region 공부정리
        // AutoResetEvent : 비동기에서 동기적인 문법 사용을 위한 보완. 비동기 스레드 내에 AutoResetEvent(instance).WaitOne() 을 할 시, 하단의 내용이 시행되지 않음.
        // AutoResetEvent(instance).Set(); 을 할 시[이는 비동기매서드 외부에서 호출] 에는 , 다시 비동기 매서드가 시행됨
        // 하나의 AutoResetEvent(instance)의 WaintOne, Set을 다수의 비동기스레드가 사용할 때, 외부에서 Set을 호출시 어떤 비동기스레드가 다시 재개할지는 확실치 않음
        #endregion

        public delegate void NewClientHandler(Socket client_Socket, object token);

        public NewClientHandler callback_On_Newclient;


        public CListener()
        {
            callback_On_Newclient = null;
        }

        public void Start(string host, int port, int backLog)
        {
            listen_Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            IPAddress address;
            if (host == "0.0.0.0")
            {
                address = IPAddress.Any;
                #region 공부정리
                // ○ IPAddress.Any
                //  - 서버 아래에 인터페이스로, 192.168.0.10(내부망), 10.0.0.5(다른 내부망), 127.0.0.1(로컬호스트) 가 있다 하자. IPAddress.Any로 설정하면,
                //    클라이언트에서 192.168.0.10, 10.0.0.5, 127.0.0.1 중 하나를 IP주소로 설정하여 접속하면, 접속이 가능하도록 하는 것
                #endregion
            }
            else
            {
                address = IPAddress.Parse(host);
            }
            IPEndPoint endPoint = new IPEndPoint(address, port);

            try
            {
                listen_Socket.Bind(endPoint);
                listen_Socket.Listen(backLog);

                accept_Args = new SocketAsyncEventArgs();
                accept_Args.Completed += new EventHandler<SocketAsyncEventArgs>(on_Accept_Completed);
                #region 공부정리
                // ○ EventHandler
                // - 유니티의 addListener와 비슷한, C#의 표준 이벤트 시스템 (기능만 비슷해보일 뿐, 구조적으로는 완전히 다르다 한다.. From지피티)
                // - EventHandler는 기본 object를 매개변수로, 그리고 추가로 하나의 매개변수 타입을 < > 내에 받을 수 있다. < > 내에는 내장클래스, 사용자 정의 클래스 모두가 가능하다
                #endregion

                Thread listen_Thread = new Thread(Do_Listen);
                listen_Thread.Start();


            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private void on_Accept_Completed(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                Socket client_Socket = e.AcceptSocket;
                #region 공부정리
                // ★ e.AcceptSocket
                //  - e.AcceptSocket은, listen_Socket.AcceptAsync에서 받은, CSampleClient에 생성된 Socket이 아니라, 이 Socket에 대응되어, CSampleClient의 소켓과 연결하기 위해 CSampleServer 에서 만들어지는 소켓이다.
                //  - 따라서, CSampleServer의 CGameUser에서 socket.send는, 위 e.acceptSocket에서 발송하여, listen_Socket.AcceptAsync에서 받은, CSampleClient의 Socket에 발송하는 것
                #endregion

                flow_Control_Event.Set();

                if (callback_On_Newclient != null)
                {
                    callback_On_Newclient(client_Socket, e.UserToken);
                }

                return;
                #region 공부정리
                // ○ flow_control_event.Set(); 과 return
                //  - 위 코드에서, return을 없애고, flow_control_event.Set();을 if-else 문 밑에 하나만 위치해도 되지 않나 했는데, 지피티한테 물어보니
                //  callback_on_newClient 에서 오류가 발생해도, flow_control_event.Set();은 발동되기 위함이라 함
                #endregion
            }
            else
            {
                Console.WriteLine("Failed To Accept Client");
            }

            flow_Control_Event.Set();
            #region 공부정리
            // 앞서 AutoResetEvent 의 정수 예시로 설명했듯이, !pending일시 Set (+1) -> WaitOne(0) 순서로, 멈춤없이 바로 진행
            #endregion
        }
        private void Do_Listen()
        {
            flow_Control_Event = new AutoResetEvent(false);
            #region 공부정리
            // ○ AutoResetEvent 추가
            // - AutoResetEvent의 WaitOne, Set 관련 추가로, AutoResetEvent(instance)의 상태를 정수로 예시로 들어 설명. 상태정수의 최댓값을 1, 최솟값을 -1로 하고, 상태정수가 0, 1 일 때에는 비동기매서드가 작동. -1 일 때에는
            //   비동기매서드가 멈춰있다 고 할 때, set은 상태정수를 ++, WaitOne은 상태정수를 -- 한다 보면 된다. 
            //   new AutoResetEvent(true)일시, 상태정수가 1로 시작. new AutoResetEvent(false)일시, 상태정수가 0으로 시작
            //   따라서 true 일시, wait wait 으로 두번 해야 비동기 매서드가 정지. false 일시 wait 한번만 해도 비동기 매서드가 정지.
            // - 대부분 false로 설정한다 함
            #endregion

            while (true)
            {
                accept_Args.AcceptSocket = null;
                #region 공부정리
                // ○ SocketAsyncEventArgs(instance).acceptSocket = null
                //  - 기존에 받은 클라이언트 소켓을 초기화하고, 새로 받기 위해 null로 설정
                //  - SocketAsyncEventArgs(instance) = new SocketAsyncEventArgs(); 로 새로 생성하는 것보다 연산 효율이 좋기 때문에 이렇게 코드로 나눈건가..
                #endregion

                bool pending;
                try
                {
                    pending = listen_Socket.AcceptAsync(accept_Args);
                    #region 공부정리
                    // ○ AcceptAsync
                    //  - Accept 와 기능이 유사한 비동기 매서드. 연결요청이 들어오기를 비동기로 대기하고, 연결이 수락되면 매개변수로 받은 SocketAsynvEventArgs(instance).Completed 이벤트가 발동함
                    // 
                    // ○ pending
                    //  - AcceptAsync(..)는 바로 수락이 될시, SocketAsyncEventArgs의 Completed 이벤트가 발동하지 않음. 바로 수락되지 않고 시간이 걸려 수락될 시(비동기 작업이 아직 끝나지 않을시)
                    //    True를 반환하고, Completed 이벤트가 수락시 발동. 
                    //    바로 수락될시(비동기 작업이 바로 끝날 시) False를 반환하고, 아래 코드로 !pending일 시, on_Accept_Completed를 수동 발동 처리
                    #endregion
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }

                if (!pending)
                {
                    on_Accept_Completed(null, accept_Args);
                }

                flow_Control_Event.WaitOne();
            }
        }
    }
}
