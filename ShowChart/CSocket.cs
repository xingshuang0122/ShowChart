using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace ShowChart
{
    class CSocket
    {
        private int mPort;                          //端口号
        private IPAddress mIP;                      //ip地址
        private IPEndPoint mIPE;                    //IP地址和端口号的组合
        private Socket ioSocket;                   //socket变量

        public Socket IoSocket
        {
            get { return ioSocket; }
            set { ioSocket = value; }
        }

        public CSocket()
        {
            mPort = 12001;
            mIP = IPAddress.Parse("192.168.0.2");
            mIPE = new IPEndPoint(mIP, mPort);
                    
        }



        /// <summary>
        /// 连接服务器
        /// </summary>
        /// <returns> 返回连接的状态，0表示连接成功，1表示连接失败 </returns>
        public int ConnectServer()
        {
            ioSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);//创建Socket 
            try
            {
                ioSocket.Connect(mIPE);                
                return 0;  
            }
            catch
            {
                ioSocket.Close();
                return 1;
            }                     
        }

        /// <summary>
        /// 发送命令到服务器
        /// </summary>
        /// <param name="strSend">发送的命令</param>
        /// <returns>返回发送状态，0表示发送成功，1表示发送失败</returns>
        public int SendData(string strSend)
        {
            try
            {
                byte[] bs = Encoding.UTF8.GetBytes(strSend);                               //把字符串编码为字节
                ioSocket.Send(bs, bs.Length, 0);
                return 0;
            }
            catch
            {
                return 1;
            }
        }

        /// <summary>
        /// 接收服务器发送的数据
        /// </summary>
        /// <returns>接收的内容</returns>
        public string ReceiveData()
        {
            byte[] recvBytes = new byte[100];                //接收的字节
            int intByte=0;
            string strReceive = string.Empty;

            try
            {
                intByte = ioSocket.Receive(recvBytes, recvBytes.Length, 0);                         //从服务器端接受返回信息
                strReceive = Encoding.UTF8.GetString(recvBytes, 0, intByte);                        //把字节编码为字符串  
            }
            catch
            {
            	
            }            

            return strReceive;
        }

        /// <summary>
        /// 关闭socket
        /// </summary>
        public void CloseSocket()
        {
            //ioSocket.Shutdown(SocketShutdown.Both);
            ioSocket.Close();
        }

    }
}
