using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FreeNet;

namespace CSampleClient
{
    using System.Data.SqlTypes;
    using GameServer;
    internal class CRemoteServerPeer : IPeer
    {
        public CUserToken token { get; private set; }
        public CRemoteServerPeer(CUserToken token)
        {
            this.token = token;
            this.token.Set_peer(this);
        }

        void IPeer.On_message(Const<byte[]> buffer)
        {
            CPacket msg = new CPacket(buffer.Value, this);
            PROTOCOL protocol_id = (PROTOCOL)msg.Pop_protocol_id();
            switch (protocol_id)
            {
                case PROTOCOL.CHAT_MSG_ACK:
                    {
                        string text = msg.Pop_string();
                        Console.WriteLine($"text {text}");
                    }
                    break;
            }
        }

        void IPeer.On_removed()
        {
            Console.WriteLine("Server Removed");
        }
        void IPeer.Send(CPacket msg)
        {
            this.token.Send(msg);
        }
        void IPeer.Disconnect()
        {
            this.token.socket.Disconnect(false);
        }
        void IPeer.Process_user_operation(CPacket msg) { }
    }
}
