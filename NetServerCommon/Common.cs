using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetServerCommon
{
    public class Var
    {
        public static readonly bool Use_DB = false;
    }

    /// <summary>
    /// 마스터 서버 관련 정보
    /// </summary>
    public class MasterServerConnect
    {
        public static string master_ipaddr = "127.0.0.1";
        public static readonly UInt16 master_portnum = 35000;
    }


    /// <summary>
    /// 클라이언트가 접속할 로비서버의 주소
    /// </summary>
    public class Lobby
    {
        public static readonly string ipaddr = "127.0.0.1";
        public static readonly UInt16 portnum = 22000;
    }

    /// <summary>
    /// 클라이언트가 접속할 룸서버의 주소
    /// </summary>
    public class Room
    {
        public static readonly string ipaddr = "127.0.0.1";
        public static readonly UInt16 portnum = 25000;
    }


    /// <summary>
    /// 서버이동시 동기화할 유저 데이터
    /// </summary>
    public struct UserDataSync
    {
        public Guid userID;
        public string userName;
        public int money_cash;
        public int money_game;
        public string temp;
    }

    /// <summary>
    /// 각 서버에서 사용할 유저 데이터
    /// </summary>
    public class CUser
    {
        public UserDataSync data;
        public int dummy;

        // 인증여부
        public bool joined = false;
    }


    public class Common
    {
        // 서버이동시 동기화할 유저 데이터 구성
        static public void UserDataMove_Start(CUser rc, out ZNet.ArrByte buffer)
        {
            ZNet.CMessage msg = new ZNet.CMessage();
            msg.Write(rc.data.userID);
            msg.Write(rc.data.userName);
            msg.Write(rc.data.money_cash);
            msg.Write(rc.data.money_game);
            msg.Write(rc.data.temp);
            buffer = msg.m_array;
        }

        // 서버이동 완료시 동기화할 유저 데이터 복구
        static public void UserDataMove_Complete(ZNet.ArrByte buffer, out CUser data)
        {
            CUser rc = new CUser();

            ZNet.CMessage msg = new ZNet.CMessage();
            msg.m_array = buffer;

            msg.Read(out rc.data.userID);
            msg.Read(out rc.data.userName);
            msg.Read(out rc.data.money_cash);
            msg.Read(out rc.data.money_game);
            msg.Read(out rc.data.temp);
            data = rc;

            // 서버이동 입장인 경우 즉시 인증완료 상태로 세팅
            data.joined = true;
        }

        
        /// <summary>
        /// 여러가지 상태를 출력합니다
        /// </summary>
        /// <param name="svr"></param>
        static public void DisplayStatus(ZNet.CoreServerNet svr)
        {
            ZNet.ServerState status;
            svr.GetCurrentState(out status);


            // 기본 정보
            Console.WriteLine(string.Format(
                "[NetInfo]  Connect/Join {0}/{1}  Connect(Server) {2}  Accpet/Max {3}/{4}",

                // 실제 연결된 client
                status.m_CurrentClient,

                // 서버에 입장완료상태의 client
                status.m_JoinedClient,

                // 서버간 direct p2p 연결된 server
                status.m_ServerP2PCount,

                // 이 서버에 추가 연결 가능한 숫자
                status.m_nIoAccept,

                // 이 서버에 최대 연결 가능한 숫자
                status.m_MaxAccept
                ));


            // 네트워크 IO 상태 정보
            Console.WriteLine(string.Format(
                "[IO Info]  Close {0}  Event {1}  Recv {2}  Send {3}",

                // current io close
                status.m_nIoClose,

                // current io event
                status.m_nIoEvent,

                // current io recv socket
                status.m_nIoRecv,

                // current io send socket
                status.m_nIoSend
            ));
        }
    }
}
