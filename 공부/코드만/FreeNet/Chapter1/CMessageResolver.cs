using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeNet
{
    internal class CMessageResolver
    #region 공부정리
    // ○ CMessageResolver
    //  - TCP는 바이트 스트림 기반으로, CUserToken에서 메세지를 수신할 때, 메세지1, 메세지2, 메세지3.. 등의 여러 메세지를 구분하여 받지 않고, byte[] 로 받을 때 메세지1,메세지2.. 가 함께 받아질 수 있다. (메세지1,메세지2의 순서는 보장된다)
    //    CMessageResolver 클래스의 기능은, 위처럼 byte[] 에 섞여져서 들어온 메세지1,메시지2,.. 들을 분리하여, CUserToken에게 분리된 메세지들을 전달하는 역할을 한다
    #endregion
    {
        public delegate void CompleteMessageCallback(Const<byte[]> buffer);

        int message_size;
        int position_to_read;
        byte[] message_buffer = new byte[1024];
        int current_position;
        int remain_bytes;

        public CMessageResolver()
        {
            message_size = 0;
            position_to_read = 0;
            current_position = 0;
            remain_bytes = 0;
        }

        public void on_receive(byte[] buffer, int offset, int transffered, CompleteMessageCallback callback)
        {
            this.remain_bytes = transffered;

            int src_position = offset;

            while (this.remain_bytes > 0)
            {
                bool completed = false;

                if (this.current_position < Defines.HEADERSIZE)
                {
                    this.position_to_read = Defines.HEADERSIZE;

                    completed = read_until(buffer, ref src_position, offset, transffered);
                    if (!completed)
                    {
                        return;
                    }

                    this.message_size = get_body_size();

                    this.position_to_read = this.message_size + Defines.HEADERSIZE;
                }

                completed = read_until(buffer, ref src_position, offset, transffered);

                if (completed)
                {
                    callback(new Const<byte[]>(this.message_buffer));
                    #region 공부정리
                    // ○ delegate를 매서드의 매개변수로 받는 경우의 구독 처리
                    //  - delegate를 매서드의 매개변수로 받는 경우를 처음 보는데, 이렇게 하면, 해당 매서드의 인자로 delegate와 매개변수가 동일한 매서드를 인자로 입력시, 구독( += ) 처리가 된다.
                    //  - 여기서는 CUserToken 에서 CMessageResolver(instance).on_receive(... (4)항 : on_message ) 로 매서드를 호출하는데, callback 시, 구독된 CUserToken의 on_message가 발동된다
                    #endregion

                    clear_buffer();
                }
            }
        }

        private bool read_until(byte[] buffer, ref int src_position, int offset, int transffered)
        {
            if (this.current_position >= offset + transffered) return false;

            int copy_size = position_to_read - this.current_position;

            if(this.remain_bytes < copy_size)
            {
                copy_size = this.remain_bytes;
            }

            Array.Copy(buffer, src_position, this.message_buffer, this.current_position, copy_size);

            src_position += copy_size;
            this.current_position += copy_size;

            this.remain_bytes -= copy_size;

            if (this.current_position < this.position_to_read) return false;


            return true;

            #region 공부정리
            // ○ while(this.remain_bytes > 0 ) 과 이 매서드의 return true, false 구분 필요
            //  - 처음 클래스를 봤을 때, 이게 가장 헷갈렸다. 아래 나오는 공부 정리에 while(this.remain_ytes > 0 ) 설명 부분을 봐야 이해가 잘될텐데, 2개이상의 메세지가 혼합되어 수신되는 경우를 이해해야 한다.
            //    결론으로 이 매서드의 return bool 은 반복문이 시행될지 여부를 정하는 게 아니라, callback을 보낼지에 대한 판정이다.
            #endregion
        }

        #region 공부정리
        // ○ 왜 on_receive 에서 while(this.remain_bytes > 0 )을 사용하나? while(int i = 0; i < 2; i++) 을 사용해도 되지 않나? 
        //  -> 만약 on_receive 의 buffer 에 메세지 하나만 있다면, while(int i = 0; i < 2; i++) 로 해도 헤더 버퍼 때 한번 read_until 사용, 실데이터 버퍼 때 read_until 사용 총 2번의 read_until 을 사용해도 문제가 되지 않는다.
        //     하지만, on_receive에서 받는 buffer에, 메세지가 2개 이상 담겨 있다면, 총 두번 시행으로는 안된다.
        //     예를 들어, buffer 의 transffered가 24이고, 첫번째 메세지는 총 14 버퍼, 두번째 메세지는 총 10 버퍼라 하자.
        //     readuntil 이 발동될 때에 remain_bytes를 나열해보면, 24(처음도착) -> 22(첫번째 헤더버퍼읽음) -> 10(첫번째 메세지 읽음) -> 8(두번째 메세지 헤더 읽음) -> (두번째 메세지도 모두 읽음)
        //     => 이렇게 on_receive가 총 4번 시행된다. 위 예시는 2개의 메세지가 한번의 on_receive에 읽히는 경우고,
        //        한번에 읽히지 않을 경우, on_receive가 2번 이상 입력돼야 하기 때문에, 더 복잡해진다. 따라서 remain_bytes > 0 로 반복 시행해야 함
        #endregion

        private int get_body_size()
        {
            Type type = Defines.HEADERSIZE.GetType();
            if (type.Equals(typeof(Int16)))
            {
                return BitConverter.ToInt16(this.message_buffer, 0);
            }

            return BitConverter.ToInt32(this.message_buffer, 0);
        }

        private void clear_buffer()
        {
            Array.Clear(this.message_buffer, 0, this.message_buffer.Length);

            this.current_position = 0;
            this.message_size = 0;
        }
    }
}
