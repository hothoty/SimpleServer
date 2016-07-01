﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityCommon;
using ZNet;

namespace Server.User
{
    public class UserServer : Base.BaseServer
    {
        // 클라이언트 목록
        public Dictionary<ZNet.RemoteID, CUser> RemoteClients = new Dictionary<ZNet.RemoteID, CUser>();



        public UserServer(FormServer f, UnityCommon.Server s, int portnum) : base(f, s, portnum)
        {
        }

        protected override void BeforeStart(out StartOption param)
        {
            param = new StartOption();


            param.m_IpAddressListen = ListenAddr.m_ip;
            param.m_PortListen = ListenAddr.m_port;


            // 접속을 받을 최대 동접 숫자 :
            // 작게 잡을 경우 동적으로 동접 한계치에 가까워지면 최대 동접 숫자가 늘어납니다
            // 동적 접속자 증가를 비활성화 할 수도 있습니다 m_bExpandMaxConnect옵션을 false로 지정하면 됩니다
            param.m_MaxConnectionCount = 20000;


            // 주기적으로 서버에서 처리할 콜백 함수 시간을 설정합니다
            param.m_RefreshServerTickMs = 10000;


            // 서버와 클라이언트간의 프로토콜 버전 내용입니다, 서로 다른 경우 경고 메세지 이벤트가 발생됩니다
            param.m_ProtocolVersion = UnityCommon.Join.protocol_ver;


            // 내부 스레드 사용을 중단 
            param.m_LogicThreadCount = 0;


            // 클라이언트의 반응이 없을경우 내부적으로 접속을 해제시킬 시간을 설정합니다(초단위)
            m_Core.SetKeepAliveOption(30);


            // 클라이언트의 서버 이동 요청을 처리해줍니다
            stub.server_move = (ZNet.RemoteID remote, ZNet.CPackOption pkOption, int server_type) =>
            {
                // 유효한 유저인지 확인
                CUser rc;
                if (RemoteClients.TryGetValue(remote, out rc) == false) return true;
                if (rc.joined == false) return true;    // 인증여부 확인


                // 클라이언트가 보내온 이동할 서버타입의 서버에 대한 정보를 확인합니다
                ZNet.MasterInfo[] svr_array;
                m_Core.GetServerList(server_type, out svr_array);

                if (svr_array == null)
                    return true;

                // 해당 서버 타입의 서버가 여러개 존재할 수도 있으므로...
                foreach (var obj in svr_array)
                {
                    // 서버이동시 필요한 파라미터 정보입니다
                    // -> 특정 서버의 특정한 방에 들어간다든지 할때 활용 가능하지만 현재는 사용하지 않습니다
                    ZNet.ArrByte param_buffer = new ZNet.ArrByte();


                    // 요청한 유저에 대한 서버 이동 처리를 시작합니다
                    m_Core.ServerMoveStart(

                        // 서버이동할 유저
                        remote,

                        // 이동할 서버 주소
                        obj.m_Addr,


                        // 현재 간단한 서버이동만을 다루므로 아래 2개 파라미터에 대한 설명은 생략합니다
                        // - 서버 이동 관련 파라미터 정보와 특정한 방의 식별자로 활용 가능한 옵션 기능입니다

                        new ZNet.ArrByte(),
                        new Guid()
                    );

                    form.printf("서버이동 Start : move to {0}", (UnityCommon.Server)server_type);
                    return true;
                }
                return true;
            };


            // 클라이언트의 연결이 이루어지는 경우 발생되는 이벤트를 처리합니다
            m_Core.client_join_handler = (ZNet.RemoteID remote, ZNet.NetAddress addr, ZNet.ArrByte move_server, ZNet.ArrByte move_param) =>
            {
                // 서버이동으로 입장한 경우
                if (move_server.Count > 0)
                {
                    CUser rc;
                    UserData.UserDataMove_Complete(move_server, out rc);

                    form.printf("move server complete  {0} {1} {2} {3}", rc.data.userID, rc.data.money_cash, rc.data.money_game, rc.data.temp);
                    RemoteClients.Add(remote, rc);
                }
                else
                {
                    if (this.Type == UnityCommon.Server.Login)
                    {
                        // 로그인 서버에서만 일반입장을 허용
                        CUser rc = new CUser();
                        rc.data.userID = Guid.Empty;
                        rc.data.temp = "최초입장_인증받기전";
                        RemoteClients.Add(remote, rc);
                    }
                    else
                    {
                        form.printf("로그인 서버외의 서버에서 일반 입장은 허용하지 않습니다");
                    }
                }

                form.printf("Client {0} is Join {1}:{2}. Current={3}\n", remote, addr.m_ip, addr.m_port, RemoteClients.Count);
            };

            // 클라이언트의 연결이 종료되는 경우 발생되는 이벤트 처리입니다
            m_Core.client_leave_handler = (ZNet.RemoteID remote, bool bMoveServer) =>
            {
                // 서버 이동중이 아닌상태에서 퇴장하는 경우 로그아웃에 대한 처리를 해줍니다
                if (bMoveServer == false)
                {
                    CUser rc;
                    if (RemoteClients.TryGetValue(remote, out rc))
                    {
                        form.printf("[DB로그아웃] 처리, user {0}\n", rc.data.userName);
                        if (Var.Use_DB)
                        {
                            Task.Run(() =>
                            {
                                try
                                {
                                    Simple.Data.Database.Open().UserInfo.UpdateByUserUUID(UserUUID: rc.data.userID, StateOnline: false);
                                }
                                catch (Exception e)
                                {
                                    form.printf("[DB로그아웃] 예외발생 {0}\n", e.ToString());
                                }
                            });
                        }
                    }
                }

                RemoteClients.Remove(remote);
                form.printf("Client {0} Leave\n", remote);
            };


            // 서버이동 도중에 이전 서버에 퇴장은 되었으나 새로운 서버로 입장을 실패하는 경우(회선문제 등으로)
            // ---> 이때 확실하게 로그아웃 처리를 마무리 해줄 필요가 있음
            m_Core.move_server_failed_handler = (ZNet.ArrByte move_server) =>
            {
                CUser rc;
                UserData.UserDataMove_Complete(move_server, out rc);

                form.printf("[DB로그아웃] 서버이동을 실패한 경우에 대한 마무리 처리, user {0}\n", rc.data.userName);
                if (Var.Use_DB)
                {
                    Task.Run(() =>
                    {
                        try
                        {
                            Simple.Data.Database.Open().UserInfo.UpdateByUserUUID(UserUUID: rc.data.userID, StateOnline: false);
                        }
                        catch (Exception e)
                        {
                            form.printf("[DB로그아웃] 예외발생 {0}\n", e.ToString());
                        }
                    });
                }
            };


            // 어떤 유저가 서버 이동을 시작한 경우 발생하는 이벤트입니다
            m_Core.move_server_start_handler = (ZNet.RemoteID remote, out ZNet.ArrByte buffer) =>
            {
                // 해당 유저의 유효성 체크
                CUser rc;
                if (RemoteClients.TryGetValue(remote, out rc) == false)
                {
                    buffer = null;
                    return;
                }

                // 인증여부 확인
                if (rc.joined == false)
                {
                    buffer = null;
                    return;
                }

                // 데이터 이동 완료시 목표 서버에서 정상적인 데이터인지 확인을 위한 임시 데이터 구성
                rc.data.temp = this.Name;

                // 동기화 할 유저 데이터를 구성하여 buffer에 넣어둔다 -> 이동 목표 서버에서 해당 데이터를 그대로 받게된다
                UserData.UserDataMove_Start(rc, out buffer);

                form.printf("move server start  {0} {1} {2} {3}", rc.data.userID, rc.data.money_cash, rc.data.money_game, rc.data.temp);
            };


            // 마스터 서버에 입장 성공한 이벤트
            m_Core.server_master_join_hanlder = (ZNet.RemoteID remote, ZNet.RemoteID myRemoteID) =>
            {
                form.printf(string.Format("마스터서버에 입장성공 remoteID {0}", myRemoteID));
            };

            // 마스터 서버에 퇴장된 이벤트
            m_Core.server_master_leave_hanlder = () =>
            {
                form.printf(string.Format("마스터서버와 연결종료!!!"));
                form.Close();    // 마스터 서버를 종료하면 모든 서버 프로그램이 자동 종료처리 되게 하는 내용...
            };

            // 마스터 서버에 연결된 모든 서버들로부터 주기적으로 자동 받게되는 정보
            m_Core.server_refresh_hanlder = (ZNet.MasterInfo master_info) =>
            {
                //form.printf(string.Format("서버P2P remote:{0} type:{1}[{2}] current:{3} addr:{4}:{5}",

                //    // 정보를 보낸 서버의 remoteID
                //    master_info.m_remote,

                //    // 정보를 보낸 서버의 종류 : 정보를 보낸 서버가 MasterConnect시 입력한 4번째 파라미터를 의미합니다
                //    (UnityCommon.Server)master_info.m_ServerType,

                //    // 정보를 보낸 서버의 설명 : 정보를 보낸 서버가 MasterConnect시 입력한 3번째 파라미터를 의미합니다
                //    master_info.m_Description,

                //    // 정보를 보낸 서버의 현재 동접 숫자 : 이것을 근거로 나중에 서버이동시 로드벨런싱에 사용할것입니다
                //    master_info.m_Clients,

                //    // 정보를 보낸 서버의 주소
                //    master_info.m_Addr.m_ip,
                //    master_info.m_Addr.m_port
                //    ));
            };
        }

        protected override void AfterStart()
        {
            base.AfterStart();

            m_Core.MasterConnect(
                Properties.Settings.Default.MasterIp,
                Properties.Settings.Default.MasterPort,
                this.Name,
                (int)this.Type
                );
        }
    }
}
