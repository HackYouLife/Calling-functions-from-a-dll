using System;
using System.Reflection;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Diagnostics;
using System.IO.Ports;

namespace GetFun
{

    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        bool stat = false ; 
        ////////////////Загружаем  именно тот драйвер , который нужен пользователю ///////////////////////
        [DllImport("kernel32")]
        public static extern IntPtr CreateRemoteThread(
          IntPtr hProcess,
          IntPtr lpThreadAttributes,
          uint dwStackSize,
          UIntPtr lpStartAddress, 
          IntPtr lpParameter,
          uint dwCreationFlags,
          out IntPtr lpThreadId
        );

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(
            UInt32 dwDesiredAccess,
            Int32 bInheritHandle,
            Int32 dwProcessId
            );

        [DllImport("kernel32.dll")]
        public static extern Int32 CloseHandle(
        IntPtr hObject
        );

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern bool VirtualFreeEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            UIntPtr dwSize,
            uint dwFreeType
            );

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true)]
        public static extern UIntPtr GetProcAddress(
            IntPtr hModule,
            string procName
            );

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern IntPtr VirtualAllocEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            uint dwSize,
            uint flAllocationType,
            uint flProtect
            );

        [DllImport("kernel32.dll")]
        static extern bool WriteProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            string lpBuffer,
            UIntPtr nSize,
            out IntPtr lpNumberOfBytesWritten
        );

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(
            string lpModuleName
            );

        [DllImport("kernel32", SetLastError = true, ExactSpelling = true)]
        internal static extern Int32 WaitForSingleObject(
            IntPtr handle,
            Int32 milliseconds
            );

        public Int32 GetProcessId(String proc)
        {
            Process[] ProcList;
            ProcList = Process.GetProcessesByName(proc);
            return ProcList[0].Id;
        }

        public void InjectDLL(IntPtr hProcess, String strDLLName)
        {
            IntPtr bytesout;

            
            Int32 LenWrite = strDLLName.Length + 1;
            
            IntPtr AllocMem = (IntPtr)VirtualAllocEx(hProcess, (IntPtr)null, (uint)LenWrite, 0x1000, 0x40); //allocation pour WriteProcessMemory

            
            WriteProcessMemory(hProcess, AllocMem, strDLLName, (UIntPtr)LenWrite, out bytesout);
          
            UIntPtr Injector = (UIntPtr)GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");

            if (Injector == null)
            {
              
                return;
            }

            
            IntPtr hThread = (IntPtr)CreateRemoteThread(hProcess, (IntPtr)null, 0, Injector, AllocMem, 0, out bytesout);
            
            if (hThread == null)
            {
               
                return;
            }
           
            int Result = WaitForSingleObject(hThread, 10 * 1000);
          
            if (Result == 0x00000080L || Result == 0x00000102L || Result == 0xFFFFFFFF)
            {
               
             
                if (hThread != null)
                {
                    
                    CloseHandle(hThread);
                }
                return;
            }
            
            Thread.Sleep(1000);
            
            VirtualFreeEx(hProcess, AllocMem, (UIntPtr)0, 0x8000);
            
            if (hThread != null)
            {
                
                CloseHandle(hThread);
            }
           
            stat = true; 
            return;
        }

        ///////////////////////////////////////////
        //Класс для вызова функций из dll 
       internal   class clDll
        {
            [DllImport("test.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
            public static extern int DeletePluginObject(int id);
            [DllImport("test.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
            public static extern int CreatePluginObject();
             [DllImport("test.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
            public static extern bool  GetPluginInfo(ref plugininfo pluginInf, ref PLUGIN_RESULT result);
                       [DllImport("test.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
             public static extern bool PluginUnload();

             [DllImport("test.dll")]
             public static extern bool PluginGetChildList(int id, ref ChildDeviceInfo pList, int nBufferCount, int pnReturnCount);
            [DllImport("test.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
             public static extern bool PluginObjectHandler(int id, ref  DEVICE_PLUGIN_CODE code, ref  PluginParameter parameter, ref PLUGIN_RESULT result);
           
        
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            MessageBox.Show("Please select the appropriate driver from the list and click load driver");
            string[] ports = SerialPort.GetPortNames();
            //Очистка содержимого бокса
            comboBox2.Items.Clear();
            //Добавление найденных портов в бокс
            comboBox2.Items.AddRange(ports);
            this.textBox1.ForeColor = System.Drawing.Color.Green;
            textBox1.Visible = false;
            button2.Visible = false;
            button3.Visible = false;
            button4.Visible = false; 
            string patch = Environment.CurrentDirectory;
            string[] files = Directory.GetFiles(patch, "*.dll");
            for (int i = 0; files.Length > i; i++)
            {
                comboBox1.Items.Add(Path.GetFileNameWithoutExtension(files[i]));

            }

        }
        
        private void button1_Click(object sender, EventArgs e)
        {

            string dllnams =" ";
            string infdriver = "";
            if (comboBox1.SelectedIndex > -1)
            {
                if (comboBox1.SelectedItem.ToString() == "test")
                {
                    infdriver = "test"; 
                    dllnams = "test.dll";
                }
                else if (comboBox1.SelectedItem.ToString() == "Тут будут другие модули")
                {
                    dllnams = "Так же название модуля";
                }


                //   Thread.Sleep(1000);
                String strDLLName = dllnams;
                String strProcessName = "GetFun";
                Int32 ProcID = GetProcessId(strProcessName);
                if (ProcID >= 0)
                {
                    IntPtr hProcess = (IntPtr)OpenProcess(0x1F0FFF, 1, ProcID);
                    if (hProcess == null)
                    {
                        return;
                    }
                    else
                    {
                        InjectDLL(hProcess, strDLLName);
                    }
                }
                if (stat == true)
                {

                    label2.Text = string.Format("The driver " + infdriver + " was loaded successfully!!!!"); 
                    textBox1.Visible = true;
                    button2.Visible = true;
                    button3.Visible = true;
                    button4.Visible = true; 
                }
                else
                {
                    label2.Text = "Load driver error";
                }
            }
            else 
            {
                label2.Text = "Please select a driver"; 

            }
          
        }
        int nBufferCount;
        int pnReturnCount;
        int id; 
        private void button2_Click(object sender, EventArgs e)
        {
      
            var data = new plugininfo();
            var res = PLUGIN_RESULT.PLUGIN_RESULT_ERROR_INVALID_PARAMETER;
            data.dwSize = Convert.ToUInt32(Marshal.SizeOf(data));

            clDll.GetPluginInfo(ref data, ref res);
            if (res == PLUGIN_RESULT.PLUGIN_RESULT_OK)
            {
                textBox1.Text = ""; 
                textBox1.Text += "Plagin name: " + Marshal.PtrToStringAnsi(data.szPluginName) + '\r' + '\n';
        textBox1.Text += "Plagin type: "+Marshal.PtrToStringAnsi(data.szPluginType) + '\r' + '\n';
        textBox1.Text += "Plagin version:"+Marshal.PtrToStringAnsi(data.szVersion) + '\r' + '\n';

            }
            else
            {
                textBox1.Text = "Function call error ";
            }
        }
       
     
       ///////////////////////// //Сруктуры/////////////////////////////////////////
       
            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            internal unsafe struct plugininfo
            {

                internal uint dwSize;

                internal IntPtr szPluginType;
                internal IntPtr szPluginName;
                internal IntPtr szPluginDescription;
                internal IntPtr szVersion;
                [MarshalAs(UnmanagedType.ByValArray, SizeConst =64)]
                internal byte[] bReserved; 
              
            }


         [StructLayout(LayoutKind.Sequential, Pack = 1)]
            internal unsafe struct ChildDeviceInfo
            {

             int hDevice;
	         int nFlags;
             IntPtr szClass;
             IntPtr szName;
             [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
             internal byte[] reserved;

            }


         [StructLayout(LayoutKind.Sequential, Pack = 1)]
         internal unsafe struct PluginParameter
         {
             
             internal uint dwSize;
             internal IntPtr* pInBuffer;
             internal int nInBufferSize;
             internal IntPtr* pOutBuffer;
             internal int nOutBufferSize;
             internal uint dwReturnBytes;
             internal uint dwResultExt;

         }





            internal enum PLUGIN_RESULT{
         PLUGIN_RESULT_OK = 0,
	     PLUGIN_RESULT_ERROR_EXTENDED ,
	     PLUGIN_RESULT_ERROR_INVALID_PARAMETER , 
         PLUGIN_RESULT_ERROR_CODE_NOT_SUPPORT,
         PLUGIN_RESULT_ERROR_MESSAGE_NOT_FOUND ,
	    PLUGIN_RESULT_MAX ,
            } ;


            internal enum DEVICE_PLUGIN_CODE
            {
                DEVICE_PLUGIN_CODE_GET_PARAM = 0,
                DEVICE_PLUGIN_CODE_SET_PARAM,
                DEVICE_PLUGIN_CODE_OPEN,
                DEVICE_PLUGIN_CODE_CLOSE,
                DEVICE_PLUGIN_CODE_SET_EVENT,
                DEVICE_PLUGIN_CODE_READ,
                DEVICE_PLUGIN_CODE_WRITE,
                DEVICE_PLUGIN_CODE_IOCONTROL,
                DEVICE_PLUGIN_CODE_GET_PROTOCOL,
                DEVICE_PLUGIN_CODE_SET_PROTOCOL,
                DEVICE_PLUGIN_CODE_GET_PROTOCOL_LIST,
                DEVICE_PLUGIN_CODE_GET_EXT_ERROR_MESSAGE,
                DEVICE_PLUGIN_CODE_GET_VIEW,
                DEVICE_PLUGIN_CODE_RELEASE_VIEW,

            };


        /// ///////////////////////////////////////////////////////////////////////////////
       

        public bool isInjected { get; set; }
       
        private void button3_Click(object sender, EventArgs e)
        {
             var ff = DEVICE_PLUGIN_CODE.DEVICE_PLUGIN_CODE_GET_PROTOCOL; 
            var rr = new PluginParameter() ;
            var tt = PLUGIN_RESULT.PLUGIN_RESULT_ERROR_CODE_NOT_SUPPORT ;
             id = clDll.CreatePluginObject();
            rr.dwSize = Convert.ToUInt32(Marshal.SizeOf(rr));
           textBox1.Text = "";
            textBox1.Text += "id: "+id.ToString() + '\r' + '\n';
            clDll.PluginObjectHandler(id, ref ff,ref  rr,ref  tt);
            textBox1.Text += tt.ToString() + '\r' + '\n';
        }
        int ew;
        IntPtr re;
        IntPtr ss;
        private void button4_Click(object sender, EventArgs e)
        {
            clDll.DeletePluginObject(id);
            textBox1.Text = "";
            textBox1.Text += "id: " + id.ToString() + '\r' + '\n';
            
           
        }
        /// <summary>
        /// Работа с com portam
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

       
        SerialPort _serialPort;
        private void button5_Click(object sender, EventArgs e)
        {
            if (comboBox2.SelectedIndex > -1)
            {
                string selectit = comboBox2.SelectedItem.ToString();
                _serialPort = new SerialPort(selectit,
                                              4800,
                                              Parity.None,
                                              8,
                                              StopBits.One);
                _serialPort.Handshake = Handshake.None;
                _serialPort.DtrEnable = true;

              
                _serialPort.Open();
                if(_serialPort.IsOpen)
                {
                    MessageBox.Show("Port Open");
                }

            
            }
            else
            {
                MessageBox.Show("Please select a COM port");
            }

         
        }


        ////////////////////////////////////////////////////////////////////

        private void button6_Click(object sender, EventArgs e)
        {
            if (_serialPort.IsOpen)
            {
               
                Thread.Sleep(500);
                _serialPort.Close();
                MessageBox.Show("the port was closed"); 
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
               byte[] send = new byte[] {0x7F,0x02,};
            
                _serialPort.Write(send, 0, send.Length);
               // string E = _serialPort.ReadLine();
               // textBox1.Text += E; 
        }
        //////////////////////////////////////////////////////////////////////////
    }
}