using System;
using System.Collections.Generic;
using System.Configuration.Install;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using WebAgent;

namespace KIOSKWindowControl
{
    class Program
    {
        static Thread thread;//主线程
        static Thread ziThread;//子线程
        static Agent agent;
        static string userEnty;//接收在控制台输入的信息
        int i;//代表已启动了代理服务
        static TcpListener listener = null;//代理端的监听
        static TcpClient client = null;//监听到客户端的socket
        private static bool setConsoleColor;//标志控制台背景颜色
        static string kioskServieName = "KIOSKSysService";//安装之后的服务名称    这里的kioskServieName是对应真实项目中的服务名称
        static string path = Directory.GetCurrentDirectory() + "\\KIOSKWindowService.exe";//获取当前目录下的KIOSKWindowService.exe文件
        static void Main(string[] args)
        {
            Console.WriteLine(path);
            Console.WriteLine("\r\n=========================Welcome To KIOSK Agent Service=========================");
            Console.WriteLine("================================================================================");
            Console.WriteLine("==========请选择以下操作继续...");
            Console.WriteLine("\r\n==========[r]：【以控制台程序形式【启动】服务】");
            Console.WriteLine("\r\n==========[s]：【以控制台程序形式【停止】服务】");
            Console.WriteLine("\r\n==========[i]：【安装Window服务】");
            Console.WriteLine("\r\n==========[u]：【卸载Window服务】");
            Console.WriteLine("\r\n==========[e]：【退出】");
            Console.WriteLine("\r\n================================================================================");
            do
            {
                userEnty = Console.ReadLine();
                installAndUinstall();//安装和卸载服务器
            } while (userEnty != "e");//输入e时代表程序退出
            Console.WriteLine("已退");
            Environment.Exit(0);
        }

