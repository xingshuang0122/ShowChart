using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net.Sockets;

namespace ShowChart
{
    public partial class Form1 : Form
    {
        private delegate void ProcessDelegate();
        #region 成员变量

        private Queue<double> dblQueueAngle;
        private Queue<string> strQueueAngle;
        private Random randomAngle;

        private Queue<string> strQueueForce;
        private Queue<double> intQueueForce;
        private Random randomForce;
        private double dblNewDataAngle;
        private double dblNewDataForce;

        private CSocket ioCSocket;

        private byte[] buffer;
        ProcessDelegate prssDlgt;

        #endregion

        /// <summary>
        /// 初始化变量
        /// </summary>
        private void InitVariable()
        {
            dblQueueAngle = new Queue<double>();
            strQueueAngle = new Queue<string>();
            randomAngle = new Random();

            intQueueForce = new Queue<double>();
            strQueueForce = new Queue<string>();
            randomForce = new Random();
            dblNewDataAngle = 0.0;
            dblNewDataForce = 0.0;

            ioCSocket = new CSocket();

            buffer = new byte[41];                              //这里使用41的目的：由于服务端的数据buffer是40，字符串要额外加一个'\0';
            prssDlgt = new ProcessDelegate(HandleWithData);     //委托的声明
        }

        /// <summary>
        /// 初始化图表
        /// </summary>
        private void InitChart()
        {
            string strTime = DateTime.Now.Minute.ToString() + ":" + DateTime.Now.Second.ToString();
            int intRandom = 0;

            for (int i = 0; i < 25; i++)
            {
                dblQueueAngle.Enqueue(intRandom);
                strQueueAngle.Enqueue(strTime);

                intQueueForce.Enqueue(intRandom);
                strQueueForce.Enqueue(strTime);
            }

            this.chartAngle.Series[0].Points.DataBindXY(strQueueAngle, dblQueueAngle);
            this.chartForce.Series[0].Points.DataBindXY(strQueueForce, intQueueForce);
        }

        public Form1()
        {
            InitializeComponent();
            InitVariable();
            InitChart();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            tmrUpdate.Enabled = true;
        }

        private void tmrUpdate_Tick(object sender, EventArgs e)
        {
            strQueueAngle.Enqueue(DateTime.Now.Minute.ToString() + ":" + DateTime.Now.Second.ToString());   //字符串型时间入队
            dblQueueAngle.Enqueue(randomAngle.Next(-100, 100));
            //dblQueueAngle.Enqueue(dblNewDataAngle);     //double型角度值入队
            strQueueAngle.Dequeue();                    //出队
            dblQueueAngle.Dequeue();                    //出队
            this.chartAngle.Series[0].Points.DataBindXY(strQueueAngle, dblQueueAngle);

            strQueueForce.Enqueue(DateTime.Now.Minute.ToString() + ":" + DateTime.Now.Second.ToString());   //字符串型时间入队
            intQueueForce.Enqueue(randomForce.Next(-20, 40));
            //intQueueForce.Enqueue(dblNewDataForce);     //int型力值入队
            strQueueForce.Dequeue();                    //出队
            intQueueForce.Dequeue();                    //出队
            this.chartForce.Series[0].Points.DataBindXY(strQueueForce, intQueueForce);

            //label1.Text = ioCSocket.ReceiveData();
        }

        /// <summary>
        /// 连接服务器
        /// </summary>
        private void ConnectServer()
        {
            int intFlag = ioCSocket.ConnectServer();        //连接服务器
            if (intFlag == 1)
            {
                MessageBox.Show("连接失败！");
            }
            else
            {
                MessageBox.Show("连接成功！");
                //启动异步接收
                ioCSocket.IoSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), ioCSocket.IoSocket);
            }
        }

        /// <summary>
        /// 异步接收数据的响应函数
        /// </summary>
        /// <param name="result">连接服务器的Socket</param>
        private void ReceiveCallback(IAsyncResult result)
        {

            Socket ts = (Socket)result.AsyncState;      //这里的Socket是客户端的Socket

            //对Socket连接状态进行判断，如果断开则关闭Socket
            if (ts.Connected == false)
            {
                ts.Close();
                //MessageBox.Show("connect");
                return;
            }

            //结束当前的数据接收线程，并关闭线程资源
            try
            {
                ts.EndReceive(result);
                result.AsyncWaitHandle.Close();
            }
            catch
            {
                MessageBox.Show("endreceive");
                ts.Close();
                return;
            }

            //Console.WriteLine("收到消息：{0}", Encoding.ASCII.GetString(buffer));
            this.Invoke(prssDlgt);          //异步接收数据之后进行处理，由于异步接收是另一个线程，因此采用委托和Invoke来调用主线程的函数

            //清空数据，重新开始异步接收  
            //buffer = new byte[buffer.Length];
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i] == 0)
                {
                    break;
                }
                else
                {
                    buffer[i] = 0;
                }
            }

            //对Socket连接状态进行判断，如果断开则关闭Socket
            if (ts.Connected == false)
            {
                ts.Close();
                //MessageBox.Show("connect");
                return;
            }

            //开启另一个新的接收数据线程
            try
            {
                ts.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), ts);
            }
            catch
            {
                ts.Close();
                MessageBox.Show("错误begin！");
            }

        }

        /// <summary>
        /// 处理数据
        /// </summary>
        private void HandleWithData()
        {
            string strBuffer = System.Text.Encoding.ASCII.GetString(buffer);        //接收数据的buffer，将字节型数据转换成字符型
            
            string[] strSplit = strBuffer.Split(new Char[] { '\0',';' }, StringSplitOptions.RemoveEmptyEntries);    //对接收数据进行拆分

            label1.Text = strSplit[0];
            string strAngle = Convert.ToDouble(strSplit[0]).ToString("f1");         //这个存放接收的角度，并保留1位有效数值
            string strForce = Convert.ToDouble(strSplit[1]).ToString("f1");         //这个存放接收的力，并保留1位有效数值

            try
            {
                dblNewDataAngle = Convert.ToDouble(strAngle);                       //将字符串类型转换为double类型
            }
            catch
            {
                dblNewDataAngle = 0.0;
            }

            try
            {
                dblNewDataForce = Convert.ToDouble(strForce);                       //将字符串类型转换为double类型
            }
            catch
            {
                dblNewDataForce = 0.0;
            }
            
        }

        /// <summary>
        /// 连接服务器
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnConnect_Click(object sender, EventArgs e)
        {
            ConnectServer();
            btnClose.Enabled = true;
            btnConnect.Enabled = false;
        }

        /// <summary>
        /// 关闭服务器
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnClose_Click(object sender, EventArgs e)
        {
            ioCSocket.CloseSocket();
            btnConnect.Enabled = true;
            btnClose.Enabled = false;
            btnStart.Enabled = true;
            btnStop.Enabled = false;
        }

        /// <summary>
        /// 启动服务器的数据发送
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnStart_Click(object sender, EventArgs e)
        {
            ioCSocket.SendData("1");
            btnStart.Enabled = false;
            btnStop.Enabled = true;
        }

        /// <summary>
        /// 关闭服务器的数据发送
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnStop_Click(object sender, EventArgs e)
        {
            ioCSocket.SendData("2");
            btnStart.Enabled = true;
            btnStop.Enabled = false;
        }
    }
}
