using System.Net.Sockets;


namespace FreeNet
{
    internal class BufferManager
    {
        private byte[] total_buffer;
        private int total_size;
        private int buffer_size;
        private Stack<int> buffer_index_pool;
        private int current_index;

        public BufferManager(int total_size, int buffer_size)
        {
            this.total_size = total_size;
            this.buffer_size = buffer_size;

            this.buffer_index_pool = new Stack<int>();
            this.current_index = 0;
        }

        public void InitBuffer()
        {
            this.total_buffer = new byte[this.total_size];
        }

        public bool SetBuffer(SocketAsyncEventArgs args)
        {
            if (this.buffer_index_pool.Count > 0)
            {
                args.SetBuffer(total_buffer, buffer_index_pool.Pop(), buffer_size);
            }
            else
            {
                if (total_size >= current_index + buffer_size)
                {
                    args.SetBuffer(total_buffer, current_index, buffer_size);
                    this.current_index += buffer_size;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        public void FreeBuffer(SocketAsyncEventArgs args)
        {
            this.buffer_index_pool.Push(args.Offset);
            args.SetBuffer(null, 0, 0);
        }
    }
}