        #region 安装和卸载服务器
        /// <summary>
        /// 安装和卸载服务器的方法
        /// </summary>
        public static void installAndUinstall()
        {
            switch (userEnty)
            {
                case"r":
                    CheckCanSetConsoleColor();//重置控制台原来的颜色
                    Console.WriteLine("\r\n=========================Welcome To KIOSK Agent Service=========================");
                    thread = new Thread(new ThreadStart(reClient));//开启一个主线程用于监听客户端的连接和接收多个客户端发来的tcp/ip连接
                    thread.Start();
                    break;
                case "s":
                    CheckCanSetConsoleColor();//重置控制台原来的颜色
                    if (listener != null)
                    {
                        listener.Stop();
                        listener.Server.Close();
                        listener = null;
                        if (ziThread != null)
                        {
                            ziThread.Abort();
                        }
                        thread.Abort();
                        if (agent != null)
                        {
                            agent.stop();
                        }
                        Console.WriteLine("\r\n================================================================================");
                        SetConsoleColor(ConsoleColor.Green);
                        Console.WriteLine("================================《服务已停止》==================================");
                        CheckCanSetConsoleColor();//重置控制台原来的颜色
                        Console.WriteLine("\r\n================================================================================");
                    }
                    else 
                    {
                        Console.WriteLine("\r\n================================================================================");
                        SetConsoleColor(ConsoleColor.Yellow);
                        Console.WriteLine("================================《服务还没启动》===============================");
                        CheckCanSetConsoleColor();//重置控制台原来的颜色
                        Console.WriteLine("\r\n================================================================================");
                    }
                    break;
                case "i":
                    CheckCanSetConsoleColor();//重置控制台原来的颜色
                    string[] KIOSKWindowServiceI = { path };//需要执行的可执行文件(即可执行的服务程序文件)
                    if (!ServiceIsExisted(kioskServieName))//检测安装服务名是否存在,如果不存在 则执行以下操作
                    {
                        try
                        {
                            Console.WriteLine("================================================================================");
                            SetConsoleColor(ConsoleColor.Green);
                            Console.WriteLine("正在安装中...");
                            CheckCanSetConsoleColor();//重置控制台原来的颜色
                            Console.WriteLine("\r\n================================================================================");
                            SetConsoleColor(ConsoleColor.Green);
                            ManagedInstallerClass.InstallHelper(KIOSKWindowServiceI);  //参数exe就是你用 InstallUtil.exe 工具安装时的参数。一般就是一个exe的文件名
                            CheckCanSetConsoleColor();//重置控制台原来的颜色
                            Console.WriteLine("\r\n================================================================================");
                            SetConsoleColor(ConsoleColor.Green);
                            Console.WriteLine("================================《服务安装成功》================================");
                            CheckCanSetConsoleColor();//重置控制台原来的颜色
                            Console.WriteLine("\r\n================================================================================");
                        }
                        catch (Exception e)
                        {
                            CheckCanSetConsoleColor();//重置控制台原来的颜色
                            Console.WriteLine("================================================================================");
                            SetConsoleColor(ConsoleColor.Red);
                            Console.WriteLine("================================《服务安装失败》================================");
                            CheckCanSetConsoleColor();//重置控制台原来的颜色
                            Console.WriteLine("\r\n================================================================================");
                            return;
                        }
                    }
                    else
                    {
                        CheckCanSetConsoleColor();//重置控制台原来的颜色
                        Console.WriteLine("================================================================================");
                        SetConsoleColor(ConsoleColor.Yellow);
                        Console.WriteLine("========================《该服务已经存在，不用重复安装》========================");
                        CheckCanSetConsoleColor();//重置控制台原来的颜色
                        Console.WriteLine("================================================================================");
                    }
                    break;
                case "u":
                    CheckCanSetConsoleColor();//重置控制台原来的颜色
                    string[] KIOSKWindowServiceU = { "/u", path };//需要执行的可执行文件(即可执行的服务程序文件)
                    if (ServiceIsExisted(kioskServieName))//如果不存在 则执行以下操作
                    {
                        try
                        {
                            Console.WriteLine("================================================================================");
                            SetConsoleColor(ConsoleColor.Green);
                            Console.WriteLine("==========正在卸载中...\r\n");
                            CheckCanSetConsoleColor();//重置控制台原来的颜色
                            Console.WriteLine("================================================================================");
                            SetConsoleColor(ConsoleColor.Green);
                            ManagedInstallerClass.InstallHelper(KIOSKWindowServiceU);  //参数 exe 就是你用 InstallUtil.exe 工具安装时的参数。一般就是一个exe的文件名
                            CheckCanSetConsoleColor();//重置控制台原来的颜色
                            Console.WriteLine("\r\n================================================================================");
                            SetConsoleColor(ConsoleColor.Green);
                            Console.WriteLine("================================《服务卸载成功》================================");
                            CheckCanSetConsoleColor();//重置控制台原来的颜色
                            Console.WriteLine("================================================================================");
                        }
                        catch (Exception ex)
                        {
                            CheckCanSetConsoleColor();//重置控制台原来的颜色
                            Console.WriteLine("================================================================================");
                            SetConsoleColor(ConsoleColor.Red);
                            Console.WriteLine("================================《服务卸载失败》================================");
                            CheckCanSetConsoleColor();//重置控制台原来的颜色
                            Console.WriteLine("================================================================================");
                            return;
                        }
                    }
                    else
                    {
                        CheckCanSetConsoleColor();//重置控制台原来的颜色
                        Console.WriteLine("================================================================================");
                        SetConsoleColor(ConsoleColor.Yellow);
                        Console.WriteLine("===========================《该服务不存在，无需卸载》===========================");
                        CheckCanSetConsoleColor();//重置控制台原来的颜色
                        Console.WriteLine("================================================================================");
                    }
                    break;
                case "e":
                    //退出程序
                    Console.WriteLine(listener == null?true:false);
                    if (listener != null)
                    {
                        CheckCanSetConsoleColor();//重置控制台原来的颜色
                        Console.WriteLine("\r\n================================================================================");
                        SetConsoleColor(ConsoleColor.Red);
                        Console.WriteLine("======================《服务运行中,暂不能退出,先停止服务》======================");
                        CheckCanSetConsoleColor();//重置控制台原来的颜色
                        Console.WriteLine("================================================================================");
                        userEnty = "";
                    }
                    break;
                default:
                    CheckCanSetConsoleColor();//重置控制台原来的颜色
                    SetConsoleColor(ConsoleColor.Red);
                    Console.WriteLine("==================================《选择无效》==================================");
                    CheckCanSetConsoleColor();//重置控制台原来的颜色
                    break;
            }
        }
        #endregion

