using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using WebAgent;

namespace KIOSKWindowControl.Service.Ticket
{
    public class GetBookingIDService
    {
        public void GetBookingID(String reqHeader,String reqContent,TcpClient client) 
        {
            #region 与数据库A-B建立连接
            try
            {
                string socketSetr = WebService.ContentValue("DataSourceA", "DataServiceA", WebService.WASConfigPath);//读取ini文件内容中需要转发的数据源的IP和端口
                String ip = socketSetr.Split(':').FirstOrDefault(); //获取IP
                int port = Convert.ToInt32(socketSetr.Split(':').LastOrDefault()); //获取端口
                using (TcpClient tcpClient = new TcpClient(ip, port)) //创建tcpClient并连接
                {
                    NetworkStream networkStream = tcpClient.GetStream();//取得web服务器的流
                    byte[] bytes = Encoding.GetEncoding("gb2312").GetBytes(reqHeader + reqContent);//对请求信息进行编码操作
                    networkStream.Write(bytes, 0, bytes.Length);//将编码之后的请求信息发往web服务器
                    StringBuilder sb = new StringBuilder(); //用来拼接字符串
                    byte[] readBytes = new byte[5000];
                    tcpClient.ReceiveTimeout = 5000; //设置接收超时时间
                    while(true)
                    {
                        int count = 0;
                        try
                        {
                            count = tcpClient.Client.Receive(readBytes);
                        }
                        catch (Exception) { } //捕捉接收超时异常
                        String str = System.Text.Encoding.GetEncoding("gb2312").GetString(readBytes, 0, count);//以gb2312把字节转换字符串
                        sb.Append(str);
                        if (count != readBytes.Length) //如果不等于字节长度,说明是最后一段数据，跳出此循环
                        {
                            break;
                        }
                    }
                    bytes=Encoding.GetEncoding("gb2312").GetBytes(sb.ToString());
                    client.GetStream().Write(bytes, 0, bytes.Length);
                    client.Close();
                }
            }
            catch (Exception)
            {
                return;
            }
            #endregion
        }
    }
}
