using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net;
using System.IO;
using iTR.Lib;
using System.Xml;
using Invoice.Utils;
using InvoiceUtils.OAAttachment;


namespace iTR.OP.Invoice
{
    public class InvoiceHelper
    {
        XmlDocument cfgDoc = null;
        private string oawsUrl = "";
        public InvoiceHelper()
        {
            cfgDoc = new XmlDocument();
            cfgDoc.Load(AppDomain.CurrentDomain.BaseDirectory + "\\cfg.xml");
            oawsUrl = cfgDoc.SelectSingleNode("Configuration/OAWSUrl").InnerText;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="fileType">1:PDF</param>
        /// <param name="timeOutSecond"></param>
        /// <returns></returns>
        public InvoiceCheckResult Scan_Check(string fileName, string fileType, int timeOutSecond = 8,string mode="1", Dictionary<string,string> param=null)
        {
            InvoiceCheckResult result = null;
            try
            {
                FileLogger.WriteLog("开始调用KingDeeApi接口", 1, "OAInvoicehelper", "Run", "DataService", "AppMessage");
                if (mode == "1")
                {
                    //解密后的文件名
                    string decryptFileName = fileName + "_D";


                    System.IO.FileInfo fileInfo = new System.IO.FileInfo(fileName);
                    if (fileInfo.Length > 1024 * 1024 * 4)
                    {
                        result = new InvoiceCheckResult();
                        result.errcode = "333333";
                        result.description = "附件大小超过4M";
                    }
                    else
                    {
                        //若没有解过密，再先解密
                        if (!File.Exists(decryptFileName))
                        {
                            EncrptionUtil.AttDecrypt(fileName, decryptFileName);
                        }
                        
                        byte[] bytes = bytes = GetBytesByPath(decryptFileName);
                        string base64String = Convert.ToBase64String(bytes);
                        result = KingDeeApi.Check(fileName, base64String);
                    }
                }
                else
                {
                    result = KingDeeApi.ManualCheck(param["InvoiceCode"], param["InvoiceNo"], param["InvoiceDate"], param["InvoiceMoney"], param["InvoieCheckCode"]);
                }
                FileLogger.WriteLog("结束调用KingDeeApi接口", 1, "OAInvoicehelper", "Run", "DataService", "AppMessage");
            }
            catch (System.Exception err)
            {
                FileLogger.WriteLog("处理文件异常： " + err.Message+err.StackTrace+err.InnerException??"", 1, "InvoiceHelper", "Check_Scan", "DataService", "ErrMessage");
            }

            return result;
        }

        public static byte[] GetBytesByPath(string path)
        {
            FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            System.Text.Encoding utf8 = System.Text.Encoding.UTF8;
            BinaryReader br = new BinaryReader(fs, utf8);
            //BinaryReader br = new BinaryReader(fs);
            byte[] bytes = br.ReadBytes((int)fs.Length);
            fs.Flush();
            fs.Close();
            return bytes;
        }

        public static string InvoiceCheckResult2Xml(InvoiceCheckResult chkResult)
        {
           
            string xmlString = @"<InvoiceResult>
                                               <errcode>{0}</errcode>
                                               <description>{1}</description>
                                               <CheckDetailList>{2}</CheckDetailList></InvoiceResult> ";
            string detailStringFormat = @"<InvoiceCheckDetail>
			                                        <serialNo>{0}</serialNo>
			                                        <invoiceCode>{1}</invoiceCode>
			                                        <invoiceDate>{2}</invoiceDate>
			                                        <salerName>{3}</salerName>
			                                        <amount>{4}</amount>
			                                        <taxAmount>{5}</taxAmount>
			                                        <invoiceType>{6}</invoiceType>
			                                        <buyerTaxNo>{7}</buyerTaxNo>
			                                        <salerAccount>{8}</salerAccount>
			                                        <checkStatus>{9}</checkStatus>
			                                        <checkErrcode>{10}</checkErrcode>
			                                        <checkCode>{11}</checkCode>
			                                        <checkDescription>{12}</checkDescription>
			                                        <invoiceMoney>{13}</invoiceMoney>
			                                        <printingSequenceNo>{14}</printingSequenceNo>
			                                        <taxRate>{15}</taxRate>
			                                        <electronicTicketNum>{16}</electronicTicketNum>
                                                   <invoiceNo>{17}</invoiceNo>
		                                        </InvoiceCheckDetail>";
            string detailString="",detailStrings = "";
            foreach (InvoiceCheckDetail i in chkResult.CheckDetailList)
            {
                detailString = string.Format(detailStringFormat, i.serialNo,i.invoiceCode,i.invoiceDate,i.salerName,
                                                                                  i.amount,i.taxAmount,i.invoiceType,i.buyerTaxNo,
                                                                                  i.salerAccount,i.checkStatus,i.checkErrcode,i.checkCode,
                                                                                  i.checkDescription,i.invoiceMoney,i.printingSequenceNo,
                                                                                  i.taxRate,i.electronicTicketNum,i.invoiceNo);
                detailStrings = detailStrings + detailString;
            }
            xmlString = string.Format(xmlString, chkResult.errcode, chkResult.description, detailStrings);
            return xmlString;
        }

        public static  InvoiceCheckResult  Xml2InvoiceCheckResult(string xmlInvoiceString)
        {
            InvoiceCheckResult chkResult = new InvoiceCheckResult();

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlInvoiceString);
            chkResult.errcode = doc.SelectSingleNode("InvoiceResult/errcode").InnerText;
            chkResult.description = doc.SelectSingleNode("InvoiceResult/description").InnerText;
            XmlNodeList detailNodes = doc.SelectNodes("InvoiceResult/CheckDetailList/InvoiceCheckDetail");
            List < InvoiceCheckDetail >invoiceList= new List<InvoiceCheckDetail>();
            foreach(XmlNode i in detailNodes)
            {
                InvoiceCheckDetail detail = new InvoiceCheckDetail();
                detail.serialNo = i.SelectSingleNode("serialNo").InnerText;
                detail.invoiceCode = i.SelectSingleNode("invoiceCode").InnerText;
                detail.invoiceNo = i.SelectSingleNode("invoiceNo").InnerText;
                detail.invoiceDate = i.SelectSingleNode("invoiceDate").InnerText;
                detail.salerName = i.SelectSingleNode("salerName").InnerText;
                detail.amount = i.SelectSingleNode("amount").InnerText;
                detail.taxAmount = i.SelectSingleNode("taxAmount").InnerText;
                detail.invoiceType = i.SelectSingleNode("invoiceType").InnerText;
                detail.buyerTaxNo = i.SelectSingleNode("buyerTaxNo").InnerText;
                detail.salerAccount = i.SelectSingleNode("salerAccount").InnerText;
                detail.checkStatus = i.SelectSingleNode("checkStatus").InnerText;
                detail.checkErrcode = i.SelectSingleNode("checkErrcode").InnerText;
                detail.checkCode = i.SelectSingleNode("checkCode").InnerText;
                detail.checkDescription = i.SelectSingleNode("checkDescription").InnerText;
                detail.invoiceMoney = i.SelectSingleNode("invoiceMoney").InnerText;
                detail.printingSequenceNo = i.SelectSingleNode("printingSequenceNo").InnerText;
                detail.taxRate = i.SelectSingleNode("taxRate").InnerText;
                detail.electronicTicketNum = i.SelectSingleNode("electronicTicketNum").InnerText;
                invoiceList.Add(detail);
            }
            chkResult.CheckDetailList = invoiceList;
            return chkResult;
        }

