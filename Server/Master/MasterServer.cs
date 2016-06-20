using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityCommon;
using ZNet;

namespace Server.Master
{
    class MasterServer : Base.BaseServer
    {
        public MasterServer(FormServer f, UnityCommon.Server s, int portnum) : base(f, s, portnum)
        {
        }

        protected override void BeforeStart(out StartOption param)
        {
            param = new StartOption();

            param.m_IpAddressListen = Properties.Settings.Default.MasterIp;
            param.m_PortListen = Properties.Settings.Default.MasterPort;
            param.m_MaxConnectionCount = 5000;

            m_Core.SetKeepAliveOption(60);


            // 마스터 서버에서만 발생되는 이벤트 처리 : 마스터 클라이언트 서버 입장 시점
            m_Core.master_server_join_hanlder = (ZNet.RemoteID remote, string description, int type, ZNet.NetAddress addr) =>
            {
                form.printf("마스터 Client Join remoteID({0}) {1} type({2})", remote, description, type);
            };

            // 마스터 서버에서의 접속해제 이벤트 -> 마스터 클라이언트의 퇴장
            m_Core.client_disconnect_handler = (ZNet.RemoteID remote) =>
            {
                form.printf("마스터 Client Leave remoteID({0})", remote);
            };
        }

        protected override void NewCore()
        {
            // Master서버만 인자가 다르므로 재정의
            m_Core = new ZNet.CoreServerNet(true);
        }
    }
}
