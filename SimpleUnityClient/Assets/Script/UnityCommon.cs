using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


// 유니티와 서버/클라간 공유할 내용입니다
namespace UnityCommon
{
    /// <summary>
    /// 최초 로그인할 서버 주소
    /// </summary>
    public class Join
    {
        public static readonly string ipaddr = "127.0.0.1";
        public static readonly UInt16 portnum = 20000;
        public static readonly UInt32 protocol_ver = 0;
    }


    /// <summary>
    /// 서버 종류
    /// </summary>
    public enum Server : int
    {
        None = 0,
        Login,
        Lobby,
        Room
    }
}
