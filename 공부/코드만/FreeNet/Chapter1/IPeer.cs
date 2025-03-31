using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeNet
{
    public interface IPeer
    {
        void On_message(Const<byte[]> buffer);

        void On_removed();

        void Send(CPacket msg);

        void Disconnect();

        void Process_user_operation(CPacket msg);
    }
}
