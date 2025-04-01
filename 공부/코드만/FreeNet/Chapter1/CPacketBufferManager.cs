

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
            lock (cs_cPacket_pool)
            {
                cPacket_pool.Push(packet);
            }
        }
    }
}