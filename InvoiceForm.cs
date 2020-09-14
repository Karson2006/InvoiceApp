using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using System.Net;
using System.IO;
using System.Threading;
using iTR.Lib;
using System.Xml;
using iTR.OP.Invoice;
using Invoice.Utils;

namespace InvoiceApp
{
    public partial class InvoiceForm : Form
    {
        ThreadStart start;
        Thread thd;
        int interval = 5;
        public InvoiceForm()
        {
            try
            {
                InitializeComponent();
                start = new ThreadStart(ThreadAction);
                thd = new Thread(start);
                XmlDocument doc = new XmlDocument();
                doc.Load("cfg.xml");
                XmlNode node = doc.SelectSingleNode("Configuration/Interval");
                if(node!=null|| node.InnerText.Trim().Length >0 )
                {
                    interval = int.Parse(node.InnerText.Trim());
                }

            }
            catch (Exception err)
            {
                FileLogger.WriteLog(err.Message, 1,"InvoiceForm","Structure","DataService","ErrMeassag" );
            }

        }



        private void btStart_Clik(object sender, EventArgs e)
        {
           
            thd.IsBackground = true;
            thd.Start();
            btStart.Enabled = false;
        }

        public void ThreadAction()
        {
            while (true)
            {
                try
                {
                    OAInvoiceHelper oa = new OAInvoiceHelper();
                    string result = oa.Run();
                    if (int.Parse(result) > 0)
                    {
                        FileLogger.WriteLog( "成功处理", 1, "InvoiceForm", "ThreadAction", "DataService", "AppMessage");
                    }


                    System.Threading.Thread.Sleep(interval * 60*1000);
                }
                catch (Exception err)
                {
                    FileLogger.WriteLog(err.Message, 1, "InvoiceForm", "ThreadAction", "DataService", "ErrMessage");
                    System.Threading.Thread.Sleep(interval * 60 * 1000);
                }
            }
        }

        private void btExit_Click(object sender, EventArgs e)
        {
            thd.Abort();
            this.Dispose(); 
        }

       

        private void InvoiceForm_StyleChanged(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.notifyIcon1.Visible = true;
                this.Hide();
            }
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (this.WindowState == FormWindowState.Normal)
            {
                this.WindowState = FormWindowState.Minimized;
                this.Hide();
            }
            else if (this.WindowState == FormWindowState.Minimized)
            {
                this.Show();
                this.WindowState = FormWindowState.Normal;
                this.Activate();
            }
        }

      

        private void btDebug_Click(object sender, EventArgs e)
        {
            try
            {
                if (txFileName.Text.Trim().Length == 0)
                {
                    MessageBox.Show("单号不能为空", "系统提示");
                }
                else
                {
                    iTR.OP.Invoice.OAInvoiceHelper invoice = new OAInvoiceHelper();
                    invoice.Run(1, txFileName.Text.Trim());
                }
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message, "系统提示");
            }
        }
    }

}
