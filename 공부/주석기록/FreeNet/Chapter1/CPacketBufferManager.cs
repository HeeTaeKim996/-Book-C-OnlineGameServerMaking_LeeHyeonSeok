using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeNet
{
    public class CPacketBufferManager
    {
        private static object cs_buffer = new object();
        private static Stack<CPacket> pool;
        private static int pool_capacity;

        public static void Initialize(int capacity)
        {
            pool = new Stack<CPacket>();
            pool_capacity = capacity;
            allocate();
        }

        private static void allocate()
        {
            for(int i = 0; i < pool_capacity; i++)
            {
                pool.Push(new CPacket());
            }
        }

        public static CPacket pop()
        {
            lock (cs_buffer)
            {
                if(pool.Count <= 0)
                {
                    Console.WriteLine("ReAllocate.");
                    allocate();
                }

                return pool.Pop();
            }
        }

        public static void Push(CPacket packet)
        {
            lock (cs_buffer)
            {
                pool.Push(packet);
            }
        }
    }
}
