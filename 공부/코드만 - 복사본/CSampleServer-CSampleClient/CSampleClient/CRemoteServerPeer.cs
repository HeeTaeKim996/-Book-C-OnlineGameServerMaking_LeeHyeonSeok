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
            this.token.set_peer(this);
        }

        void IPeer.on_message(Const<byte[]> buffer)
        {
            CPacket msg = new CPacket(buffer.Value, this);
            PROTOCOL protocol_id = (PROTOCOL)msg.pop_protocol_id();
            switch (protocol_id)
            {
                case PROTOCOL.CHAT_MSG_ACK:
                    {
                        string text = msg.pop_string();
                        Console.WriteLine($"text {text}");
                    }
                    break;
            }
        }

        void IPeer.on_removed()
        {
            Console.WriteLine("Server Removed");
        }
        void IPeer.send(CPacket msg)
        {
            this.token.send(msg);
        }
        void IPeer.disconnect()
        {
            this.token.socket.Disconnect(false);
        }
        void IPeer.process_user_operation(CPacket msg) { }
    }
}
