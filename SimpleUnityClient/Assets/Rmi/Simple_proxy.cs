// Auto created from IDLCompiler.exe
using System;
using System.Collections.Generic;
using System.Net;


namespace Rmi 
{

public class Proxy : ZNet.PKProxy
{
	public bool request_Login(ZNet.RemoteID remote, ZNet.CPackOption pkOption, string name, string pass )
	{
		ZNet.CMessage Msg = new ZNet.CMessage();
		ZNet.PacketType msgID = (ZNet.PacketType)Common.request_Login; 
		
		Msg.WriteStart( msgID, pkOption, 0, true );

		RemoteClass.Marshaler.Write( Msg, name );
		RemoteClass.Marshaler.Write( Msg, pass );

		return PacketSend( remote, pkOption, Msg );
	} 

	public bool reponse_Login(ZNet.RemoteID remote, ZNet.CPackOption pkOption, bool bResult )
	{
		ZNet.CMessage Msg = new ZNet.CMessage();
		ZNet.PacketType msgID = (ZNet.PacketType)Common.reponse_Login; 
		
		Msg.WriteStart( msgID, pkOption, 0, true );

		RemoteClass.Marshaler.Write( Msg, bResult );

		return PacketSend( remote, pkOption, Msg );
	} 

	public bool server_move(ZNet.RemoteID remote, ZNet.CPackOption pkOption, int server_type )
	{
		ZNet.CMessage Msg = new ZNet.CMessage();
		ZNet.PacketType msgID = (ZNet.PacketType)Common.server_move; 
		
		Msg.WriteStart( msgID, pkOption, 0, true );

		RemoteClass.Marshaler.Write( Msg, server_type );

		return PacketSend( remote, pkOption, Msg );
	} 

	public bool Chat(ZNet.RemoteID remote, ZNet.CPackOption pkOption, string txt )
	{
		ZNet.CMessage Msg = new ZNet.CMessage();
		ZNet.PacketType msgID = (ZNet.PacketType)Common.Chat; 
		
		Msg.WriteStart( msgID, pkOption, 0, true );

		RemoteClass.Marshaler.Write( Msg, txt );

		return PacketSend( remote, pkOption, Msg );
	} 

}

}

