using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace KIOSKWindowControl
{
    /**
     * 
     * 用于监听客户端与之建立起连接的类
     * 
     * */
    class Agent
    {
        private TcpClient client;//WEB服务器端A的socket
        private TcpClient webClientA;//WEB服务器端A的socket
        //private TcpClient webClientB;
        private TcpListener listener = null;//本代理端的监听
        private NetworkStream streamToClient;//客户端的流
        private int clientBufferSize = 500;//客户端缓存存放的字节数
        private byte[] readClient;//存放客户端的请求信息的缓存
        private string ipAddressA;//解析出来的web服务器ip
        //private string ipAddressB;
        private Thread ziThread;//处理客户端请求的线程
        private List<Socket> socketList = new List<Socket>();//存储客户端和目标服务器的连接（客户端需要访问的WEB服务器）
        private bool book = false;//标志是否为订票接口。true为是，false为不是
        string DataSourceIpOrPortA;//数据源IP和port
        string DataSourceIpOrPortB;

        /**
         * 
         * TcpClient client:传入与之建立连接的客户端的TcpClient
         * NetworkStream streamToClient:传入与之建立连接的客户端的TcpClient
         * string DataSourceIpOrPortA：传入目标服务器的路径A
         * string DataSourceIpOrPortB：传入目标服务器的路径B
         * 
         * */
        public Agent(TcpClient client, NetworkStream streamToClient, string DataSourceIpOrPortA, string DataSourceIpOrPortB)
        {
            this.client = client;
            this.streamToClient = streamToClient;
            this.DataSourceIpOrPortA = DataSourceIpOrPortA;
            this.DataSourceIpOrPortB = DataSourceIpOrPortB;
        }

        #region 处理子线程多条请求方法
        public void dele(object ojs)
        {
            int bytesReadClient = 0;//读取客户端发来信息的字节数
            string reqHeader = null;//存放请求头信息的字符串变量
            string clientInfo = "";//存储客户端发来的全部信息

            #region 接收客户端发来的请求
            if (client != null)//如果客户端连接不为null
            {
                while (true)//分段读取，直到读取完为止
                {
                    readClient = new byte[clientBufferSize];//客户端缓存
                    if (client.Connected)//如果客户端连接处于连接状态
                    {
                        try
                        {
                            bytesReadClient = streamToClient.Read(readClient, 0, readClient.Length); //从客户端流中读出数据并保存在了readClient缓存中
                            string InfoPoint = Encoding.GetEncoding("gb2312").GetString(readClient, 0, bytesReadClient);//读取流中的字节数据转为字符串
                            clientInfo += InfoPoint;//从缓存中获取到了实际请求的字符串;获取客户端发来的请求信息进行解码
                            string flag = "\r\n\r\n";//通过两空行进行将请求头和请求内容进行拆分
                            int rnIndex = clientInfo.IndexOf(flag);//获取客户端请求信息中两空行的下标
                            if (reqHeader != null && bytesReadClient < readClient.Length)//如果请求头不为空并且读取的字节小于缓存的长度(请求信息完整):说明已经读完
                            {
                                break;
                            }
                            //如果两空行存在则
                            if (rnIndex != -1)
                            {
                                reqHeader = clientInfo.Substring(0, rnIndex + "\r\n\r\n".Length);//设置请求头内容
                                clientInfo = clientInfo.Replace(reqHeader, "");//设置请求内容
                            }
                        }
                        catch (System.IO.IOException)
                        {
                            return;
                        }
                    }
                    else
                    {
                        return;
                    }
                }
            }
            else
            {
                return;
            }
            #endregion

            #region 与数据库A-B建立连接
            try
            {
                string ipA = DataSourceIpOrPortA;
                //判断ip
                ipAddressA = ipA.Substring(0, ipA.IndexOf(":"));
                int portA = Convert.ToInt32(ipA.Substring(ipA.IndexOf(":") + 1, ipA.Length - ipA.IndexOf(":") - 1));
                webClientA = new TcpClient();//新建web服务器端A的socket
                if (ipAddressA != null)
                {
                    webClientA.Connect(ipAddressA, portA);//用指定的ip与web服务器端A建立连接
                }
            }
            catch (Exception)
            {
                return;
            }
            #endregion

            #region 发送请求和接收响应

            #region 刚启动系统时默认向A数据数据服务发送请求操作
            try
            {
                if (webClientA != null && webClientA.Connected)//如果webClientA连接处于连接状态，则取到webClientA该连接的流进行发送信息
                {
                    NetworkStream streamToWeb = webClientA.GetStream();//取得web服务器的流
                    byte[] bufferWeb = Encoding.GetEncoding("gb2312").GetBytes(reqHeader + clientInfo);//对请求信息进行编码操作
                    streamToWeb.Write(bufferWeb, 0, bufferWeb.Length);//将编码之后的请求信息发往web服务器
                }
                else
                {
                    return;
                }
            }
            catch (Exception)
            {
                return;
            }
            #endregion

            #region 检测发送和响应
            while (true)
            {
                try
                {
                    socketList.Clear();//清空存放socket的集合列表
                    socketList.Add(webClientA.Client);//没调用过输入卡号api,则添加客户端和数据库A连接(true就跳到数据库A)
                    socketList.Add(client.Client);
                    Socket.Select(socketList, null, null, 3000);//检测列表中的连接的状态,超时为3秒
                }
                catch (Exception)
                {
                    return;
                }
                #region 遍历集合中的连接，检测是客户端(向对应的服务端发送信息)还是服务端(客户端响应信息)
                for (int i = 0; i < socketList.Count; i++)//遍历集合中的tcp连接
                {
                    Socket clients = (Socket)socketList[i];//将tcp连接转为Socket类型

                    #region 读取当前流中的信息
                    string listInfo = null;//保存当前流中的信息
                    byte[] readWebs = null;//保存当前流中的信息的字节信息
                    int readBytes = 0;
                    try
                    {
                        if (clients == null || !clients.Connected)//如果当前clients为null或断开连接则跳出for循环
                        {
                            break;
                        }
                        readWebs = new byte[10000];
                        readBytes = clients.Receive(readWebs);//读取当前流中的数据
                        if (readBytes == 0)
                        {
                            /*
                                * 长度为0则释放掉连接资源
                                * */
                            client.Close();
                            webClientA.Close();
                            client = null;
                            webClientA = null;
                            return;
                        }
                        listInfo = System.Text.Encoding.GetEncoding("gb2312").GetString(readWebs, 0, readBytes);//以gb2312把字节转换字符串
                        //如果接收的是订票接口，则将book弄为true
                        if (listInfo.IndexOf("POST //api/basic/bookingOrder HTTP/1.1") != -1)
                        {
                            book = true;
                        }
                    }
                    catch (Exception)
                    {
                        return;
                    }
                    #endregion


                    #region 判断当前连接clients是否为客户端连接(判断发往哪个数据库服务)
                    if (clients == client.Client)
                    {
                        webClientA.GetStream().Write(readWebs, 0, readBytes);//发往数据库
                    }
                    #endregion


                    #region 判断当前连接clients是否为服务端A-B连接
                    if (clients == webClientA.Client)//判断clients是否为目标服务连接
                    {
                        if (client != null && client.Connected)
                        {
                            string infoCode = null;//1this.txtCodeValue.Text.ToString() 
                            //string infoCodeH = null;//2
                            string nullInfoCode = "<infoCode />";
                            string nullInfoCodeH = "\"infoCode\":\"\"";
                            string ticketNoF = "<ticketNo>";
                            string ticketNoB = "</ticketNo>";
                            string rowIdF = "<rowId>";
                            string rowIdB = "</rowId>";
                            string colIdF = "<colId>";
                            string colIdB = "</colId>";
                            //string ticketNoH = "ticketNo";
                            //string rowIdH = "rowId";
                            //string colIdH = "colId";
                            int index = listInfo.IndexOf(nullInfoCode);
                            int indexH = listInfo.IndexOf(nullInfoCodeH);
                            try
                            {
                                if (index != -1)//系统1
                                {
                                    string flag = "\r\n\r\n";//通过两空行进行将请求头和请求内容进行拆分
                                    int rnIndex = listInfo.IndexOf(flag);//获取客户端请求信息中两空行的下标
                                    string heads = null;
                                    string content = null;
                                    //如果两空行存在则
                                    if (rnIndex != -1)
                                    {
                                        heads = listInfo.Substring(0, rnIndex + "\r\n\r\n".Length);//设置请求头内容
                                        content = listInfo.Replace(heads, "");//设置请求内容
                                        int j = 0;
                                        int f = 0;
                                        int b = 0;
                                        while (true)
                                        {
                                            f++;
                                            b++;
                                            string replaceContent = null;
                                            int tickNoIndexF = content.IndexOf(ticketNoF, f);
                                            int tickNoIndexB = content.IndexOf(ticketNoB, b);
                                            int rowIdIndexF = content.IndexOf(rowIdF, f);
                                            int rowIdIndexB = content.IndexOf(rowIdB, b);
                                            int colIdIndexF = content.IndexOf(colIdF, f);
                                            int colIdIndexB = content.IndexOf(colIdB, b);
                                            if (tickNoIndexF != -1)
                                            {
                                                replaceContent += content.Substring(tickNoIndexF + ticketNoF.Length, (tickNoIndexB + 1) - tickNoIndexF - ticketNoB.Length);
                                                f = tickNoIndexF;
                                                b = tickNoIndexB;
                                            }
                                            if (rowIdIndexF != -1)
                                            {
                                                replaceContent += "," + content.Substring(rowIdIndexF + rowIdF.Length, (rowIdIndexB + 1) - rowIdIndexF - rowIdB.Length);
                                                f = rowIdIndexF;
                                                b = rowIdIndexB;
                                            }
                                            if (colIdIndexF != -1)
                                            {
                                                replaceContent += "," + content.Substring(colIdIndexF + colIdF.Length, (colIdIndexB + 1) - colIdIndexF - colIdB.Length);
                                                f = colIdIndexF;
                                                b = colIdIndexB;
                                            }
                                            infoCode = "<infoCode>" + replaceContent + "</infoCode>";//1this.txtCodeValue.Text.ToString()
                                            int findIndex = content.IndexOf(nullInfoCode, j);//从0开始找【的下标
                                            if (findIndex != -1)
                                            {
                                                content = content.Remove(findIndex, nullInfoCode.Length).Insert(findIndex, infoCode);
                                                j = findIndex + 1;
                                            }
                                            else
                                            {
                                                break;
                                            }
                                        }
                                        if (heads.IndexOf("Content-Length") != -1)
                                        {
                                            string repl = heads.Substring(heads.IndexOf("Content-Length"), heads.Length - heads.IndexOf("Content-Length"));
                                            heads = heads.Replace(repl, "Content-Length: " + content.Length);
                                        }
                                    }
                                    listInfo = heads + "\r\n\r\n" + content;
                                    byte[] buff = Encoding.GetEncoding("gb2312").GetBytes(listInfo);//对响应信息进行编码操作
                                    client.GetStream().Write(buff, 0, buff.Length);
                                }
                                else if (indexH != -1)//系统2
                                {
                                    if (book == false)
                                    {
                                        client.GetStream().Write(readWebs, 0, readBytes);
                                    }
                                    else if (book == true)
                                    {
                                        string flag = "\r\n\r\n";//通过两空行进行将请求头和请求内容进行拆分
                                        int rnIndex = listInfo.IndexOf(flag);//获取客户端请求信息中两空行的下标
                                        string heads = null;
                                        string content = null;
                                        //如果两空行存在则
                                        if (rnIndex != -1)
                                        {
                                            heads = listInfo.Substring(0, rnIndex + "\r\n\r\n".Length);//设置请求头内容
                                            content= listInfo.Replace(heads, "");//设置请求内容
                                            HttpContent httpContent = new StringContent(content, Encoding.GetEncoding("GB2312"), "text/plain");
                                            var httpClient = new HttpClient();
                                            string uri = "http://" + DataSourceIpOrPortB + "/api/request/getinfocode";
                                            var str = httpClient.PostAsync(uri, httpContent).Result.Content.ReadAsStringAsync().Result;//该段代码：发往9100  str是响应回来的信息
                                            listInfo = heads + str;
                                        }
                                        byte[] buff = Encoding.GetEncoding("gb2312").GetBytes(listInfo);//对响应信息进行编码操作
                                        client.GetStream().Write(buff, 0, buff.Length);
                                        book = false;
                                    }
                                }
                                else
                                {
                                    client.GetStream().Write(readWebs, 0, readBytes);
                                }
                            }
                            catch (Exception)
                            {
                                return;
                            }
                        }
                        else
                        {
                            client.Close();
                            client = null;
                        }
                    }
                    #endregion
                }
                #endregion
                
            }
            #endregion

            #endregion

        }
        #endregion


        #region 停止代理服务器的方法
        public void stop()
        {
            if (listener != null)
            {
                listener.Stop();
                listener = null;
            }
            if (ziThread != null)
            {
                ziThread.Abort();
                ziThread = null;
            }
        }
        #endregion
    }
}





