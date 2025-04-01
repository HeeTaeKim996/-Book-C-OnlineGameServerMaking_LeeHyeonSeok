using System.Runtime.CompilerServices;
using System.Text;

namespace FreeNet
{
    public class CPacket 
    {
        public IPeer owner { get; private set; }
        public byte[] buffer { get; private set; }
        public int position { get; private set; }
        public Int16 protocol_id { get; private set; }


        public CPacket()
        {
            this.buffer = new byte[1024];
        }
        public CPacket(byte[] source, IPeer owner)
        {
            this.buffer = source;
            this.owner = owner;
            position = Defines.HEADERSIZE;
        }


        public static CPacket Create(Int16 protocol_id)
        {
            CPacket packet = CPacketBufferManager.Pop();
            packet.Set_protocol_id(protocol_id);
            return packet;
        }
        public static void Destroy(CPacket packet)
        {
            CPacketBufferManager.Push(packet);
        }


        public void Set_protocol_id(Int16 protocol_id)
        {
            this.protocol_id = protocol_id;
            position = Defines.HEADERSIZE;

            Push(protocol_id);
        }
        public Int16 Pop_protocol_id()
        {
            return Pop_int16();
        }


        public void Copy_to(CPacket copiedTarget)
        {
            copiedTarget.Set_protocol_id(this.protocol_id);
            copiedTarget.Overwrite(this.buffer, this.position);
        }
        public void Overwrite(byte[] source, int position)
        {
            Buffer.BlockCopy(source, 0, this.buffer, 0, source.Length);
            this.position = position;
        }



        public byte Pop_byte()
        {
            byte data = (byte)BitConverter.ToInt16(this.buffer, this.position);
            this.position += sizeof(byte);
            return data;
        }
        public Int16 Pop_int16()
        {
            Int16 data = BitConverter.ToInt16(this.buffer, this.position);
            this.position += sizeof(Int16);
            return data;
        }
        public Int32 Pop_Int32()
        {
            int data = BitConverter.ToInt32(this.buffer, this.position);
            this.position += sizeof(Int32);
            return data;
        }
        public string Pop_string()
        {
            Int16 len = BitConverter.ToInt16(this.buffer, this.position);
            this.position += sizeof(Int16);

            string data = Encoding.UTF8.GetString(this.buffer, this.position, len);
            this.position += len;

            return data;
        }



        public void Push(byte data)
        {
            byte[] push_buffer = new byte[] { data };
            Buffer.BlockCopy(push_buffer, 0, this.buffer, position, sizeof(byte));
            this.position += sizeof(byte);
        }
        public void Push(Int16 data)
        {
            byte[] push_buffer = BitConverter.GetBytes(data);
            Buffer.BlockCopy(push_buffer, 0, this.buffer, position, sizeof(Int16));
            this.position += sizeof(Int16);
        }
        public void Push(Int32 data)
        {
            byte[] push_buffer = BitConverter.GetBytes(data);
            Buffer.BlockCopy(push_buffer, 0, this.buffer, position, sizeof(int));
            this.position += sizeof(int);
        }
        public void Push(string data)
        {
            byte[] push_stringBuffer = Encoding.UTF8.GetBytes(data);
            Int16 len = (Int16)push_stringBuffer.Length;
            byte[] push_lenBuffer = BitConverter.GetBytes(len);

            Buffer.BlockCopy(push_lenBuffer, 0, this.buffer, position, sizeof(Int16));
            position += sizeof(Int16);

            Buffer.BlockCopy(push_stringBuffer, 0, this.buffer, position, len);
            position += len;
        }


        public void Record_size()
        {
            Int16 bodyLen = (Int16)(position - Defines.HEADERSIZE);
            byte[] push_headerBuffer = BitConverter.GetBytes(bodyLen);
            Buffer.BlockCopy(push_headerBuffer, 0, this.buffer, 0, Defines.HEADERSIZE);
        }
        
    }
}