using FreeNet;

namespace CSampleServer
{
    using GameServer;

    internal class CGameUser : IPeer
    {
        private CUserToken token;

        public CGameUser(CUserToken userToken)
        {
            this.token = userToken;
            this.token.Set_peer(this);
        }

        void IPeer.On_message(FreeNet.Const<byte[]> buffer)
        {
            CPacket msg = new CPacket(buffer.Value, this);
            PROTOCOL protocol = (PROTOCOL)msg.Pop_protocol_id();

            switch (protocol)
            {
                case PROTOCOL.CHAT_MSG_REQ:
                    {
                        string recv_message = msg.Pop_string();
                        Console.WriteLine($"Text : {recv_message}");

                        CPacket packet = CPacket.Create((short)PROTOCOL.CHAT_MSG_ACK);
                        packet.Push(recv_message);
                        Send(packet);
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
            Console.WriteLine("클라이언트와 연결이 해제됐습니다");

            Program.Remove_user(this);
        }

        void IPeer.Disconnect()
        {
            this.token.socket.Disconnect(false);
        }

        void IPeer.Process_user_operation(FreeNet.CPacket msg) { }
    }
}