        #region 接收多个客户端
        public static void reClient()
        {
            //注意：WebService类是处于WebAgent.dll类库中编译的.dll文件中的一个类，用于读取ini文件的中的数据，在此添加dll引用之后直接使用即可
            string listenerIp = WebService.ContentValue("Listener", "ListenerIp", WebService.WASConfigPath);//读取ini文件内容中监听的IP地址(本地ip)
            string listenerPort = WebService.ContentValue("Listener", "ListenerPort", WebService.WASConfigPath);//读取ini文件内容中监听的端口号(本地端口)
            string DataSourceIpOrPortA = WebService.ContentValue("DataSourceA", "DataServiceA", WebService.WASConfigPath);//读取ini文件内容中需要转发的数据源的IP和端口
            string DataSourceIpOrPortB = WebService.ContentValue("DataSourceB", "DataServiceB", WebService.WASConfigPath);//读取ini文件内容中需要转发的数据源的IP和端口
            //构建服务端的监听IP
            IPAddress ip = IPAddress.Parse(listenerIp);
            //构建服务端的监听IP和端口号(服务器端的url目标路径TcpListener)
            listener = new TcpListener(ip, Convert.ToInt32(listenerPort));//使用本机Ip地址和端口号创建一个System.Net.Sockets.TcpListener的实例
            CheckCanSetConsoleColor();//重置控制台原来的颜色
            Console.WriteLine("================================================================================");
            try
            {
                listener.Start(); //监听客户端的请求：开始侦听
            }
            catch (SocketException)
            {
                CheckCanSetConsoleColor();//重置控制台原来的颜色
                Console.WriteLine("================================================================================");
                SetConsoleColor(ConsoleColor.Red);
                Console.WriteLine("代理服务正在运行中,暂不能重新启动服务...");
                CheckCanSetConsoleColor();//重置控制台原来的颜色
                Console.WriteLine("\r\n================================================================================");
                CheckCanSetConsoleColor();//重置控制台原来的颜色
                return;
            }
            try
            {
                SetConsoleColor(ConsoleColor.Green);
                Console.WriteLine("代理服务器开启监听，代理服务器正在运行中...\r\n");
                CheckCanSetConsoleColor();//重置控制台原来的颜色
                Console.WriteLine("================================================================================");
                //等待接收多个客户端(与该监听建立连接)
                while (true) 
                {
                    client = listener.AcceptTcpClient();//获取单一客户端连接
                    agent = new Agent(client, client.GetStream(), DataSourceIpOrPortA, DataSourceIpOrPortB);
                    ziThread = new Thread(agent.dele);//接收到客户端建立连接之后，单一客户端可以进入发送多条请求   //该线程只负责发送和接收多条请求，与其他线程操作无关
                    ziThread.Start(null);
                }
            }
            catch (Exception)
            {
                return;
            }
        }
        #endregion

        #region 检查服务是否存在
        /// <summary>   
        /// 检查指定的服务是否存在
        /// </summary>   
        /// <param name="serviceName">要查找的服务名字</param>   
        /// <returns></returns>   
        private static bool ServiceIsExisted(string koiskName)
        {
            ServiceController[] services = ServiceController.GetServices();//搜索系统的所有service服务
            foreach (ServiceController sysService in services)//遍历服务
            {
                if (sysService.ServiceName == koiskName)//服务名对等时执行以下操作
                {
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region 改变控制台背景颜色
        /// <summary>
        /// 改变控制台前景色
        /// </summary>
        /// <param name="color"></param>
        private static void SetConsoleColor(ConsoleColor color)
        {
            if (setConsoleColor)
            {
                Console.ForegroundColor = color;
            }
        }
        #endregion

        #region 改变控制台显示的颜色
        /// <summary>
        /// 改变控制台显示的颜色
        /// </summary>
        static void CheckCanSetConsoleColor()
        {
            try
            {
                Console.ForegroundColor = ConsoleColor.Black;//前景色
                Console.ResetColor();
                setConsoleColor = true;
            }
            catch
            {
                setConsoleColor = false;
            }
        }
        #endregion


    }
}
