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
        public InvoiceHelper()
        {
            cfgDoc = new XmlDocument();
            cfgDoc.Load("cfg.xml");
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
            }
            catch (System.Exception err)
            {
                FileLogger.WriteLog("处理文件异常： " + err.Message, 1, "InvoiceHelper", "Check_Scan", "DataService", "ErrMessage");
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
    }


}
