namespace InvoiceApp
{
    partial class InvoiceForm
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(InvoiceForm));
            this.btStart = new System.Windows.Forms.Button();
            this.btExit = new System.Windows.Forms.Button();
            this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
            this.openInvoiceFile = new System.Windows.Forms.OpenFileDialog();
            this.label1 = new System.Windows.Forms.Label();
            this.txFileName = new System.Windows.Forms.TextBox();
            this.btOpen = new System.Windows.Forms.Button();
            this.btDebug = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btStart
            // 
            this.btStart.Location = new System.Drawing.Point(154, 29);
            this.btStart.Name = "btStart";
            this.btStart.Size = new System.Drawing.Size(62, 23);
            this.btStart.TabIndex = 0;
            this.btStart.Text = "Start";
            this.btStart.UseVisualStyleBackColor = true;
            this.btStart.Click += new System.EventHandler(this.btStart_Clik);
            // 
            // btExit
            // 
            this.btExit.Location = new System.Drawing.Point(219, 30);
            this.btExit.Name = "btExit";
            this.btExit.Size = new System.Drawing.Size(62, 23);
            this.btExit.TabIndex = 2;
            this.btExit.Text = "Exit";
            this.btExit.UseVisualStyleBackColor = true;
            this.btExit.Click += new System.EventHandler(this.btExit_Click);
            // 
            // notifyIcon1
            // 
            this.notifyIcon1.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon1.Icon")));
            this.notifyIcon1.Text = "发票验真查真平台";
            this.notifyIcon1.Visible = true;
            this.notifyIcon1.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.notifyIcon1_MouseDoubleClick);
            // 
            // openInvoiceFile
            // 
            this.openInvoiceFile.FileName = "openFileDialog1";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(1, 6);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(53, 12);
            this.label1.TabIndex = 3;
            this.label1.Text = "发票文件";
            // 
            // txFileName
            // 
            this.txFileName.Location = new System.Drawing.Point(57, 1);
            this.txFileName.Name = "txFileName";
            this.txFileName.ReadOnly = true;
            this.txFileName.Size = new System.Drawing.Size(181, 21);
            this.txFileName.TabIndex = 4;
            // 
            // btOpen
            // 
            this.btOpen.Location = new System.Drawing.Point(241, 1);
            this.btOpen.Name = "btOpen";
            this.btOpen.Size = new System.Drawing.Size(40, 23);
            this.btOpen.TabIndex = 5;
            this.btOpen.Text = "...";
            this.btOpen.UseVisualStyleBackColor = true;
            this.btOpen.Click += new System.EventHandler(this.btOpen_Click);
            // 
            // btDebug
            // 
            this.btDebug.Location = new System.Drawing.Point(87, 29);
            this.btDebug.Name = "btDebug";
            this.btDebug.Size = new System.Drawing.Size(62, 23);
            this.btDebug.TabIndex = 6;
            this.btDebug.Text = "Debug";
            this.btDebug.UseVisualStyleBackColor = true;
            this.btDebug.Click += new System.EventHandler(this.btDebug_Click);
            // 
            // InvoiceForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(285, 56);
            this.Controls.Add(this.btDebug);
            this.Controls.Add(this.btOpen);
            this.Controls.Add(this.txFileName);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btExit);
            this.Controls.Add(this.btStart);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "InvoiceForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "发票验真查重应用";
            this.WindowState = System.Windows.Forms.FormWindowState.Minimized;
            this.StyleChanged += new System.EventHandler(this.InvoiceForm_StyleChanged);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btStart;
        private System.Windows.Forms.Button btExit;
        private System.Windows.Forms.NotifyIcon notifyIcon1;
        private System.Windows.Forms.OpenFileDialog openInvoiceFile;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txFileName;
        private System.Windows.Forms.Button btOpen;
        private System.Windows.Forms.Button btDebug;
    }
}

