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
                    
                }
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
                #endregion
            }
        }



    }
}
