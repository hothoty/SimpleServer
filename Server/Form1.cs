using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Server
{
    public partial class FormServer : Form
    {
        Base.BaseServer svr;

        int printf_cnt = 1;


        public FormServer(UnityCommon.Server s, int portnum)
        {
            string server_name = string.Format("{0}-{1}", s, portnum);


            Log.logger = log4net.LogManager.GetLogger(server_name);


            log4net.Config.XmlConfigurator.Configure();


            InitializeComponent();


            switch (s)
            {
                case UnityCommon.Server.Login:
                    svr = new User.LoginServer(this, s, portnum);
                    break;

                case UnityCommon.Server.Lobby:
                    svr = new User.LobbyServer(this, s, portnum);
                    break;

                case UnityCommon.Server.Room:
                    svr = new User.RoomServer(this, s, portnum);
                    break;

                case UnityCommon.Server.Master:
                    svr = new Master.MasterServer(this, s, portnum);
                    break;
            }

            Text = string.Format("Simple Server  : {0}", server_name);
        }


        public void printf(string txt, params object[] args)
        {
            printf(string.Format(txt, args));
        }

        public void printf(string txt)
        {
            Log.logger.Debug(txt);

            this.Invoke(new Action(() =>
            {
                listBox1.Items.Add(string.Format("{0}:  {1}", printf_cnt++, txt));

                if (listBox1.Items.Count > 1000)
                    listBox1.Items.RemoveAt(0);

                listBox1.SetSelected(listBox1.Items.Count - 1, true);
            }));
        }


        private void timer1_Tick(object sender, EventArgs e)
        {
            // net thread process
        }

        private void FormServer_Load(object sender, EventArgs e)
        {
            printf("Start server.");


            svr.OnStart();


            timer1.Start();
        }
    }
}
