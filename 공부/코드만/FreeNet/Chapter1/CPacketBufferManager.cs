

namespace FreeNet
{
    public class CPacketBufferManager
    {
        private static int pool_capacity;
        private static Stack<CPacket> cPacket_pool;
        private static object cs_cPacket_pool;

        public static void Initialize(int capacity)
        {
            pool_capacity = capacity;
            cPacket_pool = new Stack<CPacket>(pool_capacity);
            cs_cPacket_pool = new object();
            Allocate();
        }

        private static void Allocate()
        {
            lock (cs_cPacket_pool)
            {
                for (int i = 0; i < pool_capacity; i++)
                {
                    cPacket_pool.Push(new CPacket());
                }
            }
        }

        public static CPacket Pop()
        {
            lock (cs_cPacket_pool)
            {
                if (cPacket_pool.Count <= 0)
                {
                    Console.WriteLine("CPacketBufferManager : CPacketBufferManager.Pop()을 시행했지만, cPacket_pool.Count <= 0 이라, cPacket을 재생성함. 초기화 때 더 많은 capacity 필요 예상됨");
                    Allocate();
                }

                return cPacket_pool.Pop();
            }
        }
        
        public static void Push(CPacket packet)
        {
            // @@@@@@@@@@@@@@@@@@@@@ !!!!!!!!!!!!!!!!!!!!!!!!!!! 작성기준 유니티 전까지 만들었는데, Push를 해야할 CUserToken의 Destroy를 발동하는 경우가 없다. CSampleServer와 CSampleClient에서 CPacket packet = new CPacket으로 처리하는데, 
            // CPacket을 Destroy처리 하지 않고 있다. 아마 원래에는 Pop을 사용하고, 사용이 다하면 Destroy처리를 해야하는 것 같은데, VirusGame을 만들 때에는 이 클래스의 Pop과 CPacket의 Destroy를 할지 보고, 만약 그 때도 처리하지 않는다면, 수정하자
            lock (cs_cPacket_pool)
            {
                cPacket_pool.Push(packet);
            }
        }
    }
}