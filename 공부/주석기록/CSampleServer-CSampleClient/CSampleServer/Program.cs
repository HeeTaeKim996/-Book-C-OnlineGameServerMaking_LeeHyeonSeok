using System;
using System.Collections.Generic;
using FreeNet;

namespace CSampleServer
{
    class Program
    {
        private static List<CGameUser> userList;

        static void Main(string[] args)
        {
            CPacketBufferManager.Initialize(20_000);
            userList = new List<CGameUser>();

            CNetworkService service = new CNetworkService();

            service.session_created_callback = on_session_created;

            service.Initialize();
            service.listen("0.0.0.0", 7979, 100);

            Console.WriteLine("Started !");
            while (true)
            {
                Thread.Sleep(1_000);
                #region 공부정리
                // ○ 서버 프로그램이 종료되지 않도록, 무한루프
                #endregion
            }
        }

        private static void on_session_created(CUserToken token)
        {
            CGameUser user = new CGameUser(token);
            lock (userList)
            {
                userList.Add(user);
            }
        }


        public static void remove_user(CGameUser user)
        {
            lock (userList)
            {
                userList.Remove(user);
            }
        }
    }

}