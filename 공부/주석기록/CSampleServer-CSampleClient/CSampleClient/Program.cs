using System;
using System.Collections.Generic;
using System.Net;
using FreeNet;

namespace CSampleClient
{
    using System.Data.SqlTypes;
    using GameServer;

    class Program
    {
        static List<IPeer> game_servers = new List<IPeer>();

        static void Main(string[] args)
        {
            CPacketBufferManager.Initialize(2_000);
            CNetworkService service = new CNetworkService();
            CConnector connector = new CConnector(service);

            connector.connected_callback += on_connected_gameserver;
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 7979);
            connector.connect(endPoint);

            while (true)
            {
                Console.Write("> ");
                string line = Console.ReadLine();
                if (line == "q") break;

                CPacket msg = CPacket.create((short)PROTOCOL.CHAT_MSG_REQ);
                msg.push(line);
                game_servers[0].send(msg);
            }

            ((CRemoteServerPeer)game_servers[0]).token.disconnect();

            Console.ReadKey();
        }

        private static void on_connected_gameserver(CUserToken server_token)
        {
            lock (game_servers)
            {
                IPeer server = new CRemoteServerPeer(server_token);
                game_servers.Add(server);
                #region 공부정리 
                // ○ 매개변수 server_token은 위 Main의 CConnector 생성자에서 생성된 server_token이다. CSampleServer에서는 CListener를 통해 접속시도한 클라이언트 소켓에 대응되어 만들어진 e.AcceptSocket을 리스트로 관리하는 반면에,
                //   클라이언트는 CConnector생성자에서 생성된 토큰을, ConnectAsync하고, 비동기가 완료된 이 토큰을 game_servers 리스트에서 관리한다
                #endregion
                Console.WriteLine("Connected!");
            }
        }

    }
}