using System.Net.Sockets;

namespace FreeNet
{
    public class CUserToken
    {
        public Socket socket { get; set; }
        public SocketAsyncEventArgs receive_arg { get; private set; }
        public SocketAsyncEventArgs send_arg { get; private set; }
        private CMessageResolver message_resolver;
        private IPeer peer;

        private Queue<CPacket> sending_queue;
        private object cs_sending_queue;

        public CUserToken()
        {
            message_resolver = new CMessageResolver();
            peer = null;
            sending_queue = new Queue<CPacket>();
            cs_sending_queue = new object();
        }

        public void Set_peer(IPeer peer)
        {
            this.peer = peer;
        }
        public void Set_args(SocketAsyncEventArgs receive_arg, SocketAsyncEventArgs send_arg)
        {
            this.receive_arg = receive_arg;
            this.send_arg = send_arg;
        }

        public void On_receive(byte[] buffer, int offset, int transferred)
        {
            this.message_resolver.on_receive(buffer, offset, transferred, On_message);
        }
        private void On_message(Const<Byte[]> buffer)
        {
            this.peer.On_message(buffer);
        }

        public void Send(CPacket msg)
        {
            CPacket clone = new CPacket();
            msg.copy_to(clone);

            lock (cs_sending_queue)
            {
                if(this.sending_queue.Count <= 0)
                {
                    this.sending_queue.Enqueue(clone);
                    Start_send();
                }
                else
                {
                    Console.WriteLine("sending_queue가 아직 비지 않았습니다. 메세지를 Enqueue 합니다");
                    this.sending_queue.Enqueue(clone);
                }
            }
        }

        private void Start_send()
        {
            lock (cs_sending_queue)
            {
                CPacket msg = sending_queue.Peek();
                msg.record_size();

                this.send_arg.SetBuffer(this.send_arg.Offset, msg.position);
                Buffer.BlockCopy(msg.buffer, 0, this.send_arg.Buffer, this.send_arg.Offset, msg.position);

                bool pending = socket.SendAsync(send_arg);
                if (!pending)
                {
                    Process_send(send_arg);
                }
            }
        }



        private static int sent_count = 0;
        private static object cs_sent_count = new object();
        public void Process_send(SocketAsyncEventArgs e)
        {
            lock (cs_sending_queue)
            {
                if (e.BytesTransferred <= 0 || e.SocketError != SocketError.Success) return;

                int size = this.sending_queue.Peek().position;
                if (e.BytesTransferred != size)
                {
                    Console.WriteLine($"실제로 보낸 메세의 크기와, sending_queue.Peek() 의 크기가 일치하지 않습니다. 다시 보내야 합니다. transferred : {e.BytesTransferred}, peek_size : {size}");
                    return;
                }

                if (this.sending_queue.Count <= 0)
                {
                    throw new Exception("sneding_queue.Coun 가 0보다 작습니다. (소스코드를 보니 이럴 확률은 없다 보면 되지만, 혹시나 해서 넣어놓은듯 싶음)");
                }



                lock (cs_sent_count)
                {
                    sent_count++;
                    Console.WriteLine($"보낸 소켓 : {e.SocketError}, Transferred : {e.BytesTransferred}, send_count : {sent_count}");
                }
                sending_queue.Dequeue();
                if(sending_queue.Count > 0)
                {
                    Start_send();
                }
                
            }
        }

        public void On_removed()
        {
            this.sending_queue.Clear();
            if(this.peer != null)
            {
                peer.On_removed();
            }
        }

        public void Disconnect()
        {
            try
            {
                this.socket.Shutdown(SocketShutdown.Send);
            }
            catch (Exception) { }

            this.socket.Close();
        }

        public void Start_keepAlive()
        {
            Timer keepAlive = new Timer((object e) =>
            {
                CPacket msg = CPacket.create(0);
                msg.push(0);
                Send(msg);
            }, null, 0, 3_000);
        }

    }
}