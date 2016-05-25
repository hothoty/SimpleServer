﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoginServer
{
    public class CServerLogin
    {
        // 실제 네트워크 처리를 담당할 클래스입니다
        public ZNet.CoreServerNet m_Core = null;


        // IDL파일에 정의한 패킷들을 가지고 있는 클래스 입니다. 이것을 통해 정의된 패킷들을 송/수신 할 수 있습니다
        public Rmi.Proxy proxy;
        public Rmi.Stub stub;


        // 서버 메인루프를 계속 실행할지 여부... 
        public bool run_program = true;



        // 클라이언트 목록
        public Dictionary<ZNet.RemoteID, NetServerCommon.CUser> RemoteClients = new Dictionary<ZNet.RemoteID, NetServerCommon.CUser>();



        public CServerLogin()
        {
            // 네트워크 코어 클래스 생성
            m_Core = new ZNet.CoreServerNet();


            // 패킷 RMI 생성
            proxy = new Rmi.Proxy();
            stub = new Rmi.Stub();


            // 코어 클래스에 기본 Rmi클래스를 붙여줍니다(여러개를 붙여서 처리가능하지만 여기서는 한개만 사용합니다)
            m_Core.Attach(proxy, stub);


            // 클라이언트에게 받은 메세지
            stub.Chat = (ZNet.RemoteID remote, ZNet.CPackOption pkOption, string txt) =>
            {
                // 접속된 모두에게 그대로 돌려주는 단순한 처리입니다
                foreach (var obj in RemoteClients)
                {
                    proxy.Chat(obj.Key, ZNet.CPackOption.Basic, txt);
                }
                return true;
            };


            // 클라이언트의 로그인 인증 요청을 처리합니다
            stub.request_Login = (ZNet.RemoteID remote, ZNet.CPackOption pkOption, string name, string pass) =>
            {
                // 유효한 유저인지 확인
                NetServerCommon.CUser rc;
                if (RemoteClients.TryGetValue(remote, out rc) == false) return true;


                Action LoginAsync = async () =>
                {
                    NetServerCommon.UserDataSync dummy = new NetServerCommon.UserDataSync();
                    
                    var result = await Task.Run(() =>
                    {
                        Console.WriteLine("[DB로그인] 처리 시작. 결과를 기다리는 중입니다 ....\n");

                        try
                        {
                            dynamic result_db = Simple.Data.Database.Open().UserInfo.FindAllByUserName(name).FirstOrDefault();


                            if (result_db == null)
                            {
                                Console.WriteLine("[DB로그인] 해당 유저가 DB에 존재하지 않아 새로운 계정을 생성합니다\n");

                                System.Guid newID = System.Guid.NewGuid();
                                var user = Simple.Data.Database.Open().UserInfo.Insert(UserUUID: newID, UserName: name, PassWord: pass);


                                // 생성한 계정을 로그인 상태로 변경
                                if (Simple.Data.Database.Open().UserInfo.UpdateByUserUUID(UserUUID: newID, StateOnline: true) == 1)
                                {
                                    dummy.userID = newID;
                                    dummy.userName = name;
                                    dummy.money_cash = 0;
                                    dummy.money_game = 500;
                                    return 1;   // 인증 성공
                                }
                            }
                            else
                            {
                                Console.WriteLine("[DB로그인] 인증진행중...\n");
                                if (result_db.StateOnline) // 이미 로그인한 상태 --> 중복 로그인 오류
                                {
                                    return 2;
                                }
                                else if (result_db.PassWord == pass) // 암호 검사
                                {
                                    // 암호검사 통과 -> 해당 계정을로그인 상태로 변경
                                    if (Simple.Data.Database.Open().UserInfo.UpdateByUserUUID(UserUUID: result_db.UserUUID, StateOnline: true) == 1)
                                    {
                                        // 로딩한 DB데이터 보관
                                        dummy.userID = result_db.UserUUID;
                                        dummy.userName = result_db.UserName;
                                        dummy.money_cash = result_db.CashMoney;
                                        dummy.money_game = result_db.GameMoney;
                                        return 1;
                                    }
                                }
                                else
                                {
                                    return 3;   // 인증 실패 : 암호오류
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("[DB로그인] 예외발생 {0}\n", e.ToString());
                        }

                        return 0;
                    });

                    
                    // 상단의 DB Task가 완료되는 시점이므로 다시 유저가 유효한 상태인지 확인합니다
                    if (RemoteClients.TryGetValue(remote, out rc) == false)
                    {
                        Console.WriteLine("[DB로그인 결과받음] 결과 받은 시점에 해당유저 연결종료됨\n");
                        return;
                    }

                    // 인증 성공
                    if (result == 1)
                    {
                        Console.WriteLine("[DB로그인 결과받음] DB인증성공 : CashMoney : {0}  GameMoney : {1}  \n", rc.data.money_cash, rc.data.money_game);
                        rc.data = dummy;
                        rc.joined = true;
                        proxy.reponse_Login(remote, ZNet.CPackOption.Basic, true);
                    }
                    else
                    {
                        if (result == 2)
                            Console.WriteLine("[DB로그인 결과받음] Login 실패 : 중복로그인  idname={0}\n", name);
                        else if(result == 3)
                            Console.WriteLine("[DB로그인 결과받음] Login 실패 : 암호오류  idname={0}\n", name);
                        else
                            Console.WriteLine("[DB로그인 결과받음] Login 실패 : ???  idname={0}\n", name);

                        // 인증 실패를 알려줌
                        proxy.reponse_Login(remote, ZNet.CPackOption.Basic, false);
                    }
                    return;
                };

                if (NetServerCommon.Var.Use_DB)
                {
                    // 로그인 DB 인증 비동기 방식으로 실행
                    LoginAsync.Invoke();
                }
                else
                {
                    Console.WriteLine("[DB로그인] DB작업없이 진행합니다 설정을 확인해주세요.\n");

                    rc.data.userID = Guid.Empty;
                    rc.data.userName = "김철수";
                    rc.data.money_cash = 0;
                    rc.data.money_game = 500;
                    rc.joined = true;
                    proxy.reponse_Login(remote, ZNet.CPackOption.Basic, true);
                }

                return true;
            };

            // 클라이언트의 서버 이동 요청을 처리해줍니다
            stub.server_move = (ZNet.RemoteID remote, ZNet.CPackOption pkOption, int server_type) =>
            {
                // 유효한 유저인지 확인
                NetServerCommon.CUser rc;
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

                    Console.WriteLine("서버이동 Start : move to {0}", (UnityCommon.Server)server_type);
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
                    NetServerCommon.CUser rc;
                    NetServerCommon.Common.UserDataMove_Complete(move_server, out rc);

                    Console.WriteLine("move server complete  {0} {1} {2} {3}", rc.data.userID, rc.data.money_cash, rc.data.money_game, rc.data.temp);
                    RemoteClients.Add(remote, rc);
                }
                else
                {
                    // 일반 입장
                    NetServerCommon.CUser rc = new NetServerCommon.CUser();
                    rc.data.userID = Guid.Empty;
                    rc.data.temp = "최초입장_인증받기전";
                    RemoteClients.Add(remote, rc);
                }

                Console.WriteLine("Client {0} is Join {1}:{2}. Current={3}\n", remote, addr.m_ip, addr.m_port, RemoteClients.Count);
            };


            // 클라이언트의 연결이 종료되는 경우 발생되는 이벤트 처리입니다
            m_Core.client_leave_handler = (ZNet.RemoteID remote, bool bMoveServer) =>
            {
                // 서버 이동중이 아닌상태에서 퇴장하는 경우 로그아웃에 대한 처리를 해줍니다
                if (bMoveServer == false)
                {
                    NetServerCommon.CUser rc;
                    if (RemoteClients.TryGetValue(remote, out rc))
                    {
                        Console.WriteLine("[DB로그아웃] 처리, user {0}\n", rc.data.userName);
                        if (NetServerCommon.Var.Use_DB)
                        {
                            Task.Run(() =>
                            {
                                try
                                {
                                    Simple.Data.Database.Open().UserInfo.UpdateByUserUUID(UserUUID: rc.data.userID, StateOnline: false);
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine("[DB로그아웃] 예외발생 {0}\n", e.ToString());
                                }
                            });
                        }
                    }
                }

                RemoteClients.Remove(remote);
                Console.WriteLine("Client {0} Leave\n", remote);
            };

            // 서버이동 도중에 이전 서버에 퇴장은 되었으나 새로운 서버로 입장을 실패하는 경우(회선문제 등으로)
            // ---> 이때 확실하게 로그아웃 처리를 마무리 해줄 필요가 있음
            m_Core.move_server_failed_handler = (ZNet.ArrByte move_server) =>
            {
                NetServerCommon.CUser rc;
                NetServerCommon.Common.UserDataMove_Complete(move_server, out rc);

                Console.WriteLine("[DB로그아웃] 서버이동을 실패한 경우에 대한 마무리 처리, user {0}\n", rc.data.userName);
                if (NetServerCommon.Var.Use_DB)
                {
                    Task.Run(() =>
                    {
                        try
                        {
                            Simple.Data.Database.Open().UserInfo.UpdateByUserUUID(UserUUID: rc.data.userID, StateOnline: false);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("[DB로그아웃] 예외발생 {0}\n", e.ToString());
                        }
                    });
                }
            };


            // 어떤 유저가 서버 이동을 시작한 경우 발생하는 이벤트입니다
            m_Core.move_server_start_handler = (ZNet.RemoteID remote, out ZNet.ArrByte buffer) =>
            {
                // 해당 유저의 유효성 체크
                NetServerCommon.CUser rc;
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
                rc.data.temp = "로그인서버";

                // 동기화 할 유저 데이터를 구성하여 buffer에 넣어둔다 -> 이동 목표 서버에서 해당 데이터를 그대로 받게된다
                NetServerCommon.Common.UserDataMove_Start(rc, out buffer);

                Console.WriteLine("move server start  {0} {1} {2} {3}", rc.data.userID, rc.data.money_cash, rc.data.money_game, rc.data.temp);
            };


            // 예외상황등에 발생하는 이벤트들을 처리합니다  
            m_Core.message_handler = (ZNet.ResultInfo result) =>
            {
                string str_msg = "Msg : ";
                str_msg += result.msg;
                Console.WriteLine(str_msg);
            };
            m_Core.exception_handler = (Exception e) =>
            {
                string str_msg = "Exception : ";
                str_msg += e.ToString();
                Console.WriteLine(str_msg);
            };


            // 마스터 서버에 입장 성공한 이벤트
            m_Core.server_master_join_hanlder = (ZNet.RemoteID remote, ZNet.RemoteID myRemoteID) =>
            {
                Console.WriteLine(string.Format("마스터서버에 입장성공 remoteID {0}", myRemoteID));
            };

            // 마스터 서버에 퇴장된 이벤트
            m_Core.server_master_leave_hanlder = () =>
            {
                Console.WriteLine(string.Format("마스터서버와 연결종료!!!"));
                run_program = false;    // 마스터 서버를 종료하면 모든 서버 프로그램이 자동 종료처리 되게 하는 내용...
            };

            // 마스터 서버에 연결된 모든 서버들로부터 주기적으로 자동 받게되는 정보
            m_Core.server_refresh_hanlder = (ZNet.MasterInfo master_info) =>
            {
                //Console.WriteLine(string.Format("서버P2P remote:{0} type:{1}[{2}] current:{3} addr:{4}:{5}",

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
    }

    class Program
    {
        static void Main(string[] args)
        {
            // 로그인 서버 클래스 생성
            CServerLogin Svr = new CServerLogin();


            // 서버 시작 관련 기본적인 정보를 설정합니다
            ZNet.StartOption param = new ZNet.StartOption();


            // 접속을 받을 IP
            param.m_IpAddressListen = UnityCommon.Join.ipaddr;


            // 접속을 받을 포트
            param.m_PortListen = UnityCommon.Join.portnum;


            // 접속을 받을 최대 동접 숫자 :
            // 작게 잡을 경우 동적으로 동접 한계치에 가까워지면 최대 동접 숫자가 늘어납니다
            // 동적 접속자 증가를 비활성화 할 수도 있습니다 m_bExpandMaxConnect옵션을 false로 지정하면 됩니다
            param.m_MaxConnectionCount = 20000;


            // 주기적으로 서버에서 처리할 콜백 함수 시간을 설정합니다
            param.m_RefreshServerTickMs = 10000;


            // 서버와 클라이언트간의 프로토콜 버전 내용입니다, 서로 다른 경우 경고 메세지 이벤트가 발생됩니다
            param.m_ProtocolVersion = UnityCommon.Join.protocol_ver;


            // 클라이언트의 반응이 없을경우 내부적으로 접속을 해제시킬 시간을 설정합니다(초단위)
            Svr.m_Core.SetKeepAliveOption(30);



            // 설정된 시작 파라미터를 근거로 서비스를 시작합니다
            ZNet.ResultInfo outResult = new ZNet.ResultInfo();
            if (Svr.m_Core.Start(param, outResult))
            {
                Console.WriteLine("LoginServer Start ok.\n");
                NetCommon.Common.DisplayHelpCommand();
            }
            else
            {
                Console.WriteLine("Start error : {0} \n", outResult.msg);
            }



            Svr.m_Core.MasterConnect(
                NetServerCommon.MasterServerConnect.master_ipaddr,
                NetServerCommon.MasterServerConnect.master_portnum,
                "LoginServer",
                (int)UnityCommon.Server.Login
                );


            var ret = NetCommon.Common.ReadLineAsync();
            while (Svr.run_program)
            {
                if (ret.IsCompleted)
                {
                    switch (ret.Result)
                    {
                        case "/h":
                            NetCommon.Common.DisplayHelpCommand();
                            break;

                        case "/stat":
                            NetServerCommon.Common.DisplayStatus(Svr.m_Core);
                            break;

                        case "/q":
                            Console.WriteLine("quit Server...");
                            Svr.run_program = false;
                            break;
                    }

                    if (Svr.run_program)
                        ret = NetCommon.Common.ReadLineAsync();
                }

                System.Threading.Thread.Sleep(10);
            }

            Console.WriteLine("Start Closing...  ");
            Svr.m_Core.Dispose();
            Console.WriteLine("Close complete.");

            System.Threading.Thread.Sleep(1000 * 2);
        }
    }
}