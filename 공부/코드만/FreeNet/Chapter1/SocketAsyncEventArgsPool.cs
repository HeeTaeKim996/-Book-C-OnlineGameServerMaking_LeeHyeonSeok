using System.Net.Sockets;


namespace FreeNet
{
    internal class SocketAsyncEventArgsPool
    {
        private Stack<SocketAsyncEventArgs> args_pool;
        
        public SocketAsyncEventArgsPool(int pool_capacity)
        {
            args_pool = new Stack<SocketAsyncEventArgs>(pool_capacity);
        }
        public SocketAsyncEventArgs Pop()
        {
            lock (args_pool)
            {
                return args_pool.Pop();
            }
        }
        public void Push(SocketAsyncEventArgs args)
        {
            if(args == null)
            {
                throw new ArgumentNullException("SokcetAsyncEventArgsPool : args_pool 에 Push 하는 args는 null 일 수 없습니다");
            }
            lock (args_pool)
            {
                args_pool.Push(args);
            }
        }
        public int Count
        {
            get
            {
                return args_pool.Count;
            }
        }
    }
}