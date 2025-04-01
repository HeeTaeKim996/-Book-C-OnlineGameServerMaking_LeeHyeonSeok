

namespace FreeNet
{
    internal class CMessageResolver
    {
        private byte[] message_buffer;
        private int remain_bytes;
        private int quantity_to_read;
        private int current_position;

        public delegate void CompleteMessageCallback(Const<byte[]> buffer);


        public CMessageResolver()
        {
            this.message_buffer = new byte[1024];
            this.remain_bytes = 0;
            this.quantity_to_read = 0;
            this.current_position = 0;
        }


        public void On_receive(byte[] buffer, int offset, int transferred, CompleteMessageCallback callback)
        {
            this.remain_bytes = transferred;
            int src_position = offset;

            while (this.remain_bytes > 0)
            {
                bool completed;

                if (current_position < Defines.HEADERSIZE)
                {
                    quantity_to_read = Defines.HEADERSIZE;

                    completed = Read_until(buffer, ref src_position, offset, transferred);
                    if (!completed) return;

                    quantity_to_read += Get_body_size();
                }


                completed = Read_until(buffer, ref src_position, offset, transferred);
                if (completed)
                {
                    callback(new Const<byte[]>(message_buffer));
                    Clear_buffer();
                }
            }
        }
        private bool Read_until(byte[] buffer, ref int src_position, int offset, int transferred)
        {
            if (src_position >= offset + transferred) return false;


            int copy_size = quantity_to_read - current_position;
            if (remain_bytes < copy_size)
            {
                copy_size = remain_bytes;
            }


            Buffer.BlockCopy(buffer, src_position, this.message_buffer, current_position, copy_size);

            this.remain_bytes -= copy_size;
            src_position += copy_size;
            this.current_position += copy_size;


            if (this.current_position < this.quantity_to_read) return false;

            return true;
        }

        private int Get_body_size()
        {
            Type type = Defines.HEADERSIZE.GetType();

            if (type.Equals(typeof(Int16)))
            {
                return BitConverter.ToInt16(this.message_buffer);
            }
            else
            {
                return BitConverter.ToInt32(this.message_buffer);
                // 의도가 뭔지 모르겠다. else 로  Int32 리턴을 왜 했을까
            }
        }

        private void Clear_buffer()
        {
            Array.Clear(this.message_buffer, 0, this.message_buffer.Length);
            current_position = 0;
        }
    }
}