﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Data;
using System.Xml;
using Invoice.Utils;
using System.Diagnostics;
using iTR.Lib;
using Newtonsoft.Json;

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

                doc.Load(AppDomain.CurrentDomain.BaseDirectory + "\\cfg.xml");
                //CnnString = doc.SelectSingleNode("Configuration/CnnString").InnerText;
                //CnnString = iTR.Lib.Common.RSADecrypt(privateKey, CnnString);
            }
            catch (Exception err)
            {
                throw err;
            }
        }

        //public string WebCheck(string billNumber)
        //{
        //    string checkresult = Run(1, billNumber, true);
        //    return checkresult;
        //}

        /// <summary>
        ///
        /// </summary>
        /// <param name="mode">0:识别+查验+所有发票</param>
        /// <returns></returns>
        public string Run(int mode = 0, string billNo = "", bool status = false)
        {
            string result = "0";
            string fileName = "";
            string path = "";
            string fileType = "2";
            int chkCount = 0;
            string sql = "";
            DateTime checkDate;

            FileLogger.WriteLog("开始发票查验", 1, "OAInvoicehelper", "Run" + billNo, "DataService", "AppMessage");
            try
            {
                path = doc.SelectSingleNode("Configuration/Path").InnerText;
                //尝试次数field0033少于3次，已经过了验证日期的，开发日期小于 当天，状态为
                //sql = "Select ID, formmain_ID as pid, field0020 as FileID,field0013 as folder,field0012 as FileName,field0014,Isnull(field0033,0) as field0033," +
                //          "    Isnull(field0053,'') As field0053,field0015,field0016,field0017,field0050,field0032 " +
                //          "    from formson_5248   ";

                sql = @"Select t1.ID, t1.formmain_ID as pid, t1.field0020 as FileID,t1.field0013 as folder,t1.field0012 as FileName,t1.field0014,Isnull(t1.field0033,0) as field0033,
                        Isnull(t1.field0053,'') As field0053,t1.field0015,t1.field0016,t1.field0017,t1.field0050,t1.field0032,t3.Name  AS field0006,  t2.field0008,t1.field0018,t2.field0005
                        from v3x.dbo.formson_5248   t1
                        Left Join v3x.dbo.formmain_5247 t2 On t1.formmain_id = t2.ID
                        Left Join v3x.dbo.ORG_MEMBER  t3 On t2.field0006 = convert(varchar(100),t3.ID)";

                if (mode == 0)
                {
                    sql = sql + " Where    isnull(t1.field0033,0) < 2 and isnull(t1.field0039,'') ='是'  and CONVERT(varchar(100),t1.field0017, 23) <CONVERT(varchar(100),getdate(), 23)  " + "   and  t1.formmain_id='8342463430625887200'" +
                                      "  and  (isnull(t1.field0023,'')   Not In('通过','重号') or isnull(t1.field0053,'')='-4875734478274671070')  " +
                                      "   and  t1.field0042<='" + DateTime.Now.ToString() + "'  and field0014 In ('机打卷票','电子普通票','电子专用票','纸质普通票','纸质专用票') order by t1.field0042 desc";

                    //   sql = sql + "     t1.formmain_id='8342463430625887200'";
                }

                if (mode == 1)
                {
                    sql = sql + " Where t1.formmain_Id In ( Select ID from formmain_5247 Where field0008 = '" + billNo + "') " +
                        "  and t1.field0014 In ('机打卷票','电子普通票','电子专用票','纸质普通票','纸质专用票','普通纸质发票')  and  isnull(t1.field0027,'')   Not In('通过') ";
                    //"  and isnull(field0039,'') ='是' ";
                }
                SQLServerHelper runner = new SQLServerHelper();
                FileLogger.WriteLog("sql获取未查询发票 " + sql, 1, "OAInvoicehelper", "Run" + billNo, "DataService", "AppMessage");
                DataTable dt = runner.ExecuteSql(sql);
                FileLogger.WriteLog("sql获取未查询发票完成", 1, "OAInvoicehelper", "Run" + billNo, "DataService", "AppMessage");
                InvoiceHelper invoice = new InvoiceHelper();

                foreach (DataRow row in dt.Rows)
                {
                    try
                    {
                        chkCount = int.Parse(row["field0033"].ToString()) + 1;//设置检查次数
                        InvoiceCheckResult chkResult = new InvoiceCheckResult();
                        //调用金蝶发票查验接口
                        FileLogger.WriteLog("开始获取Scan_Check查验结果", 1, "OAInvoicehelper", "Run" + billNo, "DataService", "AppMessage");
                        if (row["field0053"].ToString() == "-4875734478274671070")//手工重验
                        {
                            Dictionary<string, string> param = new Dictionary<string, string>();
                            param["InvoiceCode"] = row["field0015"].ToString().Trim();
                            param["InvoiceNo"] = row["field0016"].ToString().Trim();
                            param["InvoiceDate"] = row["field0017"].ToString().Trim();
                            param["InvoiceMoney"] = row["field0050"].ToString().Trim();
                            param["InvoieCheckCode"] = row["field0032"].ToString().Trim();
                            chkResult = invoice.Scan_Check(fileName, fileType, 8, "2", param);
                        }
                        else//自动扫描与查验
                        {
                            fileName = path + "\\" + row["folder"].ToString() + "\\" + row["FileID"].ToString();
                            chkResult = invoice.Scan_Check(fileName, fileType);
                        }
                        if (chkResult == null)//云接口调用报错，没有正常返回
                        {
                            FileLogger.WriteLog("调用发票云接口错误：返回值为空 FileName:" + fileName, 1, "OAInvoicehelper", "Run", "DataService", "AppMessage");
                            continue;
                        }
                        FileLogger.WriteLog("结束获取Scan_Check查验结果", 1, "OAInvoicehelper", "Run" + billNo, "DataService", "AppMessage");
                        //调用成功
                        if (fileName.Length > 0)
                            FileLogger.WriteLog("调用接口成功,文件：" + fileName, 1, "OAInvoicehelper", "Run", "DataService", "AppMessage");

                        switch (chkResult.errcode)//操作错误代码
                        {
                            case "0000"://调用成功

                                #region 调用成功

                                int invoiceSeq = 0;
                                string rowID = "-1";
                                foreach (InvoiceCheckDetail i in chkResult.CheckDetailList)//每张发票查验结果
                                {
                                    switch (i.checkErrcode)
                                    {
                                        case "0000":
                                            rowID = row["ID"].ToString();
                                            //类型为其他、发票号码或代码为空，不是发票,设置不是发票状态，以免下次还继续查验
                                            //王天池 2021-07-20 增加代码为空时也设置为不是发票
                                            if (i.invoiceNo.Trim().Length == 0 || i.invoiceCode.Trim().Length == 0)
                                            {
                                                sql = "Update formson_5248 Set field0039='否' Where ID='" + rowID + "'";
                                                runner.ExecuteSqlNone(sql);
                                                continue;
                                            }
                                            else
                                            {
                                                decimal taxamout = 0;
                                                invoiceSeq = invoiceSeq + 1;
                                                string lianhao = "", lianhao1 = "";//同单连号备注
                                                string lianhaoType = "";//连号类型
                                                decimal invoiceTotalamout = 0;

                                                int invoiceCount = 0, invoiceCount1 = 0;//同单连号发票张数，异单连号发票张数

                                                if (i.taxAmount.Trim().Length > 0)
                                                    taxamout = decimal.Parse(i.taxAmount.Trim());
                                                // 王天池，2021-07-21，修正代码与号码设置反了的问题
                                                sql =
                                                    "Select *  From  v3x.dbo.formson_5248 Where field0015='{0}' and field0016='{1}'  and  field0027='通过'";

                                                //sql = @"Select field0015 from v3x.dbo.formson_5248 Where (Select count(*) From  v3x.dbo.formson_5248 where field0015='{0}' and field0016='{1}'  and  field0027='通过') >= 1";//验重判断
                                                sql = string.Format(sql, i.invoiceCode, i.invoiceNo);
                                                DataTable dt1 = new DataTable();
                                                dt1 = runner.ExecuteSql(sql);
                                                if (dt1.Rows.Count > 0)
                                                {
                                                    i.checkStatus = "重号";
                                                }
                                                else// 增加发票连号检查功能，王天池，2021-07-20,
                                                    // 王天池，2021-08-27，根据新的发票管控规则，优化
                                                {
                                                    sql = @"Select   t2.field0005 ,t2.field0008,t3.Name  AS field0006,t1.field0016,t1.field0019,convert(varchar(100),t1.field0017,23) AS field0017
                                                            From v3x.dbo.formson_5248 t1
                                                            Left Join v3x.dbo.formmain_5247 t2 On t1.formmain_id = t2.ID
                                                            Left Join v3x.dbo.ORG_MEMBER  t3 On t2.field0006 = convert(varchar(100),t3.ID)
                                                            Where Isnull(t1.field0016,'')<> '' and t2.field0004 >= '2021-01-01' and len(field0016)= 8 and Isnull(t1.field0015,'')= '{0}'
                                                            and Convert(int,'1' + t1.field0016)> (Convert(int, '1' + '{1}') - 5)  and Convert(int,'1' + t1.field0016)< (Convert(int, '1' + '{1}') + 5)  and
                                                             t1.field0016 <> '{1}'";
                                                    sql = string.Format(sql, i.invoiceCode, i.invoiceNo);

                                                    DataTable dt2 = runner.ExecuteSql(sql);

                                                    //连号备注初始化，格式为：发票代码：XXXX 号码：XXX 与以下单据存在连号情况：单据,单号 
                                                    if (dt2.Rows.Count > 0)
                                                    {
                                                        foreach (DataRow r in dt2.Rows)
                                                        {
                                                            if (row["field0008"].ToString() == r["field0008"].ToString()) //同单连号
                                                            {
                                                                invoiceCount = invoiceCount + 1;
                                                                lianhao = r["field0016"].ToString() + "(" + r["field0019"].ToString() + ")  " + lianhao;
                                                            }
                                                            else//异单连号
                                                            {

                                                                invoiceCount1 = invoiceCount1 + 1;
                                                                lianhao1 = "单据：" + r["field0005"].ToString() + " 单号：" + r["field0008"].ToString() + r["field0016"].ToString() + "(" + r["field0019"].ToString() + ")  " + r["field0006"].ToString() + " \r\n" + lianhao1;
                                                            }


                                                        }

                                                        //获取开票机构180天累计开票额度
                                                        if (row["field0005"].ToString() == "付款申请5.0")//服务类
                                                        {
                                                            sql = @" Select Isnull(field0006,0) As field0006 from v3x.dbo.formmain_8899 Where field0002  = '服务类'  and field0001 ='{0}'";
                                                        }
                                                        else
                                                        {
                                                            sql = @" Select Isnull(field0006,0) As field0006 from v3x.dbo.formmain_8899 Where field0002 = '餐饮类'  and field0001 ='{0}'";
                                                        }
                                                        sql = string.Format(sql, row["field0018"].ToString());
                                                        DataTable dtInvoice = runner.ExecuteSql(sql);
                                                        if (dtInvoice.Rows.Count > 0)
                                                        {
                                                            invoiceTotalamout = decimal.Parse(dtInvoice.Rows[0]["field0006"].ToString());
                                                        }


                                                        if (lianhao.Length > 0)
                                                        {
                                                            lianhao = "【同单连号】代码 " + i.invoiceCode + " 号码 " + i.invoiceNo + "的连号情况：张数（ " + invoiceCount.ToString() + "） " + row["field0018"].ToString() + "(" + ")）\r\n" + lianhao;
                                                        }
                                                        if (lianhao1.Length > 0)
                                                        {
                                                            lianhao1 = "【异单连号】代码 " + i.invoiceCode + " 号码 " + i.invoiceNo + "的连号情况：张数（ " + invoiceCount.ToString() + "） " + row["field0018"].ToString() + "(" + ")）\r\n" + lianhao1;
                                                        }

                                                        if (lianhao1.Length > 0)
                                                        {
                                                            lianhaoType = "异单";
                                                        }
                                                        if (lianhao.Length > 0)
                                                        {
                                                            lianhaoType = lianhaoType.Length > 0 ? "同异单" : "同单";
                                                            lianhao = lianhao1.Length > 0 ? lianhao + " \r\n" + lianhao1 : lianhao;
                                                        }
                                                    }

                                                }

                                                if (invoiceSeq == 1)//文件中只有一张发票
                                                {
                                                }
                                                else//文件中有多张发票,先插入新纪录（先判断是否存在，不存在则插入）
                                                {
                                                    //先判断相应的多张发票记录已存在
                                                    sql = "Select ID from formson_5248 Where field0020='{0}' and isnull(field0016,'') ='{1}'";
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
                                                ///处理没有返回值的数字型属性
                                                //保存查验结果
                                                decimal amount = 0;
                                                if (i.totalAmount.ToString().Trim().Length == 0)
                                                    amount = 0;
                                                else
                                                    amount = decimal.Parse(i.totalAmount);

                                                if (i.invoiceMoney.ToString().Trim().Length == 0)
                                                    i.invoiceMoney = "0";

                                                //设置查验日期
                                                checkDate = DateTime.Now;

                                                sql = @"update formson_5248 Set field0033= isnull(field0033,0)+1,field0015='{0}',field0016='{1}',field0017='{2}',
                                                        field0018='{3}',field0019='{4}',field0021='{5}',field0022='{6}',field0023  ='{7}',
                                                        field0024='{8}',field0025='{9}',field0026='{10}',field0027='{7}',field0032='{11}',field0034='{12}' , field0042='{14}',
                                                        field0049='{15}', field0050='{16}' ,field0053='',field0056='{17}',field0057='{18}',field0058={19}  Where ID={13}";

                                                sql = string.Format(sql, i.invoiceCode, i.invoiceNo, i.invoiceDate, i.salerName, amount, i.buyerTaxNo, i.salerAccount,
                                                                                i.checkStatus, i.checkErrcode, i.checkDescription, taxamout, i.checkCode, i.invoiceType, rowID, checkDate.ToString(),
                                                                                i.taxRate, i.invoiceMoney, lianhao, lianhaoType, invoiceTotalamout);

                                                runner.ExecuteSqlNone(sql);
                                                if (fileName.Length > 0)
                                                    FileLogger.WriteLog(" 成功处理文件名：" + fileName, 1, "OAInvoicehelper", "Run", "DataService", "AppMessage");
                                            }
                                            break;

                                        case "1001"://超过该张票当天查验次数,不处理
                                            checkDate = Convert.ToDateTime(DateTime.Now.AddDays(1).ToString("yyyy-MM-dd 01:30:00"));
                                            sql = @"update formson_5248 Set field0033= {0} ,field0042 ='{1}' ,field0025='{3}',field0024 ='{4}' Where ID={2}";
                                            sql = string.Format(sql, 2, checkDate, row["ID"].ToString(), i.checkDescription, i.checkCode);
                                            runner.ExecuteSqlNone(sql);
                                            break;

                                        case "1002"://查验超时,2小时后再处理,查验次数+1
                                            checkDate = DateTime.Now.AddHours(2);
                                            sql = @"update formson_5248 Set field0033= {0} ,field0042 ='{1}' ,field0025='{3}',field0024 ='{4}' Where ID={2}";
                                            sql = string.Format(sql, chkCount, checkDate, row["ID"].ToString(), i.checkDescription, i.checkCode);
                                            runner.ExecuteSqlNone(sql);
                                            break;

                                        case "1014"://当天票不能查验，,查验次数+1
                                            checkDate = DateTime.Now;
                                            sql = @"update formson_5248 Set field0033= {0} ,field0042 ='{1}' ,field0017 ='{2}',field0025='{4}',field0024 ='{5}'   Where ID={3}";
                                            sql = string.Format(sql, chkCount, checkDate.AddDays(1), checkDate, row["ID"].ToString(), chkResult.description, chkResult.errcode);
                                            runner.ExecuteSqlNone(sql);
                                            break;

                                        case "1015"://超过一年的不能查验，
                                            sql = @"update formson_5248 Set field0033= {0} , field0027 ='{1}' Where ID={2}";
                                            sql = string.Format(sql, 2, "发票超1年", row["ID"].ToString());
                                            runner.ExecuteSqlNone(sql);
                                            break;

                                        case "3110"://发票查验地区税局服务暂停
                                            sql = @"update formson_5248 Set field0033= {0} , field0027 ='{1}' Where ID={2}";
                                            sql = string.Format(sql, 2, "地方税局暂停查验服务", row["ID"].ToString());
                                            runner.ExecuteSqlNone(sql);
                                            break;

                                        case "10002"://在官方数据库查不到此发票
                                            sql = @"update formson_5248 Set field0033= {0} , field0023 ='{1}' , field0027 ='{1}' Where ID={2}";
                                            sql = string.Format(sql, 2, "此票不存在", row["ID"].ToString());
                                            runner.ExecuteSqlNone(sql);
                                            break;

                                        case "10003"://发票查验接口无法正常使用,退出应用
                                                     //Process.GetCurrentProcess().Kill();
                                            sql = @"update formson_5248 Set field0033= {0} , field0027 ='{1}' Where ID={2}";
                                            sql = string.Format(sql, 2, "接口错误", row["ID"].ToString());
                                            runner.ExecuteSqlNone(sql);
                                            break;

                                        case "10004"://发票作废
                                            sql = @"update formson_5248 Set field0033= {0} , field0027 ='{1}' Where ID={2}";
                                            sql = string.Format(sql, 2, "此票作废", row["ID"].ToString());
                                            runner.ExecuteSqlNone(sql);
                                            break;

                                        case "10005"://
                                            sql = @"update formson_5248 Set field0033= {0} , field0027 ='{1}' Where ID={2}";
                                            sql = string.Format(sql, 2, "发票信息不全", row["ID"].ToString());
                                            runner.ExecuteSqlNone(sql);
                                            break;

                                        case "10300"://
                                            sql = @"update formson_5248 Set field0033= {0} , field0027 ='{1}' Where ID={2}";
                                            sql = string.Format(sql, 3, "发票串号", row["ID"].ToString());
                                            runner.ExecuteSqlNone(sql);
                                            break;

                                        default://10001,
                                            checkDate = DateTime.Now;
                                            SetInvoceCheckStatus(row["ID"].ToString(), checkDate, 2, i.checkDescription, i.checkErrcode);
                                            break;
                                    }
                                }
                                break;

                            #endregion 调用成功

                            case "20000"://调用接口发生异常

                                #region 调用接口错误处理，

                                //Process.GetCurrentProcess().Kill();
                                checkDate = DateTime.Now.AddHours(2);
                                sql = @"update formson_5248 Set field0033= {0} ,field0042 ='{1}' ,field0025='{3}',field0024 ='{4}' Where ID={2}";
                                sql = string.Format(sql, chkCount, checkDate, row["ID"].ToString(), chkResult.description, chkResult.errcode);
                                runner.ExecuteSqlNone(sql);

                                #endregion 调用接口错误处理，

                                break;

                            case "1011"://查验超时,2小时后再处理,查验次数+1
                                checkDate = DateTime.Now.AddHours(2);
                                sql = @"update formson_5248 Set field0033= {0} ,field0042 ='{1}' ,field0025='{3}',field0024 ='{4}' Where ID={2}";
                                sql = string.Format(sql, chkCount, checkDate, row["ID"].ToString(), chkResult.description, chkResult.errcode);
                                runner.ExecuteSqlNone(sql);
                                break;

                            case "0310"://调用接口发生异常

                                #region 调用接口错误处理

                                checkDate = DateTime.Now;
                                SetInvoceCheckStatus(row["ID"].ToString(), checkDate, 2, chkResult.description, chkResult.errcode);

                                #endregion 调用接口错误处理

                                break;

                            case "333333"://附件超大

                                #region 附件超大

                                checkDate = DateTime.Now;
                                SetInvoceCheckStatus(row["ID"].ToString(), checkDate, 2, chkResult.description, chkResult.errcode);

                                #endregion 附件超大

                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        FileLogger.WriteLog("Err:" + ex.Message + ex.StackTrace + ex.InnerException ?? "", 1, "OAInvoicehelper", fileName, "DataService", "ErrMessage");
                    }
                    //根据接口返回情况，处理发票数据库记录
                }
                //返回已处理记录数
                result = dt.Rows.Count.ToString();
            }
            catch (Exception err)
            {
                FileLogger.WriteLog("Err:" + err.Message + err.StackTrace + err.InnerException ?? "", 1, "OAInvoicehelper", fileName, "DataService", "ErrMessage");
            }
            FileLogger.WriteLog("结束发票查验", 1, "OAInvoicehelper", "Run", "DataService", "AppMessage");
            if (status)
            {
                SQLServerHelper sqlServer = new SQLServerHelper();
                var invoice = sqlServer.ExecuteSql(
                    $"Select t1.field0012 文件名,t1.field0014 类型 ,t1.field0015 代码,t1.field0016 号码,t1.field0032 校验码, t1.field0017 开票日期,t1.field0026 税金, t1.field0019 开票金额,t1.field0033 查验次数,t1.field0027 状态,t1.field0018 开票机构,t1.field0039 是否是发票,t1.field0025 结果 From   v3x.dbo.formson_5248 t1 Left Join   v3x.dbo.formmain_5247 t2 On t1.formmain_id = t2.ID Where t2.field0008='{billNo}'");
                List<Invoice> invoiceList = new List<Invoice>();
                foreach (DataRow row in invoice.Rows)
                {
                    invoiceList.Add(new Invoice() { Field0012 = row["文件名"].ToString() ?? "", Field0014 = row["类型"].ToString() ?? "", Field0015 = row["代码"].ToString() ?? "", Field0016 = row["号码"].ToString() ?? "", Field0032 = row["校验码"].ToString() ?? "", Field0017 = row["开票日期"].ToString() ?? "", Field0026 = row["税金"].ToString() ?? "", Field0019 = row["开票金额"].ToString() ?? "", Field0033 = row["查验次数"].ToString() ?? "", Field0027 = row["状态"].ToString() ?? "", Field0018 = row["开票机构"].ToString() ?? "", Field0039 = row["是否是发票"].ToString() ?? "", Field0025 = row["结果"].ToString() ?? "" });
                }

                MessageModel<List<Invoice>> me = new MessageModel<List<Invoice>>();
                var objResult = me.Success(invoiceList);
                result = JsonConvert.SerializeObject(objResult);
            }

            return result;
        }

        public static string GetID()
        {
            string result = "-1";
            try
            {
                byte[] buffer = Guid.NewGuid().ToByteArray();
                result = BitConverter.ToInt64(buffer, 0).ToString();
            }
            catch (Exception err)
            {
                throw err;
            }
            return result;
        }

        private void SetInvoceCheckStatus(string invoceIID, DateTime checkDate, int chkCount = 3, string errdescription = "", string errCode = "")
        {
            string sql = "";
            SQLServerHelper runner = new SQLServerHelper();

            sql = @"update formson_5248 Set field0033= {0} ,field0042 ='{1}' ,field0025='{3}',field0024 ='{4}',field0027='{3}'  Where ID={2}";
            sql = string.Format(sql, chkCount, checkDate, invoceIID, errdescription, errCode);
            runner.ExecuteSqlNone(sql);
        }

        public string UpdateInvoiceDB(string xmlResult, string formID, string formType)
        {
            string result = "<UpdateData> " +
                                    "<Result>{0}</Result>" +
                                    "<InvoiceResult>{1}</InvoiceResult>" +
                                    "<Description>{2}</Description></UpdateData>";
            try
            {
                string field0005 = formType;
                string field0029 = "";
                string field0035 = "";//源主表查验结果字段
                string field0036 = "";//源表发票类型字
                string field0037 = "";//源发票子表名
                string field0038 = "";//发票子表发票附件字段
                string field0040 = "";//发票子表开票日期字段
                string field0041 = "";//发票子表查验结果字段
                string field0044 = "";//子表税率字段
                string field0045 = "";//子表税金字段
                string field0046 = "";//子表不含税额字段
                string field0047 = "";//子表发票号码字段
                string field0048 = "";//子表发票代码字段
                string field0051 = "";//子表发票校验码字段
                string field0052 = "";//子表是否重验字段

                InvoiceCheckResult chkResult = InvoiceHelper.Xml2InvoiceCheckResult(xmlResult);
                chkResult = CheckInvoiceData(chkResult);
                if (chkResult == null)
                {
                    result = "<UpdateData>" +
                                   "<Result>{0}</Result>" +
                                   "<InvoiceResult>{1}</InvoiceResult>" +
                                   "<Description>{2}</Description></UpdateData>";
                    result = string.Format(result, "False", "异常", "发票不存在");
                }
                else if (chkResult.errcode == "通过")
                {
                    string sql = "Select ID from v3x.dbo.formmain_5247 Where ID = '" + formID + "'";
                    SQLServerHelper runner = new SQLServerHelper();
                    DataTable dt = runner.ExecuteSql(sql);
                    if (dt.Rows.Count == 0)//发票库中不存在，插入
                    {
                        switch (formType)
                        {
                            case "学术活动费用支付单":
                                field0029 = "formmain_5925";
                                field0035 = "field0016";
                                field0036 = "field0026";
                                field0037 = "formson_5926";
                                field0038 = "field0029";
                                field0040 = "field0031";
                                field0041 = "field0030";
                                field0044 = "field0074";
                                field0045 = "field0068";
                                field0046 = "field0069";
                                field0047 = "field0072";
                                field0048 = "field0071";
                                field0051 = "field0073";
                                field0052 = "field0070";
                                break;

                            case "招待费报销单":
                                field0029 = "formmain_5935";
                                field0035 = "field0016";
                                field0036 = "field0026";
                                field0037 = "formson_5936";
                                field0038 = "field0028";
                                field0040 = "field0030";
                                field0041 = "field0029";
                                field0044 = "field0048";
                                field0045 = "field0047";
                                field0046 = "field0049";
                                field0047 = "field0053";
                                field0048 = "field0052";
                                field0051 = "field0054";
                                field0052 = "field0051";

                                break;
                        }
                        //插入发票库主表
                        sql = @"Declare @formmainID numeric(19,0)
                                          EXEC  DataService.[dbo].[Insert_Invoice_Data20]  @formmainID output,'','','{0}','{1}','','','','','{2}','-1','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}','{12}','{13}','{14}','{15}','{16}'";
                        sql = string.Format(sql, DateTime.Now.ToString("yyyy-MM-dd"), field0005, field0029, field0035, field0036, field0037, field0038, field0040, field0041, field0044, field0045, field0046, field0047, field0048, field0051, field0052, formID);
                        runner.ExecuteSqlNone(sql);
                    }
                    //删除已存在的发票记录
                    sql = "Delete from v3x.dbo.[formson_5248]  Where formmain_Id = '" + formID + "'";
                    runner.ExecuteSqlNone(sql);
                    foreach (InvoiceCheckDetail invoiceDetail in chkResult.CheckDetailList)
                    {
                        sql = @" INSERT INTO v3x.dbo.[formson_5248]([ID],[formmain_id],[sort],[field0012],[field0013],[field0014],[field0015],[field0016],[field0017],
                                [field0018],[field0019],[field0020],[field0021],[field0022],[field0023],[field0024],[field0025],[field0026],[field0027] ,[field0032],
                                [field0034],[field0033],[field0039],[field0042],[field0050],[field0053],[field0054],[field0049])
                                Values({0},{1},1,'{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}','{12}','{13}','{14}','{15}','{16}','{17}','{18}','{19}','{20}','{21}','{22}','{23}','{24}','{25}','{26}')";
                        string formsonId = GetID();
                        sql = string.Format(sql, formsonId, formID, "YRB已验证", "", invoiceDetail.invoiceType, invoiceDetail.invoiceCode, invoiceDetail.invoiceNo, invoiceDetail.invoiceDate,
                            invoiceDetail.salerName, invoiceDetail.amount, "", invoiceDetail.buyerTaxNo, invoiceDetail.salerAccount, invoiceDetail.checkStatus, invoiceDetail.checkErrcode, invoiceDetail.checkDescription, invoiceDetail.taxAmount, "通过", "",
                            invoiceDetail.invoiceType, "2", "是", DateTime.Now.ToString("yyyy-MM-dd"), invoiceDetail.invoiceMoney, "", "-1", invoiceDetail.taxRate);
                        runner.ExecuteSqlNone(sql);
                    }
                    result = string.Format(result, "True", "通过", "");
                }
                else
                {
                    result = string.Format(result, "False", "异常", chkResult.description);
                }
            }
            catch (Exception err)
            {
                result = string.Format(result, "False", "异常", err.Message);
            }
            return result;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="chkResult">发票查验结果对象</param>
        /// <returns></returns>
        private InvoiceCheckResult CheckInvoiceData(InvoiceCheckResult chkResult)
        {
            string result = "通过", sql = "", resultDetail = "";

            SQLServerHelper runner = new SQLServerHelper();

            switch (chkResult.errcode)//操作错误代码
            {
                case "0000"://调用成功

                    #region 调用成功

                    int invoiceSeq = 0;
                    for (int i = 0; i < chkResult.CheckDetailList.Count; i++)//每张发票查验结果
                    {
                        switch (chkResult.CheckDetailList[i].checkErrcode)
                        {
                            case "0000":

                                //类型为其他、发票号为空，不是发票,设置不是发票状态，以免下次还继续查验
                                if (chkResult.CheckDetailList[i].invoiceNo.Trim().Length == 0)
                                {
                                    chkResult.CheckDetailList[i].checkStatus = "非发票";
                                    result = "异常";
                                    resultDetail = resultDetail.Trim().Length > 0 ? resultDetail + ";" : "" + " 非发票";
                                }
                                if (chkResult.CheckDetailList[i].invoiceNo.Trim().Length > 0)
                                {
                                    invoiceSeq = invoiceSeq + 1;
                                    if (chkResult.CheckDetailList[i].taxAmount.Trim().Length == 0)
                                        chkResult.CheckDetailList[i].taxAmount = "0";
                                    sql = @"Select field0015 from formson_5248 Where field0016='{0}' and  field0015='{1}'  and  Isnull(field0027,'') = '通过'";//验重判断
                                    sql = string.Format(sql, chkResult.CheckDetailList[i].invoiceNo, chkResult.CheckDetailList[i].invoiceCode);
                                    DataTable dt1 = new DataTable();
                                    dt1 = runner.ExecuteSql(sql);
                                    if (dt1.Rows.Count > 0)
                                    {
                                        chkResult.CheckDetailList[i].checkStatus = "重号";
                                        result = "异常";
                                        resultDetail = resultDetail.Trim().Length > 0 ? resultDetail + "; " : "" + "发票号：" + chkResult.CheckDetailList[i].invoiceNo + " 重号";
                                    }
                                    if (chkResult.CheckDetailList[i].totalAmount == null)
                                        chkResult.CheckDetailList[i].totalAmount = "0";

                                    if (chkResult.CheckDetailList[i].invoiceMoney.ToString().Trim().Length == 0)
                                        chkResult.CheckDetailList[i].invoiceMoney = "0";
                                }
                                break;

                            case "1001"://超过该张票当天查验次数,不处理

                                chkResult.CheckDetailList[i].checkStatus = "当天查验次数";
                                result = "异常";
                                resultDetail = resultDetail.Trim().Length > 0 ? resultDetail + "; " : "" + "发票号：" + chkResult.CheckDetailList[i].invoiceNo + chkResult.CheckDetailList[i].checkStatus;
                                break;

                            case "1002"://查验超时,2小时后再处理,查验次数+1
                                chkResult.CheckDetailList[i].checkStatus = "查验超时";
                                result = "异常";
                                resultDetail = resultDetail.Trim().Length > 0 ? resultDetail + "; " : "" + "发票号：" + chkResult.CheckDetailList[i].invoiceNo + chkResult.CheckDetailList[i].checkStatus;
                                break;

                            case "1014"://当天票不能查验，,查验次数+1
                                chkResult.CheckDetailList[i].checkStatus = "当天票不查验";
                                result = "异常";
                                resultDetail = resultDetail.Trim().Length > 0 ? resultDetail + "; " : "" + "发票号：" + chkResult.CheckDetailList[i].invoiceNo + chkResult.CheckDetailList[i].checkStatus;
                                break;

                            case "1015"://超过一年的不能查验，
                                chkResult.CheckDetailList[i].checkStatus = "超1年票";
                                result = "异常";
                                resultDetail = resultDetail.Trim().Length > 0 ? resultDetail + "; " : "" + "发票号：" + chkResult.CheckDetailList[i].invoiceNo + chkResult.CheckDetailList[i].checkStatus;
                                break;

                            case "3110"://发票查验地区税局服务暂停
                                chkResult.CheckDetailList[i].checkStatus = "官网暂停服务";
                                result = "异常";
                                resultDetail = resultDetail.Trim().Length > 0 ? resultDetail + "; " : "" + "发票号：" + chkResult.CheckDetailList[i].invoiceNo + chkResult.CheckDetailList[i].checkStatus;
                                break;

                            case "10002"://在官方数据库查不到此发票
                                chkResult.CheckDetailList[i].checkStatus = "此票不存在";
                                result = "异常";
                                resultDetail = resultDetail.Trim().Length > 0 ? resultDetail + "; " : "" + "发票号：" + chkResult.CheckDetailList[i].invoiceNo + chkResult.CheckDetailList[i].checkStatus;
                                break;

                            case "10003"://发票查验接口无法正常使用,退出应用
                                chkResult.CheckDetailList[i].checkStatus = "接口错误";
                                result = "异常";
                                resultDetail = resultDetail.Trim().Length > 0 ? resultDetail + "; " : "" + "发票号：" + chkResult.CheckDetailList[i].invoiceNo + chkResult.CheckDetailList[i].checkStatus;
                                break;

                            case "10004"://发票作废
                                chkResult.CheckDetailList[i].checkStatus = "此票作废";
                                result = "异常";
                                resultDetail = resultDetail.Trim().Length > 0 ? resultDetail + "; " : "" + "发票号：" + chkResult.CheckDetailList[i].invoiceNo + chkResult.CheckDetailList[i].checkStatus;
                                break;

                            case "10005"://
                                chkResult.CheckDetailList[i].checkStatus = "发票信息不全";
                                result = "异常";
                                resultDetail = resultDetail.Trim().Length > 0 ? resultDetail + "; " : "" + "发票号：" + chkResult.CheckDetailList[i].invoiceNo + chkResult.CheckDetailList[i].checkStatus;
                                break;

                            case "10300"://
                                chkResult.CheckDetailList[i].checkStatus = "发票串号";
                                result = "异常";
                                resultDetail = resultDetail.Trim().Length > 0 ? resultDetail + "; " : "" + "发票号：" + chkResult.CheckDetailList[i].invoiceNo + chkResult.CheckDetailList[i].checkStatus;
                                break;

                            default://10001,
                                chkResult.CheckDetailList[i].checkStatus = "查验异常";
                                result = "异常";
                                resultDetail = resultDetail.Trim().Length > 0 ? resultDetail + "; " : "" + "发票号：" + chkResult.CheckDetailList[i].invoiceNo + chkResult.CheckDetailList[i].checkStatus;
                                break;
                        }
                    }
                    break;

                #endregion 调用成功

                case "20000"://调用接口发生异常

                    result = "异常";
                    resultDetail = "调用接口发生异常";
                    break;

                case "1011"://查验超时,2小时后再处理,查验次数+1
                    result = "异常";
                    resultDetail = chkResult.description;
                    break;

                case "0310"://调用接口发生异常

                    #region 调用接口错误处理

                    result = "异常";
                    resultDetail = chkResult.description;

                    #endregion 调用接口错误处理

                    break;

                case "333333"://附件超大

                    #region 附件超大

                    result = "异常";
                    resultDetail = "附件大小超过4M";

                    #endregion 附件超大

                    break;
            }
            chkResult.errcode = result;
            chkResult.description = resultDetail;
            return chkResult;
        }
    }
}
