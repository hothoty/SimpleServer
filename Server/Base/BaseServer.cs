using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Base
{
    public abstract partial class BaseServer
    {
        UnityCommon.Server type;


        string name;


        public FormServer form;


        public dynamic db;


        public ZNet.CoreServerNet m_Core;


        public Rmi.Proxy proxy;
        public Rmi.Stub stub;



        public string Name
        {
            get { return name; }
        }

        public UnityCommon.Server Type
        {
            get { return type; }
        }


        public BaseServer(FormServer f, UnityCommon.Server s, int portnum)
        {
            this.form = f;
            this.type = s;
            this.name = string.Format("{0}-{1}", s, portnum);
        }

        ~BaseServer()
        {
        }


        protected virtual void NewCore()
        {
            m_Core = new ZNet.CoreServerNet();
        }

        protected abstract void BeforeStart(out ZNet.StartOption param);

        protected virtual void AfterStart()
        {

        }

        public void OnStart()
        {
            NewCore();

            proxy = new Rmi.Proxy();
            stub = new Rmi.Stub();

            m_Core.Attach(proxy, stub);

            m_Core.message_handler = (ZNet.ResultInfo result) =>
            {
                string str_msg = "Msg : ";
                str_msg += result.msg;
                Log.logger.Info(str_msg);
            };
            m_Core.exception_handler = (Exception e) =>
            {
                string str_msg = "Exception : ";
                str_msg += e.ToString();
                Log.logger.Error(str_msg);
            };


            ZNet.StartOption param;
            BeforeStart(out param);


            ZNet.ResultInfo outResult = new ZNet.ResultInfo();
            if (m_Core.Start(param, outResult))
            {
                Log.logger.InfoFormat("{0} start ok.", this.name);
            }
            else
            {
                Log.logger.ErrorFormat("{0} Start error : {1}", this.name, outResult.msg);
            }


            AfterStart();
        }
    }
}
