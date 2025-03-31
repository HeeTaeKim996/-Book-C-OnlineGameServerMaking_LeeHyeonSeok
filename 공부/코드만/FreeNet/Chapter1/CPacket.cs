using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeNet
{
    public class CPacket
    {
        public IPeer owner { get; private set; }
        public byte[] buffer { get; private set; }
        public int position { get; private set; }
        public Int16 protocol_id { get; private set; }
        public static CPacket create(Int16 protocol_id)
        {
            CPacket packet = CPacketBufferManager.pop();
            packet.set_protocol(protocol_id);
            return packet;
        }

        public static void destroy(CPacket packet)
        {
            CPacketBufferManager.Push(packet);
        }

        public CPacket(byte[] buffer, IPeer owner)
        {
            this.buffer = buffer;
            position = Defines.HEADERSIZE;
            this.owner = owner;
        } 
        public CPacket()
        {
            buffer = new byte[1024];
        }
        #region 공부정리
        // ○ 최적화된 byte[]를 사용하지 않는 이유
        //  - 이 클래스와, CPAcketBufferManager를 보면 알 수 있듯이, new byte[1024]로 생성된 바이트들을 풀링으로 재사용해서 송수신에 사용하는 것을 알 수 있다. 
        //  - 그런데, 아래 코드들을 보면 알 수 있듯이, protocol_id 버퍼 -> [실데이터 버퍼 -> 실데이터사이즈버퍼 => 실데이터버퍼] -> 헤더 버퍼 순으로 byte[]들이 채워지는데, 그렇다면 굳이 new byte[1024]로 생성하지 말고,
        //    최적화된 크기의 byte[]를 생성해서 보내는 것이 더 효율적이지 않나 라고 생각될 수 있는데,
        //    최적화된 크기의 byte[]를 생성하여 발송하는 것보다, 넉넉한 크기의 new byte[1024] 로 생성하여 오브젝트풀로 재사용하는 것이 더 효율적이라 한다.
        //  - 따라서, 아래 코드처럼 headerBuffer와 실데이터버퍼의 사이즈버퍼가 하는 역할은, 최적화된 크기의 buffer를 만드는 것이 아니라, 수신자가 해당 버퍼를 얼마만큼 읽어낼지 에 대한 설명서라 한다.
        #endregion

        public void copy_to(CPacket target)
        {
            target.set_protocol(protocol_id);
            target.overwrite(this.buffer, this.position);
        }
        #region
        // 혼동 주의. copy_to 는 복사 하는, overwrite는 복사 받는. 이다. copy_to 내에 overwrite가 있다 해서, 두 매서드를 실행하는 주체와 객체가 같지 않다.
        #endregion
        public void overwrite(byte[] source, int position)
        {
            Array.Copy(source, this.buffer, source.Length);
            #region
            // ○ Array.Copy
            //  - (1) : 복사되는 데이터
            //  - (2) : 복사받는 데이터
            //  - (3) : 복사되는 길이
            //   => (1)[0] 부터 (1)[(3)] 만큼, (2)에 복사한다. [ (2)[0] 부터 복사 ]
            #endregion
            this.position = position;
        }
        public Int16 pop_protocol_id()
        {
            return pop_int16();
        }


        #region 공부정리
        // 하단의 pop_.. 매서드를 통해 서버는 byte[]로 저장된 데이터를 byte, int16, in32, string으로 pop해서 읽어드린다 한다. 어떤 타입으로 읽어드릴지를, protocol_id 를 통해 서버가 구분한다고 한다. (From 지피티)
        #endregion
        public byte pop_byte()
        {
            byte data = (byte)BitConverter.ToInt16(this.buffer, this.position);
            this.position += sizeof(byte);
            return data;

            #region 보완가능
            // ○ 지피티가 더 효율적으로 처리할 수 있다 해서, 하단에 그 내용을 작성한다. 지피티는 모든 시기별, 플랫폼별, C#에서 sizeof(byte) == 1 이라 한다
            //byte data = this.buffer[this.position];
            //this.position += 1;
            //return data;
            #endregion
        }
        public Int16 pop_int16()
        {
            Int16 data = BitConverter.ToInt16(this.buffer, this.position);
            this.position += sizeof(Int16);
            // 지피티는 우항이 2도, Defines.HEADERSIZE 도 아닌 sizeof(Int16)으로 한 이유는, 명시성 을 위한 것이라는데, 예전에 어느 교재였나 강의에서 플랫폼별 타입의 사이즈가 다르다고 들었음.. 그것 때문인 것 같은데, 확실치는 않다
            // 지피티는 과거 플랫폼별 C, C++ 에서 sizeof(...) 값이 다른 건 맞지만, c#에서는 sizeof(...) 가 모두 동일하다 한다. 
            return data;
        }
        public Int32 pop_int32()
        {
            Int32 data = BitConverter.ToInt32(this.buffer, this.position);
            this.position += sizeof(Int32);
            return data;
        }
        public string pop_string()
        {
            Int16 len = BitConverter.ToInt16(this.buffer, this.position);
            this.position += sizeof(Int16);

            string data = Encoding.UTF8.GetString(this.buffer, this.position, len);
            this.position += len;

            return data;
        }
        public void set_protocol(Int16 protocol_id)
        {
            this.protocol_id = protocol_id;
            position = Defines.HEADERSIZE;

            push_int16(protocol_id);
        }
        public void push_int16(Int16 data)
        {
            byte[] temp_buffer = BitConverter.GetBytes(data);
            temp_buffer.CopyTo(this.buffer, this.position);
            #region 공부정리
            // ○ byte[](instance).CoptTo ( (1), (2) )
            //  - (1) :  byte[](instance)를 (1)에 복사
            //  - (2) :  (1) 의 (2) 위치부터 복사
            #endregion
            this.position += temp_buffer.Length;
        }

        public void record_size()
        {
            Int16 body_size = (Int16)(this.position - Defines.HEADERSIZE);
            #region 공부정리
            // body_size 는 총데이터크기 - 2 이다. 버퍼의 내용이 헤더버퍼 (2) / 프로토콜버퍼 (2) / 실데이터버퍼 이라면, 총데이터크기 -4 여야 하지 않나 라고 처음에 생각했었는데, 이유는 프로토콜버퍼와 실데이터버퍼를 함께 발송하기 때문이다
            // 즉 수신자는 (client).Receive(bufferA, intSize..) 를 총 2번(헤더버퍼, 프로토콜버퍼-실데이터버퍼) 한다.
            #endregion
            byte[] header = BitConverter.GetBytes(body_size);
            #region 공부정리
            // ○ !! BitConverter.GetBytes(int16(instance)) 는 항상 크기(sizeof) 2를 보장한다 
            //  - !! C#교재 맨 처음에는 항상, 타입(short, int, long, float, double, decimal, byte.. 의 크기 를 설명하는데, 위 내용과 동일하다. byte -> 1, int16 -> 2, int32 -> 4, int64 -> 8, float -> 4, double -> 8, bool -> 1
            //      => 이래서 서버 코드에서는 Int32 외에 Int16 도 같이 사용하는건가
            #endregion
            header.CopyTo(this.buffer, 0);
        }

        public void push(byte data)
        {
            //byte[] temp_buffer = BitConverter.GetBytes(data);
            byte[] temp_buffer = new byte[] { data };
            #region 공부정리
            // 교재에는 위 // 구문이며, 2015년 기준에서는 잘 작동했지만, 현재 2025년에는 오버로드 해석이 더 엄격해져, 오류가 난다. 따라서 위 구문으로 명시적으로 byte 배열로 생성하여 처리함
            #endregion
            temp_buffer.CopyTo(this.buffer, this.position);
            this.position += sizeof(byte);
        }
        public void push(Int16 data)
        {
            byte[] temp_buffer = BitConverter.GetBytes(data);
            temp_buffer.CopyTo(this.buffer, this.position);
            this.position += temp_buffer.Length;
        }
        public void push(Int32 data)
        {
            byte[] temp_buffer = BitConverter.GetBytes(data);
            temp_buffer.CopyTo(this.buffer, this.position);
            this.position += temp_buffer.Length;
        }
        public void push(string data)
        {
            byte[] temp_buffer = Encoding.UTF8.GetBytes(data);

            Int16 len = (Int16)temp_buffer.Length;
            byte[] len_buffer = BitConverter.GetBytes(len);
            len_buffer.CopyTo(this.buffer, this.position);
            this.position += sizeof(Int16);

            temp_buffer.CopyTo(this.buffer, this.position);
            this.position += temp_buffer.Length;
            #region 공부정리
            // ○ 실데이터에도 왜 len_buffer (문자열길이를 알려주는 버퍼) 가 앞에 오는지? headerBuffer 와 역할이 중복되지 않나?
            //  -> 둘의 기능은 유사하지만, len_buffer로 다시 한번 실데이터 크기를 알려줘서, Encoding.UTF8로 정확히 실데이터의 크기를 읽어드린다.

            // ○ HeaderBuffer의 경우 Defines.HEADERSize로 크기가 2인 것을 알고, (client).Receive(... , defines.HEADERSIZE) 로 읽어드린다. 그런데 len_buffer의 크기는 어떻게 알고, 실데이터와 구분하여 읽어드리나?
            //  -> BitConverter.GetBytes(Int16(instance)) 의 크기는 항상 2. 앞선 공부내용 주석에 설명

            // ○ this.position += temp_buffer.Length 의 우항이 len 이면 안되나?
            //  -> len은 Int16으로 크기 손실 우려가 있다 한다(그런데 왜 사용하지? From지피티. 근데 더는 알고 싶지않다.) , buffer.Length 는 Int32로 손실 가능성이 적어, temp_buffer.Length를 사용한다 한다
            #endregion
        }



        #region 공부정리
        // ○ Array.Copy Vs byte[](instance).CopyTo Vs Buffer.BlockCopy
        //   -> 위 세 매서드는, byte[] 를 다른 byte[]에 복사할 때 사용할 수 있으며, 차이점들이 있음
        //    -> Array.Copy : 연산효율 최악. 다만 유일하게 복사되는 데이터의 길이 를 지정할 수 있음. byte[]외에 다양한 타입의 배열복사를 지원
        //    -> byte[](instance).CopyTo : 연산효율 중간. 코드 편의성이 Buffer.BlockCopy 보다 좋다함 (From지피티 .. 근데 이게 말이 되나? 코드 편의성이라니 이게 무슨.. 코드 좀만 더 쓰는게(Length를 지정해주는게) 어렵나? 납득이 안간다
        //    -> Buffer.BlockCopy : 연산효율 최고. 이것만 써도 되지 않나 싶다. 굳이 단점이라면, (5)항에 길이 를 지정해준다는 건데, byte[](instance).Length 입력하는 게 힘든 일도 아니고.. 
        #endregion
    }
}
