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
        private ThreadStart start;
        private Thread thd;
        private int interval = 5;

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
                if (node != null || node.InnerText.Trim().Length > 0)
                {
                    interval = int.Parse(node.InnerText.Trim());
                }
            }
            catch (Exception err)
            {
                FileLogger.WriteLog(err.Message, 1, "InvoiceForm", "Structure", "DataService", "ErrMeassag");
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
                        FileLogger.WriteLog("成功处理", 1, "InvoiceForm", "ThreadAction", "DataService", "AppMessage");
                    }

                    System.Threading.Thread.Sleep(interval * 60 * 1000);
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
                this.Cursor = System.Windows.Forms.Cursors.WaitCursor;
                if (txFileName.Text.Trim().Length == 0)
                {
                    MessageBox.Show("单号不能为空", "系统提示");
                }
                else
                {
                    iTR.OP.Invoice.OAInvoiceHelper invoice = new OAInvoiceHelper();
                    invoice.Run(1, txFileName.Text.Trim());
                }
                this.Cursor = System.Windows.Forms.Cursors.Arrow;//设置鼠标为正常状态
                MessageBox.Show("查验完成", "系统提示");
            }
            catch (Exception err)
            {
                this.Cursor = System.Windows.Forms.Cursors.Arrow;//设置鼠标为正常状态
                MessageBox.Show(err.Message, "系统提示");
            }
        }

        private void btnCheck_Click(object sender, EventArgs e)
        {
            try
            {
                if (txtCode.Text.Trim().Length == 0)
                    throw new Exception("查重发票代码不能为空");
                if (txtNo.Text.Trim().Length == 0)
                    throw new Exception("查重发票号码不能为空");
                string sql = @"Select field0005 , field0008 ,t2.Name
                                        from v3x.dbo.formmain_5247 t1
                                        Left Join v3x.dbo.ORG_Member t2 On t1.field0006= t2.ID
                                        where t1.ID In
                                        (Select formmain_id from formson_5248 Where  field0015='{0}'  and field0016='{1}')";
                sql = string.Format(sql, txtCode.Text.Trim(), txtNo.Text.Trim());
                SQLServerHelper runner = new SQLServerHelper();
                DataTable dt = runner.ExecuteSql(sql);
                if (dt.Rows.Count == 0)
                {
                    txtResult.Text = "你输入的发票号码不存在";
                }
                else
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        txtResult.Text = txtResult.Text + "单据：" + row["field0005"].ToString() + " 单号：" + row["field0008"].ToString() + " 申请人：" + row["Name"].ToString() + "\r\n";
                    }
                }
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
            }
        }

        private void InvoiceForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                //是否取消close操作
                e.Cancel = true;
                this.WindowState = FormWindowState.Minimized;
            }
        }
    }
}