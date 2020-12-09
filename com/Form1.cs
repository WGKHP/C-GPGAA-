using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using System.Management;

namespace com
{
    public partial class Form1 : Form
    {


        private bool IsOver;

        double b = 0.0;
        long X;
        long Y;
        int Cnt;
        public Form1()
        {
            InitializeComponent();
            FormClosing += MainForm_FormClosing;

            IsOver = false;
            serialPort1.PortName = GetKey("com.exe.config", "PortName");
            serialPort2.PortName = GetKey("com.exe.config", "SendPortName");

            
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            Thread t = new Thread(new ThreadStart(Read));
            t.Start();
        }
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            IsOver = true;
            Environment.Exit(0);
        }


        delegate void SETStringDelegate(TextBox box, string str);


        private void TextBox_setString(TextBox box, string str)
        {
            if (box.InvokeRequired)
            {
                SETStringDelegate d = new SETStringDelegate(TextBox_setString);
                try
                {
                    box.Invoke(d, box, str);
                }
                catch (Exception)
                {
                }
            }
            else
            {
                box.Text = str;
            }
        }
        public void Read()
        {

            //在线了要干啥？自动打开串口接收数据 发送数据

            // 接收串口不一开始就不在线，接收串口在线又掉线又上线 ，上线后一直掉线 发送串口同样如此

        
            //直到又检测到串口在线了进行下一步工作如此循环


            while (!IsOver)
            {
                //检测串口是否在线，循环检测 
                AutoCheckSerialPortIsOnline();

                string vReadStr;
                try
                {
                    vReadStr = serialPort1.ReadLine();
                  
                }
                catch (Exception)
                {

                    serialPort1.Close();//关闭串口
                    serialPort1.Dispose();
                    continue;
                }
                //按“；”分割字符含义
                var NavDatas = vReadStr.Split(',');

                //解析数据 X Y A 
                try
                {
                    if (NavDatas[0].Equals("#HEADING2A"))
                    {
                        /* 判断是否取得固定解 */
                        if (NavDatas[10].Equals("NARROW_INT"))
                        {
                            /* 航向角 */
                            double a = Double.Parse(NavDatas[12]);
                            b = a;
                            TextBox_setString(textBox_a, a.ToString());
                        }
                    }
                    else if (NavDatas[0].Equals("$GPGGA"))
                    {
                        /* 判断是否取得RTK固定解,达到标称精度 */
                        if (NavDatas[6].Equals("4"))
                        {
                            /* 经度 */
                            nx_dd = (int)double.Parse(NavDatas[4]) / 100;
                            nx_mm = double.Parse(NavDatas[4]) - nx_dd * 100;
                            nx = nx_dd + nx_mm / 60;
                            /* 纬度 */
                            ny_dd = (int)double.Parse(NavDatas[2]) / 100;
                            ny_mm = double.Parse(NavDatas[2]) - ny_dd * 100;
                            ny = ny_dd + ny_mm / 60;
                            /* 卫星数 */
                            double cnt = Double.Parse(NavDatas[7]);
                            Cnt = (int)cnt;
                            /* 显示当前数据 */
                            TextBox_setString(textBox_cnt, cnt.ToString());
                            TextBox_setString(textBox_NX, nx.ToString());
                            TextBox_setString(textBox_NY, ny.ToString());
                            TextBox_setString(textBox_OX, ox.ToString());
                            TextBox_setString(textBox_OY, oy.ToString());

                            /* 计算相对坐标 */
                            long x = (long)(R * Math.Cos(oy * Math.PI / 180) * Math.PI / 180 * (nx - ox));
                            long y = (long)(R * Math.PI / 180 * (ny - oy));
                            TextBox_setString(textBox_x, x.ToString());
                            TextBox_setString(textBox_y, y.ToString());

                            X = x;
                            Y = y;
                        }
                    }

                    /* 发送数据给其他串口设备*/

                    try
                    {
                        string senddata= X.ToString() + "," + Y.ToString() + "," + ((int)b).ToString() + "," + Cnt.ToString() + "\n";
                        if (senddata.Length>10) {
                            serialPort2.Write(senddata);
                        }
                        
                    }
                    catch (Exception)
                    {
                        serialPort2.Close();
                        serialPort2.Dispose();
                        continue;
                    }
                }
                catch (Exception)
                { 
                    continue;
                }
            }//while 循环
        }

        /// <summary>
        /// 自动检测串口是否在线 这里收发端口都一样
        /// </summary>
    public void AutoCheckSerialPortIsOnline()
        {
            bool IsConnected = false;
            
            while (!IsConnected)
            {
                string[] ports = SerialPort.GetPortNames();
                if (ports != null&ports.Contains(serialPort1.PortName) ) {
                    configSerialPort(ports);
                    IsConnected=true;
                }
                Thread.Sleep(100);
            }

        }

        /// <summary>
        /// 组态收发串口
        /// </summary>
        public void configSerialPort(string[] PortsNames  ) {

            if (PortsNames.Contains(serialPort1.PortName) & PortsNames.Contains(serialPort2.PortName))//接收串口和发送都有
            {
                serialPort1.BaudRate = int.Parse(GetKey("com.exe.config", "BaudRate"));
                serialPort1.Encoding = Encoding.UTF8;
                if (!serialPort1.IsOpen) { serialPort1.Open(); }
               
                serialPort2.BaudRate = int.Parse(GetKey("com.exe.config", "SendBaudRate"));
                serialPort2.Encoding = Encoding.UTF8;
                if (!serialPort2.IsOpen) { serialPort2.Open(); }
                
            }
            else if (PortsNames.Contains(serialPort1.PortName)) //只有接收串口
            {
                serialPort1.BaudRate = int.Parse(GetKey("com.exe.config", "BaudRate"));
                serialPort1.Encoding = Encoding.UTF8;
                if (!serialPort1.IsOpen) { serialPort1.Open(); }
            }
            

        }



        private void label8_Click(object sender, EventArgs e)
        {

        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private void label7_Click(object sender, EventArgs e)
        {

        }

        private string GetKey(string configPath, string key)
        {
            Configuration ConfigurationInstance = ConfigurationManager.OpenMappedExeConfiguration(new ExeConfigurationFileMap()
            {
                ExeConfigFilename = configPath
            }, ConfigurationUserLevel.None);


            if (ConfigurationInstance.AppSettings.Settings[key] != null)
                return ConfigurationInstance.AppSettings.Settings[key].Value;
            else

                return string.Empty;
        }

        private void label9_Click(object sender, EventArgs e)
        {

        }

        private void label11_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            ox = nx;
            oy = ny;
        }

       
    }
}
