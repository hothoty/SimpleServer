using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityCommon;
using ZNet;

namespace Server.User
{
    class RoomServer : UserServer
    {
        public RoomServer(FormServer f, UnityCommon.Server s, int portnum) : base(f, s, portnum)
        {
        }

        protected override void BeforeStart(out StartOption param)
        {
            base.BeforeStart(out param);


            // 업데이트 콜백 이벤트 시간을 설정합니다
            param.m_UpdateTimeMs = 50;


            // 주기적으로 업데이트할 필요가 있는 내용들...
            m_Core.update_event_handler = () =>
            {

            };
        }
    }
}
