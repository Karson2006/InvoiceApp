using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace InvoiceApp
{
    internal static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        private static void Main()
        {
            try
            {


                if (IsApplicationAlreadyRunning() == true)
                {
                    MessageBox.Show($"发票自动查验程序已经在运行!");
                }
                else
                {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new InvoiceForm());
                }
            }
            catch (Exception ex)
            {

                string str = "";
                string strDateInfo = "出现应用程序未处理的异常：" + DateTime.Now.ToString() + "\r\n";

                if (ex != null)
                {
                    str = string.Format(strDateInfo + "异常类型：{0}\r\n异常消息：{1}\r\n异常信息：{2}\r\n",
                    ex.GetType().Name, ex.Message, ex.StackTrace);
                }
                else
                {
                    str = string.Format("应用程序线程错误:{0}", ex);
                }
                WriteTxtLog(str);
                MessageBox.Show(str);
                //MessageBox.Show(str, "系统错误", MessageBoxButtons.OK, MessageBoxIcon.Error);

            }
        }
        static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            string str = "";
            string strDateInfo = "出现应用程序未处理的异常：" + DateTime.Now.ToString() + "\r\n";
            Exception error = e.Exception as Exception;
            if (error != null)
            {
                str = string.Format(strDateInfo + "异常类型：{0}\r\n异常消息：{1}\r\n异常信息：{2}\r\n",
                error.GetType().Name, error.Message, error.StackTrace);
            }
            else
            {
                str = string.Format("应用程序线程错误:{0}", e);
            }
            WriteTxtLog(str);
            MessageBox.Show(str);
            //MessageBox.Show(str, "系统错误", MessageBoxButtons.OK, MessageBoxIcon.Error);

        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            string str = "";
            Exception error = e.ExceptionObject as Exception;
            string strDateInfo = "出现应用程序未处理的异常：" + DateTime.Now.ToString() + "\r\n";
            if (error != null)
            {
                str = string.Format(strDateInfo + "Application UnhandledException:{0};\n\r堆栈信息:{1}", error.Message, error.StackTrace);
            }
            else
            {
                str = string.Format("Application UnhandledError:{0}", e);
            }
            WriteTxtLog(str);
            MessageBox.Show(str);
            //MessageBox.Show(str, "系统错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        public static void WriteTxtLog(string str)
        {
            string path = System.Windows.Forms.Application.StartupPath; ;
            if (!File.Exists(path+"\\"+DateTime.Now.ToString("yyyy-MM-dd HH:mm")))
            {
                FileStream fs = new FileStream(path, FileMode.Create, FileAccess.ReadWrite);

                StreamWriter sw = new StreamWriter(fs);
                sw.Write(str);
                sw.Flush();
                sw.Close();
            }
            else
            {
                FileStream fs = new FileStream(path, FileMode.Append);
                //文本写入
                StreamWriter sw = new StreamWriter(fs);
                sw.Write(str);
                sw.Flush();
                sw.Close();
            }
        }
        private static bool IsApplicationAlreadyRunning()
        {
            string proc = Process.GetCurrentProcess().ProcessName;
            Process[] processes = Process.GetProcessesByName(proc);
            if (processes.Length > 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}