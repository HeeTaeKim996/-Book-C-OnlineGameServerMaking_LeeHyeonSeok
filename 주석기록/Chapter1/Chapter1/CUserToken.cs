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
            cs_sending_queue = new object();

            message_resolver = new CMessageResolver();
            peer = null;
            sending_queue = new Queue<CPacket>();
        }
    }
}
