using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityCommon;
using ZNet;

namespace Server.User
{
    public partial class LoginServer : UserServer
    {
        public LoginServer(FormServer f, UnityCommon.Server s, int portnum) : base(f, s, portnum)
        {
        }

        protected override void BeforeStart(out StartOption param)
        {
            base.BeforeStart(out param);
            
            param.m_IpAddressListen = UnityCommon.Join.ipaddr;

            // 접속을 받을 포트
            param.m_PortListen = UnityCommon.Join.portnum;



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
                        form.printf("[DB로그인] 처리 시작. 결과를 기다리는 중입니다 ....\n");

                        try
                        {
                            dynamic result_db = Simple.Data.Database.Open().UserInfo.FindAllByUserName(name).FirstOrDefault();


                            if (result_db == null)
                            {
                                form.printf("[DB로그인] 해당 유저가 DB에 존재하지 않아 새로운 계정을 생성합니다\n");

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
                                form.printf("[DB로그인] 인증진행중...\n");
                                /*if (result_db.StateOnline) // 이미 로그인한 상태 --> 중복 로그인 오류
                                {
                                    return 2;
                                }
                                else*/
                                if (result_db.PassWord == pass) // 암호 검사
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
                            form.printf("[DB로그인] 예외발생 {0}\n", e.ToString());
                        }

                        return 0;
                    });


                    // 상단의 DB Task가 완료되는 시점이므로 다시 유저가 유효한 상태인지 확인합니다
                    if (RemoteClients.TryGetValue(remote, out rc) == false)
                    {
                        form.printf("[DB로그인 결과받음] 결과 받은 시점에 해당유저 연결종료됨\n");
                        return;
                    }

                    // 인증 성공
                    if (result == 1)
                    {
                        form.printf("[DB로그인 결과받음] DB인증성공 : CashMoney : {0}  GameMoney : {1}  \n", rc.data.money_cash, rc.data.money_game);
                        rc.data = dummy;
                        rc.joined = true;
                        proxy.reponse_Login(remote, ZNet.CPackOption.Basic, true);
                    }
                    else
                    {
                        if (result == 2)
                            form.printf("[DB로그인 결과받음] Login 실패 : 중복로그인  idname={0}\n", name);
                        else if (result == 3)
                            form.printf("[DB로그인 결과받음] Login 실패 : 암호오류  idname={0}\n", name);
                        else
                            form.printf("[DB로그인 결과받음] Login 실패 : ???  idname={0}\n", name);

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
                    form.printf("[DB로그인] DB작업없이 진행합니다 설정을 확인해주세요.\n");

                    rc.data.userID = Guid.Empty;
                    rc.data.userName = "김철수";
                    rc.data.money_cash = 0;
                    rc.data.money_game = 500;
                    rc.joined = true;
                    proxy.reponse_Login(remote, ZNet.CPackOption.Basic, true);
                }

                return true;
            };
        }

        protected override void AfterStart()
        {
            base.AfterStart();

            m_Core.MasterConnect(
                NetServerCommon.MasterServerConnect.master_ipaddr,
                NetServerCommon.MasterServerConnect.master_portnum,
                this.Name,
                (int)this.Type
                );
        }
    }
}
