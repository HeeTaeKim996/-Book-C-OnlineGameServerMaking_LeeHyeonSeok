using FreeNet;

namespace CSampleClient
{
    using GameServer;
    internal class CRemoteServerPeer : IPeer
    {
        public CUserToken token { get; private set; }
        public CRemoteServerPeer(CUserToken token)
        {
            this.token = token;
            this.token.Set_peer(this);
        }

        void IPeer.On_message(FreeNet.Const<byte[]> buffer)
        {
            CPacket msg = new CPacket(buffer.Value, this);
            PROTOCOL protocol = (PROTOCOL)msg.Pop_protocol_id();

            switch (protocol)
            {
                case PROTOCOL.CHAT_MSG_ACK:
                    {
                        string recv_text = msg.Pop_string();
                        Console.WriteLine($"Text : {recv_text}");
                    }
                    break;
            }
        }

        public void Send(CPacket msg)
        {
            this.token.Send(msg);
        }

        void IPeer.On_removed()
        {
            Console.WriteLine("서버와 연결이 해제됐습니다");
        }

        void IPeer.Disconnect()
        {
            this.token.socket.Disconnect(false);
        }

        void IPeer.Process_user_operation(CPacket msg){ }
    }
}