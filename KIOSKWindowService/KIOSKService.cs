using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using WebAgent;

namespace KIOSKWindowService
{
    public partial class KIOSKService : ServiceBase
    {
        Thread thread;
        Thread ziThread;
        Agent agent;
        TcpListener listener = null;//代理端的监听
        TcpClient client = null;//监听到客户端的socket

        public KIOSKService()
        {
            InitializeComponent();
        }

        #region 启动服务
        /// <summary>
        /// 启动服务
        /// </summary>
        /// <param name="args"></param>
        protected override void OnStart(string[] args)
        {
            using (System.IO.StreamWriter sw = new System.IO.StreamWriter("C:\\KIOSKlog1.txt", true))
            {
                sw.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss ") + "Start.");
                sw.WriteLine("KIOSK服务已启动...");
            }
            thread = new Thread(new ThreadStart(reClient));
            thread.Start();
        }

        #endregion

        #region 停止服务
        /// <summary>
        /// 停止服务
        /// </summary>
        /// <param name="args"></param>
        protected override void OnStop()
        {
            using (System.IO.StreamWriter sw = new System.IO.StreamWriter("C:\\KIOSKlog.txt", true))
            {
                sw.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss ") + "Stop.");
                sw.WriteLine("KIOSK服务已停止...");
            }
            if (listener != null)
            {
                listener.Server.Close();
                listener.Stop();
                listener = null;
            }
            if (thread != null)
            {
                thread.Abort();
            }
            if (ziThread != null)
            {
                ziThread.Abort();
            }
            if (agent != null)
            {
                agent.stop();
            }
        }
        #endregion

        #region 接收多个客户端
        public void reClient()
        {
            string listenerIp = WebService.ContentValue("Listener", "ListenerIp", WebService.WASConfigPath);//监听的IP地址
            string listenerPort = WebService.ContentValue("Listener", "ListenerPort", WebService.WASConfigPath);//监听的端口号
            string DataSourceIpOrPortA = WebService.ContentValue("DataSourceA", "DataServiceA", WebService.WASConfigPath);//数据源的IP和端口A
            string DataSourceIpOrPortB = WebService.ContentValue("DataSourceB", "DataServiceB", WebService.WASConfigPath);//数据源的IP和端口B
            using (System.IO.StreamWriter sws = new System.IO.StreamWriter("C:\\KIOSKlog2.txt", true))
            {
                sws.WriteLine(DataSourceIpOrPortA);
                sws.WriteLine(DataSourceIpOrPortB);
            }
            IPAddress ip = IPAddress.Parse(listenerIp);//IP地址
            listener = new TcpListener(ip, Convert.ToInt32(listenerPort));//使用本机Ip地址和端口号创建一个System.Net.Sockets.TcpListener的实例
            try
            {
                listener.Start(); ////监听客户端的请求：开始侦听 
            }
            catch (Exception)
            {
                return;
            }
            try
            {
                while (true) //接收多个客户端
                {
                    client = listener.AcceptTcpClient();//获取单一客户端连接
                    agent = new Agent(client, client.GetStream(), DataSourceIpOrPortA, DataSourceIpOrPortB);
                    ziThread = new Thread(agent.dele);//单一客户端可以发送多条请求
                    ziThread.Start(null);
                }
            }
            catch (SocketException)
            {
            }
        }
        #endregion
    }
}
