

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {


        }

    }

    public class CNetworkService
    {
        CListener client_listener;

        SocketAsyncEventArgsPool receive_Event_Args_Pool;
        SocketAsyncEventArgsPool send_Event_Args_Pool;




        BufferManager buffer_Manager = new BufferManager(max_Connections* buffer_Size * pre_Alloc_Count, buffer_Size);


        public delegate void SessionHandler(CUserToken token);
        public SessionHandler session_created_callback { get; set; }



        public void listen(string host, int port, int backlog)
        {
            CListener listener = new CListener();
            listener.callback_On_Newclient += on_New_Client;
            listener.Start(host, port, backlog);

            // @@@ 여기부터 교재가 좀 이상한 것 같은데, 여기에 코드를 적는게 맞나?
            receive_Event_Args_Pool = new SocketAsyncEventArgsPool(max_Connections);
            send_Event_Args_Pool = new SocketAsyncEventArgsPool(max_Connections);

            for(int i = 0; i < max_Connections; i++)
            {
                CUserToken token = new CUserToken();


                // receive Pool (교재내용그대로)
                {
                    SocketAsyncEventArgs arg = new SocketAsyncEventArgs();
                    arg.Completed += new EventHandler<SocketAsyncEventArgs>(receive_Completed);
                    arg.UserToken = token;

                    receive_Event_Args_Pool.Push(arg);
                }

                // sendPool (교재내용그대로)
                {
                    SocketAsyncEventArgs arg = new SocketAsyncEventArgs();
                    arg.Completed += new EventHandler<SocketAsyncEventArgs>(send_Completed);
                    arg.UserToken = token;

                    send_Event_Args_Pool.Push(arg);
                }
            }
        }
    }

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
                void Do_Listen()
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

                        bool pending = true;   // @@@ 어차피 listen_Socket.AcceptAsync(accept_Args) 는 바로 입력되면 false, 입력에 시간이 걸리면 true인데. 왜 굳이 = true를 붙인거지. 일단 = true로 해보고, 작동 잘되면 ; 만해서 해보자
                        try
                        {
                            pending = listen_Socket.AcceptAsync(accept_Args);
                            #region 공부정리
                            // ○ AcceptAsync
                            //  - Accept 와 기능이 유사한 비동기 매서드. 연결요청이 들어오기를 비동기로 대기하고, 연결이 수락되면 매개변수로 받은 SocketAsynvEventArgs(instance).Completed 이벤트가 발동함
                            // 
                            // ○ pending
                            //  - AcceptAsync(..)는 바로 수락이 될시, SocketAsyncEventArgs의 Completed 이벤트가 발동하지 않음. 바로 수락되지 않고 시간이 걸려 수락될 시, True를 반환하고, Completed 이벤트가 수락시 발동. 
                            //    바로 수락될시, False를 반환하고, 아래 코드로 !pending일 시, on_Accept_Completed를 수동 발동 처리
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
                //flow_Control_Event = new AutoResetEvent(false); // 교재에는 여기에 있는데, 오류인 것 같음..

                void on_Accept_Completed(object sender, SocketAsyncEventArgs e)
                {
                    if(e.SocketError == SocketError.Success)
                    {
                        Socket client_Socket = e.AcceptSocket;

                        flow_Control_Event.Set();

                        if(callback_On_Newclient != null)
                        {
                            callback_On_Newclient(client_Socket, e.UserToken);
                        }

                        return;
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
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }

    internal class SocketAsyncEventArgsPool
    {
        Stack<SocketAsyncEventArgs> m_Pool;

        public SocketAsyncEventArgsPool(int capacity)
        {
            m_Pool = new Stack<SocketAsyncEventArgs>(capacity);
        }

        public void Push(SocketAsyncEventArgs item)
        {
            if(item == null)
            {
                throw new ArgumentNullException("Items added to a SocketAsyncEventArgsPool cannot be null");
            }
            lock (m_Pool)
            {
                m_Pool.Push(item);
            }
        }
        public SocketAsyncEventArgs Pop()
        {
            lock (m_Pool)
            {
                return m_Pool.Pop();
            }
        }
        public int Count
        {
            get
            {
                return m_Pool.Count;
            }
        }
    }

    internal class BufferManager
    {
        int m_numBytes;
        byte[] m_buffer;
        Stack<int> m_freeIndexPool;
        int m_currentIndex;
        int m_bufferSize;

        public BufferManager(int totalBytes, int bufferSize)
        {
            m_numBytes = totalBytes;
            m_currentIndex = 0;
            m_bufferSize = bufferSize;
            m_freeIndexPool = new Stack<int>();
        }
        public void InitBuffer()
        {
            m_buffer = new byte[m_numBytes];
        }
        public bool SetBuffer(SocketAsyncEventArgs args)
        {
            if(m_freeIndexPool.Count > 0)
            {
                args.SetBuffer(m_buffer, m_freeIndexPool.Pop(), m_bufferSize);
                #region 공부정리
                // ○ SocketAsyncEventArgs(instance).SetBuffer 
                //  - 해당 인스턴스가 사용할 버퍼의 위치와 크기를 지정함
                //  - (1) : 참조되는 byte[]
                //  - (2) : 버퍼의 시작 인덱스
                //  - (3) : 사용할 버퍼의 크기
                //   => SocketAsynEventArgs Pool 이 공통의 m_buffer를 나눠 사용하는 구조이기 때문에, (2)항을 m_freeIndexPool.Pop()으로, (3)항을 m_bufferSize로 사용
                //      => 처음 시작하면, m_freeIndexPool.Count == 0 이고, else{ 구문이 작동. 사용을 다한 SocketasyncEventArgs는 하단의 FreeBuffers매서드를 통해 m_freeIndexPool로 푸시되어, 위 if(m_freeIndexPool.Count에서 사용됨) 
                #endregion
            }
            else
            {
                if( (m_numBytes - m_bufferSize) < m_currentIndex)
                {
                    return false;
                }
                args.SetBuffer(m_buffer, m_currentIndex, m_bufferSize);
                m_currentIndex += m_bufferSize;
            }
            return true;
        }
        public void FreeBuffers(SocketAsyncEventArgs args)
        {
            m_freeIndexPool.Push(args.Offset);
            args.SetBuffer(null, 0, 0);
        }
    }
    

}