        public string InvoiceCheck(string xmlString)
        {
            string mode = "0",base64String="", InvoiceCode="", InvoiceNo="",InvoiceDate="",InvoiceMoney= "",InvoieVCode="";
            string formID = "",formType="";

            string result = "<UpdateData> " +
                                  "<Result>{0}</Result>" +
                                  "<InvoiceResult>{1}</InvoiceResult>" +
                                  "<Description>{2}</Description></UpdateData>";
           
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xmlString);

                XmlNode node = doc.SelectSingleNode("UpdateData/Mode");
                if (node != null && node.InnerText.Trim().Length > 0)
                    mode = node.InnerText.Trim();

                node = doc.SelectSingleNode("UpdateData/FormID");
                if (node != null && node.InnerText.Trim().Length > 0)
                    formID = node.InnerText.Trim();
                else
                    throw new System.Exception("FormID不能为空");


                node = doc.SelectSingleNode("UpdateData/Form");
                if (node != null && node.InnerText.Trim().Length > 0)
                    formType = node.InnerText.Trim();
                else
                    throw new System.Exception("OA表单不能为空");

                InvoiceCheckResult chkresult = null;
                if (mode == "0")//扫描查验
                {
                    node = doc.SelectSingleNode("UpdateData/InvoiceData");
                    if (node != null && node.InnerText.Trim().Length > 0)
                        base64String = node.InnerText.Trim();
                    else
                        throw new System.Exception("发票数据不能为空");
                    chkresult = KingDeeApi.Check(OAInvoiceHelper.GetID(), base64String);
                }
                if (mode == "1")//手工
                {
                    node = doc.SelectSingleNode("UpdateData/InvoiceCode");
                    if (node != null && node.InnerText.Trim().Length > 0)
                        InvoiceCode = node.InnerText.Trim();
                    else
                        throw new System.Exception("发票代码不能为空");

                    node = doc.SelectSingleNode("UpdateData/InvoiceNo");
                    if (node != null && node.InnerText.Trim().Length > 0)
                        InvoiceNo = node.InnerText.Trim();
                    else
                        throw new System.Exception("发票代码不能为空");


                    node = doc.SelectSingleNode("UpdateData/InvoiceDate");
                    if (node != null && node.InnerText.Trim().Length > 0)
                        InvoiceDate = node.InnerText.Trim();
                    else
                        throw new System.Exception("发票日期为空");

                    node = doc.SelectSingleNode("UpdateData/InvoiceMoney");
                    if (node != null && node.InnerText.Trim().Length > 0)
                        InvoiceMoney = node.InnerText.Trim();
                    else
                        throw new System.Exception("发票不含税金额为空");

                    node = doc.SelectSingleNode("UpdateData/InvoiceVCode");
                    if (node != null && node.InnerText.Trim().Length > 0)
                        InvoieVCode = node.InnerText.Trim();

                    chkresult = KingDeeApi.ManualCheck(InvoiceCode, InvoiceNo, InvoiceDate, InvoiceMoney, InvoieVCode);
                }
             
             
                string xmlResult = "";
                // 只有通过验证的发票财务更新到OA发票数据库
                if (chkresult != null)
                {
                    if (chkresult.errcode == "0000" && chkresult.CheckDetailList.Count > 0)
                    {
                        xmlResult = InvoiceCheckResult2Xml(chkresult);
                        WebInvoke invoke = new WebInvoke();
                        object[] param = new object[] { xmlResult, formID, formType };
                        //OAInvoiceHelper o = new OAInvoiceHelper();
                        //o.UpdateInvoiceDB(xmlResult, formID, formType);
                        oawsUrl = oawsUrl + "FinaceAppService.asmx";
                        xmlResult = invoke.Invoke(oawsUrl, "FinaceAppService", "CheckInvoiceData", param, null, 8000).ToString();
                        result = xmlResult;
                    }
                }
                else
                {
                    result = string.Format(result, "False", "异常", "不是发票或接口异常");
                }
            }
            catch(System.Exception  err)
            {
                throw err;
            }
            return result;
        }
    }


}
