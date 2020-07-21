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
        public InvoiceCheckResult Scan_Check(string fileName, string fileType,int timeOutSecond = 8)
        {
            InvoiceCheckResult result = null;
         
            try
            {
                //解密后的文件名
                string decryptFileName = fileName + "_D";
                //若没有解过密，再先解密
                if(!File.Exists(decryptFileName))
                {
                    EncrptionUtil.AttDecrypt(fileName, decryptFileName);
                }
                byte[] bytes = bytes = GetBytesByPath(decryptFileName);
                string base64String = Convert.ToBase64String(bytes);
                result = KingDeeApi.Check(fileName, base64String);
            }
            catch (Exception err)
            {
                //throw err;
                FileLogger.WriteLog(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "处理文件异常： " + err.Message, 2);
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

    }

    //public class InvoiceCheckResult
    //{
    //    public string errcode { get; set; }
    //    public string description { get; set; }

    //    public List<InvoiceDetail> list;
    //}


    //public class InvoiceDetail
    //{
    //    /// <summary>
    //    /// 发票流水号
    //    /// </summary>
    //    public string serialNo { get; set; }
    //    /// <summary>
    //    /// 
    //    /// </summary>
    //    public string invoiceCode { get; set; }
    //    public string invoiceNo { get; set; }
    //    public string invoiceDate { get; set; }
    //    public string salerName { get; set; }
    //    public string amount { get; set; }
    //    public string taxAmount { get; set; }
    //    public string totalAmount { get; set; }
    //    public string invoiceType { get; set; }
    //    public string buyerTaxNo { get; set; }
    //    public string salerAccount { get; set; }
    //    public string checkStatus { get; set; }
    //    public string checkErrcode { get; set; }
    //    public string checkCode { get; set; }
    //    public string checkDescription { get; set; }
        
    //}
}
