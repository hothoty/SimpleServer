using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityCommon;
using ZNet;

namespace Server.User
{
    class LobbyServer : UserServer
    {
        public LobbyServer(FormServer f, UnityCommon.Server s, int portnum) : base(f, s, portnum)
        {
        }

        protected override void BeforeStart(out StartOption param)
        {
            base.BeforeStart(out param);


            // 접속을 받을 IP
            param.m_IpAddressListen = NetServerCommon.Lobby.ipaddr;


            // 접속을 받을 포트
            param.m_PortListen = NetServerCommon.Lobby.portnum;
        }
    }
}
