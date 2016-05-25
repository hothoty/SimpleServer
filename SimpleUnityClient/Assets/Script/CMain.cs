using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using ZNet;

public class CMain : MonoBehaviour {

	string input_text;
    List<string> received_texts;
    Vector2 currentScrollPos = new Vector2();



    CoreClientNet m_Core;
    Rmi.Proxy proxy;
    Rmi.Stub stub;


    // 현재 서버 위치
    public UnityCommon.Server server_now = UnityCommon.Server.None;


    // 목표 서버 위치
    public UnityCommon.Server server_tag = UnityCommon.Server.None;


    void Awake()
	{
        input_text = "";
        received_texts = new List<string>();

        proxy = new Rmi.Proxy();
        stub = new Rmi.Stub();
    }

	void Start()
	{
		this.m_Core = new CoreClientNet();
        m_Core.Attach(proxy, stub);


        // 서버가 보낸 로그인인증 결과패킷을 처리합니다
        stub.reponse_Login = (ZNet.RemoteID remote, ZNet.CPackOption pkOption, bool bResult) =>
        {
            this.received_texts.Add("로그인 인증 결과" + bResult);
            this.currentScrollPos.y = float.PositiveInfinity;
            return true;
        };

        // 서버로부터 받은 메세지
        stub.Chat = (ZNet.RemoteID remote, ZNet.CPackOption pkOption, string txt) =>
        {
            this.received_texts.Add(txt);
            this.currentScrollPos.y = float.PositiveInfinity;
            return true;
        };


        // 서버이동 시도에 대한 실패 이벤트
        m_Core.move_fail_handler = () =>
        {
            this.received_texts.Add("서버이동 처리가 실패하였습니다.");
            this.currentScrollPos.y = float.PositiveInfinity;
        };


        // 서버에 입장된 시점
        m_Core.server_join_handler = (ZNet.ConnectionInfo info) =>
        {
            if (info.moved)
            {
                // 서버이동이 성공한 시점 : 위치를 목표했던 서버로 설정
                server_now = server_tag;
                this.received_texts.Add(string.Format("서버이동성공 [{0}:{1}] {2}", info.addr.m_ip, info.addr.m_port, server_now));
                this.currentScrollPos.y = float.PositiveInfinity;
            }
            else
            {
                // 최초 입장의 성공시점 : 위치를 로그인 서버로 설정
                server_now = UnityCommon.Server.Login;
                this.received_texts.Add(string.Format("서버입장성공 {0}", server_now));
                this.currentScrollPos.y = float.PositiveInfinity;

                // 최초 로그인 DB인증 시도 요청
                proxy.request_Login(ZNet.RemoteID.Remote_Server, ZNet.CPackOption.Basic, "철수", "abcd");
            }
        };

        // 서버에 퇴장된 시점
        m_Core.server_leave_handler = (ZNet.ConnectionInfo info) =>
        {
            if (info.moved)
            {
                this.received_texts.Add(string.Format("서버이동을 위해 퇴장, 이동할서버 [{0}:{1}]", info.addr.m_ip, info.addr.m_port));
                this.currentScrollPos.y = float.PositiveInfinity;
            }
            else
            {
                this.received_texts.Add(string.Format("서버퇴장성공"));
                this.currentScrollPos.y = float.PositiveInfinity;
            }


            // 어떤 서버에서 퇴장하든 재접속시 최초접속과 구분하기 위하여 로그인 서버로 세팅해둡니다
            server_now = UnityCommon.Server.Login;
        };

        // 서버에 연결된 시점
        m_Core.server_connect_result_handler = (bool isConnectSuccess) =>
        {
            if (isConnectSuccess)
            {
                this.received_texts.Add("Connected!");
                this.currentScrollPos.y = float.PositiveInfinity;
            }
            else
            {
                this.received_texts.Add("Connect Fail!");
                this.currentScrollPos.y = float.PositiveInfinity;
            }
        };

        m_Core.message_handler = (ZNet.ResultInfo result) =>
        {
            string str_msg = "Msg : ";
            str_msg += result.msg;
            this.received_texts.Add(str_msg);
            this.currentScrollPos.y = float.PositiveInfinity;
        };


        this.received_texts.Add("프로그램 시작");
        this.currentScrollPos.y = float.PositiveInfinity;

        // 최초 로그인 시도
        m_Core.Connect(
            "127.0.0.1",
            20000/*tcp port*/,
            0,/*protocol version*/
            0/*udp disable=0*/,
            true/*mobile*/,
            false/*RecoveryUse*/
        );
	}

