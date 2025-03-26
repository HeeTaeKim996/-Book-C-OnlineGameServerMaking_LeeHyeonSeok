using System.Net;
using System.Net.Sockets;


namespace Server
{
    public class CNetworkService
    {
        CListener client_listener;

        SocketAsyncEventArgsPool receive_event_args_pool;
        SocketAsyncEventArgsPool send_event_args_pool;

        BufferManager bufferManager = new BufferManager(max_connections * buffer_size * pre_alloc_count, buffer_size);

        public delegate void SessionHandler(CUserToken token);
        public SessionHandler session_created_callback { get; set; }

        
        public void Listen(string host, int port, int backlog)
        {

        }
    }

    internal class CListener
    {
        private SocketAsyncEventArgs accept_args;

        private Socket listen_socket;

        private AutoResetEvent flow_control_event;

        public delegate void NewClientHandler(Socket client_socket, object token);
        public NewClientHandler callback_on_newClient;

        public CListener()
        {
            callback_on_newClient = null;
        }

        public void Start(string host, int port, int backlog)
        {
            listen_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

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


            try
            {
                listen_socket.Bind(endPoint);
                listen_socket.Listen(backlog);

                accept_args = new SocketAsyncEventArgs();
                accept_args.Completed += new EventHandler<SocketAsyncEventArgs>(On_Accept_Completed);



                void On_Accept_Completed(object sender, SocketAsyncEventArgs e)
                {
                    if(e.SocketError == SocketError.Success)
                    {
                        Socket clientSocket = e.AcceptSocket;

                        flow_control_event.Set();

                        if(callback_on_newClient != null)
                        {
                            callback_on_newClient(clientSocket, e.UserToken);
                        }

                        return;
                    }
                    else
                    {
                        Console.WriteLine("Failed to accept client");
                    }

                    flow_control_event.Set();
                }

                void DoListen()
                {
                    flow_control_event = new AutoResetEvent(false);

                    while (true)
                    {
                        accept_args.AcceptSocket = null;
                        bool pending = true;

                        try
                        {
                            pending = listen_socket.AcceptAsync(accept_args);
                        }
                        catch(Exception e)
                        {
                            Console.WriteLine(e.Message);
                            continue;
                        }

                        if (!pending)
                        {
                            On_Accept_Completed(null, accept_args);
                        }

                        flow_control_event.WaitOne();
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

    }
    
    internal class SocketAsyncEventArgsPool
    {
        Stack<SocketAsyncEventArgs> m_pool;

        public SocketAsyncEventArgsPool(int capacity)
        {
            m_pool = new Stack<SocketAsyncEventArgs>(capacity);
        }
        public void Push(SocketAsyncEventArgs item)
        {
            if(item == null)
            {
                throw new ArgumentNullException("Items added to a SocketAsyncEventArgsPool cannot be null");
            }
            lock (m_pool)
            {
                m_pool.Push(item);
            }
        }

        public SocketAsyncEventArgs Pop()
        {
            lock (m_pool)
            {
                return m_pool.Pop();
            }
        }
        public int Count
        {
            get
            {
                return m_pool.Count;
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
            #region 공부정리
            // args.Offset : SetBuffer의 (2)값을 SocketAsyncEventArgs(instance).offset 으로 저장
            #endregion
            args.SetBuffer(null, 0, 0);
        }

    }
}