using System.Net.Sockets;
using FreeNet;


namespace CSampleClient
{
    using System.Net;
    using GameServer;
    class Program
    {
        private static List<CRemoteServerPeer> game_servers;

        static void Main(string[] args)
        {
            CPacketBufferManager.Initialize(10);
            game_servers = new List<CRemoteServerPeer>();


            CNetworkService cNetworkService = new CNetworkService();
            CConnector cConnector = new CConnector(cNetworkService);

            cConnector.connected_callback += On_connected_gameServer;
            IPEndPoint remote_endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 7979);
            cConnector.Connect(remote_endPoint);

            while (true)
            {
                Console.Write("> ");
                string send_text = Console.ReadLine();

                if (send_text == "q") break;

                CPacket msg = CPacket.Create((short)PROTOCOL.CHAT_MSG_REQ);
                msg.Push(send_text);
                game_servers[0].Send(msg);
            }

            game_servers[0].token.Disconnect();

            Console.ReadKey(); // 이게 있는 이유는 모르겠다. 추후 필요한 건가
        }

        private static void On_connected_gameServer(CUserToken newToken)
        {
            CRemoteServerPeer newServer = new CRemoteServerPeer(newToken);
            lock (game_servers)
            {
                game_servers.Add(newServer);
            }
            Console.WriteLine("서버와 연결 성공했습니다");
        }
    }
}