    void OnApplicationQuit()
    {
        m_Core.Destroy();
    }

	/// <summary>
	/// 네트워크 관련 이벤트 처리
	/// </summary>
	void Update()
	{
        m_Core.NetLoop();
	}
	
	void OnGUI()
	{
#if UNITY_ANDROID || UNITY_IPHONE
		GUI.skin.label.fontSize = 30;
		GUI.skin.button.fontSize = 30;
#endif

        // Received text.
        GUILayout.BeginVertical();
		currentScrollPos = GUILayout.BeginScrollView(
            currentScrollPos,
            GUILayout.MaxWidth(Screen.width), GUILayout.MinWidth(Screen.width),
            GUILayout.MaxHeight(Screen.height - 100), GUILayout.MinHeight(Screen.height - 100)
            );
		
		foreach (string text in this.received_texts)
		{
			GUILayout.BeginHorizontal();
			GUI.skin.label.wordWrap = true;
			GUILayout.Label(text);
			GUILayout.EndHorizontal();
		}
		
		GUILayout.EndScrollView();
		GUILayout.EndVertical();


        // Input.
        GUILayout.BeginHorizontal();
		this.input_text = GUILayout.TextField(
            this.input_text,
            GUILayout.MaxWidth(Screen.width - 100), GUILayout.MinWidth(Screen.width - 100),
            GUILayout.MaxHeight(50), GUILayout.MinHeight(50)
            );

        int screenW = Screen.width / 2 / 10;
        int screenH = Screen.height / 2 / 10;
        if (GUI.Button(new Rect(Screen.width / 2 - screenW - screenW, screenH * 8, screenW, screenH), "Send"))
        {
            if (this.server_now != UnityCommon.Server.None) // 서버에 연결되있을때만 패킷을 보내기 위해
            {
                // 서버로 채팅 메세지 보내기
                proxy.Chat(RemoteID.Remote_Server, CPackOption.Basic, this.input_text);
                this.input_text = "";
            }
        }

        if (GUI.Button(new Rect(Screen.width / 2 - screenW - screenW - screenW, screenH * 9, screenW, screenH), "goLogin"))
        {
            if (this.server_now != UnityCommon.Server.None) // 서버에 연결되있을때만 패킷을 보내기 위해
            {
                this.server_tag = UnityCommon.Server.Login;
                this.proxy.server_move(ZNet.RemoteID.Remote_Server, ZNet.CPackOption.Basic, (int)this.server_tag);
            }
        }
        if (GUI.Button(new Rect(Screen.width / 2 - screenW - screenW, screenH * 9, screenW, screenH), "goLobby"))
        {
            if (this.server_now != UnityCommon.Server.None) // 서버에 연결되있을때만 패킷을 보내기 위해
            {
                this.server_tag = UnityCommon.Server.Lobby;
                this.proxy.server_move(ZNet.RemoteID.Remote_Server, ZNet.CPackOption.Basic, (int)this.server_tag);
            }
        }
        if (GUI.Button(new Rect(Screen.width / 2 - screenW, screenH * 9, screenW, screenH), "goRoom"))
        {
            if (this.server_now != UnityCommon.Server.None) // 서버에 연결되있을때만 패킷을 보내기 위해
            {
                this.server_tag = UnityCommon.Server.Room;
                this.proxy.server_move(ZNet.RemoteID.Remote_Server, ZNet.CPackOption.Basic, (int)this.server_tag);
            }
        }

        GUILayout.EndHorizontal();
	}
}
