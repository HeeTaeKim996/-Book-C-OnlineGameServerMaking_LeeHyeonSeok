using System.Runtime.CompilerServices;
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


            CNetworkService cNetworkService = new CNetworkService();
            cNetworkService.session_created_callback += On_session_created;
            cNetworkService.Initialize();
            cNetworkService.Listen("0.0.0.0", 7979, 100);


            Console.WriteLine("Server Started!");
            while (true)
            {
                Thread.Sleep(1_000);
            }
        }


        private static void On_session_created(CUserToken newToken)
        {
            CGameUser newUser = new CGameUser(newToken);
            lock (userList)
            {
                userList.Add(newUser);
            }
        }
        public static void Remove_user(CGameUser removingUser)
        {
            lock (userList)
            {
                userList.Remove(removingUser);
            }
        }
    }

}