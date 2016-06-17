using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Server
{
    public class Log
    {
        public static log4net.ILog logger = null;
    }

    static class Program
    {
        /// <summary>
        /// 해당 응용 프로그램의 주 진입점입니다.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            UnityCommon.Server launchMode = UnityCommon.Server.Login;
            int portnum = 0;

            if (args.Length >= 1)
            {
                switch (args[0])
                {
                    case "Login":
                        launchMode = UnityCommon.Server.Login;
                        break;

                    case "Lobby":
                        launchMode = UnityCommon.Server.Lobby;
                        break;

                    case "Room":
                        launchMode = UnityCommon.Server.Room;
                        break;

                    case "Master":
                        launchMode = UnityCommon.Server.Master;
                        break;
                }
            }

            // 포트 설정
            if (args.Length == 2)
            {
                portnum = int.Parse(args[1]);
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FormServer(launchMode, portnum));
        }
    }
}
