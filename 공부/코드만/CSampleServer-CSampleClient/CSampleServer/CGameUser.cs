using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FreeNet;

namespace CSampleServer
{
    using GameServer;
    internal class CGameUser : IPeer
    {
        CUserToken token;

        public CGameUser(CUserToken token)
        {
            this.token = token;
            this.token.Set_peer(this);
        }

        void IPeer.On_message(Const<byte[]> buffer)
        {
            CPacket msg = new CPacket(buffer.Value, this);
            PROTOCOL protocol = (PROTOCOL)msg.pop_protocol_id();
            Console.WriteLine("------------------------------------------------");
            Console.WriteLine("Protocol id : " + protocol);
            switch (protocol)
            {
                case PROTOCOL.CHAT_MSG_REQ:
                    {
                        string text = msg.pop_string();
                        Console.WriteLine($"text {text}");

                        CPacket response = CPacket.create((short)PROTOCOL.CHAT_MSG_ACK);
                        response.push(text);
                        Send(response);
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
            Console.WriteLine("The client disconnected");

            Program.remove_user(this);
        }

        void IPeer.Disconnect()
        {
            this.token.socket.Disconnect(false);
            #region 공부정리
            // ○ Socket(instance).Disconnect(bool) : 공통적으로 소켓 연결 해제. bool = false일시, 소켓을 재사용하지 않고 관련 자원을 해제. true일시, 재사용 가능하도록, 소켓핸들을 유지해서, 다른 원격주소나 포트에 연결 가능하도록 처리
            #endregion
        }

        void IPeer.Process_user_operation(CPacket msg) { }
    }
}
