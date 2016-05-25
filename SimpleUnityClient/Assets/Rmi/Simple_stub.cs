// Auto created from IDLCompiler.exe
using System;
using System.Collections.Generic;
using System.Net;


namespace Rmi 
{
	
public class Stub : ZNet.PKStub
{
	public delegate bool request_LoginDelegate(ZNet.RemoteID remote, ZNet.CPackOption pkOption, string name, string pass);
	public request_LoginDelegate request_Login = delegate(ZNet.RemoteID remote, ZNet.CPackOption pkOption, string name, string pass)
	{
		return false;
	};
	public delegate bool reponse_LoginDelegate(ZNet.RemoteID remote, ZNet.CPackOption pkOption, bool bResult);
	public reponse_LoginDelegate reponse_Login = delegate(ZNet.RemoteID remote, ZNet.CPackOption pkOption, bool bResult)
	{
		return false;
	};
	public delegate bool server_moveDelegate(ZNet.RemoteID remote, ZNet.CPackOption pkOption, int server_type);
	public server_moveDelegate server_move = delegate(ZNet.RemoteID remote, ZNet.CPackOption pkOption, int server_type)
	{
		return false;
	};
	public delegate bool ChatDelegate(ZNet.RemoteID remote, ZNet.CPackOption pkOption, string txt);
	public ChatDelegate Chat = delegate(ZNet.RemoteID remote, ZNet.CPackOption pkOption, string txt)
	{
		return false;
	};

	public override bool ProcessMsg(ZNet.CRecvedMsg rm) 
	{
		ZNet.RemoteID remote = rm.remote;
		if( remote == ZNet.RemoteID.Remote_None )
		{
			//err
		}

		ZNet.CPackOption pkOption = rm.pkop;
		ZNet.CMessage __msg = rm.msg;
		ZNet.PacketType PkID = rm.pkID;
		if( PkID < ZNet.PacketType.PacketType_User )
			return true;

		switch( PkID ) 
		{
		case Common.request_Login: 
			{
				string name; RemoteClass.Marshaler.Read(__msg, out name);
				string pass; RemoteClass.Marshaler.Read(__msg, out pass);

				bool bRet = request_Login( remote, pkOption, name, pass );
				if( bRet==false )
					NeedImplement("request_Login");
			} 
			break; 

		case Common.reponse_Login: 
			{
				bool bResult; RemoteClass.Marshaler.Read(__msg, out bResult);

				bool bRet = reponse_Login( remote, pkOption, bResult );
				if( bRet==false )
					NeedImplement("reponse_Login");
			} 
			break; 

		case Common.server_move: 
			{
				int server_type; RemoteClass.Marshaler.Read(__msg, out server_type);

				bool bRet = server_move( remote, pkOption, server_type );
				if( bRet==false )
					NeedImplement("server_move");
			} 
			break; 

		case Common.Chat: 
			{
				string txt; RemoteClass.Marshaler.Read(__msg, out txt);

				bool bRet = Chat( remote, pkOption, txt );
				if( bRet==false )
					NeedImplement("Chat");
			} 
			break; 

			default: goto __fail;
		}

		return true;

		__fail:
		{
			//err
			return false;
		}
	}

}

}

