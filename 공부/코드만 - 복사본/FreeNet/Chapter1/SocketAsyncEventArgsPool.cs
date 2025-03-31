using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace FreeNet
{
    internal class SocketAsyncEventArgsPool
    {
        Stack<SocketAsyncEventArgs> m_Pool;

        public SocketAsyncEventArgsPool(int capacity)
        {
            m_Pool = new Stack<SocketAsyncEventArgs>(capacity);
        }

        public void Push(SocketAsyncEventArgs item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("Items added to a SocketAsyncEventArgsPool cannot be null");
            }
            lock (m_Pool)
            {
                m_Pool.Push(item);
            }
        }
        public SocketAsyncEventArgs Pop()
        {
            lock (m_Pool)
            {
                return m_Pool.Pop();
            }
        }
        public int Count
        {
            get
            {
                return m_Pool.Count;
            }
        }
    }
}
