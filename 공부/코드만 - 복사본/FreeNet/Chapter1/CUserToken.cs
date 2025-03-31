using System.Net;
using System.Net.Sockets;
using System.Text;

namespace FreeNet
{
    public class CUserToken
    {
        public Socket socket { get; set; }
        public SocketAsyncEventArgs receive_event_args { get; private set; }
        public SocketAsyncEventArgs send_event_args { get; private set; }

        CMessageResolver message_resolver;

        IPeer peer;

        Queue<CPacket> sending_queue;

        private object cs_sending_queue;

        public CUserToken()
        {
            this.cs_sending_queue = new object();

            this.message_resolver = new CMessageResolver();
            this.peer = null;
            this.sending_queue = new Queue<CPacket>();
        }
        
        public void set_peer(IPeer peer)
        {
            this.peer = peer;
        }
        public void set_event_args(SocketAsyncEventArgs receive_event_args, SocketAsyncEventArgs send_event_args)
        {
            this.receive_event_args = receive_event_args;
            this.send_event_args = send_event_args;
        }
        public void on_receive(byte[] buffer, int offset, int transffered)
        {
            this.message_resolver.on_receive(buffer, offset, transffered, on_message);
        }
        void on_message(Const<byte[]> buffer)
        {
            if(this.peer != null)
            {
                this.peer.on_message(buffer);
            }
        }

        public void on_removed()
        {
            this.sending_queue.Clear();
            if(this.peer != null)
            {
                this.peer.on_removed();
            }
        }

        public void send(CPacket msg)
        {
            CPacket clone = new CPacket();
            msg.copy_to(clone);

            lock (this.cs_sending_queue)
            {
                if(this.sending_queue.Count <= 0)
                {
                    this.sending_queue.Enqueue(clone);
                    start_send();
                    return;
                }

                Console.WriteLine("Queue is not empty. Copy and Enqueue a message. protocol id : " + msg.protocol_id);
                this.sending_queue.Enqueue(clone);
            }
        }
        void start_send()
        {
            lock (this.cs_sending_queue)
            {
                CPacket msg = this.sending_queue.Peek();

                msg.record_size();

                this.send_event_args.SetBuffer(this.send_event_args.Offset, msg.position);
                #region 공부정리
                // ○ SetBuffer((2)항)
                //  - (3)항일 때에는 (1)_byte[], (2)시작인덱스, (3)_길이
                //  - (2)개의 항일 때에는, buffer (byte[]) 가 이미 설정돼있어야 한다. 이 때에는, (1)_시작인덱스, (2)_길이

                // ○ 
                //  - 위 send_event_args는 set_event_args에서 할당된 SocketAsyncEventArgs이며, 해당 매서드는 CNetworkService에서 호출한다. CNetworkService는 BufferManager를 통해 생성된 SocketAsyncEventArgs를 할당한다.
                //    BufferManager는 풀 로 SocketAsyncEventArgs를 관리하는데, 풀을 생성할 때, 전체 버퍼에서 일부를, 생성된 각 SocketAsyncEventArgs에 할당한다.
                //    예를 들어, 버퍼의 크기가 0 _100 이고, 전체 10개의 SocketAsyncEventArgs를 풀로 관리한다면, 두번째 풀은 10_19의 크기의 버퍼를 할당받는다. 만약 이 버퍼가 CUserToken(instance)에 할당된다 했을 때,
                //    여기 send에서 다시 SetBuffer를 사용하여, 메세지를 보낼 때 필요한 길이만큼 다시 SetBuffer로 할당한다. 만약 msg.position의 크기가 5라면, 할당된 버퍼는, 10_14가 된다. 
                //    msg.position <= 할당된 버퍼의 크기 는 보장된다 한다.(작성기준 아직 어떻게 보장되는지는 확인안함)
                #endregion
                Array.Copy(msg.buffer, 0, this.send_event_args.Buffer, this.send_event_args.Offset, msg.position);

                bool pending = this.socket.SendAsync(this.send_event_args);

                if (!pending)
                {
                    process_send(this.send_event_args);
                }
            }
        }

        static int sent_count = 0;
        static object cs_count = new object();

        public void process_send(SocketAsyncEventArgs e)
        {
            if (e.BytesTransferred <= 0 || e.SocketError != SocketError.Success) return;

            lock (this.cs_sending_queue)
            {
                if(this.sending_queue.Count <= 0)
                {
                    throw new Exception("Sending queue count is Less than zero");
                }

                int size = this.sending_queue.Peek().position;
                if(e.BytesTransferred != size)
                {
                    Console.WriteLine($"Need to send more! transffered {e.BytesTransferred}, packetSize {size}");
                    return;
                }

                lock (cs_count)
                {
                    ++sent_count;

                    Console.WriteLine($"process sent : {e.SocketError}, transfered {e.BytesTransferred}, send count {sent_count}");
                }

                this.sending_queue.Dequeue();

                if(this.sending_queue.Count > 0)
                {
                    start_send();
                }
            }
        }

        public void disconnect()
        {
            try
            {
                this.socket.Shutdown(SocketShutdown.Send);
            }
            catch (Exception) { }
            this.socket.Close();
        }

        public void start_keepAlive()
        {
            System.Threading.Timer keepAlive = new System.Threading.Timer((object e) =>
            {
                CPacket msg = CPacket.create(0);
                msg.push(0);
                send(msg);
            }, null, 0, 3000);
            #region 공부정리
            // ○ Timer
            //  - (1) : 델리게이트 object Method(object state) => 의 델리게이트
            //  - (2) : (1) 델리게이트의 인자 object인 state. 위 코드에서는 null로 할당
            //  - (3) : dueTime _ 처음 시작할때까지 기다리는 밀리세컨드
            //  - (4) : 발동후 다시 재개할때까지 기다리는 밀리세컨드
            //   => 위 코드는, ipProtocol 이 0, 담겨진 데이터도 0인 메세지를, 3초마다 보내도록 처리
            #endregion
        }

    }
}
