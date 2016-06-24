using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    public class CClient
    {
        public ZNet.CoreClientNet m_Core;

        public Rmi.Proxy proxy;
        public Rmi.Stub stub;


        // 현재 서버 위치
        public UnityCommon.Server server_now = UnityCommon.Server.None;


        // 목표 서버 위치
        public UnityCommon.Server server_tag = UnityCommon.Server.None;


        public CClient()
        {
            m_Core = new ZNet.CoreClientNet();

            proxy = new Rmi.Proxy();
            stub = new Rmi.Stub();

            m_Core.Attach(proxy, stub);


            // 서버가 보낸 로그인인증 결과패킷을 처리합니다
            stub.reponse_Login = (ZNet.RemoteID remote, ZNet.CPackOption pkOption, bool bResult) =>
            {
                Console.WriteLine("로그인 인증 결과" + bResult);
                return true;
            };

            // 서버로부터 받은 메세지
            stub.Chat = (ZNet.RemoteID remote, ZNet.CPackOption pkOption, string txt) =>
            {
                Console.WriteLine(txt);
                return true;
            };



            // 서버이동 시도에 대한 실패 이벤트
            m_Core.move_fail_handler = () =>
            {
                Console.WriteLine("서버이동 처리가 실패하였습니다.");
            };

            // 서버로의 입장이 성공한 이벤트 처리
            m_Core.server_join_handler = (ZNet.ConnectionInfo info) =>
            {
                if (info.moved)
                {
                    // 서버이동이 성공한 시점 : 위치를 목표했던 서버로 설정
                    server_now = server_tag;
                    Console.WriteLine("서버이동성공 [{0}:{1}] {2}", info.addr.m_ip, info.addr.m_port, server_now);
                }
                else
                {
                    // 최초 입장의 성공시점 : 위치를 로그인 서버로 설정
                    server_now = UnityCommon.Server.Login;
                    Console.WriteLine("서버입장성공 {0}", server_now);

                    // 최초 로그인 DB인증 시도 요청
                    proxy.request_Login(ZNet.RemoteID.Remote_Server, ZNet.CPackOption.Basic, "철수", "abcd");
                }
            };

            // 서버로의 퇴장이 처리된 이벤트 처리
            m_Core.server_leave_handler = (ZNet.ConnectionInfo info) =>
            {
                if (info.moved)
                    Console.WriteLine("서버이동을 위해 퇴장, 이동할서버 [{0}:{1}]", info.addr.m_ip, info.addr.m_port);
                else
                    Console.WriteLine("서버퇴장성공");


                // 어떤 서버에서 퇴장하든 재접속시 최초접속과 구분하기 위하여 로그인 서버로 세팅해둡니다
                server_now = UnityCommon.Server.Login;
            };


            // 예외 상황에 대한 정보 출력 이벤트
            m_Core.message_handler = (ZNet.ResultInfo result) =>
            {
                string str_msg = "Msg : ";
                str_msg += result.msg;
                Console.WriteLine(str_msg);
            };
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            // 클라이언트 처리 클래스 생성
            CClient Client = new CClient();


            PutCommandList();


            // 비동기 방식으로 명령어를 입력받습니다
            var ret = NetCommon.Common.ReadLineAsync();


            // 프로그램 반복 실행을 위해...
            bool run_program = true;


            bool test = false;
            if (test)
            {
                Client.m_Core.Connect(UnityCommon.Join.ipaddr, UnityCommon.Join.portnum, UnityCommon.Join.protocol_ver);
                Console.WriteLine("Connect to server ({0}:{1}) ...", UnityCommon.Join.ipaddr, UnityCommon.Join.portnum);

                while (true)
                {
                    Client.m_Core.Leave();
                    Client.m_Core.NetLoop();

                    Client.m_Core.ReConnect();
                    Client.m_Core.NetLoop();

                    System.Threading.Thread.Sleep(1);
                }
            }


            // 프로그램 종료시까지 명령어 받고.. 네트워크 관련 처리하고.. 무한 반복
            while (run_program)
            {
                if (ret.IsCompleted)
                {
                    switch (ret.Result)
                    {
                        case "/c":
                            // 서버로 접속을 시도합니다
                            if (Client.server_now < UnityCommon.Server.Login)
                            {
                                // 최초 로그인 시도인 경우
                                Client.m_Core.Connect(UnityCommon.Join.ipaddr, UnityCommon.Join.portnum, UnityCommon.Join.protocol_ver);
                                Console.WriteLine("Connect to server ({0}:{1}) ...", UnityCommon.Join.ipaddr, UnityCommon.Join.portnum);
                            }
                            else
                            {
                                // 재접속을 시도하는 경우
                                Client.m_Core.ReConnect(UnityCommon.Join.ipaddr, UnityCommon.Join.portnum);
                            }
                            break;

                        case "/q":
                            // 서버에서 퇴장합니다
                            Client.m_Core.Leave();
                            break;

                        case "/exit":
                            // 콘솔 프로그램을 종료합니다
                            run_program = false;
                            break;

                        case "/h":
                            // 명령어 도움말 출력
                            PutCommandList();
                            break;


                        case "/login":
                            // 로그인 서버로 이동 요청
                            if (Client.server_now != UnityCommon.Server.None) // 서버에 연결되있을때만 패킷을 보내기 위해
                            {
                                Client.server_tag = UnityCommon.Server.Login;
                                Client.proxy.server_move(ZNet.RemoteID.Remote_Server, ZNet.CPackOption.Basic, (int)Client.server_tag);
                            }
                            break;

                        case "/lobby":
                            // 로비 서버로 이동 요청
                            if (Client.server_now != UnityCommon.Server.None) // 서버에 연결되있을때만 패킷을 보내기 위해
                            {
                                Client.server_tag = UnityCommon.Server.Lobby;
                                Client.proxy.server_move(ZNet.RemoteID.Remote_Server, ZNet.CPackOption.Basic, (int)Client.server_tag);
                            }
                            break;

                        case "/room":
                            // 룸 서버로 이동 요청
                            if (Client.server_now != UnityCommon.Server.None) // 서버에 연결되있을때만 패킷을 보내기 위해
                            {
                                Client.server_tag = UnityCommon.Server.Room;
                                Client.proxy.server_move(ZNet.RemoteID.Remote_Server, ZNet.CPackOption.Basic, (int)Client.server_tag);
                            }
                            break;

                        default:
                            if (Client.server_now != UnityCommon.Server.None) // 서버에 연결되있을때만 패킷을 보내기 위해
                                Client.proxy.Chat(ZNet.RemoteID.Remote_Server, ZNet.CPackOption.Basic, ret.Result);
                            else
                                Console.WriteLine("/c 로 서버에 먼저 연결하세요");
                            break;
                    }

                    if (run_program)
                        ret = NetCommon.Common.ReadLineAsync();
                }

                Client.m_Core.NetLoop();
                System.Threading.Thread.Sleep(1);
            }

            Client.m_Core.Destroy();
            System.Threading.Thread.Sleep(1000 * 2);
        }

        public static void PutCommandList()
        {
            Console.WriteLine("/Cmd:  c(접속)  q(퇴장)  h(Help)");
        }
    }
}
