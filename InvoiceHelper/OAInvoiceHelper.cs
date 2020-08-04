﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using iTR.Lib;
using System.Data;
using System.Xml;
using Invoice.Utils;
using System.Diagnostics;

namespace iTR.OP.Invoice
{
    public class OAInvoiceHelper
    {
        //string privateKey = @"<RSAKeyValue><Modulus>mce2SdkzX+cmwffCYWxhy0O8IknEb9gJY6dMl6N+G1kdttqhk9X3jIyU8ST5CFY0Iax5naMloUgldcoI8AUx7YRiVOzCQpuDnj/iPwllEzbBUAAKnWlliklkz4vMi/XDMVhV87mW+h0Vi588mrTc9kW0iDWxNDKxL7CgTFquCs0=</Modulus><Exponent>AQAB</Exponent><P>yVj9ogyzt7bokcMTXcAS/ZR0kiDUoLBDgajwuXoiOwF4f1TLyCx49Zuf8PAEc6+J9Xr8+Llb+tWerQ49FlD47w==</P><Q>w4VoE7Gb/8QGRVUNjSzPPwdW64CufnhfDBiODptGQHvRi6Uc6FY4ZmvEDYrURbSlr3OaqWrg4yt5bJOu/LbgAw==</Q><DP>QaeM/Nxbddpkt7L+i6FoD9vqrwOZkdQoDw2BgVl78/WkzxBdaqZlwuC+JJh/OyHQQIWcG5aFkaM6nH96F97LbQ==</DP><DQ>Mq52GATGBzps1bQCW0HuRsxEP6+Pi8DwAlarHCYrw7NU0fnu0FrpK8NrgocmFxuIhz5ULO5DdR9jzj1J8sAEuQ==</DQ><InverseQ>xSwIuQDReGHDbJ2F8zhniie88UhHUc6XakmjPCEKWSIMa+xvzi5UKOTJvvPjB3TNf4ibuR8fSieHz1uJ+lx21Q==</InverseQ><D>YTDDmPDZc2dYK4c3JvOk6x6oLNOKf1V+uajm03/VF9u+1+5d6F120zGWgMHpUseIsy+avXJ7Oe+rHULPW0MtRgFtNScEK+LLSnFkLJz9QZcXVyRE4QTCcYM5uLW5bYa9324ZGjBmzYSWOZ10V/FjZ8X81AutMDQqBv67ToZ6F1E=</D></RSAKeyValue>";
        //string CnnString = "";
        private XmlDocument doc = null;
        public OAInvoiceHelper()
        {
            try
            {
                doc = new XmlDocument();
                doc.Load("cfg.xml");
                //CnnString = doc.SelectSingleNode("Configuration/CnnString").InnerText;
                //CnnString = iTR.Lib.Common.RSADecrypt(privateKey, CnnString);
            }
            catch (Exception err)
            {
                throw err;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="mode">0:识别+查验+所有发票</param>
        /// <returns></returns>
        public string Run(int mode =0)
        {
            string result = "0";
            string fileName = "";
            string path="";
            string fileType="2";
            int chkCount = 0;
            DateTime checkDate;

            try
            {
                InvoiceHelper invoice = new InvoiceHelper();
                path = doc.SelectSingleNode("Configuration/Path").InnerText;

                //尝试次数field0033少于3次，已经过了验证日期的，开发日期小于 当天，状态为
                string sql = @"Select ID, formmain_ID as pid, field0020 as FileID,field0013 as folder,field0012 as FileName,field0014,Isnull(field0033,0) as field0033 from formson_5248 
                                        Where    isnull(field0033,0)<3 and isnull(field0039,'') ='是'  and CONVERT(varchar(100),field0017, 23) <CONVERT(varchar(100),getdate(), 23)  and  isnull(field0023,'')   Not In('通过','不通过','重号')
                                         and field0014 In ('机打卷票','电子普通票','电子专用票','纸质普通票','纸质专用票')  and  field0014 <='" + DateTime.Now.ToString()+"'" ;

                SQLServerHelper runner = new SQLServerHelper();
                DataTable dt = runner.ExecuteSql(sql);
                foreach (DataRow row in dt.Rows)
                {

                    fileName = path + "\\" + row["folder"].ToString() + "\\" + row["FileID"].ToString();

                    chkCount = int.Parse(row["field0033"].ToString()) + 1;//设置检查次数
                    //调用金蝶发票查验接口
                    InvoiceCheckResult chkResult = invoice.Scan_Check(fileName, fileType);
                    if (chkResult == null)//云接口调用报错，没有正常返回
                    {
                        FileLogger.WriteLog("调用发票云接口错误：返回值为空 FileName:" + fileName, 1, "OAInvoicehelper", "Run", "DataService", "AppMessage");
                        continue;
                    }
                    //调用成功
                    FileLogger.WriteLog("调用接口成功文件：" + fileName, 1, "OAInvoicehelper", "Run", "DataService", "AppMessage");

                    switch (chkResult.errcode)//操作错误代码
                    {
                        case "0000"://调用成功
                            #region 接口调用成功处理
                            switch (chkResult.errcode)
                            {
                                case "0000":// 查验成功
                                    #region 查验成功
                                    int invoiceSeq = 0;
                                    string rowID = "-1";

                                    foreach (InvoiceCheckDetail i in chkResult.CheckDetailList)//每张发票查验结果
                                    {
                                        rowID = row["ID"].ToString();
                                        //类型为其他、发票号为空，不是发票,设置不是发票状态，以免下次还继续查验
                                        if (i.invoiceType == "其他" || i.invoiceNo.Trim().Length == 0)
                                        {
                                            sql = "Update formson_5248 Set field0039='否' Where ID='" + rowID + "'";
                                            runner.ExecuteSqlNone(sql);
                                            continue;
                                        }
                                        if (i.invoiceNo.Trim().Length > 0)
                                        {
                                            decimal taxamout = 0;
                                            invoiceSeq = invoiceSeq + 1;

                                            if (i.taxAmount.Trim().Length > 0)
                                                taxamout = decimal.Parse(i.taxAmount.Trim());

                                            sql = @"Select field0015 from formson_5248 Where field0016='{0}' ";//验重判断
                                            sql = string.Format(sql, i.invoiceNo);
                                            DataTable dt1 = new DataTable();
                                            dt1 = runner.ExecuteSql(sql);
                                            if (dt1.Rows.Count > 0)
                                                i.checkStatus = "重号";

                                            if (invoiceSeq == 1)//文件中只有一张发票
                                            {

                                            }
                                            else//文件中有多张发票,先插入新纪录（先判断是否存在，不存在则插入）
                                            {
                                                //先判断相应的多张发票记录已存在
                                                sql = "Select ID from formson_5248 Where field0020='{0}' and field0016 ='{1}'";
                                                sql = string.Format(sql, row["FileID"].ToString(), i.invoiceCode);
                                                dt1 = runner.ExecuteSql(sql);
                                                if (dt1.Rows.Count == 0)//文件中多张发票不存在，则插入
                                                {
                                                    rowID = GetID();
                                                    sql = @"Insert Into [formson_5248](ID,formmain_id,sort,field0012,field0013,field0014,field0020)
			                                    Values({0},{1},1,'{2}','{3}','{4}','{5}')";
                                                    sql = string.Format(sql, rowID, row["pid"].ToString(), row["FileName"].ToString(), row["folder"].ToString(), row["field0014"].ToString(), row["FileID"].ToString());
                                                    runner.ExecuteSqlNone(sql);
                                                }
                                            }

                                            //保存查验结果
                                            decimal amount = 0;
                                            if (i.totalAmount.ToString().Trim().Length == 0)
                                                amount = 0;
                                            else
                                                amount = decimal.Parse(i.totalAmount);

                                            //设置查验日期
                                            checkDate = DateTime.Now;

                                            sql = @"update formson_5248 Set field0033= isnull(field0033,0)+1,field0015='{0}',field0016='{1}',field0017='{2}',
                                            field0018='{3}',field0019='{4}',field0021='{5}',field0022='{6}',field0023  ='{7}',
                                            field0024='{8}',field0025='{9}',field0026='{10}',field0027='{7}',field0032='{11}',field0034='{12}' , field0042='{14}'  Where ID={13}";

                                            sql = string.Format(sql, i.invoiceCode, i.invoiceNo, i.invoiceDate, i.salerName, amount, i.buyerTaxNo, i.salerAccount,
                                                  i.checkStatus, i.checkErrcode, i.checkDescription, taxamout, i.checkCode, i.invoiceType, rowID, checkDate.ToString());

                                            runner.ExecuteSqlNone(sql);
                                            FileLogger.WriteLog(" 成功处理文件名：" + fileName, 1, "OAInvoicehelper", "Run", "DataService", "AppMessage");
                                        }
                                    }
                                    #endregion
                                    break;
                                case "1001"://超过该张票当天查验次数,不处理
                                    continue;
                                case "1002"://查验超时,2小时后再处理,查验次数+1
                                    checkDate = DateTime.Now.AddHours(2);
                                    sql = @"update formson_5248 Set field0033= {0} ,field0042 ='{1}' ,field0025='{3}',field0024 ='{4}' Where ID={2}";
                                    sql = string.Format(sql, chkCount, checkDate, row["ID"].ToString(),chkResult.description, chkResult.errcode);
                                    runner.ExecuteSqlNone(sql);  
                                    break;

                                case "1014"://当天票不能查验，,查验次数+1
                                    checkDate = DateTime.Now; 
                                      sql = @"update formson_5248 Set field0033= {0} ,field0042 ='{1}' ,field0017 ='{2}',field0025='{4}',field0024 ='{5}'   Where ID={3}";
                                    sql = string.Format(sql, chkCount, checkDate.AddDays(1) , checkDate, row["ID"].ToString(), chkResult.description, chkResult.errcode);
                                    runner.ExecuteSqlNone(sql);
                                    break;
                                case "10003"://发票查验接口无法正常使用,退出应用
                                    Process.GetCurrentProcess().Kill();
                                    break;
                                default://10001,
                                    chkCount = 3;
                                    checkDate = DateTime.Now;
                                    sql = @"update fomson_5248 Set field0033= {0} ,field0042 ='{1}' ,field0025='{3}',field0024 ='{4}'  Where ID={2}";
                                    sql = string.Format(sql, chkCount, checkDate, row["ID"].ToString(), chkResult.description, chkResult.errcode);
                                    runner.ExecuteSqlNone(sql);
                                    break;
                               
                            }

                            #endregion
                            break;
                        case "20000"://调用接口发生异常
                            #region 调用接口错误处理，
                            Process.GetCurrentProcess().Kill();
                            #endregion
                            break;
                        case "0310"://调用接口发生异常
                            #region 调用接口错误处理
                            Process.GetCurrentProcess().Kill();
                            //result = "退出App";
                            #endregion
                            break;
                    }
                    
                    //根据接口返回情况，处理发票数据库记录
                    
                }
                //返回已处理记录数
                result = dt.Rows.Count.ToString();

            }
            catch(Exception err)
            {
               
                FileLogger.WriteLog( "Err:"  + err.Message,  1, "OAInvoicehelper", fileName, "DataService", "ErrMessage");
            }

            return result;
        }

        public string GetID ()
        {
            string result = "-1";
            try
            {
        
                byte[] buffer = Guid.NewGuid().ToByteArray();
                result= BitConverter.ToInt64(buffer, 0).ToString();
            }
            catch (Exception err)
            {
                throw err;
            }
            return result;
        }
        
    }
}
