using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeNet
{
    public interface IPeer
    {
        void on_message(Const<byte[]> buffer);

        void on_removed();

        void send(CPacket msg);

        void disconnect();

        void process_user_operation(CPacket msg);

        #region 공부정리
        // ○ IPeer 
        //  - IPeer : 서버에서, 서버에 연결된 Client에 대응되는 객체가 IPeer 인터페이스를 구현. 서버는 IPeer를 통해 클라이언트와의 입출력, 상태관리, 메세지처리 등을 수행
        //  - on_message : 클라이언트에서 보낸 메세지를, 서버가 수신했을 때, 그 클라이언트에 대응하는 객체에 발동
        //  - on_removed : 해당 클라이언트가 서버에서 연결 종료됐을 때 호출
        //  - send       : 서버가 해당 클라이언트에 메세지를 보낼 때 호출
        //  - disconnect : 서버가 해당 클라이언트를 강제 연결 해제시킬 때 호출
        //  - process_user_operation : 수신한 메세지를 업무 로직에 따라 분기 처리할 때 사용(Ex. 로그인, 채팅, 이동) 
        #endregion
    }
}
